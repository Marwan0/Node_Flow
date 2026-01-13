using UnityEngine;

namespace QuizSystem
{
    public abstract class QuestionValidator : IQuestionValidator
    {
        protected QuestionData questionData;
        protected int currentAttempt;

        public QuestionValidator(QuestionData data)
        {
            questionData = data;
            currentAttempt = 0;
        }

        public abstract ValidationResult ValidateAnswer(object answer);

        public string GetHint(int attemptNumber)
        {
            if (questionData.hints == null || questionData.hints.Length == 0)
                return "";

            int hintIndex = attemptNumber - 1;
            if (hintIndex >= 0 && hintIndex < questionData.hints.Length)
            {
                return questionData.hints[hintIndex];
            }

            return "";
        }

        public bool HasReachedMaxAttempts()
        {
            return currentAttempt >= questionData.maxAttempts;
        }

        public int GetCurrentAttempt()
        {
            return currentAttempt;
        }

        public virtual void Reset()
        {
            currentAttempt = 0;
        }

        protected ValidationResult HandleWrongAnswer()
        {
            currentAttempt++;

            if (HasReachedMaxAttempts())
            {
                return new ValidationResult(
                    false,
                    $"Maximum attempts reached. Correct answer will be shown.",
                    true
                );
            }

            string hint = GetHint(currentAttempt);
            return new ValidationResult(
                false,
                string.IsNullOrEmpty(hint) ? "Incorrect. Try again." : hint
            );
        }
    }
}

