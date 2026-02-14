using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OmniEvent.UI
{
    /// <summary>
    /// Enhanced Slider component with multi-argument OmniEvent support.
    /// Replaces standard Unity Slider for use with the OmniEvent system.
    /// </summary>
    [AddComponentMenu("OmniEvent/UI/OmniSlider")]
    [RequireComponent(typeof(RectTransform))]
    public class OmniSlider : Selectable, IDragHandler, IInitializePotentialDragHandler, ICanvasElement
    {
        [Header("Slider Configuration")]
        [SerializeField] private RectTransform m_FillRect;
        [SerializeField] private RectTransform m_HandleRect;
        [SerializeField] private Direction m_Direction = Direction.LeftToRight;
        [SerializeField] private float m_MinValue = 0;
        [SerializeField] private float m_MaxValue = 1;
        [SerializeField] private bool m_WholeNumbers = false;
        [SerializeField] private float m_Value = 0;

        [Header("OmniEvent Configuration")]
        [Tooltip("Event triggered when value changes (passes float value)")]
        public OmniEvent<float> onValueChanged = new OmniEvent<float>();

        [Tooltip("Event triggered with value and normalized value (0-1)")]
        public OmniEvent<float, float> onValueChangedWithNormalized = new OmniEvent<float, float>();

        [Tooltip("Event triggered with slider ID, value, and normalized value")]
        public OmniEvent<string, float, float> onValueChangedWithID = new OmniEvent<string, float, float>();

        [Header("Slider Settings")]
        [Tooltip("Custom identifier for this slider")]
        public string sliderID = "";

        public enum Direction
        {
            LeftToRight,
            RightToLeft,
            BottomToTop,
            TopToBottom,
        }

        // Properties
        public RectTransform fillRect { get { return m_FillRect; } set { m_FillRect = value; UpdateVisuals(); } }
        public RectTransform handleRect { get { return m_HandleRect; } set { m_HandleRect = value; UpdateVisuals(); } }
        public Direction direction { get { return m_Direction; } set { m_Direction = value; UpdateVisuals(); } }
        public float minValue { get { return m_MinValue; } set { m_MinValue = value; Set(m_Value); } }
        public float maxValue { get { return m_MaxValue; } set { m_MaxValue = value; Set(m_Value); } }
        public bool wholeNumbers { get { return m_WholeNumbers; } set { m_WholeNumbers = value; Set(m_Value); } }

        public float value
        {
            get { return m_Value; }
            set { Set(value); }
        }

        public float normalizedValue
        {
            get
            {
                if (Mathf.Approximately(minValue, maxValue))
                    return 0;
                return Mathf.InverseLerp(minValue, maxValue, value);
            }
            set
            {
                this.value = Mathf.Lerp(minValue, maxValue, value);
            }
        }

        private DrivenRectTransformTracker m_Tracker;
        private bool m_DelayedUpdateVisuals = false;

        protected OmniSlider()
        { }

        protected override void OnEnable()
        {
            base.OnEnable();
            Set(m_Value, false);
            UpdateVisuals();
        }

        protected override void OnDisable()
        {
            m_Tracker.Clear();
            base.OnDisable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (wholeNumbers)
            {
                m_MinValue = Mathf.Round(m_MinValue);
                m_MaxValue = Mathf.Round(m_MaxValue);
            }

            if (IsActive())
            {
                UpdateVisuals();
                Set(m_Value, false);
            }

            var prefabType = UnityEditor.PrefabUtility.GetPrefabAssetType(this);
            if (prefabType != UnityEditor.PrefabAssetType.Regular && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }
#endif

        public void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged?.Invoke(value);
#endif
        }

        public void LayoutComplete()
        { }

        public void GraphicUpdateComplete()
        { }

        protected void Set(float input, bool sendCallback = true)
        {
            float newValue = ClampValue(input);
            if (m_Value == newValue)
                return;

            m_Value = newValue;
            UpdateVisuals();
            
            if (sendCallback)
            {
                InvokeEvents();
            }
        }

        private void InvokeEvents()
        {
            float normalized = normalizedValue;
            
            onValueChanged?.Invoke(m_Value);
            onValueChangedWithNormalized?.Invoke(m_Value, normalized);
            
            string id = string.IsNullOrEmpty(sliderID) ? gameObject.name : sliderID;
            onValueChangedWithID?.Invoke(id, m_Value, normalized);
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            if (!IsActive())
                return;

            UpdateVisuals();
        }

        private float ClampValue(float input)
        {
            float newValue = Mathf.Clamp(input, minValue, maxValue);
            if (wholeNumbers)
                newValue = Mathf.Round(newValue);
            return newValue;
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            if (!MayDrag(eventData))
                return;
            UpdateDrag(eventData, eventData.pressEventCamera);
        }

        public virtual void OnInitializePotentialDrag(PointerEventData eventData)
        {
            eventData.useDragThreshold = false;
        }

        private void UpdateDrag(PointerEventData eventData, Camera cam)
        {
            RectTransform clickRect = m_HandleRect ?? m_FillRect;
            if (clickRect != null && clickRect.rect.size.x > 0)
            {
                Vector2 localCursor;
                if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(clickRect, eventData.position, cam, out localCursor))
                    return;
                
                localCursor -= clickRect.rect.position;

                float val = Mathf.Clamp01((localCursor - Vector2.zero).x / clickRect.rect.size.x);
                normalizedValue = (m_Direction == Direction.LeftToRight || m_Direction == Direction.BottomToTop) ? val : 1f - val;
            }
        }

        private bool MayDrag(PointerEventData eventData)
        {
            return IsActive() && IsInteractable() && eventData.button == PointerEventData.InputButton.Left;
        }

        private void UpdateVisuals()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                UpdateCachedReferences();
#endif

            m_Tracker.Clear();

            if (m_FillRect != null)
            {
                m_Tracker.Add(this, m_FillRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                if (m_Direction == Direction.LeftToRight)
                    anchorMax.x = normalizedValue;
                else if (m_Direction == Direction.RightToLeft)
                    anchorMin.x = 1f - normalizedValue;
                else if (m_Direction == Direction.BottomToTop)
                    anchorMax.y = normalizedValue;
                else
                    anchorMin.y = 1f - normalizedValue;

                m_FillRect.anchorMin = anchorMin;
                m_FillRect.anchorMax = anchorMax;
            }

            if (m_HandleRect != null)
            {
                m_Tracker.Add(this, m_HandleRect, DrivenTransformProperties.Anchors);
                Vector2 anchorMin = Vector2.zero;
                Vector2 anchorMax = Vector2.one;

                if (m_Direction == Direction.LeftToRight || m_Direction == Direction.RightToLeft)
                {
                    anchorMin.x = normalizedValue;
                    anchorMax.x = normalizedValue;
                }
                else
                {
                    anchorMin.y = normalizedValue;
                    anchorMax.y = normalizedValue;
                }

                m_HandleRect.anchorMin = anchorMin;
                m_HandleRect.anchorMax = anchorMax;
            }
        }

        private void UpdateCachedReferences()
        {
            if (m_FillRect && !m_FillRect.gameObject.activeInHierarchy)
                m_FillRect = null;
            if (m_HandleRect && !m_HandleRect.gameObject.activeInHierarchy)
                m_HandleRect = null;
        }

        /// <summary>
        /// Set the value without triggering callbacks.
        /// </summary>
        public void SetValueWithoutNotify(float input)
        {
            Set(input, false);
        }
    }
}
