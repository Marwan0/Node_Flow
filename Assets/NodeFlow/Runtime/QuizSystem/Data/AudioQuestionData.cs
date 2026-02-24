using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "AudioQuestion", menuName = "Quiz System/Audio Question")]
    public class AudioQuestionData : QuestionData
    {
        [Header("Audio")]
        [Tooltip("The audio clip to play")]
        public AudioClip audioClip;

        [Tooltip("Allow user to replay the audio")]
        public bool allowReplay = true;

        [Tooltip("Auto-play audio when question is shown")]
        public bool autoPlay = false;

        [Tooltip("Number of times user can play the audio")]
        [Range(1, 10)]
        public int maxPlayCount = 3;

        [Header("Answer")]
        [Tooltip("Type of answer expected")]
        public AudioAnswerType answerType = AudioAnswerType.MultipleChoice;

        [Tooltip("Answer options for multiple choice")]
        public List<string> answerOptions = new List<string>();

        [Tooltip("Index of correct answer")]
        public int correctAnswerIndex = 0;

        [Tooltip("Correct answer text (for fill-in-the-blank)")]
        public string correctAnswerText = "";

        [Tooltip("Case sensitive answer")]
        public bool caseSensitive = false;

        private void OnEnable()
        {
            questionType = QuestionType.Audio;
        }
    }

    public enum AudioAnswerType
    {
        MultipleChoice,
        FillInTheBlank
    }
}
