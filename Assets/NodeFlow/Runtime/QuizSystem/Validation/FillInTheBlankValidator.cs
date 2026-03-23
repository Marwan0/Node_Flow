using System.Collections.Generic;

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
            if (fillBlankData == null)
                return new ValidationResult(false, "Invalid question data.");

            int n = fillBlankData.GetBlankSlotCount();

            if (answer is string userAnswer)
            {
                if (n != 1)
                    return new ValidationResult(false, "Invalid answer format.");

                if (fillBlankData.IsAnswerCorrect(userAnswer))
                    return new ValidationResult(true, "Correct!");
                return HandleWrongAnswer();
            }

            if (answer is string[] arr)
            {
                if (fillBlankData.AreAllAnswersCorrect(arr))
                    return new ValidationResult(true, "Correct!");
                return HandleWrongAnswer();
            }

            if (answer is List<string> list)
            {
                if (fillBlankData.AreAllAnswersCorrect(list))
                    return new ValidationResult(true, "Correct!");
                return HandleWrongAnswer();
            }

            return new ValidationResult(false, "Invalid answer format.");
        }
    }
}
