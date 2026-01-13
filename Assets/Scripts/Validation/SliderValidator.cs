namespace QuizSystem
{
    public class SliderValidator : QuestionValidator
    {
        private SliderQuestionData sliderData;

        public SliderValidator(QuestionData data) : base(data)
        {
            sliderData = data as SliderQuestionData;
        }

        public override ValidationResult ValidateAnswer(object answer)
        {
            if (answer is float userValue)
            {
                if (sliderData.IsValueCorrect(userValue))
                {
                    return new ValidationResult(true, "Correct value!");
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

