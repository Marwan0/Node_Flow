using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "MultipleChoiceQuestion", menuName = "Quiz System/Multiple Choice Question")]
    public class MultipleChoiceQuestionData : QuestionData
    {
        [BoxGroup("Answers")]
        [InfoBox("Exactly 4 answer options are required. One must be marked as correct.")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        [ValidateInput("ValidateAnswers", "Must have exactly 4 answers")]
        [Tooltip("The 4 answer options")]
        public string[] answers = new string[4];

        [BoxGroup("Answers")]
        [ValueDropdown("GetAnswerOptions")]
        [ValidateInput("ValidateCorrectAnswer", "Correct answer index must be valid")]
        [Tooltip("Index of the correct answer (0-3)")]
        public int correctAnswerIndex = 0;

        private void OnEnable()
        {
            questionType = QuestionType.MultipleChoice;
            if (answers == null || answers.Length != 4)
            {
                answers = new string[4];
            }
        }

        private System.Collections.Generic.IEnumerable<ValueDropdownItem<int>> GetAnswerOptions()
        {
            for (int i = 0; i < answers.Length; i++)
            {
                string label = string.IsNullOrEmpty(answers[i]) ? $"Answer {i + 1}" : answers[i];
                yield return new ValueDropdownItem<int>(label, i);
            }
        }

        private bool ValidateAnswers()
        {
            return answers != null && answers.Length == 4;
        }

        private bool ValidateCorrectAnswer()
        {
            if (answers == null || answers.Length != 4)
                return false;
            return correctAnswerIndex >= 0 && correctAnswerIndex < 4;
        }
    }
}

