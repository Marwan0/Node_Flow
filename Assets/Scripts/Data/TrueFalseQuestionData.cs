using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "TrueFalseQuestion", menuName = "Quiz System/True/False Question")]
    public class TrueFalseQuestionData : QuestionData
    {
        [BoxGroup("Answer")]
        [EnumToggleButtons]
        [Tooltip("The correct answer (True or False)")]
        public bool correctAnswer = true;

        [Button("Set to True")]
        [ButtonGroup("Answer/Quick Set")]
        private void SetTrue() => correctAnswer = true;

        [Button("Set to False")]
        [ButtonGroup("Answer/Quick Set")]
        private void SetFalse() => correctAnswer = false;

        private void OnEnable()
        {
            questionType = QuestionType.TrueFalse;
        }
    }
}

