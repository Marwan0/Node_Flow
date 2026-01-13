using System.Collections.Generic;
using System.Linq;

namespace QuizSystem
{
    public class MultiSelectValidator : QuestionValidator
    {
        private MultiSelectQuestionData multiSelectData;

        public MultiSelectValidator(QuestionData data) : base(data)
        {
            multiSelectData = data as MultiSelectQuestionData;
        }

        public override ValidationResult ValidateAnswer(object answer)
        {
            if (answer is List<int> selectedIndices)
            {
                var correctIndices = new HashSet<int>(multiSelectData.correctAnswerIndices);
                var selectedSet = new HashSet<int>(selectedIndices);

                // Check if all correct are selected and no incorrect are selected
                bool allCorrectSelected = correctIndices.IsSubsetOf(selectedSet);
                bool noIncorrectSelected = selectedSet.IsSubsetOf(correctIndices);

                if (allCorrectSelected && noIncorrectSelected)
                {
                    return new ValidationResult(true, "All correct answers selected!");
                }
                else if (allCorrectSelected && multiSelectData.allowPartialCredit)
                {
                    // Some correct selected but also some incorrect
                    int correctCount = selectedIndices.Count(i => correctIndices.Contains(i));
                    int totalCorrect = correctIndices.Count;
                    return new ValidationResult(
                        false,
                        $"Partially correct. You selected {correctCount} out of {totalCorrect} correct answers."
                    );
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

