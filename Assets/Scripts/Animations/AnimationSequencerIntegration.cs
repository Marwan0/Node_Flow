using UnityEngine;
using Sirenix.OdinInspector;
using DG.Tweening;
using System;

namespace QuizSystem
{
    /// <summary>
    /// Integration helper for Animation Sequencer.
    /// Provides a bridge between our custom animation system and Animation Sequencer.
    /// Works with or without Animation Sequencer installed.
    /// </summary>
    public static class AnimationSequencerIntegration
    {
        private static bool? _isAnimationSequencerAvailable = null;

        /// <summary>
        /// Checks if Animation Sequencer is available in the project.
        /// </summary>
        public static bool IsAnimationSequencerAvailable
        {
            get
            {
                if (_isAnimationSequencerAvailable.HasValue)
                    return _isAnimationSequencerAvailable.Value;

                // Try to find Animation Sequencer type
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                foreach (var assembly in assemblies)
                {
                    var sequencerType = assembly.GetType("Brunomikoski.AnimationSequencer.AnimationSequencer");
                    if (sequencerType != null)
                    {
                        _isAnimationSequencerAvailable = true;
                        return true;
                    }
                }

                _isAnimationSequencerAvailable = false;
                return false;
            }
        }

        /// <summary>
        /// Gets an Animation Sequencer component from a GameObject, or creates one if available.
        /// Returns null if Animation Sequencer is not available.
        /// </summary>
        public static Component GetOrCreateAnimationSequencer(GameObject target)
        {
            if (!IsAnimationSequencerAvailable)
                return null;

            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            System.Type sequencerType = null;

            foreach (var assembly in assemblies)
            {
                sequencerType = assembly.GetType("Brunomikoski.AnimationSequencer.AnimationSequencer");
                if (sequencerType != null) break;
            }

            if (sequencerType == null)
                return null;

            // Check if component already exists
            var existing = target.GetComponent(sequencerType);
            if (existing != null)
                return existing;

            // Create new component
            return target.AddComponent(sequencerType);
        }

        /// <summary>
        /// Plays an Animation Sequencer component if available.
        /// Falls back to custom animation if not available.
        /// </summary>
        public static void PlayAnimationSequencer(Component sequencerComponent, System.Action onComplete = null)
        {
            if (sequencerComponent == null)
            {
                onComplete?.Invoke();
                return;
            }

            if (!IsAnimationSequencerAvailable)
            {
                onComplete?.Invoke();
                return;
            }

            // Use reflection to call Play() method
            var playMethod = sequencerComponent.GetType().GetMethod("Play", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (playMethod != null)
            {
                playMethod.Invoke(sequencerComponent, null);
                
                // Note: Animation Sequencer has its own callback system
                // You may need to configure it in the inspector for onComplete
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        /// <summary>
        /// Creates a custom quiz animation action that can be used with Animation Sequencer.
        /// This is a template - you'll need to implement specific actions.
        /// </summary>
        [System.Serializable]
        public class QuizAnimationActionBase
        {
            public virtual string DisplayName => "Quiz Animation Action";
            
            [SerializeField]
            protected float delay = 0f;
            
            [SerializeField]
            protected float duration = 0.5f;

            public float Delay => delay;
            public float Duration => duration;

            public virtual void AddToSequence(Sequence sequence)
            {
                // Override in derived classes
            }
        }
    }

    /// <summary>
    /// MonoBehaviour wrapper that can use either Animation Sequencer or custom animations.
    /// Provides a unified interface for both systems.
    /// </summary>
    public class QuizAnimationController : MonoBehaviour
    {
        [BoxGroup("Animation System")]
        [InfoBox("If Animation Sequencer is available, it will be used. Otherwise, custom DOTween animations will be used.")]
        [Tooltip("Enable to use Animation Sequencer if available")]
        public bool preferAnimationSequencer = true;

        [BoxGroup("Animation System")]
        [ShowIf("preferAnimationSequencer")]
        [Tooltip("Animation Sequencer component (auto-created if available)")]
        [ReadOnly]
        public Component animationSequencerComponent;

        [BoxGroup("Fallback Settings")]
        [HideIf("preferAnimationSequencer")]
        [Tooltip("Duration for fallback custom animations")]
        [Range(0.1f, 2f)]
        public float fallbackDuration = 0.5f;

        private void Awake()
        {
            if (preferAnimationSequencer && AnimationSequencerIntegration.IsAnimationSequencerAvailable)
            {
                animationSequencerComponent = AnimationSequencerIntegration.GetOrCreateAnimationSequencer(gameObject);
            }
        }

        /// <summary>
        /// Plays the animation using the preferred system.
        /// </summary>
        public void PlayAnimation(System.Action onComplete = null)
        {
            if (preferAnimationSequencer && animationSequencerComponent != null)
            {
                AnimationSequencerIntegration.PlayAnimationSequencer(animationSequencerComponent, onComplete);
            }
            else
            {
                // Fallback to custom animation
                PlayCustomAnimation(onComplete);
            }
        }

        private void PlayCustomAnimation(System.Action onComplete)
        {
            // Simple fade animation as fallback
            var canvasGroup = GetComponent<UnityEngine.CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, fallbackDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                onComplete?.Invoke();
            }
        }
    }
}

