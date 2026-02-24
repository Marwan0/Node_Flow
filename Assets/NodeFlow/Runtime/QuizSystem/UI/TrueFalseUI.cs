using UnityEngine;
using UnityEngine.UI;

namespace QuizSystem
{
    public class TrueFalseUI : QuestionUI
    {
        [Header("True/False UI")]
        [SerializeField] private Button trueButton;
        [SerializeField] private Button falseButton;

        private bool answerSelected = false;

        protected override void SetupQuestion()
        {
            if (trueButton != null)
            {
                trueButton.onClick.RemoveAllListeners();
                trueButton.onClick.AddListener(() => OnButtonClicked(true));
            }

            if (falseButton != null)
            {
                falseButton.onClick.RemoveAllListeners();
                falseButton.onClick.AddListener(() => OnButtonClicked(false));
            }

            answerSelected = false;
            EnableButtons(true);
        }

        private void OnButtonClicked(bool answer)
        {
            if (answerSelected) return;

            answerSelected = true;
            EnableButtons(false);

            var result = validator.ValidateAnswer(answer);
            HandleValidationResult(result);
        }

        private void EnableButtons(bool enabled)
        {
            if (trueButton != null) trueButton.interactable = enabled;
            if (falseButton != null) falseButton.interactable = enabled;
        }

        public override void OnAnswerSubmitted()
        {
            // True/False doesn't need a submit button - answers are submitted immediately
        }

        protected override string GetCorrectAnswerDisplay()
        {
            if (currentQuestion is TrueFalseQuestionData tfData)
            {
                return tfData.correctAnswer ? "True" : "False";
            }
            return "";
        }
    }
}

