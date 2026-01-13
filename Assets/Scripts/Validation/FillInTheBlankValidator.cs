namespace QuizSystem
{
    public class FillInTheBlankValidator : QuestionValidator
    {
        private FillInTheBlankQuestionData fillBlankData;

        public FillInTheBlankValidator(QuestionData data) : base(data)
        {
            fillBlankData = data as FillInTheBlankQuestionData;
        }

        public override ValidationResult ValidateAnswer(object answer)
        {
            if (answer is string userAnswer)
            {
                if (fillBlankData.IsAnswerCorrect(userAnswer))
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

