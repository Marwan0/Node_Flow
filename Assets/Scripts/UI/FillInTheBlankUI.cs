using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizSystem
{
    public class FillInTheBlankUI : QuestionUI
    {
        [Header("Fill in the Blank UI")]
        [SerializeField] private TMP_InputField answerInput;
        [SerializeField] private Button submitButton;

        protected override void SetupQuestion()
        {
            if (answerInput != null)
            {
                answerInput.text = "";
                answerInput.onSubmit.RemoveAllListeners();
                answerInput.onSubmit.AddListener(OnInputSubmitted);
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnInputSubmitted);
                submitButton.interactable = true;
            }
        }

        private void OnInputSubmitted(string answer)
        {
            OnInputSubmitted();
        }

        private void OnInputSubmitted()
        {
            if (answerInput == null || string.IsNullOrWhiteSpace(answerInput.text))
            {
                ShowHint("Please enter an answer.");
                return;
            }

            var result = validator.ValidateAnswer(answerInput.text.Trim());
            HandleValidationResult(result);

            if (result.IsCorrect || result.ShouldAutoCorrect)
            {
                if (answerInput != null)
                    answerInput.interactable = false;
                if (submitButton != null)
                    submitButton.interactable = false;
            }
        }

        public override void OnAnswerSubmitted()
        {
            OnInputSubmitted();
        }

        protected override string GetCorrectAnswerDisplay()
        {
            if (currentQuestion is FillInTheBlankQuestionData fillData)
            {
                return fillData.correctAnswer;
            }
            return "";
        }
    }
}

