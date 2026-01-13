namespace QuizSystem
{
    public class TrueFalseValidator : QuestionValidator
    {
        private TrueFalseQuestionData trueFalseData;

        public TrueFalseValidator(QuestionData data) : base(data)
        {
            trueFalseData = data as TrueFalseQuestionData;
        }

        public override ValidationResult ValidateAnswer(object answer)
        {
            if (answer is bool userAnswer)
            {
                if (userAnswer == trueFalseData.correctAnswer)
                {
                    return new ValidationResult(true, "Correct!");
                }
                else
                {
                    return HandleWrongAnswer();
                }
            }

            return new ValidationResult(false, "Invalid answer format.");
        }
    }
}

