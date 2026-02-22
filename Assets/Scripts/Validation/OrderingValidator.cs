using System.Collections.Generic;
using System.Linq;

namespace QuizSystem
{
    public class OrderingValidator : QuestionValidator
    {
        private OrderingQuestionData orderingData;

        public OrderingValidator(QuestionData data) : base(data)
        {
            orderingData = data as OrderingQuestionData;
        }

        public override ValidationResult ValidateAnswer(object answer)
        {
            if (answer is List<int> userOrder)
            {
                if (orderingData == null)
                    return new ValidationResult(false, "Ordering question data is missing.");

                if (orderingData.enforceStartIndex && !orderingData.HasRequiredStart(userOrder))
                {
                    return HandleWrongAnswer($"Start must be item index {orderingData.requiredStartIndex}.");
                }

                if (orderingData.IsOrderCorrect(userOrder))
                {
                    return new ValidationResult(true, "Correct order!");
                }
                else if (orderingData.allowPartialCredit)
                {
                    float partialCredit = orderingData.GetPartialCredit(userOrder);
                    if (partialCredit > 0.5f) // More than 50% correct
                    {
                        return new ValidationResult(
                            false,
                            $"Partially correct. {System.Math.Round(partialCredit * 100)}% of items are in the correct position."
                        );
                    }
                }

                return HandleWrongAnswer();
            }

            return new ValidationResult(false, "Invalid answer format.");
        }

        public string GetExpectedOrderDebugText()
        {
            if (orderingData == null)
                return string.Empty;

            var expected = orderingData.GetExpectedOrder();
            return string.Join(" -> ", expected.Select(i => i.ToString()));
        }
    }
}

