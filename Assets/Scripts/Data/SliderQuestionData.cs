using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "SliderQuestion", menuName = "Quiz System/Slider Question")]
    public class SliderQuestionData : QuestionData
    {
        [BoxGroup("Range")]
        [MinMaxSlider(0, 100, true)]
        [Tooltip("Minimum and maximum values for the slider")]
        public Vector2 valueRange = new Vector2(0, 100);

        [BoxGroup("Answer")]
        [PropertyRange("valueRange.x", "valueRange.y")]
        [Tooltip("The correct value (or center of range if using tolerance)")]
        public float correctValue = 50f;

        [BoxGroup("Answer")]
        [Tooltip("Allow answers within a tolerance range of the correct value")]
        public bool useTolerance = true;

        [BoxGroup("Answer")]
        [ShowIf("useTolerance")]
        [PropertyRange(0.1f, 50f)]
        [Tooltip("Tolerance range (±value)")]
        public float tolerance = 5f;

        [BoxGroup("Answer")]
        [HideIf("useTolerance")]
        [Tooltip("Must match exact value (no tolerance)")]
        [InfoBox("Exact match required - no tolerance")]
        private bool exactMatch = true;

        [BoxGroup("Display")]
        [Tooltip("Show value labels on slider")]
        public bool showValueLabels = true;

        [BoxGroup("Display")]
        [Tooltip("Show current value as user drags")]
        public bool showCurrentValue = true;

        [BoxGroup("Display")]
        [Tooltip("Number of decimal places to display")]
        [PropertyRange(0, 3)]
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

        [Button("Test Value")]
        [BoxGroup("Answer")]
        private void TestValue(float testValue)
        {
            bool correct = IsValueCorrect(testValue);
            Debug.Log($"Value {testValue} is {(correct ? "CORRECT" : "INCORRECT")}. " +
                     $"Correct range: {correctValue - tolerance} to {correctValue + tolerance}");
        }
    }
}

