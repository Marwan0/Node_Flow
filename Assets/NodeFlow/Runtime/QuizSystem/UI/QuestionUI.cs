using System.Collections.Generic;
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

        [Header("Hover Effects")]
        [Tooltip("Hover configuration for answer point elements. Assign a PointHoverConfig asset to enable hover feedback.")]
        [SerializeField] protected PointHoverConfig pointHoverConfig;

        [Header("Answer Feedback")]
        [Tooltip("Visual/audio feedback for correct and wrong answers. Assign an AnswerFeedbackConfig asset to enable.")]
        [SerializeField] protected AnswerFeedbackConfig answerFeedbackConfig;

        protected QuestionData currentQuestion;
        protected IQuestionValidator validator;
        protected QuizManager quizManager;
        protected RectTransform hintPanelRectTransform;
        protected CanvasGroup hintPanelCanvasGroup;

        // Used for UI lock/unlock during answer feedback
        private CanvasGroup _uiLockCanvasGroup;
        private bool _isLocked = false;
        // Action deferred until after feedback chain completes (e.g. quizManager.OnQuestionAnswered)
        private System.Action _pendingOnAnswered;

        // Hover effect tracking
        private AudioSource _hoverAudioSource;
        private List<PointHoverEffect> _registeredHoverEffects = new List<PointHoverEffect>();

        // Answer feedback audio (shared, separate from hover)
        private AudioSource _feedbackAudioSource;

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

            // Subscribe to lock/unlock events so this UI can be controlled by nodes
            QuizState.OnUIUnlockRequested += UnlockUI;
            QuizState.OnUILockRequested += LockUI;
        }

        protected virtual void OnDestroy()
        {
            QuizState.OnUIUnlockRequested -= UnlockUI;
            QuizState.OnUILockRequested -= LockUI;

            if (_hoverAudioSource != null)
                Destroy(_hoverAudioSource.gameObject);
            if (_feedbackAudioSource != null)
                Destroy(_feedbackAudioSource.gameObject);
        }

        protected abstract void SetupQuestion();
        public abstract void OnAnswerSubmitted();

        #region Hover Effect Helpers

        protected PointHoverEffect RegisterHoverEffect(GameObject answerElement)
        {
            if (answerElement == null || pointHoverConfig == null) return null;

            var effect = answerElement.GetComponent<PointHoverEffect>();
            if (effect == null)
                effect = answerElement.AddComponent<PointHoverEffect>();

            effect.SetConfig(pointHoverConfig);
            effect.SetSharedAudioSource(GetOrCreateHoverAudioSource());
            _registeredHoverEffects.Add(effect);
            return effect;
        }

        protected void ForceExitAllHoverEffects()
        {
            foreach (var effect in _registeredHoverEffects)
            {
                if (effect != null) effect.ForceExitHover();
            }
        }

        protected void RecaptureAllHoverIdleStates()
        {
            foreach (var effect in _registeredHoverEffects)
            {
                if (effect != null) effect.RecaptureIdleState();
            }
        }

        protected void ClearRegisteredHoverEffects()
        {
            _registeredHoverEffects.Clear();
        }

        private AudioSource GetOrCreateHoverAudioSource()
        {
            if (_hoverAudioSource != null) return _hoverAudioSource;

            var sfxObj = new GameObject("HoverSFX");
            sfxObj.transform.SetParent(transform, false);
            _hoverAudioSource = sfxObj.AddComponent<AudioSource>();
            _hoverAudioSource.playOnAwake = false;
            _hoverAudioSource.loop = false;
            return _hoverAudioSource;
        }

        #endregion

        #region Answer Feedback Helpers

        /// <summary>
        /// Applies correct/wrong visual feedback directly to a button's Image.
        /// Uses Image.color (not Button.colors) so it's visible even when CanvasGroup is disabled.
        /// </summary>
        protected void ApplyAnswerFeedback(Button button, bool isCorrect)
        {
            if (button == null || answerFeedbackConfig == null) return;

            var image = button.GetComponent<Image>();
            if (image == null) return;

            Color targetColor = isCorrect ? answerFeedbackConfig.correctColor : answerFeedbackConfig.wrongColor;
            Sprite targetSprite = isCorrect ? answerFeedbackConfig.correctSprite : answerFeedbackConfig.wrongSprite;

            // Sprite swap (instant)
            if (targetSprite != null)
                image.sprite = targetSprite;

            // Color transition
            if (answerFeedbackConfig.colorTransitionDuration > 0f)
            {
                image.DOKill();
                image.DOColor(targetColor, answerFeedbackConfig.colorTransitionDuration)
                    .SetEase(answerFeedbackConfig.transitionEase)
                    .SetTarget(image);
            }
            else
            {
                image.color = targetColor;
            }

            // Play SFX
            PlayFeedbackSFX(isCorrect);
        }

        /// <summary>
        /// Plays the correct or wrong SFX from the AnswerFeedbackConfig.
        /// </summary>
        protected void PlayFeedbackSFX(bool isCorrect)
        {
            if (answerFeedbackConfig == null) return;

            AudioClip clip = isCorrect ? answerFeedbackConfig.correctSFX : answerFeedbackConfig.wrongSFX;
            if (clip == null) return;

            AudioSource source = GetOrCreateFeedbackAudioSource();
            source.Stop();
            source.clip = clip;
            source.volume = answerFeedbackConfig.sfxVolume;
            source.Play();
        }

        private AudioSource GetOrCreateFeedbackAudioSource()
        {
            if (_feedbackAudioSource != null) return _feedbackAudioSource;

            var sfxObj = new GameObject("FeedbackSFX");
            sfxObj.transform.SetParent(transform, false);
            _feedbackAudioSource = sfxObj.AddComponent<AudioSource>();
            _feedbackAudioSource.playOnAwake = false;
            _feedbackAudioSource.loop = false;
            return _feedbackAudioSource;
        }

        #endregion

        /// <summary>
        /// The single correct way for any QuestionUI subclass to complete the question.
        /// Locks the UI, fires the appropriate feedback event (correct or wrong),
        /// and defers quizManager.OnQuestionAnswered until after the feedback chain finishes.
        /// Use this instead of calling quizManager.OnQuestionAnswered directly.
        /// </summary>
        protected void FinalizeQuestion(bool wasCorrect, int points)
        {
            // Lock UI immediately
            LockUI();

            // Store the deferred completion call
            _pendingOnAnswered = () => quizManager?.OnQuestionAnswered(wasCorrect, points, currentQuestion);

            // Animate
            if (enableFeedbackAnimations)
            {
                if (wasCorrect) AnimateCorrectAnswer();
                else AnimateWrongAnswer();
            }

            // Fire feedback event — auto-unlock if nothing is connected
            bool hasFeedbackListeners = QuizState.Instance != null &&
                (wasCorrect
                    ? QuizState.Instance.NotifyCorrectAnswerFeedback()
                    : QuizState.Instance.NotifyWrongAnswerFeedback());

            if (!hasFeedbackListeners)
            {
                UnlockUI(); // also fires _pendingOnAnswered
            }
        }

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

            // Lock UI immediately so user cannot re-submit during feedback
            LockUI();

            // Animate correct answer feedback
            if (enableFeedbackAnimations)
            {
                AnimateCorrectAnswer();
            }

            // Defer OnQuestionAnswered until after all feedback nodes finish
            _pendingOnAnswered = () => quizManager?.OnQuestionAnswered(true, currentQuestion.points, currentQuestion);

            // Notify quiz system — if no feedback nodes are wired, unlock (and fire pending) right away
            bool hasFeedbackListeners = QuizState.Instance != null &&
                QuizState.Instance.NotifyCorrectAnswerFeedback();
            if (!hasFeedbackListeners)
            {
                UnlockUI();
            }
        }

        protected virtual void OnWrongAnswer()
        {
            Debug.Log("Wrong answer. Try again.");

            // Lock UI so user cannot spam answers during feedback
            LockUI();

            // Animate wrong answer feedback
            if (enableFeedbackAnimations)
            {
                AnimateWrongAnswer();
            }

            // Notify QuizState of wrong attempt (for VFX/sounds via node system)
            // This does NOT complete the question - user can still try again after feedback
            QuizState.Instance?.NotifyWrongAttempt();

            // Fire feedback nodes — if nothing is wired, unlock immediately
            bool hasFeedbackListeners = QuizState.Instance != null &&
                QuizState.Instance.NotifyWrongAnswerFeedback();
            if (!hasFeedbackListeners)
            {
                UnlockUI();
            }
        }

        /// <summary>
        /// Locks all interaction on this question UI.
        /// Called automatically when an answer is submitted.
        /// </summary>
        public virtual void LockUI()
        {
            if (_isLocked) return;
            _isLocked = true;

            ForceExitAllHoverEffects();

            // Lazily acquire or create a CanvasGroup on this object for blocking input
            if (_uiLockCanvasGroup == null)
                _uiLockCanvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();

            _uiLockCanvasGroup.interactable = false;
            _uiLockCanvasGroup.blocksRaycasts = false;
            Debug.Log("[QuestionUI] UI locked");
        }

        /// <summary>
        /// Unlocks interaction on this question UI.
        /// Called by QuizState.OnUIUnlockRequested (from UnlockQuizUINode or auto-unlock).
        /// </summary>
        public virtual void UnlockUI()
        {
            if (!_isLocked) return;
            _isLocked = false;

            if (_uiLockCanvasGroup != null)
            {
                _uiLockCanvasGroup.interactable = true;
                _uiLockCanvasGroup.blocksRaycasts = true;
            }
            Debug.Log("[QuestionUI] UI unlocked");

            // Fire any deferred action (e.g. quizManager.OnQuestionAnswered) now that feedback is done
            var pending = _pendingOnAnswered;
            _pendingOnAnswered = null;
            pending?.Invoke();
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

            // Lock UI so user cannot interact while feedback plays
            LockUI();

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

            // Defer OnQuestionAnswered until after feedback chain finishes
            _pendingOnAnswered = () => quizManager?.OnQuestionAnswered(false, 0, currentQuestion);

            // Notify quiz system — reuse wrong feedback event (all attempts exhausted = wrong outcome)
            bool hasFeedbackListeners = QuizState.Instance != null &&
                QuizState.Instance.NotifyWrongAnswerFeedback();
            if (!hasFeedbackListeners)
            {
                UnlockUI();
            }
        }

        protected abstract string GetCorrectAnswerDisplay();
    }
}
