using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Sirenix.OdinInspector;
using DG.Tweening;

namespace QuizSystem
{
    public class MultipleChoiceUI : QuestionUI
    {
        [Header("Multiple Choice UI")]
        [InfoBox("Answers auto-submit immediately when clicked - no submit button needed!", InfoMessageType.Info)]
        [SerializeField] private Button[] answerButtons = new Button[4];
        [SerializeField] private TextMeshProUGUI[] answerTexts = new TextMeshProUGUI[4];

        [BoxGroup("Button Animations")]
        [ShowIf("enableFeedbackAnimations")]
        [Tooltip("Enable staggered button entrance animation")]
        public bool enableButtonEntrance = true;

        [BoxGroup("Button Animations")]
        [ShowIf("enableButtonEntrance")]
        [Range(0.05f, 0.3f)]
        [Tooltip("Delay between each button appearance")]
        public float buttonStaggerDelay = 0.1f;

        [BoxGroup("Button Animations")]
        [ShowIf("enableButtonEntrance")]
        [Range(0.1f, 0.5f)]
        [Tooltip("Duration of button entrance animation")]
        public float buttonEntranceDuration = 0.3f;
        
        // Note: submitButton field is inherited from QuestionUI base class but not used here
        // It's needed for other question types (FillInTheBlank, DragDrop, etc.) but Multiple Choice auto-submits on click

        private MultipleChoiceQuestionData mcData;
        private int selectedAnswerIndex = -1;
        private bool answerSubmitted = false;

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
            for (int i = 0; i < answerButtons.Length && i < mcData.answers.Length; i++)
            {
                if (answerButtons[i] != null)
                {
                    int index = i; // Capture for closure
                    answerButtons[i].onClick.RemoveAllListeners();
                    answerButtons[i].onClick.AddListener(() => OnAnswerButtonClicked(index));
                    answerButtons[i].interactable = true;
                }

                if (answerTexts[i] != null && i < mcData.answers.Length)
                {
                    answerTexts[i].text = mcData.answers[i];
                }
            }

            selectedAnswerIndex = -1;
            answerSubmitted = false;
            UpdateButtonVisuals();

            // Animate button entrance
            if (enableFeedbackAnimations && enableButtonEntrance)
            {
                AnimateButtonEntrance();
            }
        }

        private void AnimateButtonEntrance()
        {
            for (int i = 0; i < answerButtons.Length; i++)
            {
                if (answerButtons[i] != null && answerButtons[i].transform != null)
                {
                    Vector3 originalScale = answerButtons[i].transform.localScale;
                    answerButtons[i].transform.localScale = Vector3.zero;

                    answerButtons[i].transform.DOScale(originalScale, buttonEntranceDuration)
                        .SetDelay(i * buttonStaggerDelay)
                        .SetEase(Ease.OutBack);
                }
            }
        }

        private void OnAnswerButtonClicked(int index)
        {
            if (answerSubmitted) return;

            // Update selection (user can click different answers to change selection)
            selectedAnswerIndex = index;
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
                    // Visual feedback for selected button
                    var colors = answerButtons[i].colors;
                    if (i == selectedAnswerIndex)
                    {
                        colors.normalColor = new Color(0.6f, 0.8f, 1f); // Light blue for selected
                        colors.selectedColor = new Color(0.5f, 0.7f, 1f);
                    }
                    else
                    {
                        colors.normalColor = Color.white;
                        colors.selectedColor = Color.white;
                    }
                    answerButtons[i].colors = colors;
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
            base.OnCorrectAnswer();
            
            // Highlight correct answer in green
            if (selectedAnswerIndex >= 0 && selectedAnswerIndex < answerButtons.Length && answerButtons[selectedAnswerIndex] != null)
            {
                var colors = answerButtons[selectedAnswerIndex].colors;
                colors.normalColor = Color.green;
                answerButtons[selectedAnswerIndex].colors = colors;

                // Animate correct answer button
                if (enableFeedbackAnimations)
                {
                    AnimateCorrectButton(answerButtons[selectedAnswerIndex].transform);
                }
            }
            
            // Hide submit button if it exists (not needed for multiple choice)
            if (submitButton != null)
            {
                submitButton.gameObject.SetActive(false);
            }
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
            base.OnWrongAnswer();
            
            // Highlight wrong answer in red
            if (selectedAnswerIndex >= 0 && selectedAnswerIndex < answerButtons.Length && answerButtons[selectedAnswerIndex] != null)
            {
                var colors = answerButtons[selectedAnswerIndex].colors;
                colors.normalColor = Color.red;
                answerButtons[selectedAnswerIndex].colors = colors;

                // Animate wrong answer button
                if (enableFeedbackAnimations)
                {
                    AnimateWrongButton(answerButtons[selectedAnswerIndex].transform);
                }
            }

            // Re-enable buttons for retry
            answerSubmitted = false;
            selectedAnswerIndex = -1;
            foreach (var button in answerButtons)
            {
                if (button != null)
                    button.interactable = true;
            }
            UpdateButtonVisuals();
        }

        private void AnimateWrongButton(Transform buttonTransform)
        {
            if (buttonTransform == null) return;

            // Shake animation
            buttonTransform.DOShakePosition(feedbackDuration, 8f, 10, 90f, false, true);
        }

        protected override void OnAutoCorrect()
        {
            base.OnAutoCorrect();
            
            // Highlight correct answer in green
            if (mcData != null && mcData.correctAnswerIndex >= 0 && mcData.correctAnswerIndex < answerButtons.Length && answerButtons[mcData.correctAnswerIndex] != null)
            {
                var colors = answerButtons[mcData.correctAnswerIndex].colors;
                colors.normalColor = Color.green;
                answerButtons[mcData.correctAnswerIndex].colors = colors;

                // Animate correct answer reveal
                if (enableFeedbackAnimations)
                {
                    AnimateCorrectButton(answerButtons[mcData.correctAnswerIndex].transform);
                }
            }
            DisableAllButtons();
        }

        protected override string GetCorrectAnswerDisplay()
        {
            if (mcData != null && mcData.correctAnswerIndex >= 0 && mcData.correctAnswerIndex < mcData.answers.Length)
            {
                return mcData.answers[mcData.correctAnswerIndex];
            }
            return "";
        }
    }
}

