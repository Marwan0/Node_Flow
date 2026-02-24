using UnityEngine;

namespace QuizSystem
{
    public abstract class QuestionData : ScriptableObject
    {
        [Header("Question Info")]
        [TextArea(3, 5)]
        [Tooltip("The question text displayed to the user")]
        public string questionText;

        [Tooltip("The type of question")]
        public QuestionType questionType;

        [Header("Hints & Attempts")]
        [Tooltip("Hints shown for each wrong attempt (one per attempt)")]
        public string[] hints = new string[3];

        [Range(1, 10)]
        [Tooltip("Maximum number of attempts before auto-correct")]
        public int maxAttempts = 3;

        [Header("Scoring")]
        [Range(0, 1000)]
        [Tooltip("Points awarded for correct answer")]
        public int points = 10;

        [Tooltip("Explanation shown after answering (or after max attempts)")]
        [TextArea(3, 5)]
        public string explanation;

        [Header("Layout Override")]
        [Tooltip("If set, this prefab is used instead of QuizManager's default for this question type. Must use the same QuestionUI variant as the type (e.g. MultipleChoiceUI for Multiple Choice).")]
        public GameObject customUIPrefab;

        private bool ValidateCustomUIPrefab(GameObject prefab)
        {
            if (prefab == null) return true;
            return prefab.GetComponentInChildren<QuestionUI>(true) != null;
        }
    }
}
