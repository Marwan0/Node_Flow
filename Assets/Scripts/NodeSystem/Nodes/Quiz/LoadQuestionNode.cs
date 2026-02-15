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
        [SerializeField]
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

        [NonSerialized]
        private QuizManager _quizManager;

        [NonSerialized]
        private bool _questionAnswered = false;

        [NonSerialized]
        private bool _lastAnswerCorrect = false;

        [NonSerialized]
        private AnswerAnimationSettings[] _answerAnimations;

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
                new PortData("on_wrong", "On Wrong Attempt", PortDirection.Output),
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

            // Find QuizManager
            var managerObj = GameObject.Find(quizManagerPath);
            if (managerObj == null)
            {
                Debug.LogWarning($"[LoadQuestionNode] QuizManager not found: {quizManagerPath}");
                Complete();
                return;
            }

            _quizManager = managerObj.GetComponent<QuizManager>();
            if (_quizManager == null)
            {
                Debug.LogWarning($"[LoadQuestionNode] No QuizManager component on: {quizManagerPath}");
                Complete();
                return;
            }

            // Drive question container from node if set (so parent for instantiate is node-controlled)
            Transform container = ResolveQuestionContainer();
            if (container != null)
                _quizManager.questionContainer = container;

            // Load question from path
#if UNITY_EDITOR
            var question = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestionData>(questionAssetPath);
#else
            QuestionData question = null;
            Debug.LogWarning("[LoadQuestionNode] Runtime question loading requires Resources folder or Addressables");
#endif

            if (question == null)
            {
                Debug.LogWarning($"[LoadQuestionNode] Question not found: {questionAssetPath}");
                Complete();
                return;
            }

            // Add question to manager if not already there
            if (!_quizManager.questions.Contains(question))
            {
                _quizManager.questions.Add(question);
            }

            int questionIndex = _quizManager.questions.IndexOf(question);

            if (layoutOverridePrefab != null)
                _quizManager.SetUIPrefabOverrideForLoad(questionIndex, layoutOverridePrefab);

            // Set animations IMMEDIATELY before loading question (synchronously)
            if (QuizState.Instance != null)
            {
                QuizState.Instance.SetAnswerAnimations(_answerAnimations);
            }

            // Show THIS question immediately when this node runs (avoids wrong order when
            // multiple LoadQuestion nodes are connected to the same output, e.g. from StartQuiz).
            _quizManager.ShowQuestionByIndex(questionIndex);

            if (waitForAnswer)
            {
                QuizState.OnLastAnswerResult += OnAnswerReceived;
                QuizState.OnWrongAttempt += OnWrongAttemptReceived;
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
            QuizState.OnWrongAttempt -= OnWrongAttemptReceived;

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

        private Transform ResolveQuestionContainer()
        {
            if (questionContainerRef != null)
            {
                if (questionContainerRef is Transform t) return t;
                if (questionContainerRef is GameObject go) return go.transform;
                if (questionContainerRef is Component c) return c.transform;
            }
            if (string.IsNullOrEmpty(questionContainerPath)) return null;
            var found = GameObject.Find(questionContainerPath);
            if (found != null) return found.transform;
            var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            var parts = questionContainerPath.Split('/');
            if (parts.Length > 0)
            {
                foreach (var rootGo in rootObjects)
                {
                    if (rootGo.name != parts[0]) continue;
                    if (parts.Length == 1) return rootGo.transform;
                    var t = rootGo.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
                    if (t != null) return t;
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
            QuizState.OnWrongAttempt -= OnWrongAttemptReceived;
        }
    }
}
