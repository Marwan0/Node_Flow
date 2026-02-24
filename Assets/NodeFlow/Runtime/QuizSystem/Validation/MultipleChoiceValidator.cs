namespace QuizSystem
{
    public class MultipleChoiceValidator : QuestionValidator
    {
        private MultipleChoiceQuestionData multipleChoiceData;

        public MultipleChoiceValidator(QuestionData data) : base(data)
        {
            multipleChoiceData = data as MultipleChoiceQuestionData;
        }

        public override ValidationResult ValidateAnswer(object answer)
        {
            if (answer is int selectedIndex)
            {
                if (selectedIndex == multipleChoiceData.correctAnswerIndex)
                {
                    return new ValidationResult(true, "Correct!");
                }
                else
                {
                    return HandleWrongAnswer();
                }
            }

            return new ValidationResult(false, "Invalid answer format. Expected integer index.");
        }
    }
}

