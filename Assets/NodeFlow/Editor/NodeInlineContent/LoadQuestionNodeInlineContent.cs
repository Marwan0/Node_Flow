#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using NodeSystem.Nodes.Quiz;
using QuizSystem;
using NodeSystem;

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
        private bool _showExtendedQuestionFields;
        
        public override void Draw()
        {
            var node = Node as LoadQuestionNode;
            if (node == null) return;

            // === STEP 1: Object Field to select the QuestionData ===
            CreateLabel("Question Asset:");
            
            // Prefer direct reference, fallback to loading from path
            QuestionData currentQuestion = node.questionRef;
            if (currentQuestion == null && !string.IsNullOrEmpty(node.questionAssetPath))
            {
                currentQuestion = AssetDatabase.LoadAssetAtPath<QuestionData>(node.questionAssetPath);
            }

            // Object picker field
            CreateObjectField<QuestionData>("", currentQuestion, q =>
            {
                // Set both direct reference and path for compatibility
                node.questionRef = q;
                node.questionAssetPath = q != null ? AssetDatabase.GetAssetPath(q) : "";
                
                // Save the graph to persist the direct reference (stored separately for WebGL)
                var graph = GetNodeGraph();
                if (graph != null)
                {
                    // Store reference separately (works in WebGL builds)
                    graph.SetNodeAssetReference(node.Guid, q);
                    graph.SaveToJson();
                    EditorUtility.SetDirty(graph);
                }
                
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
                        // Warning: Failed to create editor: {ex.Message}
                        _questionEditor = null;
                    }
                }

                // Add a visual separator
                AddSeparator("Question Preview");

                // Compact inline editor (no nested scrollbar).
                // Full question editing stays in the normal Inspector via the button below.
                DrawCompactQuestionEditor();
                DrawQuestionAssetActions(currentQuestion);
            }

            // === STEP 3: Node-specific options ===
            AddSeparator("Node Options");

            UnityEngine.Object currentContainer = node.questionContainerRef;
            if (currentContainer == null && !string.IsNullOrEmpty(node.questionContainerPath))
            {
                var restoredContainer = FindGameObjectByPath(node.questionContainerPath);
                if (restoredContainer != null)
                {
                    currentContainer = restoredContainer;
                    node.questionContainerRef = restoredContainer;

                    var graphForRestore = GetNodeGraph();
                    if (graphForRestore != null)
                    {
                        graphForRestore.SaveToJson();
                        EditorUtility.SetDirty(graphForRestore);
                    }
                }
            }

            CreateLabel("Question Parent (optional, drag from Hierarchy)");
            CreateObjectField<UnityEngine.Object>("", currentContainer, v =>
            {
                node.questionContainerRef = v;
                if (v != null)
                {
                    var t = v is GameObject go ? go.transform : (v as Component)?.transform;
                    if (t != null)
                    {
                        node.questionContainerPath = GetHierarchyPath(t);
                        // Save the graph to persist the path
                        var graph = GetNodeGraph();
                        if (graph != null)
                        {
                            graph.SaveToJson();
                            EditorUtility.SetDirty(graph);
                        }
                    }
                }
                // Don't clear questionContainerPath when v is null - keep it for restoration
                MarkDirty();
            });
            if (!string.IsNullOrEmpty(node.questionContainerPath))
            {
                CreateLabel($"Path: {node.questionContainerPath}", new Color(0.55f, 0.55f, 0.55f));
            }

            CreateLabel("Layout Override (optional, drag prefab from Project)");
            GameObject currentLayoutPrefab = node.layoutOverridePrefab;
            if (currentLayoutPrefab == null && !string.IsNullOrEmpty(node.layoutOverridePrefabPath))
            {
                currentLayoutPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(node.layoutOverridePrefabPath);
                if (currentLayoutPrefab != null)
                {
                    node.layoutOverridePrefab = currentLayoutPrefab;
                }
            }

            CreateObjectField<GameObject>("", currentLayoutPrefab, v =>
            {
                GameObject prefabAsset = v;
                if (prefabAsset != null)
                {
                    // If a scene instance is dragged, resolve it to its source prefab asset.
                    if (!AssetDatabase.Contains(prefabAsset))
                    {
                        var source = PrefabUtility.GetCorrespondingObjectFromSource(prefabAsset);
                        if (source is GameObject sourceGo)
                        {
                            prefabAsset = sourceGo;
                        }
                        else
                        {
                            // Warning: Layout Override must be a prefab asset from Project, not a scene object.
                            prefabAsset = null;
                        }
                    }
                }

                node.layoutOverridePrefab = prefabAsset;
                node.layoutOverridePrefabPath = prefabAsset != null ? AssetDatabase.GetAssetPath(prefabAsset) : "";
                var graph = GetNodeGraph();
                if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                MarkDirty();
            });

            // Get the node from graph to ensure we're modifying the correct instance
            var graph = GetNodeGraph();
            LoadQuestionNode graphNode = null;
            if (graph != null)
            {
                graphNode = graph.GetNode(node.Guid) as LoadQuestionNode;
            }
            
            // Always use the graph's node instance if available (this is what gets serialized)
            var nodeToModify = graphNode ?? node;

            CreateToggle("Wait for Answer", node.waitForAnswer, v => node.waitForAnswer = v);
            CreateToggle("Track in State", node.trackInQuizState, v => node.trackInQuizState = v);
            CreateToggle("Show Hints", node.showHints, v => {
                if (graph != null) Undo.RecordObject(graph, "Change Show Hints");
                nodeToModify.showHints = v;
                if (node != nodeToModify) node.showHints = v;
                if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                MarkDirty();
            });

            // === STEP 4: Answer Animation Settings ===
            AddSeparator("Answer Animations");
            CreateLabel("One animation applies to all answers with staggered delay:", new Color(0.8f, 0.8f, 0.8f));
            
            CreateToggle("Enable Animations", nodeToModify.enableAnimations, v => {
                if (graph != null) Undo.RecordObject(graph, "Change Enable Animations");
                
                // Update the node that's actually in the graph
                nodeToModify.enableAnimations = v;
                // Also update local reference if different
                if (node != nodeToModify) node.enableAnimations = v;
                
                if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                MarkDirty();
            });

            if (nodeToModify.enableAnimations)
            {
                var currentAnimationType = nodeToModify.animationType;
                
                CreateEnumField("Animation Type", currentAnimationType, (AnswerAnimationType v) => {
                    if (graph != null) Undo.RecordObject(graph, "Change Animation Type");
                    
                    // CRITICAL: Update the node that's in the graph's _runtimeNodes list
                    nodeToModify.animationType = v;
                    // Also update local reference if different
                    if (node != nodeToModify) node.animationType = v;
                    
                    // Force graph save immediately - this serializes from _runtimeNodes
                    if (graph != null)
                    {
                        graph.SaveToJson();
                        EditorUtility.SetDirty(graph);
                    }
                    
                    MarkDirty();
                    RequestRefresh();
                });

                CreateFloatField("Duration", nodeToModify.animationDuration, v => {
                    if (graph != null) Undo.RecordObject(graph, "Change Animation Duration");
                    
                    nodeToModify.animationDuration = Mathf.Clamp(v, 0.1f, 2f);
                    if (node != nodeToModify) node.animationDuration = nodeToModify.animationDuration;
                    
                    if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                    MarkDirty();
                });

                CreateFloatField("Stagger Delay", nodeToModify.staggerDelay, v => {
                    if (graph != null) Undo.RecordObject(graph, "Change Stagger Delay");
                    
                    nodeToModify.staggerDelay = Mathf.Clamp(v, 0f, 0.5f);
                    if (node != nodeToModify) node.staggerDelay = nodeToModify.staggerDelay;
                    
                    if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                    MarkDirty();
                });

#if DOTWEEN
                CreateEnumField("Ease", nodeToModify.easeType, (DG.Tweening.Ease v) => {
                    if (graph != null) Undo.RecordObject(graph, "Change Animation Ease");
                    
                    nodeToModify.easeType = v;
                    if (node != nodeToModify) node.easeType = nodeToModify.easeType;
                    
                    if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                    MarkDirty();
                });
#endif

                if (nodeToModify.animationType == AnswerAnimationType.Scale || 
                    nodeToModify.animationType == AnswerAnimationType.Bounce)
                {
                    CreateFloatField("Scale Multiplier", nodeToModify.scaleMultiplier, v => {
                        if (graph != null) Undo.RecordObject(graph, "Change Scale Multiplier");
                        
                        nodeToModify.scaleMultiplier = Mathf.Clamp(v, 0.1f, 2f);
                        if (node != nodeToModify) node.scaleMultiplier = nodeToModify.scaleMultiplier;
                        
                        if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                        MarkDirty();
                    });
                }

                if (nodeToModify.animationType.ToString().StartsWith("Slide"))
                {
                    CreateFloatField("Slide Distance", nodeToModify.slideDistance, v => {
                        if (graph != null) Undo.RecordObject(graph, "Change Slide Distance");
                        
                        nodeToModify.slideDistance = Mathf.Clamp(v, 10f, 500f);
                        if (node != nodeToModify) node.slideDistance = nodeToModify.slideDistance;
                        
                        if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                        MarkDirty();
                    });
                }
            }
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
                    DrawLargeTextProperty(questionTextProp, "Question");
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

                var maxAttemptsPerConnectionProp = _serializedQuestion.FindProperty("maxAttemptsPerConnection");
                if (maxAttemptsPerConnectionProp != null)
                {
                    EditorGUILayout.PropertyField(maxAttemptsPerConnectionProp, new GUIContent("Max Attempts Per Connection"));
                }

                var showHintAfterAttemptProp1 = _serializedQuestion.FindProperty("showHintAfterAttempt");
                if (showHintAfterAttemptProp1 != null)
                {
                    EditorGUILayout.PropertyField(showHintAfterAttemptProp1, new GUIContent("Hint After Attempt", "0 = never, 1 = first wrong, 2 = second wrong, etc."));
                }

                // Draw hints array
                var hintsProp = _serializedQuestion.FindProperty("hints");
                if (hintsProp != null)
                {
                    DrawLargeStringArray(hintsProp, "Hints");
                }

                var explanationProp = _serializedQuestion.FindProperty("explanation");
                if (explanationProp != null)
                {
                    DrawLargeTextProperty(explanationProp, "Explanation");
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
                    DrawLargeTextProperty(questionTextProp, "Question");

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
                    DrawLargeStringArray(hintsProp, "Hints");

                var explanationProp = _serializedQuestion.FindProperty("explanation");
                if (explanationProp != null)
                    DrawLargeTextProperty(explanationProp, "Explanation");

                EditorGUILayout.Space(5);

                // === Type-Specific Properties ===
                // Multiple Choice - uses StringIdSelector (list + dropdown drawn by custom PropertyDrawer)
                var answersProp = _serializedQuestion.FindProperty("answers");
                if (answersProp != null)
                {
                    EditorGUILayout.PropertyField(answersProp, new GUIContent("Answers (select correct)"), true);
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
        /// Compact question editor without nested scrolling.
        /// Keeps the node lightweight and pushes full editing to normal Inspector.
        /// </summary>
        private void DrawCompactQuestionEditor()
        {
            if (_serializedQuestion == null) return;

            CreateToggle("Show Question Details", _showExtendedQuestionFields, v =>
            {
                _showExtendedQuestionFields = v;
            });

            var imguiContainer = new IMGUIContainer(() =>
            {
                _serializedQuestion.Update();

                var questionTextProp = _serializedQuestion.FindProperty("questionText");
                if (questionTextProp != null)
                {
                    DrawLargeTextProperty(questionTextProp, "Question", 56f);
                }

                var typeProp = _serializedQuestion.FindProperty("questionType");
                if (typeProp != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.PropertyField(typeProp, new GUIContent("Type"));
                    EditorGUI.EndDisabledGroup();
                }

                var pointsProp = _serializedQuestion.FindProperty("points");
                if (pointsProp != null)
                {
                    EditorGUILayout.PropertyField(pointsProp, new GUIContent("Points"));
                }

                var maxAttemptsProp = _serializedQuestion.FindProperty("maxAttempts");
                if (maxAttemptsProp != null)
                {
                    EditorGUILayout.PropertyField(maxAttemptsProp, new GUIContent("Max Attempts"));
                }

                var maxAttemptsPerConnectionProp = _serializedQuestion.FindProperty("maxAttemptsPerConnection");
                if (maxAttemptsPerConnectionProp != null)
                {
                    EditorGUILayout.PropertyField(maxAttemptsPerConnectionProp, new GUIContent("Max Attempts Per Connection"));
                }

                var showHintAfterProp = _serializedQuestion.FindProperty("showHintAfterAttempt");
                if (showHintAfterProp != null)
                {
                    EditorGUILayout.PropertyField(showHintAfterProp, new GUIContent("Hint After Attempt", "0 = never, 1 = first wrong, 2 = second wrong, etc."));
                }

                if (_showExtendedQuestionFields)
                {
                    var hintsProp = _serializedQuestion.FindProperty("hints");
                    if (hintsProp != null)
                    {
                        DrawLargeStringArray(hintsProp, "Hints", 28f);
                    }

                    var explanationProp = _serializedQuestion.FindProperty("explanation");
                    if (explanationProp != null)
                    {
                        DrawLargeTextProperty(explanationProp, "Explanation", 52f);
                    }
                }

                if (_serializedQuestion.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(_cachedQuestion);
                    MarkDirty();
                }
            });

            imguiContainer.style.marginTop = 4;
            imguiContainer.style.marginBottom = 4;
            imguiContainer.style.paddingLeft = 4;
            imguiContainer.style.paddingRight = 4;
            imguiContainer.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 0.8f);
            Container.Add(imguiContainer);
        }

        private void DrawQuestionAssetActions(QuestionData questionAsset)
        {
            if (questionAsset == null) return;

            var actionsRow = new VisualElement();
            actionsRow.style.flexDirection = FlexDirection.Row;
            actionsRow.style.marginTop = 4;
            actionsRow.style.marginBottom = 4;

            var openButton = new UnityEngine.UIElements.Button(() =>
            {
                Selection.activeObject = questionAsset;
                EditorGUIUtility.PingObject(questionAsset);
            })
            {
                text = "Open Asset"
            };
            openButton.style.flexGrow = 1;
            openButton.style.marginRight = 6;

            var pingButton = new UnityEngine.UIElements.Button(() =>
            {
                EditorGUIUtility.PingObject(questionAsset);
            })
            {
                text = "Ping"
            };
            pingButton.style.width = 72;

            actionsRow.Add(openButton);
            actionsRow.Add(pingButton);
            Container.Add(actionsRow);
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

        private static void DrawLargeTextProperty(SerializedProperty prop, string label, float minHeight = 56f)
        {
            if (prop == null || prop.propertyType != SerializedPropertyType.String)
            {
                if (prop != null)
                {
                    EditorGUILayout.PropertyField(prop, new GUIContent(label), true);
                }
                return;
            }

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUI.BeginChangeCheck();
            string updated = EditorGUILayout.TextArea(prop.stringValue ?? string.Empty, GUILayout.MinHeight(minHeight));
            if (EditorGUI.EndChangeCheck())
            {
                prop.stringValue = updated;
            }
        }

        private static void DrawLargeStringArray(SerializedProperty arrayProp, string label, float minHeight = 40f)
        {
            if (arrayProp == null || !arrayProp.isArray)
            {
                if (arrayProp != null)
                {
                    EditorGUILayout.PropertyField(arrayProp, new GUIContent(label), true);
                }
                return;
            }

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;

            int currentSize = arrayProp.arraySize;
            int newSize = EditorGUILayout.IntField("Size", currentSize);
            if (newSize != currentSize && newSize >= 0)
            {
                arrayProp.arraySize = newSize;
            }

            for (int i = 0; i < arrayProp.arraySize; i++)
            {
                var item = arrayProp.GetArrayElementAtIndex(i);
                if (item != null && item.propertyType == SerializedPropertyType.String)
                {
                    EditorGUILayout.LabelField($"Hint {i + 1}", EditorStyles.miniLabel);
                    EditorGUI.BeginChangeCheck();
                    string updated = EditorGUILayout.TextArea(item.stringValue ?? string.Empty, GUILayout.MinHeight(minHeight));
                    if (EditorGUI.EndChangeCheck())
                    {
                        item.stringValue = updated;
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(item, new GUIContent($"Element {i}"), true);
                }
            }

            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Get the NodeGraph that contains this node
        /// </summary>
        private NodeGraph GetNodeGraph()
        {
            if (Node == null) return null;
            
            // Search all NodeGraph assets to find which one contains this node
            string[] guids = AssetDatabase.FindAssets("t:NodeGraph");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var graph = AssetDatabase.LoadAssetAtPath<NodeGraph>(path);
                if (graph != null && graph.Nodes != null)
                {
                    foreach (var node in graph.Nodes)
                    {
                        if (node != null && node.Guid == Node.Guid)
                        {
                            return graph;
                        }
                    }
                }
            }
            return null;
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

        private static string GetHierarchyPath(Transform t)
        {
            if (t == null) return "";
            var parts = new List<string>();
            while (t != null)
            {
                parts.Add(t.name);
                t = t.parent;
            }
            parts.Reverse();
            return string.Join("/", parts);
        }

        private static GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var found = GameObject.Find(path);
            if (found != null) return found;

            var parts = path.Split('/');
            if (parts.Length > 0)
            {
                string rootName = parts[0];
                string relativePath = parts.Length > 1 ? string.Join("/", parts, 1, parts.Length - 1) : "";

                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    foreach (var rootGo in scene.GetRootGameObjects())
                    {
                        if (rootGo.name != rootName) continue;
                        if (parts.Length == 1) return rootGo;

                        var t = rootGo.transform.Find(relativePath);
                        if (t != null) return t.gameObject;
                    }
                }
            }

            string leafName = parts.Length > 0 ? parts[parts.Length - 1] : path;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    var byName = FindInHierarchyByName(rootGo.transform, leafName);
                    if (byName != null) return byName;
                }
            }

            return null;
        }

        private static GameObject FindInHierarchyByName(Transform parent, string targetName)
        {
            if (parent == null || string.IsNullOrEmpty(targetName)) return null;
            if (parent.name == targetName) return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindInHierarchyByName(parent.GetChild(i), targetName);
                if (result != null) return result;
            }

            return null;
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
