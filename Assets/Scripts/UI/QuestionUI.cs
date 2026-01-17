using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace QuizSystem
{
    public abstract class QuestionUI : MonoBehaviour
    {
        [Header("Common UI Elements")]
        [SerializeField] protected TextMeshProUGUI questionText;
        [SerializeField] protected TextMeshProUGUI hintText;
        [SerializeField] protected TextMeshProUGUI attemptCounterText;
        [SerializeField] protected GameObject hintPanel;
        [Tooltip("Submit button (optional - not needed for question types that auto-submit like Multiple Choice)")]
        [SerializeField] protected Button submitButton;

        [BoxGroup("Animations")]
        [Tooltip("Enable feedback animations")]
        public bool enableFeedbackAnimations = true;

        [BoxGroup("Animations")]
        [ShowIf("enableFeedbackAnimations")]
        [Range(0.1f, 1f)]
        [Tooltip("Duration of feedback animations")]
        public float feedbackDuration = 0.5f;

        [BoxGroup("Animations")]
        [ShowIf("enableFeedbackAnimations")]
        [Button("Open Animation Preview", ButtonSizes.Medium)]
        [PropertyOrder(10)]
        private void OpenAnimationPreview()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.ExecuteMenuItem("Tools/Quiz System/Animation Preview");
            
            // Use reflection to get the window type and set the questionUI field
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
                    var questionUIField = windowType.GetField("questionUI", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    questionUIField?.SetValue(window, this);
                }
            }
#endif
        }

        protected QuestionData currentQuestion;
        protected IQuestionValidator validator;
        protected QuizManager quizManager;
        protected RectTransform hintPanelRectTransform;
        protected CanvasGroup hintPanelCanvasGroup;

        public virtual void Initialize(QuestionData question, IQuestionValidator questionValidator, QuizManager manager)
        {
            currentQuestion = question;
            validator = questionValidator;
            quizManager = manager;

            if (questionText != null)
                questionText.text = question.questionText;

            // Setup hint panel components for animations
            if (hintPanel != null)
            {
                hintPanel.SetActive(false);
                hintPanelRectTransform = hintPanel.GetComponent<RectTransform>();
                hintPanelCanvasGroup = hintPanel.GetComponent<CanvasGroup>();
                if (hintPanelCanvasGroup == null)
                {
                    hintPanelCanvasGroup = hintPanel.AddComponent<CanvasGroup>();
                }
            }

            UpdateAttemptCounter();
            SetupQuestion();
        }

        protected abstract void SetupQuestion();
        public abstract void OnAnswerSubmitted();

        protected virtual void ShowHint(string hint)
        {
            if (hintPanel != null)
            {
                hintPanel.SetActive(true);

                if (hintText != null && !string.IsNullOrEmpty(hint))
                    hintText.text = hint;

                // Animate hint reveal
                if (enableFeedbackAnimations)
                {
                    AnimateHintReveal();
                }
            }
        }

        protected virtual void AnimateHintReveal()
        {
            if (hintPanelRectTransform != null && hintPanelCanvasGroup != null)
            {
                // Reset state
                hintPanelCanvasGroup.alpha = 0f;
                if (hintPanelRectTransform != null)
                {
                    Vector2 targetPos = hintPanelRectTransform.anchoredPosition;
                    hintPanelRectTransform.anchoredPosition = targetPos + Vector2.down * 30f;
                }

                // Animate in
                Sequence sequence = DOTween.Sequence();
                if (hintPanelRectTransform != null)
                {
                    Vector2 targetPos = hintPanelRectTransform.anchoredPosition;
                    targetPos.y += 30f;
                    sequence.Join(hintPanelRectTransform.DOAnchorPos(targetPos, feedbackDuration * 0.8f).SetEase(Ease.OutQuad));
                }
                sequence.Join(hintPanelCanvasGroup.DOFade(1f, feedbackDuration).SetEase(Ease.OutQuad));
            }
            else if (hintText != null)
            {
                // Fallback: just fade in text
                Color originalColor = hintText.color;
                hintText.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0f);
                hintText.DOFade(1f, feedbackDuration).SetEase(Ease.OutQuad);
            }
        }

        protected virtual void HideHint()
        {
            if (hintPanel != null)
                hintPanel.SetActive(false);
        }

        protected virtual void UpdateAttemptCounter()
        {
            if (attemptCounterText != null && validator != null)
            {
                attemptCounterText.text = $"Attempt: {validator.GetCurrentAttempt()} / {currentQuestion.maxAttempts}";
            }
        }

        protected virtual void HandleValidationResult(ValidationResult result)
        {
            UpdateAttemptCounter();

            if (result.IsCorrect)
            {
                OnCorrectAnswer();
            }
            else
            {
                if (result.ShouldAutoCorrect)
                {
                    OnAutoCorrect();
                }
                else
                {
                    ShowHint(result.Message);
                    OnWrongAnswer();
                }
            }
        }

        protected virtual void OnCorrectAnswer()
        {
            Debug.Log("Correct answer!");
            if (submitButton != null)
                submitButton.interactable = false;
            
            // Animate correct answer feedback
            if (enableFeedbackAnimations)
            {
                AnimateCorrectAnswer();
            }
            
            quizManager?.OnQuestionAnswered(true, currentQuestion.points);
        }

        protected virtual void OnWrongAnswer()
        {
            Debug.Log("Wrong answer. Try again.");

            // Animate wrong answer feedback
            if (enableFeedbackAnimations)
            {
                AnimateWrongAnswer();
            }
            
            // Notify QuizState of wrong attempt (for VFX/sounds via node system)
            // This does NOT complete the question - user can still try again
            QuizState.Instance?.NotifyWrongAttempt();
        }

        protected virtual void AnimateCorrectAnswer()
        {
            // Default: subtle scale bounce on the entire question UI
            if (transform != null)
            {
                Vector3 originalScale = transform.localScale;
                Sequence sequence = DOTween.Sequence();
                sequence.Append(transform.DOScale(originalScale * 1.05f, feedbackDuration * 0.3f).SetEase(Ease.OutQuad));
                sequence.Append(transform.DOScale(originalScale, feedbackDuration * 0.7f).SetEase(Ease.InQuad));
            }
        }

        protected virtual void AnimateWrongAnswer()
        {
            // Default: subtle shake on the entire question UI
            if (transform != null)
            {
                transform.DOShakePosition(feedbackDuration, 5f, 10, 90f, false, true);
            }
        }

        protected virtual void OnAutoCorrect()
        {
            Debug.Log("Auto-correct triggered - user exhausted all attempts.");
            ShowHint($"Correct answer: {GetCorrectAnswerDisplay()}");
            if (!string.IsNullOrEmpty(currentQuestion.explanation))
            {
                ShowHint($"{hintText.text}\n\nExplanation: {currentQuestion.explanation}");
            }
            
            // User exhausted all attempts without getting the correct answer
            // Pass false since they didn't actually answer correctly
            quizManager?.OnQuestionAnswered(false, 0);
        }

        protected abstract string GetCorrectAnswerDisplay();
    }
}

