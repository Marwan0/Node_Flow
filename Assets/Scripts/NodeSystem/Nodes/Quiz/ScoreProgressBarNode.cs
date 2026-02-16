using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Drives a UI Slider or Image (fill amount) from a score value.
    /// Value and min/max can be literals or graph variables for easy setup.
    /// Progress animates (lerp) by default instead of snapping.
    /// Safe to trigger multiple times: connect from Start Quiz, after each question type,
    /// or from any node that should refresh the bar.
    /// </summary>
    [Serializable]
    public class ScoreProgressBarNode : NodeData
    {
        public enum ValueSource
        {
            QuizScore,
            Variable
        }

        [Header("Target")]
        [SerializeField]
        [Tooltip("Assign by drag-and-drop from Hierarchy/Inspector. Slider, Image, or GameObject.")]
        public UnityEngine.Object targetRef;

        [SerializeField]
        [Tooltip("Fallback path when reference is null (e.g. in builds). Set automatically when you assign Target.")]
        public string targetPath = "";

        [Header("Value (current)")]
        [SerializeField]
        public ValueSource valueSource = ValueSource.QuizScore;

        [SerializeField]
        [Tooltip("Variable name when Value Source = Variable. Int or Float.")]
        public string valueVariableName = "";

        [Header("Range (min / max)")]
        [SerializeField]
        [Tooltip("When Value from = Quiz Score: use 0 and QuizState.maxPossibleScore (from Start Quiz). Otherwise use Min/Max below.")]
        public bool useQuizRange = true;

        [SerializeField]
        [Tooltip("Min value. Ignored if Use Quiz Range or Min Variable is set.")]
        public float minLiteral = 0f;

        [SerializeField]
        [Tooltip("Variable name for min. If set, overrides Min literal. Int or Float.")]
        public string minVariableName = "";

        [SerializeField]
        [Tooltip("Max value. Ignored if Use Quiz Range or Max Variable is set.")]
        public float maxLiteral = 100f;

        [SerializeField]
        [Tooltip("Variable name for max. If set, overrides Max literal. Int or Float.")]
        public string maxVariableName = "";

        [Header("Animation")]
        [SerializeField]
        [Tooltip("Smoothly animate the bar to the new value instead of snapping.")]
        public bool animateFill = true;

        [SerializeField]
        [Tooltip("Duration of the lerp animation in seconds.")]
        [Range(0.05f, 2f)]
        public float animationDuration = 0.3f;

        public override string Name => "Score Progress Bar";
        public override Color Color => new Color(0.85f, 0.65f, 0.25f); // Amber/Gold
        public override string Category => "Quiz";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input, PortCapacity.Multi)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "Next", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (Runner?.Graph == null)
            {
                Debug.LogWarning("[ScoreProgressBarNode] No graph runner.");
                Complete();
                return;
            }

            GameObject targetGo = ResolveTarget();
            if (targetGo == null)
            {
                Debug.LogWarning("[ScoreProgressBarNode] No target: assign one by drag-and-drop or set target path.");
                Complete();
                return;
            }

            float value = GetValue();
            float min = GetMin();
            float max = GetMax();

            float fill = 0f;
            float range = max - min;
            if (Mathf.Abs(range) > 0.0001f)
                fill = Mathf.Clamp01((value - min) / range);
            else
                fill = value >= max ? 1f : 0f;

            var slider = targetGo.GetComponent<Slider>();
            if (slider != null)
            {
                if (animateFill && animationDuration > 0f && Runner != null)
                {
                    Runner.StartCoroutine(AnimateToFill(
                        fromValue: slider.value,
                        toValue: fill,
                        duration: animationDuration,
                        onUpdate: v => slider.value = v,
                        onComplete: Complete));
                }
                else
                {
                    slider.value = fill;
                    Complete();
                }
                return;
            }

            var image = targetGo.GetComponent<Image>();
            if (image != null && image.type == Image.Type.Filled)
            {
                if (animateFill && animationDuration > 0f && Runner != null)
                {
                    Runner.StartCoroutine(AnimateToFill(
                        fromValue: image.fillAmount,
                        toValue: fill,
                        duration: animationDuration,
                        onUpdate: v => image.fillAmount = v,
                        onComplete: Complete));
                }
                else
                {
                    image.fillAmount = fill;
                    Complete();
                }
                return;
            }

            Debug.LogWarning($"[ScoreProgressBarNode] No Slider or filled Image on: {targetGo.name}");
            Complete();
        }

        private IEnumerator AnimateToFill(float fromValue, float toValue, float duration, Action<float> onUpdate, Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);
                float current = Mathf.Lerp(fromValue, toValue, t);
                onUpdate(current);
                yield return null;
            }
            onUpdate(toValue);
            onComplete?.Invoke();
        }

        private GameObject ResolveTarget()
        {
            // First, try the direct reference
            if (targetRef != null)
            {
                if (targetRef is GameObject go && go != null)
                    return go;
                if (targetRef is Component c && c != null)
                    return c.gameObject;
            }
            
            // Reference is null or invalid - try to restore from path
            if (!string.IsNullOrEmpty(targetPath))
            {
                // Try GameObject.Find first (works if object is active)
                var found = GameObject.Find(targetPath);
                if (found != null)
                {
                    // Restore the reference for next time
                    targetRef = found;
                    return found;
                }
                
                // Try hierarchical search (works even if object is inactive)
                var parts = targetPath.Split('/');
                if (parts.Length > 0)
                {
                    var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                    foreach (var rootGo in rootObjects)
                    {
                        if (rootGo.name != parts[0]) continue;
                        if (parts.Length == 1)
                        {
                            targetRef = rootGo; // Restore reference
                            return rootGo;
                        }
                        var t = rootGo.transform.Find(string.Join("/", parts, 1, parts.Length - 1));
                        if (t != null)
                        {
                            targetRef = t.gameObject; // Restore reference
                            return t.gameObject;
                        }
                    }
                }
            }
            return null;
        }

        private float GetValue()
        {
            if (valueSource == ValueSource.QuizScore)
            {
                var state = QuizState.Instance;
                return state != null ? state.currentScore : 0f;
            }
            return GetVariableNumber(valueVariableName, 0f);
        }

        private float GetMin()
        {
            if (valueSource == ValueSource.QuizScore && useQuizRange)
                return 0f;
            if (!string.IsNullOrEmpty(minVariableName))
                return GetVariableNumber(minVariableName, minLiteral);
            return minLiteral;
        }

        private float GetMax()
        {
            if (valueSource == ValueSource.QuizScore && useQuizRange)
            {
                var state = QuizState.Instance;
                return state != null && state.maxPossibleScore > 0 ? state.maxPossibleScore : maxLiteral;
            }
            if (!string.IsNullOrEmpty(maxVariableName))
                return GetVariableNumber(maxVariableName, maxLiteral);
            return maxLiteral;
        }

        private float GetVariableNumber(string variableName, float fallback)
        {
            if (string.IsNullOrEmpty(variableName) || Runner?.Graph == null)
                return fallback;
            var v = Runner.Graph.GetVariable(variableName);
            if (v == null)
                return fallback;
            if (v.Type == VariableType.Int)
                return v.GetIntValue();
            if (v.Type == VariableType.Float)
                return v.GetFloatValue();
            return fallback;
        }
    }
}
