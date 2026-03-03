using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Drives a UI Slider, Image (fill amount), or Slots (LED indicators) from a score value.
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

        public enum DisplayMode
        {
            Slider,
            FilledImage,
            Slots
        }

        [Header("Display Mode")]
        [SerializeField]
        public DisplayMode displayMode = DisplayMode.Slider;

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

        [SerializeField]
        [Tooltip("When Value from = Quiz Score, keep listening to score changes and update automatically.")]
        public bool liveUpdateFromScoreEvents = true;

        [Header("Slots Settings")]
        [SerializeField]
        public Color slotDefaultColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [SerializeField]
        public Color slotCorrectColor = new Color(0.2f, 0.8f, 0.2f, 1f);

        [SerializeField]
        public Color slotWrongColor = new Color(0.8f, 0.2f, 0.2f, 1f);

        [SerializeField]
        public Sprite slotDefaultSprite;

        [SerializeField]
        public Sprite slotCorrectSprite;

        [SerializeField]
        public Sprite slotWrongSprite;

        [SerializeField]
        public bool slotAnimateOnFill = true;

        [SerializeField]
        [Range(0.05f, 2f)]
        public float slotAnimationDuration = 0.2f;

        [NonSerialized]
        private bool _isScoreEventSubscribed;

        [NonSerialized]
        private bool _isAnswerEventSubscribed;

        [NonSerialized]
        private Coroutine _activeAnimation;

        [NonSerialized]
        private Image[] _cachedSlotImages;

        [NonSerialized]
        private int _slotFillIndex;

        [NonSerialized]
        private bool _slotsInitialized;

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

            if (displayMode == DisplayMode.Slots)
            {
                // Only initialize slots on the FIRST execution — subsequent executions
                // (e.g. if the node is re-triggered per question) must NOT reset the fill index.
                if (!_slotsInitialized)
                {
                    ResolveSlotImages();
                    InitializeSlotsToDefault();
                    _slotsInitialized = true;
                }
                SubscribeToAnswerEventsIfNeeded();
                Complete();
                return;
            }

            SubscribeToScoreEventsIfNeeded();
            ApplyCurrentValueToTarget(onComplete: Complete);
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

        private void ApplyCurrentValueToTarget(Action onComplete = null)
        {
            GameObject targetGo = ResolveTarget();
            if (targetGo == null)
            {
                Debug.LogWarning("[ScoreProgressBarNode] No target: assign one by drag-and-drop or set target path.");
                onComplete?.Invoke();
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
                ApplyToSlider(slider, fill, onComplete);
                return;
            }

            var image = targetGo.GetComponent<Image>();
            if (image != null && image.type == Image.Type.Filled)
            {
                ApplyToImage(image, fill, onComplete);
                return;
            }

            Debug.LogWarning($"[ScoreProgressBarNode] No Slider or filled Image on: {targetGo.name}");
            onComplete?.Invoke();
        }

        private void ApplyToSlider(Slider slider, float normalizedFill, Action onComplete)
        {
            if (slider == null)
            {
                onComplete?.Invoke();
                return;
            }

            float from = Mathf.Clamp01(slider.normalizedValue);
            if (animateFill && animationDuration > 0f && Runner != null)
            {
                StopActiveAnimation();
                _activeAnimation = Runner.StartCoroutine(AnimateToFill(
                    fromValue: from,
                    toValue: normalizedFill,
                    duration: animationDuration,
                    onUpdate: v => slider.normalizedValue = v,
                    onComplete: onComplete));
            }
            else
            {
                slider.normalizedValue = normalizedFill;
                onComplete?.Invoke();
            }
        }

        private void ApplyToImage(Image image, float normalizedFill, Action onComplete)
        {
            if (image == null)
            {
                onComplete?.Invoke();
                return;
            }

            float from = Mathf.Clamp01(image.fillAmount);
            if (animateFill && animationDuration > 0f && Runner != null)
            {
                StopActiveAnimation();
                _activeAnimation = Runner.StartCoroutine(AnimateToFill(
                    fromValue: from,
                    toValue: normalizedFill,
                    duration: animationDuration,
                    onUpdate: v => image.fillAmount = v,
                    onComplete: onComplete));
            }
            else
            {
                image.fillAmount = normalizedFill;
                onComplete?.Invoke();
            }
        }

        // === Slots Logic ===

        private void ResolveSlotImages()
        {
            GameObject targetGo = ResolveTarget();
            if (targetGo == null)
            {
                Debug.LogWarning("[ScoreProgressBarNode] No target for slots: assign a parent GameObject.");
                _cachedSlotImages = Array.Empty<Image>();
                return;
            }

            var list = new List<Image>();
            for (int i = 0; i < targetGo.transform.childCount; i++)
            {
                var img = targetGo.transform.GetChild(i).GetComponent<Image>();
                if (img != null)
                    list.Add(img);
            }
            _cachedSlotImages = list.ToArray();

            if (_cachedSlotImages.Length == 0)
                Debug.LogWarning($"[ScoreProgressBarNode] No child Images found under: {targetGo.name}");
            else
                Debug.Log($"[ScoreProgressBarNode] Resolved {_cachedSlotImages.Length} slot images under: {targetGo.name}");
        }

        private void InitializeSlotsToDefault()
        {
            if (_cachedSlotImages == null) return;
            _slotFillIndex = 0;

            foreach (var img in _cachedSlotImages)
            {
                if (img == null) continue;
                img.color = slotDefaultColor;
                if (slotDefaultSprite != null)
                    img.sprite = slotDefaultSprite;
                img.transform.localScale = Vector3.one;
            }
        }

        private void SubscribeToAnswerEventsIfNeeded()
        {
            if (_isAnswerEventSubscribed) return;
            QuizState.OnStepResult += OnAnswerResult;
            _isAnswerEventSubscribed = true;
            Debug.Log("[ScoreProgressBarNode] Subscribed to OnStepResult");
        }

        private void OnAnswerResult(bool wasCorrect)
        {
            Debug.Log($"[ScoreProgressBarNode] OnAnswerResult called: wasCorrect={wasCorrect}, slotIndex={_slotFillIndex}, Runner={Runner != null}, IsRunning={Runner?.IsRunning}, slotCount={_cachedSlotImages?.Length ?? -1}");

            if (Runner == null || !Runner.IsRunning) return;
            if (_cachedSlotImages == null || _slotFillIndex >= _cachedSlotImages.Length) return;

            var slot = _cachedSlotImages[_slotFillIndex];
            if (slot == null) { _slotFillIndex++; return; }

            slot.color = wasCorrect ? slotCorrectColor : slotWrongColor;

            // Apply the correct sprite — use the wrong sprite even if it's the same as default
            if (wasCorrect && slotCorrectSprite != null)
                slot.sprite = slotCorrectSprite;
            else if (!wasCorrect && slotWrongSprite != null)
                slot.sprite = slotWrongSprite;

            if (slotAnimateOnFill && slotAnimationDuration > 0f && Runner != null)
                Runner.StartCoroutine(AnimateSlotPop(slot.transform, slotAnimationDuration));

            Debug.Log($"[ScoreProgressBarNode] Filled slot {_slotFillIndex} with {(wasCorrect ? "Correct" : "Wrong")} color, advancing to {_slotFillIndex + 1}");
            _slotFillIndex++;
        }


        private IEnumerator AnimateSlotPop(Transform slotTransform, float duration)
        {
            float half = duration * 0.5f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float scale = Mathf.Lerp(1f, 1.3f, t);
                slotTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            // Scale back down
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / half);
                float scale = Mathf.Lerp(1.3f, 1f, t);
                slotTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            slotTransform.localScale = Vector3.one;
        }

        // === Slider / FilledImage Logic ===

        private void SubscribeToScoreEventsIfNeeded()
        {
            if (_isScoreEventSubscribed) return;
            if (!liveUpdateFromScoreEvents) return;
            if (valueSource != ValueSource.QuizScore) return;

            QuizState.OnScoreChanged += OnQuizScoreChanged;
            _isScoreEventSubscribed = true;
        }

        private void OnQuizScoreChanged(int _)
        {
            if (Runner == null || !Runner.IsRunning) return;
            ApplyCurrentValueToTarget();
        }

        private void StopActiveAnimation()
        {
            if (_activeAnimation != null && Runner != null)
            {
                Runner.StopCoroutine(_activeAnimation);
                _activeAnimation = null;
            }
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

        public override void Reset()
        {
            base.Reset();
            StopActiveAnimation();

            if (_isScoreEventSubscribed)
            {
                QuizState.OnScoreChanged -= OnQuizScoreChanged;
                _isScoreEventSubscribed = false;
            }

            // NOTE: We intentionally do NOT unsubscribe from OnStepResult,
            // do NOT clear _cachedSlotImages, and do NOT reset _slotFillIndex here.
            // The graph runner calls Reset() before re-executing a completed node.
            // For Slots mode, the fill index and cached images must survive across
            // re-executions so subsequent step results fill the NEXT slot, not slot 0.
        }
    }
}
