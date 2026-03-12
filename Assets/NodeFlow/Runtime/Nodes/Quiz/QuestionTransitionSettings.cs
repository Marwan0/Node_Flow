using System;
using UnityEngine;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Transition type for question enter/exit animations.
    /// </summary>
    public enum QuestionTransitionType
    {
        None,
        Fade,
        SlideFromLeft,
        SlideFromRight,
        SlideFromTop,
        SlideFromBottom,
        Scale,
        ScaleAndFade
    }

    /// <summary>
    /// Settings for a single transition direction (enter or exit).
    /// </summary>
    [Serializable]
    public class QuestionTransitionSettings
    {
        [Tooltip("Type of transition animation")]
        public QuestionTransitionType transitionType = QuestionTransitionType.Fade;

        [Tooltip("Duration of the transition in seconds")]
        [Range(0.05f, 2f)]
        public float duration = 0.3f;

#if DOTWEEN
        [Tooltip("Ease type for the transition")]
        public DG.Tweening.Ease easeType = DG.Tweening.Ease.OutQuad;
#else
        [Tooltip("Ease type for the transition (DOTween required)")]
        public int easeType = 0;
#endif

        [Tooltip("Slide distance in pixels (for Slide transitions)")]
        [Range(100f, 2000f)]
        public float slideDistance = 1000f;
    }
}
