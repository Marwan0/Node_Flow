using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Property types that can be tweened
    /// </summary>
    public enum PropertyType
    {
        Position,
        Rotation,
        Scale,
        Color,
        Alpha,
        CustomFloat
    }

    /// <summary>
    /// Animates any property on a GameObject
    /// </summary>
    [Serializable]
    public class TweenPropertyNode : NodeData
    {
        [SerializeField]
        public string targetPath = "";
        
        [SerializeField]
        public PropertyType propertyType = PropertyType.Position;
        
        [SerializeField]
        public Vector3 targetValue = Vector3.zero;
        
        [SerializeField]
        public float duration = 1f;
        
        [SerializeField]
        public float delay = 0f;

        public override string Name => "Tween Property";
        public override Color Color => new Color(0.7f, 0.4f, 0.9f); // Purple
        public override string Category => "Animation";

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
                Debug.LogWarning("[TweenPropertyNode] No target path specified");
                Complete();
                return;
            }

            var target = GameObject.Find(targetPath);
            if (target == null)
            {
                Debug.LogWarning($"[TweenPropertyNode] Target not found: {targetPath}");
                Complete();
                return;
            }

            Runner?.StartCoroutine(TweenProperty(target));
        }

        private IEnumerator TweenProperty(GameObject target)
        {
            if (delay > 0)
                yield return new WaitForSeconds(delay);

#if DOTWEEN
            Tween tween = null;

            switch (propertyType)
            {
                case PropertyType.Position:
                    tween = target.transform.DOMove(targetValue, duration);
                    break;
                case PropertyType.Rotation:
                    tween = target.transform.DORotate(targetValue, duration);
                    break;
                case PropertyType.Scale:
                    tween = target.transform.DOScale(targetValue, duration);
                    break;
                case PropertyType.Color:
                    var renderer = target.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        tween = renderer.material.DOColor(new Color(targetValue.x, targetValue.y, targetValue.z, 1f), duration);
                    }
                    break;
                case PropertyType.Alpha:
                    var canvasGroup = target.GetComponent<CanvasGroup>();
                    if (canvasGroup == null) canvasGroup = target.AddComponent<CanvasGroup>();
                    tween = canvasGroup.DOFade(targetValue.x, duration);
                    break;
            }

            if (tween != null)
            {
                yield return tween.WaitForCompletion();
            }
#else
            // Fallback without DOTween - simple lerp
            float elapsed = 0f;
            Vector3 startValue = GetStartValue(target);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                Vector3 currentValue = Vector3.Lerp(startValue, targetValue, t);
                SetValue(target, currentValue);
                yield return null;
            }

            SetValue(target, targetValue);
#endif

            Debug.Log($"[TweenPropertyNode] Tween complete: {propertyType} on {targetPath}");
            Complete();
        }

        private Vector3 GetStartValue(GameObject target)
        {
            switch (propertyType)
            {
                case PropertyType.Position:
                    return target.transform.position;
                case PropertyType.Rotation:
                    return target.transform.eulerAngles;
                case PropertyType.Scale:
                    return target.transform.localScale;
                default:
                    return Vector3.zero;
            }
        }

        private void SetValue(GameObject target, Vector3 value)
        {
            switch (propertyType)
            {
                case PropertyType.Position:
                    target.transform.position = value;
                    break;
                case PropertyType.Rotation:
                    target.transform.eulerAngles = value;
                    break;
                case PropertyType.Scale:
                    target.transform.localScale = value;
                    break;
            }
        }
    }
}

