using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OmniEvent.UI
{
    /// <summary>
    /// Enhanced Dropdown component with multi-argument OmniEvent support.
    /// Replaces standard Unity Dropdown for use with the OmniEvent system.
    /// </summary>
    [AddComponentMenu("OmniEvent/UI/OmniDropdown")]
    [RequireComponent(typeof(RectTransform))]
    public class OmniDropdown : Selectable, IPointerClickHandler, ISubmitHandler, ICancelHandler
    {
        [Header("Dropdown Configuration")]
        [SerializeField] private RectTransform m_Template;
        [SerializeField] private Text m_CaptionText;
        [SerializeField] private Image m_CaptionImage;
        [SerializeField] private Text m_ItemText;
        [SerializeField] private Image m_ItemImage;
        [SerializeField] private int m_Value;
        [SerializeField] private List<OptionData> m_Options = new List<OptionData>();

        [Header("OmniEvent Configuration")]
        [Tooltip("Event triggered when selection changes (passes int index)")]
        public OmniEvent<int> onValueChanged = new OmniEvent<int>();

        [Tooltip("Event triggered with index and selected text")]
        public OmniEvent<int, string> onValueChangedWithText = new OmniEvent<int, string>();

        [Tooltip("Event triggered with dropdown ID, index, and selected text")]
        public OmniEvent<string, int, string> onValueChangedWithID = new OmniEvent<string, int, string>();

        [Header("Dropdown Settings")]
        [Tooltip("Custom identifier for this dropdown")]
        public string dropdownID = "";

        [System.Serializable]
        public class OptionData
        {
            [SerializeField] private string m_Text;
            [SerializeField] private Sprite m_Image;

            public string text { get { return m_Text; } set { m_Text = value; } }
            public Sprite image { get { return m_Image; } set { m_Image = value; } }

            public OptionData() { }
            public OptionData(string text) { m_Text = text; }
            public OptionData(Sprite image) { m_Image = image; }
            public OptionData(string text, Sprite image) { m_Text = text; m_Image = image; }
        }

        public RectTransform template { get { return m_Template; } set { m_Template = value; RefreshShownValue(); } }
        public Text captionText { get { return m_CaptionText; } set { m_CaptionText = value; RefreshShownValue(); } }
        public Image captionImage { get { return m_CaptionImage; } set { m_CaptionImage = value; RefreshShownValue(); } }
        public Text itemText { get { return m_ItemText; } set { m_ItemText = value; RefreshShownValue(); } }
        public Image itemImage { get { return m_ItemImage; } set { m_ItemImage = value; RefreshShownValue(); } }
        public List<OptionData> options { get { return m_Options; } set { m_Options = value; RefreshShownValue(); } }

        public int value
        {
            get { return m_Value; }
            set { SetValue(value); }
        }

        protected OmniDropdown()
        { }

        protected override void Awake()
        {
            base.Awake();
            if (m_Template)
                m_Template.gameObject.SetActive(false);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            if (!IsActive())
                return;

            RefreshShownValue();
        }
#endif

        public void RefreshShownValue()
        {
            if (m_Options.Count == 0)
                return;

            int safeValue = Mathf.Clamp(m_Value, 0, m_Options.Count - 1);
            OptionData data = m_Options[safeValue];

            if (m_CaptionText)
            {
                if (data != null && data.text != null)
                    m_CaptionText.text = data.text;
                else
                    m_CaptionText.text = "";
            }

            if (m_CaptionImage)
            {
                if (data != null)
                    m_CaptionImage.sprite = data.image;
                else
                    m_CaptionImage.sprite = null;
                m_CaptionImage.enabled = (m_CaptionImage.sprite != null);
            }
        }

        public void AddOptions(List<OptionData> options)
        {
            m_Options.AddRange(options);
            RefreshShownValue();
        }

        public void AddOptions(List<string> options)
        {
            for (int i = 0; i < options.Count; i++)
                m_Options.Add(new OptionData(options[i]));
            RefreshShownValue();
        }

        public void AddOptions(List<Sprite> options)
        {
            for (int i = 0; i < options.Count; i++)
                m_Options.Add(new OptionData(options[i]));
            RefreshShownValue();
        }

        public void ClearOptions()
        {
            m_Options.Clear();
            RefreshShownValue();
        }

        private void SetValue(int value, bool sendCallback = true)
        {
            if (Application.isPlaying && (value == m_Value || m_Options.Count == 0))
                return;

            m_Value = Mathf.Clamp(value, 0, m_Options.Count - 1);
            RefreshShownValue();

            if (sendCallback)
            {
                InvokeEvents();
            }
        }

        private void InvokeEvents()
        {
            string selectedText = m_Options.Count > 0 && m_Value >= 0 && m_Value < m_Options.Count 
                ? m_Options[m_Value].text 
                : "";

            onValueChanged?.Invoke(m_Value);
            onValueChangedWithText?.Invoke(m_Value, selectedText);
            
            string id = string.IsNullOrEmpty(dropdownID) ? gameObject.name : dropdownID;
            onValueChangedWithID?.Invoke(id, m_Value, selectedText);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Show();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Show();
        }

        public void OnCancel(BaseEventData eventData)
        {
            Hide();
        }

        public void Show()
        {
            if (!IsActive() || !IsInteractable() || m_Template == null)
                return;

            // This is a simplified version - full dropdown implementation would require
            // creating the dropdown list dynamically
            Debug.Log($"OmniDropdown: Show dropdown (simplified implementation)");
        }

        public void Hide()
        {
            if (m_Template != null)
                m_Template.gameObject.SetActive(false);
        }

        /// <summary>
        /// Set the value without triggering callbacks.
        /// </summary>
        public void SetValueWithoutNotify(int input)
        {
            SetValue(input, false);
        }
    }
}
