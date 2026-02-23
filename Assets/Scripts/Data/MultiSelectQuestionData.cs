using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "MultiSelectQuestion", menuName = "Quiz System/Multi-Select Question")]
    public class MultiSelectQuestionData : QuestionData
    {
        [Header("Options")]
        [Tooltip("List of all answer options")]
        public List<string> options = new List<string>();

        [Header("Answer")]
        [Tooltip("Indices of correct answers (can select multiple)")]
        public List<int> correctAnswerIndices = new List<int>();

        [Header("Scoring")]
        [Tooltip("Award partial credit if some (but not all) correct answers are selected")]
        public bool allowPartialCredit = true;

        private void OnEnable()
        {
            questionType = QuestionType.MultiSelect;
        }
    }
}
