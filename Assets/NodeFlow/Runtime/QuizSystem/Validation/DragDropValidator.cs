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
                // Find all drag items that HAVE a correct destination
                HashSet<int> requiredDragItems = new HashSet<int>();
                foreach (var pairing in dragDropData.correctPairings)
                {
                    requiredDragItems.Add(pairing.dragIndex);
                }

                bool allCorrect = true;
                
                // 1. Check if the user placed every required item
                foreach (int requiredItem in requiredDragItems)
                {
                    if (!userPairings.ContainsKey(requiredItem))
                    {
                        allCorrect = false;
                        break;
                    }
                }

                if (!allCorrect) return HandleWrongAnswer();

                // 2. Check if every pairing the user made is valid
                foreach (var userPairing in userPairings)
                {
                    bool pairingIsValid = false;
                    foreach (var correctPairing in dragDropData.correctPairings)
                    {
                        if (correctPairing.dragIndex == userPairing.Key && correctPairing.dropIndex == userPairing.Value)
                        {
                            pairingIsValid = true;
                            break;
                        }
                    }

                    if (!pairingIsValid)
                    {
                        allCorrect = false;
                        break;
                    }
                }

                if (allCorrect)
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

