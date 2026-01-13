using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace QuizSystem
{
    public abstract class QuestionData : SerializedScriptableObject
    {
        [BoxGroup("Question Info")]
        [Required]
        [MultiLineProperty(3)]
        [Tooltip("The question text displayed to the user")]
        public string questionText;

        [BoxGroup("Question Info")]
        [EnumToggleButtons]
        [Tooltip("The type of question")]
        public QuestionType questionType;

        [BoxGroup("Hints & Attempts")]
        [InfoBox("Hints are shown in order for each wrong attempt. Leave empty if no hint for that attempt.")]
        [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = false)]
        [Tooltip("Hints shown for each wrong attempt (one per attempt)")]
        public string[] hints = new string[3];

        [BoxGroup("Hints & Attempts")]
        [PropertyRange(1, 10)]
        [Tooltip("Maximum number of attempts before auto-correct")]
        public int maxAttempts = 3;

        [BoxGroup("Scoring")]
        [PropertyRange(0, 100)]
        [Tooltip("Points awarded for correct answer")]
        public int points = 10;

        [BoxGroup("Scoring")]
        [Tooltip("Explanation shown after answering (or after max attempts)")]
        [MultiLineProperty(3)]
        public string explanation;

        [Button("Validate Question")]
        [BoxGroup("Validation")]
        private void ValidateQuestion()
        {
            if (string.IsNullOrEmpty(questionText))
            {
                Debug.LogWarning($"{name}: Question text is empty!");
                return;
            }

            if (hints == null || hints.Length == 0)
            {
                Debug.LogWarning($"{name}: No hints provided!");
            }

            Debug.Log($"{name}: Question validation passed!");
        }
    }
}

