using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "AudioQuestion", menuName = "Quiz System/Audio Question")]
    public class AudioQuestionData : QuestionData
    {
        [BoxGroup("Audio")]
        [PreviewField(200, ObjectFieldAlignment.Left)]
        [Required]
        [Tooltip("The audio clip to play")]
        public AudioClip audioClip;

        [BoxGroup("Audio")]
        [Tooltip("Allow user to replay the audio")]
        public bool allowReplay = true;

        [BoxGroup("Audio")]
        [Tooltip("Auto-play audio when question is shown")]
        public bool autoPlay = false;

        [BoxGroup("Audio")]
        [Tooltip("Number of times user can play the audio")]
        [PropertyRange(1, 10)]
        public int maxPlayCount = 3;

        [BoxGroup("Answer")]
        [Tooltip("Type of answer expected")]
        public AudioAnswerType answerType = AudioAnswerType.MultipleChoice;

        [BoxGroup("Answer")]
        [ShowIf("answerType", AudioAnswerType.MultipleChoice)]
        [TableList(ShowIndexLabels = true)]
        [Tooltip("Answer options for multiple choice")]
        public List<string> answerOptions = new List<string>();

        [BoxGroup("Answer")]
        [ShowIf("answerType", AudioAnswerType.MultipleChoice)]
        [ValueDropdown("GetOptionIndices")]
        [Tooltip("Index of correct answer")]
        public int correctAnswerIndex = 0;

        [BoxGroup("Answer")]
        [ShowIf("answerType", AudioAnswerType.FillInTheBlank)]
        [Tooltip("Correct answer text (for fill-in-the-blank)")]
        public string correctAnswerText = "";

        [BoxGroup("Answer")]
        [ShowIf("answerType", AudioAnswerType.FillInTheBlank)]
        [Tooltip("Case sensitive answer")]
        public bool caseSensitive = false;

        private void OnEnable()
        {
            questionType = QuestionType.Audio;
        }

        private IEnumerable<int> GetOptionIndices()
        {
            for (int i = 0; i < answerOptions.Count; i++)
            {
                yield return i;
            }
        }

        [Button("Add Answer Option")]
        [BoxGroup("Answer")]
        [ShowIf("answerType", AudioAnswerType.MultipleChoice)]
        private void AddAnswerOption()
        {
            answerOptions.Add("New Option");
        }
    }

    public enum AudioAnswerType
    {
        MultipleChoice,
        FillInTheBlank
    }
}

