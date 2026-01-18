using System;
using UnityEngine;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Animation type for answer reveal
    /// </summary>
    public enum AnswerAnimationType
    {
        None,
        Scale,
        Fade,
        SlideFromLeft,
        SlideFromRight,
        SlideFromTop,
        SlideFromBottom,
        Rotate,
        Bounce
    }

    /// <summary>
    /// Settings for how an answer reveals with animation
    /// </summary>
    [Serializable]
    public class AnswerAnimationSettings
    {
        [Tooltip("Enable animation for this answer")]
        public bool enabled = true;

        [Tooltip("Type of animation to use")]
        public AnswerAnimationType animationType = AnswerAnimationType.Scale;

        [Tooltip("Duration of the animation in seconds")]
        [Range(0.1f, 2f)]
        public float duration = 0.3f;

        [Tooltip("Delay before this answer starts animating (for staggered effects)")]
        [Range(0f, 1f)]
        public float delay = 0f;

#if DOTWEEN
        [Tooltip("Ease type for the animation")]
        public DG.Tweening.Ease easeType = DG.Tweening.Ease.OutBack;
#else
        [Tooltip("Ease type for the animation (DOTween required)")]
        public int easeType = 0; // Fallback when DOTween not available
#endif

        [Tooltip("Custom scale multiplier (for Scale/Bounce animations)")]
        [Range(0.1f, 2f)]
        public float scaleMultiplier = 1f;

        [Tooltip("Custom slide distance (for Slide animations)")]
        [Range(10f, 500f)]
        public float slideDistance = 100f;
    }
}
