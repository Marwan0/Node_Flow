using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "TrueFalseQuestion", menuName = "Quiz System/True/False Question")]
    public class TrueFalseQuestionData : QuestionData
    {
        [Header("Answer")]
        [Tooltip("The correct answer (True or False)")]
        public bool correctAnswer = true;

        private void OnEnable()
        {
            questionType = QuestionType.TrueFalse;
        }
    }
}
