using UnityEngine;
using DG.Tweening;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "AnswerFeedbackConfig", menuName = "Quiz System/Answer Feedback Config")]
    public class AnswerFeedbackConfig : ScriptableObject
    {
        [Header("Correct Answer")]
        public Color correctColor = new Color(0.2f, 0.9f, 0.2f, 1f);
        [Tooltip("Optional sprite to show on the correct answer button.")]
        public Sprite correctSprite;
        [Tooltip("Sound played on correct answer.")]
        public AudioClip correctSFX;

        [Header("Wrong Answer")]
        public Color wrongColor = new Color(0.9f, 0.2f, 0.2f, 1f);
        [Tooltip("Optional sprite to show on the wrong answer button.")]
        public Sprite wrongSprite;
        [Tooltip("Sound played on wrong answer.")]
        public AudioClip wrongSFX;

        [Header("Transition")]
        [Range(0f, 1f)]
        public float colorTransitionDuration = 0.25f;
        public Ease transitionEase = Ease.OutQuad;

        [Header("SFX Settings")]
        [Range(0f, 1f)]
        public float sfxVolume = 1f;
    }
}
