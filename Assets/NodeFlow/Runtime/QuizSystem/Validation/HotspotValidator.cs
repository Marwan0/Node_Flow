using UnityEngine;

namespace QuizSystem
{
    public class HotspotValidator : QuestionValidator
    {
        private HotspotQuestionData hotspotData;

        public HotspotValidator(QuestionData data) : base(data)
        {
            hotspotData = data as HotspotQuestionData;
        }

        public override ValidationResult ValidateAnswer(object answer)
        {
            if (answer is Vector2 normalizedPoint)
            {
                if (hotspotData.allowMultipleSelections)
                {
                    // For multiple selections, check if point is in any correct hotspot
                    foreach (int correctIndex in hotspotData.correctHotspotIndices)
                    {
                        if (hotspotData.IsPointInHotspot(normalizedPoint, correctIndex))
                        {
                            return new ValidationResult(true, "Correct hotspot selected!");
                        }
                    }
                }
                else
                {
                    if (hotspotData.IsPointInHotspot(normalizedPoint, hotspotData.correctHotspotIndex))
                    {
                        return new ValidationResult(true, "Correct hotspot selected!");
                    }
                }

                return HandleWrongAnswer();
            }

            if (answer is int hotspotIndex)
            {
                if (hotspotData.allowMultipleSelections)
                {
                    if (hotspotData.correctHotspotIndices.Contains(hotspotIndex))
                    {
                        return new ValidationResult(true, "Correct hotspot selected!");
                    }
                }
                else
                {
                    if (hotspotIndex == hotspotData.correctHotspotIndex)
                    {
                        return new ValidationResult(true, "Correct hotspot selected!");
                    }
                }

                return HandleWrongAnswer();
            }

            return new ValidationResult(false, "Invalid answer format.");
        }
    }
}

