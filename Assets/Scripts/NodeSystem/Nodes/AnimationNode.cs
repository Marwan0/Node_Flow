using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if DOTWEEN
using DG.Tweening;
#endif

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Animation types available
    /// </summary>
    public enum AnimationType
    {
        FadeIn,
        FadeOut,
        ScaleUp,
        ScaleDown,
        SlideIn,
        SlideOut,
        Punch,
        Shake
    }

    /// <summary>
    /// Slide direction for slide animations
    /// </summary>
    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    /// <summary>
    /// Animates UI elements using DOTween or built-in animations
    /// </summary>
    [Serializable]
    public class AnimationNode : NodeData
    {
        [SerializeField]
        public string targetPath = "";
        
        [SerializeField]
        public AnimationType animationType = AnimationType.FadeIn;
        
        [SerializeField]
        public SlideDirection slideDirection = SlideDirection.Left;
        
        [SerializeField]
        public float duration = 0.5f;
        
        [SerializeField]
        public float delay = 0f;

        public override string Name => "Animation";
        public override Color Color => new Color(0.9f, 0.4f, 0.7f); // Pink
        public override string Category => "UI";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "On Complete", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(targetPath))
            {
                Debug.LogError("[AnimationNode] ⚠️ No target configured! Drag a GameObject to the Target field in the inspector.");
                Complete();
                return;
            }

            var target = GameObject.Find(targetPath);
            if (target == null)
            {
                Debug.LogError($"[AnimationNode] ⚠️ Target not found in scene: {targetPath}");
                Complete();
                return;
            }

            Debug.Log($"[AnimationNode] Starting {animationType} on {targetPath} ({duration}s)");
            Runner?.StartCoroutine(PlayAnimation(target));
        }

        private IEnumerator PlayAnimation(GameObject target)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

            var rectTransform = target.GetComponent<RectTransform>();
            var canvasGroup = target.GetComponent<CanvasGroup>();
            
            // Add CanvasGroup if needed for fade
            if (canvasGroup == null && (animationType == AnimationType.FadeIn || animationType == AnimationType.FadeOut))
            {
                canvasGroup = target.AddComponent<CanvasGroup>();
            }

#if DOTWEEN
            Tween tween = null;
            
            switch (animationType)
            {
                case AnimationType.FadeIn:
                    canvasGroup.alpha = 0;
                    tween = canvasGroup.DOFade(1, duration);
                    break;
                    
                case AnimationType.FadeOut:
                    canvasGroup.alpha = 1;
                    tween = canvasGroup.DOFade(0, duration);
                    break;
                    
                case AnimationType.ScaleUp:
                    rectTransform.localScale = Vector3.zero;
                    tween = rectTransform.DOScale(Vector3.one, duration).SetEase(Ease.OutBack);
                    break;
                    
                case AnimationType.ScaleDown:
                    rectTransform.localScale = Vector3.one;
                    tween = rectTransform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack);
                    break;
                    
                case AnimationType.SlideIn:
                    var startPos = GetSlideOffset(rectTransform, slideDirection);
                    var endPos = rectTransform.anchoredPosition;
                    rectTransform.anchoredPosition = startPos;
                    tween = rectTransform.DOAnchorPos(endPos, duration).SetEase(Ease.OutCubic);
                    break;
                    
                case AnimationType.SlideOut:
                    var slideTarget = GetSlideOffset(rectTransform, slideDirection);
                    tween = rectTransform.DOAnchorPos(slideTarget, duration).SetEase(Ease.InCubic);
                    break;
                    
                case AnimationType.Punch:
                    tween = rectTransform.DOPunchScale(Vector3.one * 0.2f, duration, 10, 1);
                    break;
                    
                case AnimationType.Shake:
                    tween = rectTransform.DOShakePosition(duration, 10, 20, 90, false, true);
                    break;
            }

            if (tween != null)
            {
                yield return tween.WaitForCompletion();
            }
#else
            // Fallback without DOTween - simple coroutine animations
            float elapsed = 0;
            
            switch (animationType)
            {
                case AnimationType.FadeIn:
                    canvasGroup.alpha = 0;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        canvasGroup.alpha = elapsed / duration;
                        yield return null;
                    }
                    canvasGroup.alpha = 1;
                    break;
                    
                case AnimationType.FadeOut:
                    canvasGroup.alpha = 1;
                    while (elapsed < duration)
                    {
                        elapsed += Time.deltaTime;
                        canvasGroup.alpha = 1 - (elapsed / duration);
                        yield return null;
                    }
                    canvasGroup.alpha = 0;
                    break;
                    
                default:
                    Debug.LogWarning($"[AnimationNode] Animation type {animationType} requires DOTween");
                    yield return new WaitForSeconds(duration);
                    break;
            }
#endif

            Debug.Log($"[AnimationNode] {animationType} complete on {targetPath}");
            Complete();
        }

        private Vector2 GetSlideOffset(RectTransform rt, SlideDirection direction)
        {
            var pos = rt.anchoredPosition;
            float offset = 500f; // Default offset
            
            switch (direction)
            {
                case SlideDirection.Left:
                    return new Vector2(pos.x - offset, pos.y);
                case SlideDirection.Right:
                    return new Vector2(pos.x + offset, pos.y);
                case SlideDirection.Up:
                    return new Vector2(pos.x, pos.y + offset);
                case SlideDirection.Down:
                    return new Vector2(pos.x, pos.y - offset);
            }
            return pos;
        }
    }
}

