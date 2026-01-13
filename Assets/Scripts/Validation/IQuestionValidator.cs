namespace QuizSystem
{
    public interface IQuestionValidator
    {
        ValidationResult ValidateAnswer(object answer);
        string GetHint(int attemptNumber);
        bool HasReachedMaxAttempts();
        int GetCurrentAttempt();
        void Reset();
    }

    public class ValidationResult
    {
        public bool IsCorrect { get; set; }
        public string Message { get; set; }
        public bool ShouldAutoCorrect { get; set; }

        public ValidationResult(bool isCorrect, string message = "", bool shouldAutoCorrect = false)
        {
            IsCorrect = isCorrect;
            Message = message;
            ShouldAutoCorrect = shouldAutoCorrect;
        }
    }
}

