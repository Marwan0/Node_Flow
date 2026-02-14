using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OmniEvent.UI
{
    /// <summary>
    /// Enhanced InputField component with multi-argument OmniEvent support.
    /// Replaces standard Unity InputField for use with the OmniEvent system.
    /// </summary>
    [AddComponentMenu("OmniEvent/UI/OmniInputField")]
    public class OmniInputField : InputField
    {
        [Header("OmniEvent Configuration")]
        [Tooltip("Event triggered when text changes (passes string text)")]
        public OmniEvent<string> onTextChanged = new OmniEvent<string>();

        [Tooltip("Event triggered with field ID and text")]
        public OmniEvent<string, string> onTextChangedWithID = new OmniEvent<string, string>();

        [Tooltip("Event triggered when editing ends (passes string text)")]
        public OmniEvent<string> onEndEdit = new OmniEvent<string>();

        [Tooltip("Event triggered when editing ends with field ID and text")]
        public OmniEvent<string, string> onEndEditWithID = new OmniEvent<string, string>();

        [Header("InputField Settings")]
        [Tooltip("Custom identifier for this input field")]
        public string fieldID = "";

        protected override void Awake()
        {
            base.Awake();
            
            // Hook into the base InputField events
            onValueChanged.AddListener(HandleValueChanged);
            base.onEndEdit.AddListener(HandleEndEdit);
        }

        protected override void OnDestroy()
        {
            onValueChanged.RemoveListener(HandleValueChanged);
            base.onEndEdit.RemoveListener(HandleEndEdit);
            base.OnDestroy();
        }

        private void HandleValueChanged(string newText)
        {
            onTextChanged?.Invoke(newText);
            
            string id = string.IsNullOrEmpty(fieldID) ? gameObject.name : fieldID;
            onTextChangedWithID?.Invoke(id, newText);
        }

        private void HandleEndEdit(string finalText)
        {
            onEndEdit?.Invoke(finalText);
            
            string id = string.IsNullOrEmpty(fieldID) ? gameObject.name : fieldID;
            onEndEditWithID?.Invoke(id, finalText);
        }

        /// <summary>
        /// Set the text without triggering callbacks.
        /// </summary>
        public void SetTextWithoutNotify(string input)
        {
            SetTextWithoutNotify(input);
        }
    }
}
