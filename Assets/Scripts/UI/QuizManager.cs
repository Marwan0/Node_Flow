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
        [Tooltip("Container for question UI")]
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
        private CanvasGroup questionContainerCanvasGroup;
        private Sequence currentTransitionSequence;

        private void Awake()
        {
            // Ensure question container has CanvasGroup for fade transitions
            if (questionContainer != null)
            {
                questionContainerCanvasGroup = questionContainer.GetComponent<CanvasGroup>();
                if (questionContainerCanvasGroup == null)
                {
                    questionContainerCanvasGroup = questionContainer.gameObject.AddComponent<CanvasGroup>();
                }
            }
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

            shuffledQuestions = new List<QuestionData>(questions);
            if (shuffleQuestions)
            {
                // Shuffle
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
            LoadQuestion(0);
        }

        [Button("Next Question")]
        [BoxGroup("Quiz Controls")]
        public void NextQuestion()
        {
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
            if (currentQuestionIndex > 0)
            {
                currentQuestionIndex--;
                LoadQuestion(currentQuestionIndex);
            }
        }

        private void LoadQuestion(int index)
        {
            if (index < 0 || index >= shuffledQuestions.Count)
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
                // No transition, load immediately
                if (currentQuestionUI != null)
                {
                    Destroy(currentQuestionUI.gameObject);
                }
                LoadNewQuestion(index);
            }
        }

        private void TransitionOut(System.Action onComplete)
        {
            if (currentTransitionSequence != null && currentTransitionSequence.IsActive())
            {
                currentTransitionSequence.Kill();
            }

            currentTransitionSequence = DOTween.Sequence();

            switch (transitionStyle)
            {
                case TransitionStyle.Fade:
                    if (questionContainerCanvasGroup != null)
                    {
                        currentTransitionSequence.Append(questionContainerCanvasGroup.DOFade(0f, transitionDuration));
                    }
                    break;

                case TransitionStyle.Slide:
                    if (questionContainer != null)
                    {
                        RectTransform rectTransform = questionContainer as RectTransform;
                        if (rectTransform != null)
                        {
                            currentTransitionSequence.Append(rectTransform.DOAnchorPosX(rectTransform.anchoredPosition.x - 1000f, transitionDuration));
                        }
                    }
                    break;

                case TransitionStyle.Scale:
                    if (questionContainer != null)
                    {
                        currentTransitionSequence.Append(questionContainer.DOScale(0f, transitionDuration));
                    }
                    break;
            }

            currentTransitionSequence.OnComplete(() =>
            {
                if (currentQuestionUI != null)
                {
                    Destroy(currentQuestionUI.gameObject);
                }
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

            // Create appropriate UI
            GameObject uiPrefab = GetUIPrefabForQuestionType(question.questionType);
            if (uiPrefab == null)
            {
                Debug.LogError($"No UI prefab found for question type: {question.questionType}");
                return;
            }

            GameObject uiInstance = Instantiate(uiPrefab, questionContainer);
            currentQuestionUI = uiInstance.GetComponent<QuestionUI>();
            if (currentQuestionUI == null)
            {
                Debug.LogError($"UI prefab doesn't have QuestionUI component!");
                Destroy(uiInstance);
                return;
            }

            // Reset container state for transition in
            if (enableTransitions)
            {
                ResetContainerForTransitionIn();
            }

            currentQuestionUI.Initialize(question, currentValidator, this);

            // Animate transition in
            if (enableTransitions)
            {
                TransitionIn();
            }
        }

        private void ResetContainerForTransitionIn()
        {
            switch (transitionStyle)
            {
                case TransitionStyle.Fade:
                    if (questionContainerCanvasGroup != null)
                    {
                        questionContainerCanvasGroup.alpha = 0f;
                    }
                    break;

                case TransitionStyle.Slide:
                    if (questionContainer != null)
                    {
                        RectTransform rectTransform = questionContainer as RectTransform;
                        if (rectTransform != null)
                        {
                            Vector2 pos = rectTransform.anchoredPosition;
                            rectTransform.anchoredPosition = new Vector2(pos.x + 1000f, pos.y);
                        }
                    }
                    break;

                case TransitionStyle.Scale:
                    if (questionContainer != null)
                    {
                        questionContainer.localScale = Vector3.zero;
                    }
                    break;
            }
        }

        private void TransitionIn()
        {
            if (currentTransitionSequence != null && currentTransitionSequence.IsActive())
            {
                currentTransitionSequence.Kill();
            }

            currentTransitionSequence = DOTween.Sequence();

            switch (transitionStyle)
            {
                case TransitionStyle.Fade:
                    if (questionContainerCanvasGroup != null)
                    {
                        currentTransitionSequence.Append(questionContainerCanvasGroup.DOFade(1f, transitionDuration));
                    }
                    break;

                case TransitionStyle.Slide:
                    if (questionContainer != null)
                    {
                        RectTransform rectTransform = questionContainer as RectTransform;
                        if (rectTransform != null)
                        {
                            Vector2 targetPos = rectTransform.anchoredPosition;
                            targetPos.x -= 1000f;
                            currentTransitionSequence.Append(rectTransform.DOAnchorPos(targetPos, transitionDuration));
                        }
                    }
                    break;

                case TransitionStyle.Scale:
                    if (questionContainer != null)
                    {
                        currentTransitionSequence.Append(questionContainer.DOScale(1f, transitionDuration));
                    }
                    break;
            }

            currentTransitionSequence.SetEase(Ease.OutQuad);
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

