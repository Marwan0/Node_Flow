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

        [Header("Audio")]
        [Tooltip("When enabled, hover and feedback sounds can overlap. When disabled, each new sound stops the previous one.")]
        [SerializeField] protected bool allowAudioOverlap = false;

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
        // When true, UnlockUI fires auto-correct feedback instead of completing the question
        private bool _pendingAutoCorrectFeedback = false;

        // Hover effect tracking
        private List<PointHoverEffect> _registeredHoverEffects = new List<PointHoverEffect>();

        // Tracks original button state so we can restore after feedback
        private struct FeedbackSnapshot
        {
            public Image image;
            public Sprite originalSprite;
            public Color originalColor;
        }
        private List<FeedbackSnapshot> _feedbackSnapshots = new List<FeedbackSnapshot>();

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

            // Reset per-question attempt counter for node graph branching
            if (QuizState.Instance != null)
                QuizState.Instance.currentQuestionAttempt = 0;

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

            // Sync audio overlap setting to the centralized QuizState audio source
            QuizState.Instance.allowAudioOverlap = allowAudioOverlap;

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
        }

        protected abstract void SetupQuestion();
        public abstract void OnAnswerSubmitted();

        #region Hover Effect Helpers

        protected PointHoverEffect RegisterHoverEffect(GameObject answerElement)
        {
            if (answerElement == null) return null;

            var effect = answerElement.GetComponent<PointHoverEffect>();

            // Only add a NEW component if we have a config to give it
            if (effect == null)
            {
                if (pointHoverConfig == null) return null;
                effect = answerElement.AddComponent<PointHoverEffect>();
            }

            // Always configure existing effects with shared audio + overlap setting
            if (pointHoverConfig != null)
                effect.SetConfig(pointHoverConfig);
            effect.SetSharedAudioSource(QuizState.Instance.QuizAudioSource);
            effect.SetAudioOverlapAllowed(allowAudioOverlap);
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

        /// <summary>
        /// Enables or disables hover interactions on all registered effects.
        /// When active, hover enter/exit is blocked so answer feedback visuals aren't overridden.
        /// </summary>
        protected void SetHoverFeedbackActive(bool active)
        {
            foreach (var effect in _registeredHoverEffects)
            {
                if (effect != null) effect.SetFeedbackActive(active);
            }
        }

        #endregion

        #region Answer Feedback Helpers

        /// <summary>
        /// Applies correct/wrong visual feedback directly to a button's Image.
        /// Uses Image.color (not Button.colors) so it's visible even when CanvasGroup is disabled.
        /// Also disables hover effects so they don't fight with feedback visuals.
        /// </summary>
        protected void ApplyAnswerFeedback(Button button, bool isCorrect)
        {
            if (button == null || answerFeedbackConfig == null) return;

            // Disable hover effects so they don't override feedback visuals
            ForceExitAllHoverEffects();
            SetHoverFeedbackActive(true);

            var image = button.GetComponent<Image>();
            if (image == null) return;

            // Save original state so we can restore after feedback completes
            _feedbackSnapshots.Add(new FeedbackSnapshot
            {
                image = image,
                originalSprite = image.sprite,
                originalColor = image.color
            });

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
        /// Restores all buttons that had feedback applied back to their original sprite and color.
        /// Called automatically from UnlockUI when the feedback chain finishes.
        /// </summary>
        protected void RestoreAnswerFeedback()
        {
            if (_feedbackSnapshots.Count == 0) return;

            float duration = answerFeedbackConfig != null ? answerFeedbackConfig.colorTransitionDuration : 0f;

            foreach (var snapshot in _feedbackSnapshots)
            {
                if (snapshot.image == null) continue;

                // Restore sprite
                snapshot.image.sprite = snapshot.originalSprite;

                // Restore color (with transition if configured)
                if (duration > 0f)
                {
                    snapshot.image.DOKill();
                    snapshot.image.DOColor(snapshot.originalColor, duration)
                        .SetEase(Ease.OutQuad)
                        .SetTarget(snapshot.image);
                }
                else
                {
                    snapshot.image.color = snapshot.originalColor;
                }
            }
            _feedbackSnapshots.Clear();

            // Re-enable hover effects now that feedback visuals are restored
            SetHoverFeedbackActive(false);
        }

        /// <summary>
        /// Plays the correct or wrong SFX from the AnswerFeedbackConfig using the centralized quiz audio source.
        /// </summary>
        protected void PlayFeedbackSFX(bool isCorrect)
        {
            if (answerFeedbackConfig == null) return;

            AudioClip clip = isCorrect ? answerFeedbackConfig.correctSFX : answerFeedbackConfig.wrongSFX;
            if (clip == null) return;

            QuizState.Instance.PlaySound(clip, answerFeedbackConfig.sfxVolume);
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

            // Fire feedback event — wait for chain to finish before unlocking
            bool hasFeedbackListeners = QuizState.Instance != null &&
                (wasCorrect
                    ? QuizState.Instance.NotifyCorrectAnswerFeedback()
                    : QuizState.Instance.NotifyWrongAnswerFeedback());

            if (!hasFeedbackListeners)
            {
                // No feedback chain wired — still delay so the user sees the feedback visuals
                DOVirtual.DelayedCall(feedbackDuration, UnlockUI).SetTarget(gameObject);
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

            // Notify quiz system — if no feedback nodes are wired, delay unlock so feedback is visible
            bool hasFeedbackListeners = QuizState.Instance != null &&
                QuizState.Instance.NotifyCorrectAnswerFeedback();
            if (!hasFeedbackListeners)
            {
                DOVirtual.DelayedCall(feedbackDuration, UnlockUI).SetTarget(gameObject);
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

            // Fire feedback nodes — if nothing is wired, delay unlock so feedback is visible
            bool hasFeedbackListeners = QuizState.Instance != null &&
                QuizState.Instance.NotifyWrongAnswerFeedback();
            if (!hasFeedbackListeners)
            {
                DOVirtual.DelayedCall(feedbackDuration, UnlockUI).SetTarget(gameObject);
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

            bool isCompletion = _pendingOnAnswered != null;

            // --- Auto-correct phase transition ---
            // Wrong feedback just finished. Now show correct-answer visuals and run auto-correct chain.
            if (_pendingAutoCorrectFeedback)
            {
                _pendingAutoCorrectFeedback = false;

                // Restore wrong-answer visuals before applying correct-answer visuals
                RestoreAnswerFeedback();

                // Apply correct-answer highlight (subclass fills in the specifics)
                ApplyAutoCorrectVisuals();

                Debug.Log("[QuestionUI] Wrong feedback done — starting auto-correct feedback phase");

                // Fire auto-correct feedback chain (stays locked)
                bool hasListeners = QuizState.Instance != null &&
                    QuizState.Instance.NotifyAutoCorrectFeedback();
                if (!hasListeners)
                {
                    // No auto-correct chain wired — delay then unlock
                    DOVirtual.DelayedCall(feedbackDuration, UnlockUI).SetTarget(gameObject);
                }
                return; // Don't fire _pendingOnAnswered yet — wait for auto-correct chain to finish
            }

            // --- Retry vs Completion ---
            // On retry (wrong answer → try again): restore visuals and fully unlock.
            // On completion (correct / auto-correct): keep visuals and UI locked.
            if (!isCompletion)
            {
                RestoreAnswerFeedback();
                _isLocked = false;

                if (_uiLockCanvasGroup != null)
                {
                    _uiLockCanvasGroup.interactable = true;
                    _uiLockCanvasGroup.blocksRaycasts = true;
                }
                Debug.Log("[QuestionUI] UI unlocked (retry)");
            }
            else
            {
                Debug.Log("[QuestionUI] Feedback done — UI stays locked until next question loads");
            }

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

            // Defer OnQuestionAnswered until after ALL feedback chains finish
            _pendingOnAnswered = () => quizManager?.OnQuestionAnswered(false, 0, currentQuestion);

            // Flag: when wrong feedback finishes, fire auto-correct feedback before completing
            _pendingAutoCorrectFeedback = true;

            // Count this as a wrong attempt so AttemptCountAbove branches work correctly
            // (NotifyWrongAttempt is NOT called here to avoid firing the on_wrong port)
            if (QuizState.Instance != null)
                QuizState.Instance.currentQuestionAttempt++;

            // Animate wrong answer first
            if (enableFeedbackAnimations)
                AnimateWrongAnswer();

            // Fire wrong feedback chain first — when it finishes, UnlockUI will
            // detect _pendingAutoCorrectFeedback and start the auto-correct phase
            bool hasFeedbackListeners = QuizState.Instance != null &&
                QuizState.Instance.NotifyWrongAnswerFeedback();
            if (!hasFeedbackListeners)
            {
                DOVirtual.DelayedCall(feedbackDuration, UnlockUI).SetTarget(gameObject);
            }
        }

        /// <summary>
        /// Called by UnlockUI during the auto-correct phase (after wrong feedback finishes).
        /// Override in subclasses to apply correct-answer visuals (e.g. highlight the correct button).
        /// </summary>
        protected virtual void ApplyAutoCorrectVisuals() { }

        protected abstract string GetCorrectAnswerDisplay();
    }
}
