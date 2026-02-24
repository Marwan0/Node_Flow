using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace QuizSystem
{
    public abstract class QuestionUI : MonoBehaviour
    {
        [Header("Common UI Elements")]
        [SerializeField] protected TextMeshProUGUI questionText;
        [SerializeField] protected TextMeshProUGUI hintText;
        [SerializeField] protected TextMeshProUGUI attemptCounterText;
        [SerializeField] protected GameObject hintPanel;
        [Tooltip("Clickable hint button (optional - reveals hint on click)")]
        [SerializeField] protected Button hintButton;
        [Tooltip("Submit button (optional - not needed for question types that auto-submit like Multiple Choice)")]
        [SerializeField] protected Button submitButton;

        [Header("Animations")]
        [Tooltip("Enable feedback animations")]
        public bool enableFeedbackAnimations = true;

        [Range(0.1f, 1f)]
        [Tooltip("Duration of feedback animations")]
        public float feedbackDuration = 0.5f;

        protected QuestionData currentQuestion;
        protected IQuestionValidator validator;
        protected QuizManager quizManager;
        protected RectTransform hintPanelRectTransform;
        protected CanvasGroup hintPanelCanvasGroup;

        /// <summary>
        /// Whether hints are globally enabled (set by LoadQuestionNode via QuizState).
        /// </summary>
        protected bool HintsEnabled
        {
            get
            {
                if (QuizState.Instance != null) return QuizState.Instance.showHints;
                return true;
            }
        }

        /// <summary>
        /// Whether a hint should be displayed right now, based on global toggle
        /// and the per-question showHintAfterAttempt threshold.
        /// </summary>
        protected bool ShouldShowHintNow()
        {
            if (!HintsEnabled) return false;
            if (currentQuestion == null) return false;
            int threshold = currentQuestion.showHintAfterAttempt;
            if (threshold <= 0) return false; // 0 = never auto-show hints for this question
            if (validator == null) return threshold <= 1;
            return validator.GetCurrentAttempt() >= threshold;
        }

        /// <summary>
        /// Returns the appropriate hint string for the current attempt, remapped so that
        /// hints[0] is always the first hint shown (even if display starts on a later attempt).
        /// Returns null when no hint is available.
        /// </summary>
        protected string GetHintForCurrentAttempt()
        {
            if (currentQuestion == null || currentQuestion.hints == null || currentQuestion.hints.Length == 0)
                return null;
            int threshold = Mathf.Max(1, currentQuestion.showHintAfterAttempt);
            int attempt = validator != null ? validator.GetCurrentAttempt() : 1;
            int hintIndex = attempt - threshold;
            if (hintIndex < 0) return null;
            hintIndex = Mathf.Min(hintIndex, currentQuestion.hints.Length - 1);
            string hint = currentQuestion.hints[hintIndex];
            return string.IsNullOrEmpty(hint) ? null : hint;
        }

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

            // Setup hint button (toggles hint panel visibility on click)
            if (hintButton != null)
            {
                hintButton.onClick.RemoveAllListeners();
                if (HintsEnabled)
                    hintButton.onClick.AddListener(OnHintButtonClicked);
                hintButton.gameObject.SetActive(false); // hidden until first qualifying wrong attempt
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

        /// <summary>
        /// Called when the hint button is clicked. Toggles hint panel visibility.
        /// Override in subclasses for custom hint button behavior.
        /// </summary>
        protected virtual void OnHintButtonClicked()
        {
            if (hintPanel == null) return;

            if (hintPanel.activeSelf)
            {
                HideHint();
            }
            else
            {
                string hint = GetHintForCurrentAttempt();
                if (!string.IsNullOrEmpty(hint))
                    ShowHint(hint);
            }
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
                    // Only show hint if global toggle is on AND per-question threshold is met
                    if (ShouldShowHintNow())
                    {
                        string hint = GetHintForCurrentAttempt();
                        ShowHint(!string.IsNullOrEmpty(hint) ? hint : result.Message);
                        if (hintButton != null) hintButton.gameObject.SetActive(true);
                    }
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
            
            quizManager?.OnQuestionAnswered(true, currentQuestion.points, currentQuestion);
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

            // Only show correct-answer hint if hints are globally enabled
            if (HintsEnabled)
            {
                ShowHint($"Correct answer: {GetCorrectAnswerDisplay()}");
                if (!string.IsNullOrEmpty(currentQuestion.explanation))
                {
                    ShowHint($"{hintText.text}\n\nExplanation: {currentQuestion.explanation}");
                }
            }

            if (hintButton != null) hintButton.gameObject.SetActive(false);

            // User exhausted all attempts without getting the correct answer
            // Pass false since they didn't actually answer correctly
            quizManager?.OnQuestionAnswered(false, 0, currentQuestion);
        }

        protected abstract string GetCorrectAnswerDisplay();
    }
}
