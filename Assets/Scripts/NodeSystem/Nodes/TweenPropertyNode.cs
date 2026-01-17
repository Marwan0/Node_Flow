using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if DOTWEEN
using DG.Tweening;
#endif

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Property types that can be tweened
    /// </summary>
    public enum TweenPropertyType
    {
        Position,
        LocalPosition,
        Rotation,
        LocalRotation,
        Scale,
        AnchoredPosition,  // For RectTransform
        SizeDelta,         // For RectTransform
        Alpha,             // CanvasGroup alpha
        Color              // Renderer/Image color
    }

    /// <summary>
    /// How to specify a value - direct or from a reference transform
    /// </summary>
    public enum TweenValueMode
    {
        Direct,         // Use the Vector3/float value directly
        FromTransform   // Get value from another Transform
    }

    /// <summary>
    /// Animates any property on a GameObject from one value to another.
    /// Supports drag-drop for target and optional reference transforms for values.
    /// </summary>
    [Serializable]
    public class TweenPropertyNode : NodeData
    {
        [Header("Target")]
        [SerializeField]
        public string targetPath = "";

        [Header("Property")]
        [SerializeField]
        public TweenPropertyType propertyType = TweenPropertyType.Position;

        [Header("From Value")]
        [SerializeField]
        public TweenValueMode fromMode = TweenValueMode.Direct;
        
        [SerializeField]
        public Vector3 fromValue = Vector3.zero;
        
        [SerializeField]
        public float fromFloat = 0f;
        
        [SerializeField]
        public string fromTransformPath = "";
        
        [SerializeField]
        public bool useCurrentAsFrom = true;  // If true, start from current value

        [Header("To Value")]
        [SerializeField]
        public TweenValueMode toMode = TweenValueMode.Direct;
        
        [SerializeField]
        public Vector3 toValue = Vector3.one;
        
        [SerializeField]
        public float toFloat = 1f;
        
        [SerializeField]
        public string toTransformPath = "";

        [Header("Timing")]
        [SerializeField]
        public float duration = 1f;
        
        [SerializeField]
        public float delay = 0f;

#if DOTWEEN
        [SerializeField]
        public Ease easeType = Ease.OutQuad;
#endif

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
                Debug.LogWarning("[TweenPropertyNode] No target specified");
                Complete();
                return;
            }

            var target = FindGameObject(targetPath);
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

            // Get from value
            Vector3 startValue = GetFromValue(target);
            float startFloat = GetFromFloat(target);
            
            // Get to value
            Vector3 endValue = GetToValue(target);
            float endFloat = GetToFloat(target);

#if DOTWEEN
            Tween tween = null;

            switch (propertyType)
            {
                case TweenPropertyType.Position:
                    if (!useCurrentAsFrom) target.transform.position = startValue;
                    tween = target.transform.DOMove(endValue, duration).SetEase(easeType);
                    break;
                    
                case TweenPropertyType.LocalPosition:
                    if (!useCurrentAsFrom) target.transform.localPosition = startValue;
                    tween = target.transform.DOLocalMove(endValue, duration).SetEase(easeType);
                    break;
                    
                case TweenPropertyType.Rotation:
                    if (!useCurrentAsFrom) target.transform.eulerAngles = startValue;
                    tween = target.transform.DORotate(endValue, duration).SetEase(easeType);
                    break;
                    
                case TweenPropertyType.LocalRotation:
                    if (!useCurrentAsFrom) target.transform.localEulerAngles = startValue;
                    tween = target.transform.DOLocalRotate(endValue, duration).SetEase(easeType);
                    break;
                    
                case TweenPropertyType.Scale:
                    if (!useCurrentAsFrom) target.transform.localScale = startValue;
                    tween = target.transform.DOScale(endValue, duration).SetEase(easeType);
                    break;
                    
                case TweenPropertyType.AnchoredPosition:
                    var rectTransform = target.GetComponent<RectTransform>();
                    if (rectTransform != null)
                    {
                        if (!useCurrentAsFrom) rectTransform.anchoredPosition = new Vector2(startValue.x, startValue.y);
                        tween = rectTransform.DOAnchorPos(new Vector2(endValue.x, endValue.y), duration).SetEase(easeType);
                    }
                    break;
                    
                case TweenPropertyType.SizeDelta:
                    var rt = target.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        if (!useCurrentAsFrom) rt.sizeDelta = new Vector2(startValue.x, startValue.y);
                        tween = rt.DOSizeDelta(new Vector2(endValue.x, endValue.y), duration).SetEase(easeType);
                    }
                    break;
                    
                case TweenPropertyType.Alpha:
                    var canvasGroup = target.GetComponent<CanvasGroup>();
                    if (canvasGroup == null) canvasGroup = target.AddComponent<CanvasGroup>();
                    if (!useCurrentAsFrom) canvasGroup.alpha = startFloat;
                    tween = canvasGroup.DOFade(endFloat, duration).SetEase(easeType);
                    break;
                    
                case TweenPropertyType.Color:
                    var image = target.GetComponent<UnityEngine.UI.Image>();
                    if (image != null)
                    {
                        Color startColor = new Color(startValue.x, startValue.y, startValue.z, 1f);
                        Color endColor = new Color(endValue.x, endValue.y, endValue.z, 1f);
                        if (!useCurrentAsFrom) image.color = startColor;
                        tween = image.DOColor(endColor, duration).SetEase(easeType);
                    }
                    else
                    {
                        var renderer = target.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            Color startColor = new Color(startValue.x, startValue.y, startValue.z, 1f);
                            Color endColor = new Color(endValue.x, endValue.y, endValue.z, 1f);
                            if (!useCurrentAsFrom) renderer.material.color = startColor;
                            tween = renderer.material.DOColor(endColor, duration).SetEase(easeType);
                        }
                    }
                    break;
            }

            if (tween != null)
            {
                yield return tween.WaitForCompletion();
            }
#else
            // Fallback without DOTween - simple lerp
            float elapsed = 0f;
            Vector3 currentStart = useCurrentAsFrom ? GetCurrentValue(target) : startValue;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Simple ease out
                t = 1f - (1f - t) * (1f - t);
                
                if (propertyType == TweenPropertyType.Alpha)
                {
                    float currentFloat = useCurrentAsFrom ? GetCurrentFloat(target) : startFloat;
                    SetFloat(target, Mathf.Lerp(currentFloat, endFloat, t));
                }
                else
                {
                    SetValue(target, Vector3.Lerp(currentStart, endValue, t));
                }
                yield return null;
            }

            if (propertyType == TweenPropertyType.Alpha)
                SetFloat(target, endFloat);
            else
                SetValue(target, endValue);
#endif

            Debug.Log($"[TweenPropertyNode] Tween complete: {propertyType} on {targetPath}");
            Complete();
        }

        private Vector3 GetFromValue(GameObject target)
        {
            if (useCurrentAsFrom)
                return GetCurrentValue(target);
                
            if (fromMode == TweenValueMode.FromTransform && !string.IsNullOrEmpty(fromTransformPath))
            {
                var fromObj = FindGameObject(fromTransformPath);
                if (fromObj != null)
                    return GetValueFromTransform(fromObj.transform);
            }
            
            return fromValue;
        }

        private float GetFromFloat(GameObject target)
        {
            if (useCurrentAsFrom)
                return GetCurrentFloat(target);
            return fromFloat;
        }

        private Vector3 GetToValue(GameObject target)
        {
            if (toMode == TweenValueMode.FromTransform && !string.IsNullOrEmpty(toTransformPath))
            {
                var toObj = FindGameObject(toTransformPath);
                if (toObj != null)
                    return GetValueFromTransform(toObj.transform);
            }
            
            return toValue;
        }

        private float GetToFloat(GameObject target)
        {
            return toFloat;
        }

        private Vector3 GetValueFromTransform(Transform t)
        {
            switch (propertyType)
            {
                case TweenPropertyType.Position:
                    return t.position;
                case TweenPropertyType.LocalPosition:
                    return t.localPosition;
                case TweenPropertyType.Rotation:
                    return t.eulerAngles;
                case TweenPropertyType.LocalRotation:
                    return t.localEulerAngles;
                case TweenPropertyType.Scale:
                    return t.localScale;
                case TweenPropertyType.AnchoredPosition:
                    var rt = t.GetComponent<RectTransform>();
                    return rt != null ? new Vector3(rt.anchoredPosition.x, rt.anchoredPosition.y, 0) : Vector3.zero;
                case TweenPropertyType.SizeDelta:
                    var rect = t.GetComponent<RectTransform>();
                    return rect != null ? new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 0) : Vector3.zero;
                default:
                    return t.position;
            }
        }

        private Vector3 GetCurrentValue(GameObject target)
        {
            switch (propertyType)
            {
                case TweenPropertyType.Position:
                    return target.transform.position;
                case TweenPropertyType.LocalPosition:
                    return target.transform.localPosition;
                case TweenPropertyType.Rotation:
                    return target.transform.eulerAngles;
                case TweenPropertyType.LocalRotation:
                    return target.transform.localEulerAngles;
                case TweenPropertyType.Scale:
                    return target.transform.localScale;
                case TweenPropertyType.AnchoredPosition:
                    var rt = target.GetComponent<RectTransform>();
                    return rt != null ? new Vector3(rt.anchoredPosition.x, rt.anchoredPosition.y, 0) : Vector3.zero;
                case TweenPropertyType.SizeDelta:
                    var rect = target.GetComponent<RectTransform>();
                    return rect != null ? new Vector3(rect.sizeDelta.x, rect.sizeDelta.y, 0) : Vector3.zero;
                default:
                    return target.transform.position;
            }
        }

        private float GetCurrentFloat(GameObject target)
        {
            if (propertyType == TweenPropertyType.Alpha)
            {
                var cg = target.GetComponent<CanvasGroup>();
                return cg != null ? cg.alpha : 1f;
            }
            return 0f;
        }

        private void SetValue(GameObject target, Vector3 value)
        {
            switch (propertyType)
            {
                case TweenPropertyType.Position:
                    target.transform.position = value;
                    break;
                case TweenPropertyType.LocalPosition:
                    target.transform.localPosition = value;
                    break;
                case TweenPropertyType.Rotation:
                    target.transform.eulerAngles = value;
                    break;
                case TweenPropertyType.LocalRotation:
                    target.transform.localEulerAngles = value;
                    break;
                case TweenPropertyType.Scale:
                    target.transform.localScale = value;
                    break;
                case TweenPropertyType.AnchoredPosition:
                    var rt = target.GetComponent<RectTransform>();
                    if (rt != null) rt.anchoredPosition = new Vector2(value.x, value.y);
                    break;
                case TweenPropertyType.SizeDelta:
                    var rect = target.GetComponent<RectTransform>();
                    if (rect != null) rect.sizeDelta = new Vector2(value.x, value.y);
                    break;
            }
        }

        private void SetFloat(GameObject target, float value)
        {
            if (propertyType == TweenPropertyType.Alpha)
            {
                var cg = target.GetComponent<CanvasGroup>();
                if (cg == null) cg = target.AddComponent<CanvasGroup>();
                cg.alpha = value;
            }
        }

        #region GameObject Finding (supports disabled objects)
        
        private GameObject FindGameObject(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            // Try GameObject.Find first (only finds active)
            var obj = GameObject.Find(path);
            if (obj != null) return obj;
            
            // Search through all scenes for disabled objects
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;
                
                foreach (var root in scene.GetRootGameObjects())
                {
                    // Check if root matches
                    if (root.name == path) return root;
                    
                    // Check path from root
                    if (path.StartsWith(root.name + "/"))
                    {
                        string remaining = path.Substring(root.name.Length + 1);
                        var found = FindInHierarchy(root.transform, remaining);
                        if (found != null) return found;
                    }
                    
                    // Search entire hierarchy
                    var result = FindInHierarchy(root.transform, path);
                    if (result != null) return result;
                }
            }
            
            return null;
        }
        
        private GameObject FindInHierarchy(Transform parent, string path)
        {
            // Direct child with path
            var direct = parent.Find(path);
            if (direct != null) return direct.gameObject;
            
            // Check if first part matches any child
            string[] parts = path.Split('/');
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == parts[0])
                {
                    if (parts.Length == 1) return child.gameObject;
                    string remaining = string.Join("/", parts, 1, parts.Length - 1);
                    var found = FindInHierarchy(child, remaining);
                    if (found != null) return found;
                }
            }
            
            // Recursive search
            for (int i = 0; i < parent.childCount; i++)
            {
                var found = FindInHierarchy(parent.GetChild(i), path);
                if (found != null) return found;
            }
            
            return null;
        }
        
        #endregion
    }
}
