using UnityEngine;
using NodeSystem;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "MultipleChoiceQuestion", menuName = "Quiz System/Multiple Choice Question")]
    public class MultipleChoiceQuestionData : QuestionData
    {
        [Header("Answers")]
        [Tooltip("Add answer options and select the correct one from the dropdown")]
        public StringIdSelector answers = StringIdSelector.Create("Answer 1", "Answer 2", "Answer 3", "Answer 4");

        /// <summary>Index of the correct answer (derived from the selector's selected ID)</summary>
        public int correctAnswerIndex => answers.GetSelectedIndex();

        /// <summary>The correct answer text</summary>
        public string correctAnswer => answers.SelectedId;

        /// <summary>Number of answer options</summary>
        public int answerCount => answers.Count;

        /// <summary>Get answer text at index</summary>
        public string GetAnswer(int index) => answers.GetAt(index);

        /// <summary>Get all answers as array</summary>
        public string[] GetAnswersArray() => answers.GetIdsArray();

        private void OnEnable()
        {
            questionType = QuestionType.MultipleChoice;
        }

        public bool ValidateAnswers()
        {
            return answers != null && answers.Count >= 2;
        }

        public bool ValidateCorrectAnswer()
        {
            return answers != null && answers.IsSelectionValid();
        }
    }
}
