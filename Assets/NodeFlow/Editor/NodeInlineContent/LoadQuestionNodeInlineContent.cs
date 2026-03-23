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
                if (v != null)
                {
                    // Block prefab assets — Question Parent must be a scene object
                    bool isAsset = false;
                    if (v is GameObject goCheck) isAsset = AssetDatabase.Contains(goCheck);
                    else if (v is Component comp && comp.gameObject != null) isAsset = AssetDatabase.Contains(comp.gameObject);

                    if (isAsset)
                    {
                        EditorUtility.DisplayDialog(
                            "Question Parent — wrong place (you used the Project window)",
                            "What went wrong:\n" +
                            "You dragged something from the Project window (assets at the bottom of the editor). " +
                            "The field \"Question Parent\" does NOT accept Project files.\n\n" +
                            "What \"Question Parent\" is for:\n" +
                            "It is the empty object IN YOUR SCENE where the quiz will put the question UI — for example a panel named Quiz_Container under your Canvas. " +
                            "Think: \"where in the scene should the question appear?\"\n\n" +
                            "How to fix it (step by step):\n" +
                            "1) Open the scene that has your quiz UI.\n" +
                            "2) In the Hierarchy (left), find the GameObject that should be the parent (e.g. Quiz_Container).\n" +
                            "3) Drag THAT object from the Hierarchy into \"Question Parent\" — not from the Project.\n\n" +
                            "If you actually wanted to choose the LOOK of the question (a reusable UI prefab):\n" +
                            "Use \"Layout Override\" below instead. That field is the one that takes a prefab from the Project.\n\n" +
                            "Quick rule:\n" +
                            "• Hierarchy object → Question Parent\n" +
                            "• Project prefab → Layout Override",
                            "OK");
                        node.questionContainerRef = null;
                        node.questionContainerPath = "";
                        MarkDirty();
                        RequestRefresh();
                        return;
                    }
                }

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
                if (v == null)
                {
                    node.layoutOverridePrefab = null;
                    node.layoutOverridePrefabPath = "";
                    var graphClear = GetNodeGraph();
                    if (graphClear != null)
                    {
                        graphClear.SetNodeAssetReference(node.Guid, "layoutOverride", null);
                        graphClear.SaveToJson();
                        EditorUtility.SetDirty(graphClear);
                    }
                    MarkDirty();
                    RequestRefresh();
                    return;
                }

                if (!TryResolveLayoutOverridePrefab(v, out GameObject prefabAsset, out string errTitle, out string errMessage))
                {
                    EditorUtility.DisplayDialog(errTitle, errMessage, "OK");
                    RequestRefresh();
                    return;
                }

                node.layoutOverridePrefab = prefabAsset;
                node.layoutOverridePrefabPath = prefabAsset != null ? AssetDatabase.GetAssetPath(prefabAsset) : "";
                var graph = GetNodeGraph();
                if (graph != null)
                {
                    graph.SetNodeAssetReference(node.Guid, "layoutOverride", prefabAsset);
                    graph.SaveToJson();
                    EditorUtility.SetDirty(graph);
                }
                MarkDirty();
            });

            AddValidateReferencesButton(node);

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

            // === STEP 5: Question Transition Settings ===
            AddSeparator("Question Transitions");
            CreateLabel("Override enter/exit animations per question:", new Color(0.8f, 0.8f, 0.8f));

            CreateToggle("Override Transitions", nodeToModify.overrideTransitions, v => {
                if (graph != null) Undo.RecordObject(graph, "Change Override Transitions");
                nodeToModify.overrideTransitions = v;
                if (node != nodeToModify) node.overrideTransitions = v;
                if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                MarkDirty();
                RequestRefresh();
            });

            if (nodeToModify.overrideTransitions)
            {
                // --- Enter transition ---
                CreateLabel("Enter (how question appears):", new Color(0.7f, 0.9f, 0.7f));

                if (nodeToModify.enterTransition == null)
                    nodeToModify.enterTransition = new QuestionTransitionSettings();

                CreateEnumField("Type", nodeToModify.enterTransition.transitionType, (QuestionTransitionType v) => {
                    if (graph != null) Undo.RecordObject(graph, "Change Enter Transition Type");
                    nodeToModify.enterTransition.transitionType = v;
                    if (node != nodeToModify && node.enterTransition != null) node.enterTransition.transitionType = v;
                    if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                    MarkDirty();
                    RequestRefresh();
                });

                if (nodeToModify.enterTransition.transitionType != QuestionTransitionType.None)
                {
                    CreateFloatField("Duration", nodeToModify.enterTransition.duration, v => {
                        if (graph != null) Undo.RecordObject(graph, "Change Enter Duration");
                        nodeToModify.enterTransition.duration = Mathf.Clamp(v, 0.05f, 2f);
                        if (node != nodeToModify && node.enterTransition != null)
                            node.enterTransition.duration = nodeToModify.enterTransition.duration;
                        if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                        MarkDirty();
                    });

#if DOTWEEN
                    CreateEnumField("Ease", nodeToModify.enterTransition.easeType, (DG.Tweening.Ease v) => {
                        if (graph != null) Undo.RecordObject(graph, "Change Enter Ease");
                        nodeToModify.enterTransition.easeType = v;
                        if (node != nodeToModify && node.enterTransition != null)
                            node.enterTransition.easeType = v;
                        if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                        MarkDirty();
                    });
#endif

                    if (nodeToModify.enterTransition.transitionType.ToString().StartsWith("Slide"))
                    {
                        CreateFloatField("Slide Distance", nodeToModify.enterTransition.slideDistance, v => {
                            if (graph != null) Undo.RecordObject(graph, "Change Enter Slide Distance");
                            nodeToModify.enterTransition.slideDistance = Mathf.Clamp(v, 100f, 2000f);
                            if (node != nodeToModify && node.enterTransition != null)
                                node.enterTransition.slideDistance = nodeToModify.enterTransition.slideDistance;
                            if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                            MarkDirty();
                        });
                    }
                }

                // --- Exit transition ---
                CreateLabel("Exit (how question leaves):", new Color(0.9f, 0.7f, 0.7f));

                if (nodeToModify.exitTransition == null)
                    nodeToModify.exitTransition = new QuestionTransitionSettings();

                CreateEnumField("Type", nodeToModify.exitTransition.transitionType, (QuestionTransitionType v) => {
                    if (graph != null) Undo.RecordObject(graph, "Change Exit Transition Type");
                    nodeToModify.exitTransition.transitionType = v;
                    if (node != nodeToModify && node.exitTransition != null) node.exitTransition.transitionType = v;
                    if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                    MarkDirty();
                    RequestRefresh();
                });

                if (nodeToModify.exitTransition.transitionType != QuestionTransitionType.None)
                {
                    CreateFloatField("Duration", nodeToModify.exitTransition.duration, v => {
                        if (graph != null) Undo.RecordObject(graph, "Change Exit Duration");
                        nodeToModify.exitTransition.duration = Mathf.Clamp(v, 0.05f, 2f);
                        if (node != nodeToModify && node.exitTransition != null)
                            node.exitTransition.duration = nodeToModify.exitTransition.duration;
                        if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                        MarkDirty();
                    });

#if DOTWEEN
                    CreateEnumField("Ease", nodeToModify.exitTransition.easeType, (DG.Tweening.Ease v) => {
                        if (graph != null) Undo.RecordObject(graph, "Change Exit Ease");
                        nodeToModify.exitTransition.easeType = v;
                        if (node != nodeToModify && node.exitTransition != null)
                            node.exitTransition.easeType = v;
                        if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                        MarkDirty();
                    });
#endif

                    if (nodeToModify.exitTransition.transitionType.ToString().StartsWith("Slide"))
                    {
                        CreateFloatField("Slide Distance", nodeToModify.exitTransition.slideDistance, v => {
                            if (graph != null) Undo.RecordObject(graph, "Change Exit Slide Distance");
                            nodeToModify.exitTransition.slideDistance = Mathf.Clamp(v, 100f, 2000f);
                            if (node != nodeToModify && node.exitTransition != null)
                                node.exitTransition.slideDistance = nodeToModify.exitTransition.slideDistance;
                            if (graph != null) { graph.SaveToJson(); EditorUtility.SetDirty(graph); }
                            MarkDirty();
                        });
                    }
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

                // Fill in the Blank
                var blanksProp = _serializedQuestion.FindProperty("blanks");
                if (blanksProp != null && blanksProp.isArray)
                {
                    EditorGUILayout.LabelField("Answer", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(blanksProp, new GUIContent("Blanks (ordered)", "One entry per blank, top to bottom in the UI. Leave empty to use Correct Answer below for a single blank."), true);
                }

                if (correctAnswerBoolProp != null && correctAnswerBoolProp.propertyType == SerializedPropertyType.String)
                {
                    if (blanksProp == null || !blanksProp.isArray)
                        EditorGUILayout.LabelField("Answer", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(correctAnswerBoolProp, new GUIContent("Correct Answer", "Used only when Blanks is empty."));
                    
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

        private void AddValidateReferencesButton(LoadQuestionNode node)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 4;
            row.style.marginBottom = 4;

            var btn = new Button(() => ShowValidateReferencesDialog(node))
            {
                text = "Validate references"
            };
            btn.style.flexGrow = 1;
            row.Add(btn);
            Container.Add(row);
        }

        private void ShowValidateReferencesDialog(LoadQuestionNode node)
        {
            var lines = new List<string>();
            bool hasIssues = false;

            lines.Add("This check explains the two optional boxes: \"Question Parent\" (scene) vs \"Layout Override\" (prefab).");
            lines.Add("");

            if (node.questionContainerRef != null)
            {
                if (IsQuestionParentProjectAsset(node.questionContainerRef))
                {
                    lines.Add("QUESTION PARENT — problem");
                    lines.Add("You have a Project asset here. This slot must be a GameObject from the Hierarchy (the live scene), e.g. the panel where questions should appear.");
                    lines.Add("Fix: clear this field, then drag the correct object from the Hierarchy. If you meant a UI prefab file, use Layout Override instead.");
                    hasIssues = true;
                }
                else
                {
                    lines.Add("QUESTION PARENT — OK");
                    lines.Add("You assigned a scene object. That is correct: this tells the game which parent Transform to use when instantiating the question under your Canvas.");
                }
            }
            else if (!string.IsNullOrEmpty(node.questionContainerPath))
            {
                lines.Add("QUESTION PARENT — warning");
                lines.Add("A path was saved from an earlier assignment, but the object reference is empty (Unity lost the link — e.g. scene changed or object renamed).");
                lines.Add("Fix: open the scene, find the container again, and drag it from the Hierarchy into Question Parent. Or clear the path if you do not need a parent.");
                hasIssues = true;
            }
            else
            {
                lines.Add("QUESTION PARENT — not set");
                lines.Add("Optional. If empty, the quiz will use whatever default parent your QuizManager / setup uses. Fill this when you need a specific container in the scene.");
            }

            lines.Add("");

            if (node.layoutOverridePrefab != null)
            {
                if (!AssetDatabase.Contains(node.layoutOverridePrefab))
                {
                    lines.Add("LAYOUT OVERRIDE — problem");
                    lines.Add("This should be a prefab asset stored in the Project folder, not a scene-only object.");
                    lines.Add("Fix: drag the prefab from the Project window, or a prefab instance from the Hierarchy.");
                    hasIssues = true;
                }
                else if (node.layoutOverridePrefab.GetComponentInChildren<QuestionUI>(true) == null)
                {
                    lines.Add("LAYOUT OVERRIDE — problem");
                    lines.Add("The prefab file is valid, but it has no QuestionUI script on the root or children. The quiz cannot run without that.");
                    lines.Add("Fix: open the prefab, add the right Question UI component (e.g. FillInTheBlankUI), save, and assign again.");
                    hasIssues = true;
                }
                else
                {
                    lines.Add("LAYOUT OVERRIDE — OK");
                    lines.Add("The prefab lives in the Project and includes a QuestionUI. That is what we need for this question’s layout.");
                }
            }
            else if (!string.IsNullOrEmpty(node.layoutOverridePrefabPath))
            {
                lines.Add("LAYOUT OVERRIDE — warning");
                lines.Add("A path was saved but Unity could not load that prefab (moved, deleted, or wrong folder).");
                lines.Add("Fix: pick the prefab again from the Project, or clear the field to use the default layout from the question / QuizManager.");
                hasIssues = true;
            }
            else
            {
                lines.Add("LAYOUT OVERRIDE — not set");
                lines.Add("Optional. If empty, the game uses the default UI prefab for this question type (from the question asset or QuizManager). Use this when you want a custom layout prefab for this node only.");
            }

            string title = hasIssues ? "Reference check — please read" : "Reference check — all good";
            string body = string.Join("\n", lines);
            EditorUtility.DisplayDialog(title, body, "OK");
        }

        private static bool IsQuestionParentProjectAsset(UnityEngine.Object v)
        {
            if (v == null) return false;
            if (v is GameObject go) return AssetDatabase.Contains(go);
            if (v is Component c && c.gameObject != null) return AssetDatabase.Contains(c.gameObject);
            return false;
        }

        /// <summary>
        /// Resolves a Project prefab or a prefab instance in the scene; rejects plain scene objects and prefabs without QuestionUI.
        /// </summary>
        private static bool TryResolveLayoutOverridePrefab(GameObject v, out GameObject prefabAsset, out string errorTitle, out string errorMessage)
        {
            prefabAsset = null;
            errorTitle = "Layout Override";
            errorMessage = null;

            if (v == null)
                return true;

            GameObject candidate;

            if (AssetDatabase.Contains(v))
            {
                candidate = v;
            }
            else
            {
                var source = PrefabUtility.GetCorrespondingObjectFromSource(v);
                if (source is GameObject sourceGo && AssetDatabase.Contains(sourceGo))
                    candidate = sourceGo;
                else
                {
                    errorTitle = "Layout Override — not a prefab (plain scene object)";
                    errorMessage =
                        "What went wrong:\n" +
                        "You dragged a normal GameObject from the scene that is not an instance of a prefab (or we could not find its source prefab).\n\n" +
                        "What \"Layout Override\" is for:\n" +
                        "It must point to a UI prefab file (in the Project) so the game knows exactly which layout to spawn for this question. " +
                        "A random empty GameObject in the scene is not enough.\n\n" +
                        "How to fix it:\n" +
                        "1) In the Project window, find your quiz UI prefab (icon is usually a blue cube).\n" +
                        "2) Drag that prefab into \"Layout Override\", OR drag an instance from the Hierarchy that was created from a prefab (Unity will use the prefab it came from).\n\n" +
                        "If you only wanted to say WHERE the question should appear (which panel in the scene):\n" +
                        "Use \"Question Parent\" above instead — drag your scene container (e.g. Quiz_Container) from the Hierarchy.\n\n" +
                        "Quick rule:\n" +
                        "• Scene position / parent → Question Parent (Hierarchy)\n" +
                        "• Which UI prefab to spawn → Layout Override (Project prefab)";
                    return false;
                }
            }

            if (candidate.GetComponentInChildren<QuestionUI>(true) == null)
            {
                errorTitle = "Layout Override — prefab is missing QuestionUI";
                errorMessage =
                    "What went wrong:\n" +
                    "The GameObject you picked is a valid prefab file, but nothing on that prefab (root or children) has a QuestionUI script.\n\n" +
                    "Why that matters:\n" +
                    "The quiz system needs a component that inherits QuestionUI — for example MultipleChoiceUI, FillInTheBlankUI, TrueFalseUI. " +
                    "That script connects the buttons and inputs to scoring and the node graph.\n\n" +
                    "How to fix it:\n" +
                    "1) Double-click the prefab in the Project to open Prefab Mode.\n" +
                    "2) On the root or a child, Add Component → your question type UI (must inherit QuestionUI), or use a ready-made quiz UI prefab from this project.\n" +
                    "3) Save the prefab, then assign it again in \"Layout Override\".\n\n" +
                    "Tip: Your question asset (multiple choice, fill blank, etc.) must match the kind of UI on the prefab.";
                return false;
            }

            prefabAsset = candidate;
            return true;
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
