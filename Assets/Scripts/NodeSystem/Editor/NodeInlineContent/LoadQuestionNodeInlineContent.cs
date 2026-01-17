#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using NodeSystem.Nodes.Quiz;
using QuizSystem;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Demonstrates how to embed a ScriptableObject inspector inside a node.
    /// 
    /// KEY CONCEPTS:
    /// 1. IMGUIContainer - Bridges old IMGUI system with new UI Toolkit
    /// 2. SerializedObject - Unity's way to access serialized properties
    /// 3. Editor.CreateEditor - Creates an inspector for any Object
    /// 
    /// HOW IT WORKS:
    /// - We create an IMGUIContainer which accepts a callback function
    /// - Inside that callback, we can use EditorGUI/EditorGUILayout (IMGUI)
    /// - We create a SerializedObject from our QuestionData
    /// - We iterate through properties and draw them with PropertyField
    /// - OR we use Editor.CreateEditor to get the full inspector
    /// </summary>
    public class LoadQuestionNodeInlineContent : NodeInlineContentBase
    {
        // Cache the loaded question and editor to avoid recreating every frame
        private QuestionData _cachedQuestion;
        private UnityEditor.Editor _questionEditor;
        private SerializedObject _serializedQuestion;
        
        public override void Draw()
        {
            var node = Node as LoadQuestionNode;
            if (node == null) return;

            // === STEP 1: Object Field to select the QuestionData ===
            CreateLabel("Question Asset:");
            
            QuestionData currentQuestion = null;
            if (!string.IsNullOrEmpty(node.questionAssetPath))
            {
                currentQuestion = AssetDatabase.LoadAssetAtPath<QuestionData>(node.questionAssetPath);
            }

            // Object picker field
            CreateObjectField<QuestionData>("", currentQuestion, q =>
            {
                node.questionAssetPath = q != null ? AssetDatabase.GetAssetPath(q) : "";
                
                // Clear cache when selection changes
                _cachedQuestion = null;
                SafeDestroyEditor(_questionEditor);
                _questionEditor = null;
                _serializedQuestion = null;
                
                // Request refresh to rebuild the embedded inspector
                RequestRefresh();
            });

            // === STEP 2: Embed the ScriptableObject inspector ===
            if (currentQuestion != null)
            {
                // Update cache if needed
                if (_cachedQuestion != currentQuestion)
                {
                    _cachedQuestion = currentQuestion;
                    
                    // Clean up old editor safely
                    SafeDestroyEditor(_questionEditor);
                    
                    // Create SerializedObject for property access
                    _serializedQuestion = new SerializedObject(currentQuestion);
                    
                    // Create an Editor instance for the full inspector
                    try
                    {
                        _questionEditor = UnityEditor.Editor.CreateEditor(currentQuestion);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"[LoadQuestionNode] Failed to create editor: {ex.Message}");
                        _questionEditor = null;
                    }
                }

                // Add a visual separator
                AddSeparator("Question Preview");

                // Draw the full Odin inspector inside a scrollable area
                // This shows ALL properties including type-specific ones (answers, correctAnswerIndex, etc.)
                DrawEmbeddedInspector_WithScrollView();
            }

            // === STEP 3: Node-specific options ===
            AddSeparator("Node Options");
            CreateTextField(node.quizManagerPath, v => node.quizManagerPath = v, "QuizManager path");
            CreateToggle("Wait for Answer", node.waitForAnswer, v => node.waitForAnswer = v);
            CreateToggle("Track in State", node.trackInQuizState, v => node.trackInQuizState = v);
        }

        /// <summary>
        /// METHOD 1: Draw specific properties using SerializedObject + IMGUIContainer
        /// 
        /// PROS: Full control over what to show, can customize layout
        /// CONS: Need to know property names, manual property iteration
        /// 
        /// HOW IT WORKS:
        /// 1. Create IMGUIContainer - this is a VisualElement that runs IMGUI code
        /// 2. Inside the callback, use EditorGUILayout to draw properties
        /// 3. SerializedObject.Update() syncs the object's data
        /// 4. SerializedObject.ApplyModifiedProperties() saves changes
        /// </summary>
        private void DrawEmbeddedInspector_Method1()
        {
            if (_serializedQuestion == null) return;

            // IMGUIContainer bridges IMGUI (old) with UI Toolkit (new)
            var imguiContainer = new IMGUIContainer(() =>
            {
                // IMPORTANT: Always call Update() before reading properties
                _serializedQuestion.Update();

                // Draw specific properties we care about
                // FindProperty uses the FIELD NAME (with underscore if private)
                
                EditorGUILayout.LabelField("Question Info", EditorStyles.boldLabel);
                
                // Draw the question text
                var questionTextProp = _serializedQuestion.FindProperty("questionText");
                if (questionTextProp != null)
                {
                    EditorGUILayout.PropertyField(questionTextProp, new GUIContent("Question"));
                }

                // Draw the question type
                var typeProp = _serializedQuestion.FindProperty("questionType");
                if (typeProp != null)
                {
                    EditorGUI.BeginDisabledGroup(true); // Read-only
                    EditorGUILayout.PropertyField(typeProp, new GUIContent("Type"));
                    EditorGUI.EndDisabledGroup();
                }

                // Draw points
                var pointsProp = _serializedQuestion.FindProperty("points");
                if (pointsProp != null)
                {
                    EditorGUILayout.PropertyField(pointsProp, new GUIContent("Points"));
                }

                // Draw max attempts
                var maxAttemptsProp = _serializedQuestion.FindProperty("maxAttempts");
                if (maxAttemptsProp != null)
                {
                    EditorGUILayout.PropertyField(maxAttemptsProp, new GUIContent("Max Attempts"));
                }

                // Draw hints array
                var hintsProp = _serializedQuestion.FindProperty("hints");
                if (hintsProp != null)
                {
                    EditorGUILayout.PropertyField(hintsProp, new GUIContent("Hints"), true);
                }

                // IMPORTANT: Apply changes back to the object
                if (_serializedQuestion.ApplyModifiedProperties())
                {
                    // Mark dirty so Unity knows to save the asset
                    EditorUtility.SetDirty(_cachedQuestion);
                    MarkDirty();
                }
            });

            // Style the container
            imguiContainer.style.marginTop = 5;
            imguiContainer.style.marginBottom = 5;
            imguiContainer.style.paddingLeft = 5;
            imguiContainer.style.paddingRight = 5;
            imguiContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            Container.Add(imguiContainer);
        }

        /// <summary>
        /// Draw the COMPLETE inspector using Editor.OnInspectorGUI
        /// 
        /// This shows ALL properties including:
        /// - Base QuestionData properties (questionText, hints, points, etc.)
        /// - Type-specific properties (answers, correctAnswerIndex for MultipleChoice, etc.)
        /// - Odin Inspector attributes and decorations!
        /// 
        /// HOW IT WORKS:
        /// 1. Editor.CreateEditor creates an inspector for any UnityEngine.Object
        /// 2. OnInspectorGUI() draws the complete inspector as it appears in Inspector window
        /// </summary>
        private void DrawEmbeddedInspector_FullInspector()
        {
            if (_questionEditor == null) return;

            var imguiContainer = new IMGUIContainer(() =>
            {
                // Force serializedObject update for Odin
                if (_questionEditor.serializedObject != null)
                {
                    _questionEditor.serializedObject.Update();
                }
                
                // Draw the full inspector - includes ALL properties and Odin decorations
                _questionEditor.OnInspectorGUI();
                
                // Apply any changes
                if (_questionEditor.serializedObject != null)
                {
                    if (_questionEditor.serializedObject.ApplyModifiedProperties())
                    {
                        EditorUtility.SetDirty(_cachedQuestion);
                        MarkDirty();
                    }
                }
            });

            // Style the container
            imguiContainer.style.marginTop = 5;
            imguiContainer.style.marginBottom = 5;
            imguiContainer.style.paddingLeft = 2;
            imguiContainer.style.paddingRight = 2;
            imguiContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.8f);

            Container.Add(imguiContainer);
        }

        /// <summary>
        /// Draw the complete inspector inside a scroll view
        /// Uses manual property drawing to ensure everything displays correctly in the node
        /// </summary>
        private void DrawEmbeddedInspector_WithScrollView()
        {
            if (_serializedQuestion == null) return;

            // Create a scroll view to contain the inspector
            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.maxHeight = 400;
            scrollView.style.minHeight = 100;
            scrollView.style.marginTop = 5;
            scrollView.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.8f);

            var imguiContainer = new IMGUIContainer(() =>
            {
                _serializedQuestion.Update();

                // === Base QuestionData Properties ===
                EditorGUILayout.LabelField("Question Info", EditorStyles.boldLabel);
                
                var questionTextProp = _serializedQuestion.FindProperty("questionText");
                if (questionTextProp != null)
                    EditorGUILayout.PropertyField(questionTextProp, new GUIContent("Question"));

                var typeProp = _serializedQuestion.FindProperty("questionType");
                if (typeProp != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(typeProp, new GUIContent("Type"));
                    EditorGUI.EndDisabledGroup();
                }

                var pointsProp = _serializedQuestion.FindProperty("points");
                if (pointsProp != null)
                    EditorGUILayout.PropertyField(pointsProp, new GUIContent("Points"));

                var maxAttemptsProp = _serializedQuestion.FindProperty("maxAttempts");
                if (maxAttemptsProp != null)
                    EditorGUILayout.PropertyField(maxAttemptsProp, new GUIContent("Max Attempts"));

                var hintsProp = _serializedQuestion.FindProperty("hints");
                if (hintsProp != null)
                    EditorGUILayout.PropertyField(hintsProp, new GUIContent("Hints"), true);

                var explanationProp = _serializedQuestion.FindProperty("explanation");
                if (explanationProp != null)
                    EditorGUILayout.PropertyField(explanationProp, new GUIContent("Explanation"));

                EditorGUILayout.Space(5);

                // === Type-Specific Properties ===
                // Multiple Choice
                var answersProp = _serializedQuestion.FindProperty("answers");
                if (answersProp != null && answersProp.isArray)
                {
                    // Use PropertyField with includeChildren=true for full array editing (add/remove buttons)
                    EditorGUILayout.PropertyField(answersProp, new GUIContent("Answers"), true);
                }

                var correctAnswerIndexProp = _serializedQuestion.FindProperty("correctAnswerIndex");
                if (correctAnswerIndexProp != null)
                {
                    // Create dropdown for correct answer selection
                    if (answersProp != null && answersProp.isArray && answersProp.arraySize > 0)
                    {
                        string[] options = new string[answersProp.arraySize];
                        for (int i = 0; i < answersProp.arraySize; i++)
                        {
                            var answer = answersProp.GetArrayElementAtIndex(i).stringValue;
                            options[i] = string.IsNullOrEmpty(answer) ? $"Answer {i + 1}" : answer;
                        }
                        correctAnswerIndexProp.intValue = EditorGUILayout.Popup("Correct Answer", correctAnswerIndexProp.intValue, options);
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(correctAnswerIndexProp, new GUIContent("Correct Answer Index"));
                    }
                }

                // True/False
                var correctAnswerBoolProp = _serializedQuestion.FindProperty("correctAnswer");
                if (correctAnswerBoolProp != null && correctAnswerBoolProp.propertyType == SerializedPropertyType.Boolean)
                {
                    EditorGUILayout.LabelField("Answer", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(correctAnswerBoolProp, new GUIContent("Correct Answer"));
                }

                // Fill in the Blank (string correctAnswer)
                if (correctAnswerBoolProp != null && correctAnswerBoolProp.propertyType == SerializedPropertyType.String)
                {
                    EditorGUILayout.LabelField("Answer", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(correctAnswerBoolProp, new GUIContent("Correct Answer"));
                    
                    var altAnswersProp = _serializedQuestion.FindProperty("alternativeAnswers");
                    if (altAnswersProp != null)
                        EditorGUILayout.PropertyField(altAnswersProp, new GUIContent("Alternative Answers"), true);
                    
                    var caseSensitiveProp = _serializedQuestion.FindProperty("caseSensitive");
                    if (caseSensitiveProp != null)
                        EditorGUILayout.PropertyField(caseSensitiveProp, new GUIContent("Case Sensitive"));
                }

                // Multi-Select
                var optionsProp = _serializedQuestion.FindProperty("options");
                if (optionsProp != null && optionsProp.isArray)
                {
                    EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(optionsProp, new GUIContent("Options"), true);
                }

                var correctIndicesProp = _serializedQuestion.FindProperty("correctIndices");
                if (correctIndicesProp != null && correctIndicesProp.isArray)
                {
                    EditorGUILayout.PropertyField(correctIndicesProp, new GUIContent("Correct Indices"), true);
                }

                // Ordering
                var correctOrderProp = _serializedQuestion.FindProperty("correctOrder");
                if (correctOrderProp != null && correctOrderProp.isArray)
                {
                    EditorGUILayout.LabelField("Ordering", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(correctOrderProp, new GUIContent("Correct Order"), true);
                }

                // Slider
                var minValueProp = _serializedQuestion.FindProperty("minValue");
                var maxValueProp = _serializedQuestion.FindProperty("maxValue");
                var correctValueProp = _serializedQuestion.FindProperty("correctValue");
                if (minValueProp != null && maxValueProp != null && correctValueProp != null)
                {
                    EditorGUILayout.LabelField("Slider Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(minValueProp, new GUIContent("Min Value"));
                    EditorGUILayout.PropertyField(maxValueProp, new GUIContent("Max Value"));
                    EditorGUILayout.PropertyField(correctValueProp, new GUIContent("Correct Value"));
                    
                    var toleranceProp = _serializedQuestion.FindProperty("tolerance");
                    if (toleranceProp != null)
                        EditorGUILayout.PropertyField(toleranceProp, new GUIContent("Tolerance"));
                }

                // Apply changes
                if (_serializedQuestion.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(_cachedQuestion);
                    MarkDirty();
                }
            });

            scrollView.Add(imguiContainer);
            Container.Add(scrollView);
        }

        /// <summary>
        /// METHOD 3: Use UI Toolkit's PropertyField (most modern approach)
        /// 
        /// PROS: Native UI Toolkit, better performance, modern look
        /// CONS: Requires binding, less flexible than IMGUI
        /// 
        /// HOW IT WORKS:
        /// 1. Create PropertyField for each property you want to show
        /// 2. Bind it to the SerializedObject using bindingPath
        /// 3. Call Bind() to connect everything
        /// </summary>
        private void DrawEmbeddedInspector_Method3()
        {
            if (_serializedQuestion == null) return;

            // Create a container for the properties
            var propContainer = new VisualElement();
            propContainer.style.marginTop = 5;
            propContainer.style.paddingLeft = 5;
            propContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);

            // PropertyField automatically creates the right field type
            var questionField = new PropertyField();
            questionField.bindingPath = "questionText"; // Must match field name exactly
            questionField.label = "Question";
            propContainer.Add(questionField);

            var pointsField = new PropertyField();
            pointsField.bindingPath = "points";
            pointsField.label = "Points";
            propContainer.Add(pointsField);

            var attemptsField = new PropertyField();
            attemptsField.bindingPath = "maxAttempts";
            attemptsField.label = "Max Attempts";
            propContainer.Add(attemptsField);

            // IMPORTANT: Bind the container to the SerializedObject
            // This connects the fields to the actual data
            propContainer.Bind(_serializedQuestion);

            Container.Add(propContainer);
        }

        /// <summary>
        /// Safely destroy an editor, handling potential issues with custom editors
        /// </summary>
        private void SafeDestroyEditor(UnityEditor.Editor editor)
        {
            if (editor == null) return;
            
            try
            {
                if (editor.target != null)
                {
                    Object.DestroyImmediate(editor);
                }
                else
                {
                    // Target is null, use delayed destruction
                    EditorApplication.delayCall += () =>
                    {
                        try
                        {
                            if (editor != null)
                            {
                                Object.DestroyImmediate(editor);
                            }
                        }
                        catch
                        {
                            // Ignore - editor was likely already cleaned up
                        }
                    };
                }
            }
            catch
            {
                // Some custom editors throw on destroy - ignore
            }
        }

        /// <summary>
        /// Helper to add a visual separator with label
        /// </summary>
        private void AddSeparator(string label)
        {
            var separator = new VisualElement();
            separator.style.marginTop = 8;
            separator.style.marginBottom = 4;
            separator.style.borderTopWidth = 1;
            separator.style.borderTopColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            separator.style.paddingTop = 4;

            var labelElement = new Label(label);
            labelElement.style.fontSize = 10;
            labelElement.style.color = new Color(0.7f, 0.7f, 0.7f);
            labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            separator.Add(labelElement);

            Container.Add(separator);
        }
    }
}
#endif
