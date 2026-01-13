using System.Collections.Generic;

namespace QuizSystem
{
    public class DragDropValidator : QuestionValidator
    {
        private DragDropQuestionData dragDropData;

        public DragDropValidator(QuestionData data) : base(data)
        {
            dragDropData = data as DragDropQuestionData;
        }

        public override ValidationResult ValidateAnswer(object answer)
        {
            if (answer is Dictionary<int, int> userPairings)
            {
                // Check if all correct pairings are present and correct
                bool allCorrect = true;
                int correctCount = 0;
                int totalPairings = dragDropData.correctPairings.Count;

                foreach (var correctPairing in dragDropData.correctPairings)
                {
                    if (userPairings.ContainsKey(correctPairing.Key))
                    {
                        if (userPairings[correctPairing.Key] == correctPairing.Value)
                        {
                            correctCount++;
                        }
                        else
                        {
                            allCorrect = false;
                        }
                    }
                    else
                    {
                        allCorrect = false;
                    }
                }

                // Check for extra incorrect pairings
                foreach (var userPairing in userPairings)
                {
                    if (!dragDropData.correctPairings.ContainsKey(userPairing.Key) ||
                        dragDropData.correctPairings[userPairing.Key] != userPairing.Value)
                    {
                        allCorrect = false;
                    }
                }

                if (allCorrect && userPairings.Count == totalPairings)
                {
                    return new ValidationResult(true, "All pairings are correct!");
                }
                else
                {
                    return HandleWrongAnswer();
                }
            }

            return new ValidationResult(false, "Invalid answer format. Expected Dictionary<int, int>.");
        }
    }
}

