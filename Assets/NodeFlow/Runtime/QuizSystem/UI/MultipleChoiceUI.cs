using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace QuizSystem
{
    public class MultipleChoiceUI : QuestionUI
    {
        [Header("Multiple Choice UI")]
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private TextMeshProUGUI[] answerTexts = new TextMeshProUGUI[4];

        [Header("Button Animations")]
        [Tooltip("Enable staggered button entrance animation")]
        public bool enableButtonEntrance = true;

        [Range(0.05f, 0.3f)]
        [Tooltip("Delay between each button appearance")]
        public float buttonStaggerDelay = 0.1f;

        [Range(0.1f, 0.5f)]
        [Tooltip("Duration of button entrance animation")]
        public float buttonEntranceDuration = 0.3f;
        
        // Note: submitButton field is inherited from QuestionUI base class but not used here
        // It's needed for other question types (FillInTheBlank, DragDrop, etc.) but Multiple Choice auto-submits on click

        private MultipleChoiceQuestionData mcData;
        private int selectedAnswerIndex = -1;
        private bool answerSubmitted = false;
        private Sprite[] originalButtonSprites;
        private bool _needsRetryReset = false;

        protected override void SetupQuestion()
        {
            mcData = currentQuestion as MultipleChoiceQuestionData;
            if (mcData == null)
            {
                Debug.LogError("Question is not a MultipleChoiceQuestionData!");
                return;
            }

            // Hide submit button - not needed for multiple choice (auto-submits on click)
            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(false);
            }

            // Setup answer buttons
            for (int i = 0; i < answerButtons.Length && i < mcData.answerCount; i++)
            {
                if (answerButtons[i] != null)
                {
                    int index = i; // Capture for closure
                    answerButtons[i].onClick.RemoveAllListeners();
                    answerButtons[i].onClick.AddListener(() => OnAnswerButtonClicked(index));
                    answerButtons[i].interactable = true;
                }

                if (answerTexts[i] != null && i < mcData.answerCount)
                {
                    answerTexts[i].text = mcData.GetAnswer(i);
                }
            }

            // Capture original sprites for restoration on retry
            originalButtonSprites = new Sprite[answerButtons.Length];
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    var img = answerButtons[i].GetComponent<Image>();
                    if (img != null) originalButtonSprites[i] = img.sprite;
                }
            }

            selectedAnswerIndex = -1;
            answerSubmitted = false;
            UpdateButtonVisuals();

            // Register hover effects
            ClearRegisteredHoverEffects();
            for (int i = 0; i < answerButtons.Length && i < mcData.answerCount; i++)
            {
                if (answerButtons[i] != null)
                    RegisterHoverEffect(answerButtons[i].gameObject);
            }

            // Animate button entrance - ALWAYS check for custom animations first
            // Custom animations from LoadQuestionNode take priority over inspector settings
            AnimateButtonEntrance();
        }

        private void AnimateButtonEntrance()
        {
            // CRITICAL: Kill all existing tweens on all buttons first to prevent conflicts
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null && answerButtons[i].transform != null)
                {
                    answerButtons[i].transform.DOKill();
                    var cg = answerButtons[i].transform.GetComponent<CanvasGroup>();
                    if (cg != null) cg.DOKill();
                }
            }

            // Check if custom animations are set from LoadQuestionNode
            var customAnimations = QuizState.Instance?.currentAnswerAnimations;
            bool hasCustomAnimations = customAnimations != null && customAnimations.Length > 0;

            // Determine how many answers we actually have (from question data)
            int actualAnswerCount = mcData != null ? mcData.answerCount : answerButtons.Length;
            int maxAnswers = Mathf.Min(actualAnswerCount, answerButtons.Length);

            for (int i = 0; i < maxAnswers; i++)
            {
                if (answerButtons[i] == null || answerButtons[i].transform == null) continue;

                bool usedCustom = false;

                // PRIORITY 1: Try custom animation from LoadQuestionNode if available
                if (hasCustomAnimations && i < customAnimations.Length && customAnimations[i] != null)
                {
                    // Custom array exists — this overrides default prefab animations.
                    // If enabled, play it. If disabled, skip all animations for this button.
                    usedCustom = true;
                    if (customAnimations[i].enabled)
                    {
                        AnimateAnswerWithSettings(answerButtons[i].transform, customAnimations[i]);
                    }
                }

                // PRIORITY 2: Fall back to default animation ONLY if no custom animation config was provided
                if (!usedCustom && enableFeedbackAnimations && enableButtonEntrance)
                {
                    // Use default animation from inspector
                    // Kill any existing tweens first
                    answerButtons[i].transform.DOKill();
                    
                    Vector3 originalScale = answerButtons[i].transform.localScale;
                    answerButtons[i].transform.localScale = Vector3.zero;

                    answerButtons[i].transform.DOScale(originalScale, buttonEntranceDuration)
                        .SetDelay(i * buttonStaggerDelay)
                        .SetEase(Ease.OutBack);
                }
            }
        }

        private void AnimateAnswerWithSettings(Transform buttonTransform, NodeSystem.Nodes.Quiz.AnswerAnimationSettings settings)
        {
            if (buttonTransform == null || settings == null) return;

            var animType = settings.animationType;
            if (animType == NodeSystem.Nodes.Quiz.AnswerAnimationType.None) return;

            // CRITICAL: Kill any existing tweens first to prevent conflicts
            buttonTransform.DOKill();
            var canvasGroup = buttonTransform.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
            }

            // Capture original values BEFORE any reset
            Vector3 originalPos = buttonTransform.localPosition;
            Vector3 originalScale = buttonTransform.localScale;
            Vector3 originalRot = buttonTransform.localEulerAngles;

            // Reset based on animation type
            switch (animType)
            {
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.Scale:
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.Bounce:
                    buttonTransform.localScale = Vector3.zero;
                    break;
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.Fade:
                    // Ensure CanvasGroup exists for fade animation
                    if (canvasGroup == null) canvasGroup = buttonTransform.gameObject.AddComponent<CanvasGroup>();
                    canvasGroup.alpha = 0f;
                    // Make sure button is visible (alpha controls visibility, not active state)
                    if (!buttonTransform.gameObject.activeSelf)
                        buttonTransform.gameObject.SetActive(true);
                    break;
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.SlideFromLeft:
                    buttonTransform.localPosition = originalPos + Vector3.left * settings.slideDistance;
                    break;
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.SlideFromRight:
                    buttonTransform.localPosition = originalPos + Vector3.right * settings.slideDistance;
                    break;
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.SlideFromTop:
                    buttonTransform.localPosition = originalPos + Vector3.up * settings.slideDistance;
                    break;
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.SlideFromBottom:
                    buttonTransform.localPosition = originalPos + Vector3.down * settings.slideDistance;
                    break;
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.Rotate:
                    buttonTransform.localEulerAngles = originalRot + Vector3.forward * 180f;
                    break;
            }

            // Animate based on type
            switch (animType)
            {
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.Scale:
                    // Animate to original scale (scaleMultiplier affects the overshoot, not final size)
                    buttonTransform.DOScale(originalScale, settings.duration)
                        .SetDelay(settings.delay)
                        .SetEase(settings.easeType);
                    break;

                case NodeSystem.Nodes.Quiz.AnswerAnimationType.Bounce:
                    // For bounce, we can overshoot then settle to original
                    Sequence bounceSeq = DOTween.Sequence();
                    bounceSeq.Append(buttonTransform.DOScale(originalScale * settings.scaleMultiplier, settings.duration * 0.6f)
                        .SetEase(DG.Tweening.Ease.OutBounce));
                    bounceSeq.Append(buttonTransform.DOScale(originalScale, settings.duration * 0.4f)
                        .SetEase(DG.Tweening.Ease.InQuad));
                    bounceSeq.SetDelay(settings.delay);
                    break;

                case NodeSystem.Nodes.Quiz.AnswerAnimationType.Fade:
                    var cg = buttonTransform.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.DOFade(1f, settings.duration)
                            .SetDelay(settings.delay)
                            .SetEase(settings.easeType);
                    }
                    break;

                case NodeSystem.Nodes.Quiz.AnswerAnimationType.SlideFromLeft:
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.SlideFromRight:
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.SlideFromTop:
                case NodeSystem.Nodes.Quiz.AnswerAnimationType.SlideFromBottom:
                    buttonTransform.DOLocalMove(originalPos, settings.duration)
                        .SetDelay(settings.delay)
                        .SetEase(settings.easeType);
                    break;

                case NodeSystem.Nodes.Quiz.AnswerAnimationType.Rotate:
                    buttonTransform.DOLocalRotate(originalRot, settings.duration)
                        .SetDelay(settings.delay)
                        .SetEase(settings.easeType);
                    break;
            }
        }

        private void OnAnswerButtonClicked(int index)
        {
            if (answerSubmitted) return;

            // Update selection (user can click different answers to change selection)
            selectedAnswerIndex = index;
            lastSelectedPointIndex = index;
            UpdateButtonVisuals();

            // Auto-submit immediately on click (no submit button needed)
            SubmitAnswer();
        }

        private void UpdateButtonVisuals()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    var image = answerButtons[i].GetComponent<Image>();
                    if (image != null)
                    {
                        image.DOKill();
                        image.color = Color.white;

                        // Restore original sprite (undo any correct/wrong sprite swap)
                        if (originalButtonSprites != null && i < originalButtonSprites.Length && originalButtonSprites[i] != null)
                        {
                            image.sprite = originalButtonSprites[i];
                        }
                    }
                }
            }
        }

        private void SubmitAnswer()
        {
            if (selectedAnswerIndex < 0) return;

            answerSubmitted = true;
            DisableAllButtons();

            var result = validator.ValidateAnswer(selectedAnswerIndex);
            HandleValidationResult(result);
        }

        private void DisableAllButtons()
        {
            foreach (var button in answerButtons)
            {
                if (button != null)
                    button.interactable = false;
            }
        }

        public override void OnAnswerSubmitted()
        {
            SubmitAnswer();
        }

        protected override void OnCorrectAnswer()
        {
            // Apply visual feedback BEFORE base — base may unlock and advance the quiz immediately
            if (selectedAnswerIndex >= 0 && selectedAnswerIndex < answerButtons.Length && answerButtons[selectedAnswerIndex] != null)
            {
                ApplyAnswerFeedback(answerButtons[selectedAnswerIndex], true);

                if (enableFeedbackAnimations)
                {
                    AnimateCorrectButton(answerButtons[selectedAnswerIndex].transform);
                }
            }

            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(false);
            }

            base.OnCorrectAnswer();
        }

        private void AnimateCorrectButton(Transform buttonTransform)
        {
            if (buttonTransform == null) return;

            Vector3 originalScale = buttonTransform.localScale;
            Sequence sequence = DOTween.Sequence();
            
            // Scale bounce
            sequence.Append(buttonTransform.DOScale(originalScale * 1.15f, feedbackDuration * 0.3f).SetEase(Ease.OutQuad));
            sequence.Append(buttonTransform.DOScale(originalScale, feedbackDuration * 0.7f).SetEase(Ease.InQuad));
        }

        protected override void OnWrongAnswer()
        {
            // Capture index before base potentially changes state
            int wrongIndex = selectedAnswerIndex;

            // Apply visual feedback BEFORE base — base may unlock immediately if no feedback listeners
            if (wrongIndex >= 0 && wrongIndex < answerButtons.Length && answerButtons[wrongIndex] != null)
            {
                ApplyAnswerFeedback(answerButtons[wrongIndex], false);

                if (enableFeedbackAnimations)
                {
                    AnimateWrongButton(answerButtons[wrongIndex].transform);
                }
            }

            // Mark that we need retry reset when UI unlocks (not on a fixed timer)
            _needsRetryReset = true;

            base.OnWrongAnswer();
        }

        /// <summary>
        /// Override UnlockUI to reset retry state when the feedback chain finishes.
        /// This ensures button sprites/colors stay in their feedback state until the
        /// full feedback chain (including signal-linked delays) has completed.
        /// </summary>
        public override void UnlockUI()
        {
            // base.UnlockUI() calls RestoreAnswerFeedback() which restores sprites/colors
            base.UnlockUI();

            if (_needsRetryReset)
            {
                _needsRetryReset = false;
                answerSubmitted = false;
                selectedAnswerIndex = -1;
                foreach (var button in answerButtons)
                {
                    if (button != null)
                        button.interactable = true;
                }
                RecaptureAllHoverIdleStates();
            }
        }

        private void AnimateWrongButton(Transform buttonTransform)
        {
            if (buttonTransform == null) return;

            // Shake animation
            buttonTransform.DOShakePosition(feedbackDuration, 8f, 10, 90f, false, true);
        }

        protected override void OnAutoCorrect()
        {
            // Apply WRONG feedback on the selected answer first (base will fire wrong feedback chain)
            int wrongIndex = selectedAnswerIndex;
            if (wrongIndex >= 0 && wrongIndex < answerButtons.Length && answerButtons[wrongIndex] != null)
            {
                ApplyAnswerFeedback(answerButtons[wrongIndex], false);

                if (enableFeedbackAnimations)
                    AnimateWrongButton(answerButtons[wrongIndex].transform);
            }
            DisableAllButtons();

            // base.OnAutoCorrect() fires wrong feedback chain first, then auto-correct phase via UnlockUI
            base.OnAutoCorrect();
        }

        /// <summary>
        /// Called by UnlockUI after wrong feedback finishes during auto-correct.
        /// Highlights the correct answer button with correct feedback visuals.
        /// </summary>
        protected override void ApplyAutoCorrectVisuals()
        {
            if (mcData != null && mcData.correctAnswerIndex >= 0
                && mcData.correctAnswerIndex < answerButtons.Length
                && answerButtons[mcData.correctAnswerIndex] != null)
            {
                ApplyAnswerFeedback(answerButtons[mcData.correctAnswerIndex], true);

                if (enableFeedbackAnimations)
                    AnimateCorrectButton(answerButtons[mcData.correctAnswerIndex].transform);
            }
        }

        protected override int GetCorrectAnswerPointIndex()
        {
            return mcData != null ? mcData.correctAnswerIndex : -1;
        }

        protected override string GetCorrectAnswerDisplay()
        {
            if (mcData != null && mcData.ValidateCorrectAnswer())
            {
                return mcData.correctAnswer;
            }
            return "";
        }
    }
}
