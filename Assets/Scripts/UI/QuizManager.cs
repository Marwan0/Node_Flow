using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace QuizSystem
{
    public class QuizManager : MonoBehaviour
    {
        [BoxGroup("Quiz Settings")]
        [Tooltip("List of questions for this quiz")]
        public List<QuestionData> questions = new List<QuestionData>();

        [BoxGroup("Quiz Settings")]
        [Tooltip("Shuffle questions before starting")]
        public bool shuffleQuestions = false;

        [BoxGroup("UI References")]
        [Required]
        [Tooltip("Parent to instantiate each question under. You can disable this or animate it to remove the current question before the next one.")]
        public Transform questionContainer;

        [BoxGroup("UI References")]
        [Tooltip("Prefab for True/False questions")]
        public GameObject trueFalseUIPrefab;

        [BoxGroup("UI References")]
        [Tooltip("Prefab for Fill in the Blank questions")]
        public GameObject fillInTheBlankUIPrefab;

        [BoxGroup("UI References")]
        [Tooltip("Prefab for Multi-Select questions")]
        public GameObject multiSelectUIPrefab;

        [BoxGroup("UI References")]
        [Tooltip("Prefab for Ordering questions")]
        public GameObject orderingUIPrefab;

        [BoxGroup("UI References")]
        [Tooltip("Prefab for Hotspot questions")]
        public GameObject hotspotUIPrefab;

        [BoxGroup("UI References")]
        [Tooltip("Prefab for Slider questions")]
        public GameObject sliderUIPrefab;

        [BoxGroup("UI References")]
        [Tooltip("Prefab for Audio questions")]
        public GameObject audioUIPrefab;

        [BoxGroup("UI References")]
        [Tooltip("Prefab for Multiple Choice questions")]
        public GameObject multipleChoiceUIPrefab;

        [BoxGroup("UI References")]
        [Tooltip("Prefab for Drag & Drop questions")]
        public GameObject dragDropUIPrefab;

        [BoxGroup("UI References")]
        [Tooltip("Prefab for Connect questions")]
        public GameObject connectUIPrefab;

        [BoxGroup("Score")]
        [ReadOnly]
        [Tooltip("Current score")]
        public int currentScore = 0;

        [BoxGroup("Score")]
        [ReadOnly]
        [Tooltip("Current question index")]
        public int currentQuestionIndex = 0;

        [BoxGroup("Animations")]
        [Tooltip("Enable smooth transitions between questions")]
        public bool enableTransitions = true;

        [BoxGroup("Animations")]
        [ShowIf("enableTransitions")]
        [Tooltip("Duration of fade transition")]
        [Range(0.1f, 1f)]
        public float transitionDuration = 0.3f;

        [BoxGroup("Animations")]
        [ShowIf("enableTransitions")]
        [Tooltip("Transition style")]
        [ValueDropdown("GetTransitionStyles")]
        public TransitionStyle transitionStyle = TransitionStyle.Fade;

        [BoxGroup("Animations")]
        [ShowIf("enableTransitions")]
        [Button("Open Animation Preview", ButtonSizes.Medium)]
        [PropertyOrder(10)]
        private void OpenAnimationPreview()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Quiz System/Animation Preview");
            
            // Use reflection to get the window type and set the quizManager field
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            System.Type windowType = null;
            
            foreach (var assembly in assemblies)
            {
                windowType = assembly.GetType("QuizSystem.QuizAnimationPreviewWindow");
                if (windowType != null) break;
            }
            
            if (windowType != null)
            {
                var getWindowMethod = typeof(UnityEditor.EditorWindow).GetMethod("GetWindow", new System.Type[] { typeof(System.Type) });
                var window = getWindowMethod?.Invoke(null, new object[] { windowType });
                
                if (window != null)
                {
                    var quizManagerField = windowType.GetField("quizManager", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    quizManagerField?.SetValue(window, this);
                }
            }
#endif
        }

        public enum TransitionStyle
        {
            Fade,
            Slide,
            Scale
        }

        private ValueDropdownList<TransitionStyle> GetTransitionStyles()
        {
            return new ValueDropdownList<TransitionStyle>
            {
                { "Fade", TransitionStyle.Fade },
                { "Slide", TransitionStyle.Slide },
                { "Scale", TransitionStyle.Scale }
            };
        }

        private List<QuestionData> shuffledQuestions;
        private QuestionUI currentQuestionUI;
        private IQuestionValidator currentValidator;
        private Transform currentQuestionWrapper;
        private Sequence currentTransitionSequence;
        private Dictionary<int, GameObject> _runtimeUIPrefabOverrides;

        private void Awake()
        {
            // No longer add CanvasGroup to questionContainer; each question gets its own wrapper with transition applied
        }

        [Button("Start Quiz")]
        [BoxGroup("Quiz Controls")]
        public void StartQuiz()
        {
            if (questions == null || questions.Count == 0)
            {
                Debug.LogError("No questions assigned to quiz!");
                return;
            }
            EnsureQuizStarted();
            LoadQuestion(0);
        }

        /// <summary>
        /// Set a UI prefab to use for the next time the question at this index is loaded.
        /// Used by LoadQuestionNode to pass a layout override per load. Cleared after that load.
        /// </summary>
        public void SetUIPrefabOverrideForLoad(int questionIndex, GameObject uiPrefab)
        {
            if (uiPrefab == null) return;
            if (_runtimeUIPrefabOverrides == null)
                _runtimeUIPrefabOverrides = new Dictionary<int, GameObject>();
            _runtimeUIPrefabOverrides[questionIndex] = uiPrefab;
        }

        /// <summary>
        /// Ensures shuffledQuestions and current state are initialized (e.g. when using node graph without StartQuiz).
        /// Does not load or show a question; use LoadQuestion(0) or StartQuiz() for that.
        /// </summary>
        public void EnsureQuizStarted()
        {
            if (shuffledQuestions != null) return;
            if (questions == null || questions.Count == 0) return;
            shuffledQuestions = new List<QuestionData>(questions);
            if (shuffleQuestions)
            {
                for (int i = 0; i < shuffledQuestions.Count; i++)
                {
                    QuestionData temp = shuffledQuestions[i];
                    int randomIndex = Random.Range(i, shuffledQuestions.Count);
                    shuffledQuestions[i] = shuffledQuestions[randomIndex];
                    shuffledQuestions[randomIndex] = temp;
                }
            }
            currentScore = 0;
            currentQuestionIndex = 0;
        }

        /// <summary>
        /// Show the question at the given index immediately. Used by LoadQuestionNode so the correct
        /// question is shown when the node runs (avoids wrong order when multiple nodes run in parallel).
        /// </summary>
        public void ShowQuestionByIndex(int index)
        {
            EnsureQuizStarted();
            if (shuffledQuestions == null || index < 0 || index >= shuffledQuestions.Count) return;
            currentQuestionIndex = index;
            LoadQuestion(index);
        }

        [Button("Next Question")]
        [BoxGroup("Quiz Controls")]
        public void NextQuestion()
        {
            EnsureQuizStarted();
            if (shuffledQuestions == null || shuffledQuestions.Count == 0)
            {
                Debug.LogWarning("NextQuestion: no questions loaded.");
                return;
            }
            if (currentQuestionIndex < shuffledQuestions.Count - 1)
            {
                currentQuestionIndex++;
                LoadQuestion(currentQuestionIndex);
            }
            else
            {
                EndQuiz();
            }
        }

        [Button("Previous Question")]
        [BoxGroup("Quiz Controls")]
        public void PreviousQuestion()
        {
            EnsureQuizStarted();
            if (shuffledQuestions == null || shuffledQuestions.Count == 0) return;
            if (currentQuestionIndex > 0)
            {
                currentQuestionIndex--;
                LoadQuestion(currentQuestionIndex);
            }
        }

        private void LoadQuestion(int index)
        {
            EnsureQuizStarted();
            if (shuffledQuestions == null || index < 0 || index >= shuffledQuestions.Count)
            {
                Debug.LogError($"Invalid question index: {index}");
                return;
            }

            QuestionData question = shuffledQuestions[index];
            if (question == null)
            {
                Debug.LogError($"Question at index {index} is null!");
                return;
            }

            if (enableTransitions && currentQuestionUI != null)
            {
                // Animate transition out, then load new question
                TransitionOut(() => LoadNewQuestion(index));
            }
            else
            {
                // No transition, remove current question (and its wrapper) then load next
                if (currentQuestionWrapper != null)
                {
                    Destroy(currentQuestionWrapper.gameObject);
                    currentQuestionWrapper = null;
                }
                else if (currentQuestionUI != null)
                {
                    Destroy(currentQuestionUI.gameObject);
                }
                currentQuestionUI = null;
                LoadNewQuestion(index);
            }
        }

        private void TransitionOut(System.Action onComplete)
        {
            if (currentTransitionSequence != null && currentTransitionSequence.IsActive())
            {
                currentTransitionSequence.Kill();
            }

            Transform target = currentQuestionWrapper != null ? currentQuestionWrapper : questionContainer;
            if (target == null)
            {
                if (currentQuestionUI != null) Destroy(currentQuestionUI.gameObject);
                onComplete?.Invoke();
                return;
            }

            currentTransitionSequence = DOTween.Sequence();

            switch (transitionStyle)
            {
                case TransitionStyle.Fade:
                    var cgOut = target.GetComponent<CanvasGroup>();
                    if (cgOut == null) cgOut = target.gameObject.AddComponent<CanvasGroup>();
                    currentTransitionSequence.Append(cgOut.DOFade(0f, transitionDuration));
                    break;

                case TransitionStyle.Slide:
                    RectTransform rtOut = target as RectTransform;
                    if (rtOut == null) rtOut = target.GetComponent<RectTransform>();
                    if (rtOut != null)
                        currentTransitionSequence.Append(rtOut.DOAnchorPosX(rtOut.anchoredPosition.x - 1000f, transitionDuration));
                    break;

                case TransitionStyle.Scale:
                    currentTransitionSequence.Append(target.DOScale(0f, transitionDuration));
                    break;
            }

            currentTransitionSequence.OnComplete(() =>
            {
                if (currentQuestionWrapper != null)
                {
                    Destroy(currentQuestionWrapper.gameObject);
                    currentQuestionWrapper = null;
                }
                else if (currentQuestionUI != null)
                {
                    Destroy(currentQuestionUI.gameObject);
                }
                currentQuestionUI = null;
                onComplete?.Invoke();
            });
        }

        private void LoadNewQuestion(int index)
        {
            QuestionData question = shuffledQuestions[index];

            // Create validator
            currentValidator = ValidatorFactory.CreateValidator(question);
            if (currentValidator == null)
            {
                Debug.LogError($"Failed to create validator for question type: {question.questionType}");
                return;
            }

            // Create appropriate UI (node override > question custom prefab > type default)
            GameObject uiPrefab = null;
            if (_runtimeUIPrefabOverrides != null && _runtimeUIPrefabOverrides.TryGetValue(index, out var overr))
            {
                _runtimeUIPrefabOverrides.Remove(index);
                uiPrefab = overr;
            }
            if (uiPrefab == null)
                uiPrefab = GetUIPrefabForQuestion(question);
            if (uiPrefab == null)
            {
                Debug.LogError($"No UI prefab found for question type: {question.questionType}");
                return;
            }

            // Create a wrapper under questionContainer so we can disable or animate just this question
            currentQuestionWrapper = CreateQuestionWrapper();
            GameObject uiInstance = Instantiate(uiPrefab, currentQuestionWrapper);
            currentQuestionUI = uiInstance.GetComponent<QuestionUI>();
            if (currentQuestionUI == null)
            {
                Debug.LogError($"UI prefab doesn't have QuestionUI component!");
                Destroy(currentQuestionWrapper.gameObject);
                currentQuestionWrapper = null;
                return;
            }

            if (enableTransitions)
            {
                ResetWrapperForTransitionIn(currentQuestionWrapper);
            }

            // Note: Animations should be set in QuizState BEFORE Initialize() is called
            // This is done by LoadQuestionNode or other nodes that control question loading
            // The QuestionUI will check QuizState.Instance.currentAnswerAnimations in SetupQuestion()
            
            currentQuestionUI.Initialize(question, currentValidator, this);

            // Animate transition in
            if (enableTransitions)
            {
                TransitionIn();
            }
        }

        private Transform CreateQuestionWrapper()
        {
            if (questionContainer == null) return null;
            var wrapperGo = new GameObject("QuestionWrapper");
            wrapperGo.transform.SetParent(questionContainer, false);
            var rt = questionContainer as RectTransform;
            if (rt != null)
            {
                var wrapperRt = wrapperGo.AddComponent<RectTransform>();
                wrapperRt.anchorMin = Vector2.zero;
                wrapperRt.anchorMax = Vector2.one;
                wrapperRt.sizeDelta = Vector2.zero;
                wrapperRt.anchoredPosition = Vector2.zero;
            }
            if (transitionStyle == TransitionStyle.Fade)
            {
                var cg = wrapperGo.AddComponent<CanvasGroup>();
                cg.alpha = 0f;
            }
            return wrapperGo.transform;
        }

        private void ResetWrapperForTransitionIn(Transform wrapper)
        {
            if (wrapper == null) return;
            switch (transitionStyle)
            {
                case TransitionStyle.Fade:
                    var cg = wrapper.GetComponent<CanvasGroup>();
                    if (cg == null) cg = wrapper.gameObject.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    break;

                case TransitionStyle.Slide:
                    RectTransform rt = wrapper as RectTransform ?? wrapper.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Vector2 pos = rt.anchoredPosition;
                        rt.anchoredPosition = new Vector2(pos.x + 1000f, pos.y);
                    }
                    break;

                case TransitionStyle.Scale:
                    wrapper.localScale = Vector3.zero;
                    break;
            }
        }

        private void TransitionIn()
        {
            if (currentTransitionSequence != null && currentTransitionSequence.IsActive())
            {
                currentTransitionSequence.Kill();
            }

            Transform target = currentQuestionWrapper != null ? currentQuestionWrapper : questionContainer;
            if (target == null) return;

            currentTransitionSequence = DOTween.Sequence();

            switch (transitionStyle)
            {
                case TransitionStyle.Fade:
                    var cgIn = target.GetComponent<CanvasGroup>();
                    if (cgIn != null)
                        currentTransitionSequence.Append(cgIn.DOFade(1f, transitionDuration));
                    break;

                case TransitionStyle.Slide:
                    RectTransform rtIn = target as RectTransform ?? target.GetComponent<RectTransform>();
                    if (rtIn != null)
                    {
                        Vector2 targetPos = rtIn.anchoredPosition;
                        targetPos.x -= 1000f;
                        currentTransitionSequence.Append(rtIn.DOAnchorPos(targetPos, transitionDuration));
                    }
                    break;

                case TransitionStyle.Scale:
                    currentTransitionSequence.Append(target.DOScale(1f, transitionDuration));
                    break;
            }

            currentTransitionSequence.SetEase(Ease.OutQuad);
        }

        private GameObject GetUIPrefabForQuestion(QuestionData question)
        {
            if (question != null && question.customUIPrefab != null)
                return question.customUIPrefab;
            return GetUIPrefabForQuestionType(question != null ? question.questionType : default);
        }

        private GameObject GetUIPrefabForQuestionType(QuestionType type)
        {
            switch (type)
            {
                case QuestionType.TrueFalse:
                    return trueFalseUIPrefab;
                case QuestionType.FillInTheBlank:
                    return fillInTheBlankUIPrefab;
                case QuestionType.MultiSelect:
                    return multiSelectUIPrefab;
                case QuestionType.Ordering:
                    return orderingUIPrefab;
                case QuestionType.Hotspot:
                    return hotspotUIPrefab;
                case QuestionType.Slider:
                    return sliderUIPrefab;
                case QuestionType.Audio:
                    return audioUIPrefab;
                case QuestionType.MultipleChoice:
                    return multipleChoiceUIPrefab;
                case QuestionType.DragDrop:
                    return dragDropUIPrefab;
                case QuestionType.Connect:
                    return connectUIPrefab;
                default:
                    return null;
            }
        }

        public void OnQuestionAnswered(bool isCorrect, int points)
        {
            if (isCorrect)
            {
                currentScore += points;
                Debug.Log($"Correct! +{points} points. Total: {currentScore}");
            }
            
            // Notify QuizState to fire the OnLastAnswerResult event
            // This allows LoadQuestionNode to detect when an answer is submitted
            if (QuizState.Instance != null)
            {
                QuizState.Instance.RecordAnswer(currentQuestionIndex, isCorrect, isCorrect ? points : 0);
            }
        }

        private void EndQuiz()
        {
            Debug.Log($"Quiz Complete! Final Score: {currentScore}");
            // You can add UI for quiz completion here
        }

        [Button("Reset Quiz")]
        [BoxGroup("Quiz Controls")]
        private void ResetQuiz()
        {
            if (currentQuestionUI != null)
            {
                Destroy(currentQuestionUI.gameObject);
            }
            currentQuestionUI = null;
            currentValidator = null;
            currentScore = 0;
            currentQuestionIndex = 0;
        }
    }
}

