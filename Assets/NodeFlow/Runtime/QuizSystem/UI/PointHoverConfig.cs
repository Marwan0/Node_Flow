using UnityEngine;
using DG.Tweening;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "PointHoverConfig", menuName = "Quiz System/Point Hover Config")]
    public class PointHoverConfig : ScriptableObject
    {
        [Header("Idle State")]
        [Tooltip("Color tint in idle state. Set to white for no tint override.")]
        public Color idleColor = Color.white;

        [Tooltip("Optional sprite for idle state. Leave null to keep the original sprite.")]
        public Sprite idleSprite;

        [Header("Hover State")]
        [Tooltip("Color tint on pointer enter.")]
        public Color hoverColor = new Color(0.8f, 0.9f, 1f, 1f);

        [Tooltip("Optional sprite to swap to on hover. Leave null to keep idle sprite.")]
        public Sprite hoverSprite;

        [Tooltip("Sound effect played on pointer enter. Leave null for silent hover.")]
        public AudioClip hoverSFX;

        [Header("Transition")]
        [Range(0f, 1f)]
        [Tooltip("Duration of the color transition in seconds. 0 = instant.")]
        public float transitionDuration = 0.15f;

        [Tooltip("Ease curve for the hover transition.")]
        public Ease transitionEase = Ease.OutQuad;

        [Header("Scale Punch (Optional)")]
        [Tooltip("Enable a scale punch effect on hover enter.")]
        public bool enableScalePunch = false;

        [Range(1f, 1.5f)]
        [Tooltip("Scale multiplier for the punch.")]
        public float scalePunchAmount = 1.08f;

        [Range(0.05f, 0.5f)]
        [Tooltip("Duration of the scale punch animation.")]
        public float scalePunchDuration = 0.15f;

        [Header("SFX Settings")]
        [Range(0f, 1f)]
        [Tooltip("Volume of the hover sound effect.")]
        public float sfxVolume = 1f;
    }
}
