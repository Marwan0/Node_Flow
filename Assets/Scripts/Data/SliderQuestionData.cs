using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "SliderQuestion", menuName = "Quiz System/Slider Question")]
    public class SliderQuestionData : QuestionData
    {
        [Header("Range")]
        [Tooltip("Minimum and maximum values for the slider (x = min, y = max)")]
        public Vector2 valueRange = new Vector2(0, 100);

        [Header("Answer")]
        [Tooltip("The correct value (or center of range if using tolerance)")]
        public float correctValue = 50f;

        [Tooltip("Allow answers within a tolerance range of the correct value")]
        public bool useTolerance = true;

        [Tooltip("Tolerance range (±value)")]
        [Range(0.1f, 50f)]
        public float tolerance = 5f;

        [Header("Display")]
        [Tooltip("Show value labels on slider")]
        public bool showValueLabels = true;

        [Tooltip("Show current value as user drags")]
        public bool showCurrentValue = true;

        [Tooltip("Number of decimal places to display")]
        [Range(0, 3)]
        public int decimalPlaces = 0;

        private void OnEnable()
        {
            questionType = QuestionType.Slider;
        }

        public bool IsValueCorrect(float userValue)
        {
            if (useTolerance)
            {
                return Mathf.Abs(userValue - correctValue) <= tolerance;
            }
            else
            {
                return Mathf.Approximately(userValue, correctValue);
            }
        }
    }
}
