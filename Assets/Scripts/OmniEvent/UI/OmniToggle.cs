using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OmniEvent.UI
{
    /// <summary>
    /// Enhanced Toggle component with multi-argument OmniEvent support.
    /// Replaces standard Unity Toggle for use with the OmniEvent system.
    /// Note: This version does not support ToggleGroup. For grouped toggles, use Unity's standard Toggle.
    /// </summary>
    [AddComponentMenu("OmniEvent/UI/OmniToggle")]
    [RequireComponent(typeof(RectTransform))]
    public class OmniToggle : Selectable, IPointerClickHandler, ISubmitHandler, ICanvasElement
    {
        [Header("Toggle Configuration")]
        [SerializeField] private Graphic m_Graphic;
        [SerializeField] private bool m_IsOn = true;

        [Header("OmniEvent Configuration")]
        [Tooltip("Event triggered when toggle state changes (passes bool state)")]
        public OmniEvent<bool> onValueChanged = new OmniEvent<bool>();

        [Tooltip("Event triggered with current and previous states")]
        public OmniEvent<bool, bool> onValueChangedWithPrevious = new OmniEvent<bool, bool>();

        [Tooltip("Event triggered with toggle ID, current state, and previous state")]
        public OmniEvent<string, bool, bool> onValueChangedWithID = new OmniEvent<string, bool, bool>();

        [Header("Toggle Settings")]
        [Tooltip("Custom identifier for this toggle")]
        public string toggleID = "";

        public enum ToggleTransition
        {
            None,
            Fade
        }

        [SerializeField]
        private ToggleTransition toggleTransition = ToggleTransition.Fade;

        private bool m_PreviousValue;

        public Graphic graphic { get { return m_Graphic; } set { m_Graphic = value; } }

        public bool isOn
        {
            get { return m_IsOn; }
            set { Set(value); }
        }

        protected OmniToggle()
        { }

        protected override void Awake()
        {
            base.Awake();
            m_PreviousValue = m_IsOn;
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            Set(m_IsOn, false);
            PlayEffect(true);

            var prefabType = UnityEditor.PrefabUtility.GetPrefabAssetType(this);
            if (prefabType != UnityEditor.PrefabAssetType.Regular && !Application.isPlaying)
                CanvasUpdateRegistry.RegisterCanvasElementForLayoutRebuild(this);
        }
#endif

        public virtual void Rebuild(CanvasUpdate executing)
        {
#if UNITY_EDITOR
            if (executing == CanvasUpdate.Prelayout)
                onValueChanged?.Invoke(m_IsOn);
#endif
        }

        public virtual void LayoutComplete()
        { }

        public virtual void GraphicUpdateComplete()
        { }

        protected override void OnEnable()
        {
            base.OnEnable();
            PlayEffect(true);
        }

        protected override void OnDidApplyAnimationProperties()
        {
            if (graphic != null)
            {
                bool oldValue = !Mathf.Approximately(graphic.canvasRenderer.GetColor().a, 0);
                if (m_IsOn != oldValue)
                {
                    m_IsOn = oldValue;
                    Set(!oldValue);
                }
            }

            base.OnDidApplyAnimationProperties();
        }

        private void Set(bool value, bool sendCallback = true)
        {
            if (m_IsOn == value)
                return;

            m_PreviousValue = m_IsOn;
            m_IsOn = value;

            PlayEffect(toggleTransition == ToggleTransition.None);

            if (sendCallback)
            {
                InvokeEvents();
            }
        }

        private void InvokeEvents()
        {
            onValueChanged?.Invoke(m_IsOn);
            onValueChangedWithPrevious?.Invoke(m_IsOn, m_PreviousValue);
            
            string id = string.IsNullOrEmpty(toggleID) ? gameObject.name : toggleID;
            onValueChangedWithID?.Invoke(id, m_IsOn, m_PreviousValue);
        }

        private void PlayEffect(bool instant)
        {
            if (graphic == null)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                graphic.canvasRenderer.SetAlpha(m_IsOn ? 1f : 0f);
            else
#endif
                graphic.CrossFadeAlpha(m_IsOn ? 1f : 0f, instant ? 0f : 0.1f, true);
        }

        protected override void Start()
        {
            PlayEffect(true);
        }

        private void InternalToggle()
        {
            if (!IsActive() || !IsInteractable())
                return;

            isOn = !isOn;
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            InternalToggle();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            InternalToggle();
        }

        /// <summary>
        /// Set the toggle state without triggering callbacks.
        /// </summary>
        public void SetIsOnWithoutNotify(bool value)
        {
            Set(value, false);
        }
    }
}
