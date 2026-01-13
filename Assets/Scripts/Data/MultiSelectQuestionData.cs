using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "MultiSelectQuestion", menuName = "Quiz System/Multi-Select Question")]
    public class MultiSelectQuestionData : QuestionData
    {
        [BoxGroup("Options")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        [Tooltip("List of all answer options")]
        public List<string> options = new List<string>();

        [BoxGroup("Answer")]
        [ValueDropdown("GetOptionIndices")]
        [ValidateInput("ValidateCorrectAnswers", "At least one correct answer must be selected")]
        [Tooltip("Indices of correct answers (can select multiple)")]
        public List<int> correctAnswerIndices = new List<int>();

        [BoxGroup("Scoring")]
        [Tooltip("Award partial credit if some (but not all) correct answers are selected")]
        public bool allowPartialCredit = true;

        private void OnEnable()
        {
            questionType = QuestionType.MultiSelect;
        }

        private IEnumerable<int> GetOptionIndices()
        {
            return Enumerable.Range(0, options.Count);
        }

        private bool ValidateCorrectAnswers(List<int> indices)
        {
            return indices != null && indices.Count > 0 && indices.All(i => i >= 0 && i < options.Count);
        }

        [Button("Add Option")]
        [BoxGroup("Options")]
        private void AddOption()
        {
            options.Add("New Option");
        }
    }
}

