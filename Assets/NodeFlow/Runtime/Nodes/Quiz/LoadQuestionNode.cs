using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Loads and displays a specific question from a QuestionData asset.
    /// Waits for the question to be answered before continuing.
    /// </summary>
    [Serializable]
    public class LoadQuestionNode : NodeData
    {
        [Header("Question Asset")]
        [SerializeField]
        [Tooltip("Direct reference to the QuestionData asset. Drag from Project window. This works in WebGL builds without needing Resources folder.")]
        public QuestionData questionRef;

        [SerializeField]
        [Tooltip("Asset path (auto-synced from reference, used as fallback)")]
        public string questionAssetPath = "";

        [SerializeField]
        public string quizManagerPath = "QuizManager";

        [Header("Question Parent (optional)")]
        [SerializeField]
        [Tooltip("Parent to instantiate this question under. Drag from Hierarchy. Overrides QuizManager's container when set.")]
        public UnityEngine.Object questionContainerRef;

        [Header("Layout Override (optional)")]
        [SerializeField]
        [Tooltip("If set, this UI prefab is used for this question instead of the question asset or QuizManager default. Must have the correct QuestionUI for the question type.")]
        public GameObject layoutOverridePrefab;

        [SerializeField]
        [Tooltip("Asset path fallback for layout override prefab (auto-synced in editor)")]
        public string layoutOverridePrefabPath = "";

        [SerializeField]
        [Tooltip("Fallback path when reference is null.")]
        public string questionContainerPath = "";

        [SerializeField]
        public bool waitForAnswer = true;

        [SerializeField]
        public bool trackInQuizState = true;

        [Header("Answer Animations")]
        [SerializeField]
        [Tooltip("Animation type to apply to all answers")]
        public AnswerAnimationType animationType = AnswerAnimationType.Scale;
        
        [SerializeField]
        [Tooltip("Duration of the animation in seconds")]
        [Range(0.1f, 2f)]
        public float animationDuration = 0.3f;
        
        [SerializeField]
        [Tooltip("Delay between each answer (staggered effect)")]
        [Range(0f, 0.5f)]
        public float staggerDelay = 0.1f;
        
        [SerializeField]
        [Tooltip("Ease type for the animation")]
#if DOTWEEN
        public DG.Tweening.Ease easeType = DG.Tweening.Ease.OutBack;
#else
        public int easeType = 0; // Fallback when DOTween not available
#endif
        
        [SerializeField]
        [Tooltip("Scale multiplier (for Scale/Bounce animations)")]
        [Range(0.1f, 2f)]
        public float scaleMultiplier = 1f;
        
        [SerializeField]
        [Tooltip("Slide distance (for Slide animations)")]
        [Range(10f, 500f)]
        public float slideDistance = 100f;
        
        [SerializeField]
        [Tooltip("Enable animations")]
        public bool enableAnimations = true;

        [Header("Hints")]
        [SerializeField]
        [Tooltip("Show hints on wrong attempts for this question")]
        public bool showHints = true;

        [NonSerialized]
        private QuizManager _quizManager;

        [NonSerialized]
        private bool _questionAnswered = false;

        [NonSerialized]
        private bool _lastAnswerCorrect = false;

        [NonSerialized]
        private AnswerAnimationSettings[] _answerAnimations;

        // Tracks GUIDs of all feedback-chain nodes still running (for auto-unlock)
        [NonSerialized]
        private HashSet<string> _feedbackTrackedGuids = new HashSet<string>();

        public override string Name => "Load Question";
        public override Color Color => new Color(0.2f, 0.7f, 0.4f); // Green
        public override string Category => "Quiz";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("correct", "Correct", PortDirection.Output),
                new PortData("incorrect", "Incorrect", PortDirection.Output),
                new PortData("on_correct", "On Correct Attempt", PortDirection.Output),
                new PortData("on_wrong", "On Wrong Attempt", PortDirection.Output),
                new PortData("on_correct_feedback", "On Correct Feedback", PortDirection.Output),
                new PortData("on_wrong_feedback", "On Wrong Feedback", PortDirection.Output),
                new PortData("complete", "Complete", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            _questionAnswered = false;
            _lastAnswerCorrect = false;

            // Create animation settings array from single settings (applied to all answers with stagger)
            _answerAnimations = null;
            if (enableAnimations)
            {
                _answerAnimations = new AnswerAnimationSettings[4];
                for (int i = 0; i < 4; i++)
                {
                    _answerAnimations[i] = new AnswerAnimationSettings
                    {
                        enabled = true,
                        animationType = animationType,
                        duration = animationDuration,
                        delay = i * staggerDelay, // Stagger each answer
#if DOTWEEN
                        easeType = easeType,
#else
                        easeType = 0,
#endif
                        scaleMultiplier = scaleMultiplier,
                        slideDistance = slideDistance
                    };
                }
            }

            // Find QuizManager: use shared ref from StartQuizNode first, then fallback to path
            _quizManager = QuizState.Instance.quizManagerRef;
            if (_quizManager == null && !string.IsNullOrEmpty(quizManagerPath))
            {
                var managerObj = GameObject.Find(quizManagerPath);
                if (managerObj != null)
                    _quizManager = managerObj.GetComponent<QuizManager>();
            }
            if (_quizManager == null)
            {
                Debug.LogWarning($"[LoadQuestionNode] QuizManager not found. Set it on StartQuizNode or provide a path.");
                Complete();
                return;
            }

            // Drive question container from node if set (so parent for instantiate is node-controlled)
            Transform container = ResolveQuestionContainer();
            if (container != null)
                _quizManager.questionContainer = container;

            // Load question - try direct reference first, then path, then NodeGraph storage
            QuestionData question = null;

#if UNITY_EDITOR
            // In editor, prefer direct reference, fallback to path
            question = questionRef;
            if (question == null && !string.IsNullOrEmpty(questionAssetPath))
            {
                question = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestionData>(questionAssetPath);
            }
#else
            // At runtime, try multiple sources in order:
            // 1. Direct reference (might be null in WebGL)
            question = questionRef;
            
            // 2. Try NodeGraph's separate storage (works in WebGL)
            if (question == null && Runner != null && Runner.Graph != null)
            {
                var storedRef = Runner.Graph.GetNodeAssetReference(Guid);
                if (storedRef is QuestionData storedQuestion)
                {
                    question = storedQuestion;
                    questionRef = question; // Cache it for next time
                    Debug.Log($"[LoadQuestionNode] Restored question from NodeGraph storage: {question.name}");
                }
            }
            
            // 3. If still null, try Resources as fallback
            if (question == null && !string.IsNullOrEmpty(questionAssetPath))
            {
                // Convert path to Resources path
                string resourcePath = questionAssetPath
                    .Replace("Assets/", "")
                    .Replace("Resources/", "")
                    .Replace(".asset", "");
                
                question = Resources.Load<QuestionData>(resourcePath);
                
                // Try filename only
                if (question == null)
                {
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(questionAssetPath);
                    question = Resources.Load<QuestionData>(fileName);
                }
                
                if (question != null)
                {
                    Debug.Log($"[LoadQuestionNode] Loaded question from Resources: {question.name}");
                }
            }
#endif

            if (question == null)
            {
                Debug.LogWarning($"[LoadQuestionNode] Question not found. Reference: {(questionRef != null ? questionRef.name : "null")}, Path: {questionAssetPath}");
                Complete();
                return;
            }

            // Add question to manager if not already there
            if (!_quizManager.questions.Contains(question))
            {
                _quizManager.questions.Add(question);
            }

            int questionIndex = _quizManager.questions.IndexOf(question);

            // Restore layout override from path if the direct prefab reference was lost after serialization.
            if (layoutOverridePrefab == null && !string.IsNullOrEmpty(layoutOverridePrefabPath))
            {
#if UNITY_EDITOR
                layoutOverridePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(layoutOverridePrefabPath);
#else
                // Runtime fallback if prefab is in a Resources folder.
                string resourcePath = layoutOverridePrefabPath
                    .Replace("Assets/", "")
                    .Replace("Resources/", "")
                    .Replace(".prefab", "");
                layoutOverridePrefab = Resources.Load<GameObject>(resourcePath);
#endif
            }

            if (layoutOverridePrefab != null)
                _quizManager.SetUIPrefabOverrideForLoad(questionIndex, layoutOverridePrefab);

            // Set animations and hints IMMEDIATELY before loading question (synchronously)
            if (QuizState.Instance != null)
            {
                QuizState.Instance.SetAnswerAnimations(_answerAnimations);
                QuizState.Instance.showHints = showHints;
            }

            // Show THIS question immediately when this node runs (avoids wrong order when
            // multiple LoadQuestion nodes are connected to the same output, e.g. from StartQuiz).
            _quizManager.ShowQuestionByIndex(questionIndex);

            if (waitForAnswer)
            {
                QuizState.OnLastAnswerResult += OnAnswerReceived;
                QuizState.OnCorrectAttempt += OnCorrectAttemptReceived;
                QuizState.OnWrongAttempt += OnWrongAttemptReceived;
                QuizState.OnCorrectAnswerFeedbackStart += OnCorrectAnswerFeedbackReceived;
                QuizState.OnWrongAnswerFeedbackStart += OnWrongAnswerFeedbackReceived;
                Runner?.StartCoroutine(LoadAndWaitForAnswer(questionIndex));
            }
            else
            {
                Runner?.StartCoroutine(LoadQuestionOnly(questionIndex));
            }
        }

        private IEnumerator LoadQuestionOnly(int questionIndex)
        {
            // Question was already shown in OnExecute via ShowQuestionByIndex
            yield return null;
            if (QuizState.Instance != null && _answerAnimations != null)
                QuizState.Instance.SetAnswerAnimations(_answerAnimations);
            Complete();
        }

        private IEnumerator LoadAndWaitForAnswer(int questionIndex)
        {
            // Question was already shown in OnExecute via ShowQuestionByIndex
            yield return null;
            if (QuizState.Instance != null && _answerAnimations != null)
                QuizState.Instance.SetAnswerAnimations(_answerAnimations);

            // Wait for answer
            while (!_questionAnswered && Runner != null && Runner.IsRunning)
            {
                yield return null;
            }

            // Unsubscribe
            QuizState.OnLastAnswerResult -= OnAnswerReceived;
            QuizState.OnCorrectAttempt -= OnCorrectAttemptReceived;
            QuizState.OnWrongAttempt -= OnWrongAttemptReceived;
            QuizState.OnCorrectAnswerFeedbackStart -= OnCorrectAnswerFeedbackReceived;
            QuizState.OnWrongAnswerFeedbackStart -= OnWrongAnswerFeedbackReceived;

            // Note: QuizState.RecordAnswer is now called by QuizManager.OnQuestionAnswered()
            // so we don't need to call it here anymore

            // Set state for branching
            State = _lastAnswerCorrect ? NodeState.Completed : NodeState.Failed;
            Complete();
        }

        private void OnAnswerReceived(bool wasCorrect)
        {
            _questionAnswered = true;
            _lastAnswerCorrect = wasCorrect;
        }

        private void OnWrongAttemptReceived()
        {
            // Fire nodes connected to "on_wrong" port for VFX/sounds
            // This does NOT complete the question - just triggers feedback
            if (Runner != null && Runner.Graph != null)
            {
                var wrongNodes = Runner.Graph.GetConnectedNodes(Guid, "on_wrong");
                foreach (var node in wrongNodes)
                {
                    Runner.ExecuteNode(node);
                }
            }
        }

        private void OnCorrectAttemptReceived()
        {
            // Fire nodes connected to "on_correct" port for per-step feedback
            // (e.g. one correct Connect pair). This does NOT complete the question.
            if (Runner != null && Runner.Graph != null)
            {
                var correctNodes = Runner.Graph.GetConnectedNodes(Guid, "on_correct");
                foreach (var node in correctNodes)
                {
                    Runner.ExecuteNode(node);
                }
            }
        }

        private void OnCorrectAnswerFeedbackReceived()
        {
            ExecuteFeedbackChain("on_correct_feedback");
        }

        private void OnWrongAnswerFeedbackReceived()
        {
            ExecuteFeedbackChain("on_wrong_feedback");
        }

        /// <summary>
        /// Fires all nodes connected to the given feedback port.
        /// Tracks every node in the chain by GUID using NodeGraphRunner.OnNodeCompleted
        /// (a static event the runner fires AFTER setting up OnComplete — so it's never lost).
        /// When the last tracked node finishes the UI is auto-unlocked.
        /// </summary>
        private void ExecuteFeedbackChain(string portId)
        {
            if (Runner == null || Runner.Graph == null)
            {
                QuizState.RequestUIUnlock();
                return;
            }

            var feedbackNodes = Runner.Graph.GetConnectedNodes(Guid, portId);
            if (feedbackNodes == null || feedbackNodes.Count == 0)
            {
                QuizState.RequestUIUnlock();
                return;
            }

            // Build initial tracking set from the direct feedback root nodes
            _feedbackTrackedGuids.Clear();
            foreach (var node in feedbackNodes)
            {
                if (node != null)
                    _feedbackTrackedGuids.Add(node.Guid);
            }

            // Subscribe to the STATIC runner event — fires after every node completes,
            // is never overwritten by the runner's OnComplete = assignment.
            NodeGraphRunner.OnNodeCompleted -= OnFeedbackChainNodeCompleted;
            NodeGraphRunner.OnNodeCompleted += OnFeedbackChainNodeCompleted;

            // Launch the root feedback nodes
            foreach (var node in feedbackNodes)
            {
                Runner.ExecuteNode(node);
            }
        }

        private void OnFeedbackChainNodeCompleted(NodeGraphRunner runner, NodeData completedNode)
        {
            // Only care about nodes in our feedback chain, on our runner
            if (runner != Runner) return;
            if (!_feedbackTrackedGuids.Remove(completedNode.Guid)) return;

            // Add any downstream nodes to keep tracking the full chain
            if (Runner?.Graph != null)
            {
                // Check all output ports for downstream connections
                foreach (var port in completedNode.GetOutputPorts())
                {
                    var downstream = Runner.Graph.GetConnectedNodes(completedNode.Guid, port.id);
                    if (downstream != null)
                    {
                        foreach (var dn in downstream)
                        {
                            if (dn != null)
                                _feedbackTrackedGuids.Add(dn.Guid);
                        }
                    }
                }
            }

            // When all tracked nodes are done — the whole chain has finished
            if (_feedbackTrackedGuids.Count == 0)
            {
                NodeGraphRunner.OnNodeCompleted -= OnFeedbackChainNodeCompleted;
                // Auto-unlock (idempotent guard in QuizState prevents double-fire)
                QuizState.RequestUIUnlock();
            }
        }

        private Transform ResolveQuestionContainer()
        {
            // First, try the direct reference
            if (questionContainerRef != null)
            {
                if (questionContainerRef is Transform t && t != null) return t;
                if (questionContainerRef is GameObject go && go != null) return go.transform;
                if (questionContainerRef is Component c && c != null) return c.transform;
            }
            
            // Reference is null or invalid - try to restore from path
            if (string.IsNullOrEmpty(questionContainerPath)) return null;
            
            // Try GameObject.Find first (works if object is active)
            var found = GameObject.Find(questionContainerPath);
            if (found != null)
            {
                // Restore the reference for next time
                questionContainerRef = found;
                return found.transform;
            }
            
            // Try hierarchical search (works even if object is inactive)
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var parts = questionContainerPath.Split('/');
            if (parts.Length > 0)
            {
                foreach (var rootGo in rootObjects)
                {
                    if (rootGo.name != parts[0]) continue;
                    if (parts.Length == 1)
                    {
                        questionContainerRef = rootGo; // Restore reference
                        return rootGo.transform;
                    }
                    var t = rootGo.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
                    if (t != null)
                    {
                        questionContainerRef = t.gameObject; // Restore reference
                        return t;
                    }
                }
            }
            return null;
        }

        public override void Reset()
        {
            base.Reset();
            _questionAnswered = false;
            _lastAnswerCorrect = false;
            QuizState.OnLastAnswerResult -= OnAnswerReceived;
            QuizState.OnCorrectAttempt -= OnCorrectAttemptReceived;
            QuizState.OnWrongAttempt -= OnWrongAttemptReceived;
            QuizState.OnCorrectAnswerFeedbackStart -= OnCorrectAnswerFeedbackReceived;
            QuizState.OnWrongAnswerFeedbackStart -= OnWrongAnswerFeedbackReceived;
            // Clean up feedback chain tracking
            NodeGraphRunner.OnNodeCompleted -= OnFeedbackChainNodeCompleted;
            _feedbackTrackedGuids.Clear();
        }
    }
}
