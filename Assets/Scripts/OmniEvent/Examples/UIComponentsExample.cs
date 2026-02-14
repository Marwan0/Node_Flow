using UnityEngine;
using OmniEvent;
using OmniEvent.UI;

namespace OmniEvent.Examples
{
    /// <summary>
    /// Example demonstrating how to use UI 2.0 components (OmniButton, OmniSlider, etc.)
    /// with the OmniEvent system for efficient event handling.
    /// </summary>
    public class UIComponentsExample : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Reference to an OmniButton in the scene")]
        public OmniButton exampleButton;
        
        [Tooltip("Reference to an OmniSlider in the scene")]
        public OmniSlider exampleSlider;
        
        [Tooltip("Reference to an OmniToggle in the scene")]
        public OmniToggle exampleToggle;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Transform targetTransform;

        private void Start()
        {
            // You can also subscribe to events programmatically
            if (exampleButton != null)
            {
                exampleButton.onClick.AddListener(OnButtonClicked);
                exampleButton.onClickWithPosition.AddListener(OnButtonClickedWithPosition);
            }

            if (exampleSlider != null)
            {
                exampleSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            if (exampleToggle != null)
            {
                exampleToggle.onValueChanged.AddListener(OnToggleValueChanged);
            }
        }

        private void OnDestroy()
        {
            // Clean up listeners
            if (exampleButton != null)
            {
                exampleButton.onClick.RemoveListener(OnButtonClicked);
                exampleButton.onClickWithPosition.RemoveListener(OnButtonClickedWithPosition);
            }

            if (exampleSlider != null)
            {
                exampleSlider.onValueChanged.RemoveListener(OnSliderValueChanged);
            }

            if (exampleToggle != null)
            {
                exampleToggle.onValueChanged.RemoveListener(OnToggleValueChanged);
            }
        }

        // ==================== Button Handlers ====================

        public void OnButtonClicked()
        {
            Debug.Log("[UIComponents] Button clicked!");
        }

        public void OnButtonClickedWithPosition(Vector2 position)
        {
            Debug.Log($"[UIComponents] Button clicked at position: {position}");
        }

        public void OnButtonClickedWithNameAndPosition(string buttonName, Vector2 position)
        {
            Debug.Log($"[UIComponents] Button '{buttonName}' clicked at {position}");
        }

        // ==================== Slider Handlers ====================

        public void OnSliderValueChanged(float value)
        {
            Debug.Log($"[UIComponents] Slider value: {value}");
            
            // Example: Scale an object based on slider value
            if (targetTransform != null)
            {
                float scale = Mathf.Lerp(0.5f, 2f, value);
                targetTransform.localScale = Vector3.one * scale;
            }
        }

        public void OnSliderValueChangedWithNormalized(float value, float normalized)
        {
            Debug.Log($"[UIComponents] Slider value: {value}, Normalized: {normalized:F2}");
        }

        public void OnSliderValueChangedWithID(string sliderID, float value, float normalized)
        {
            Debug.Log($"[UIComponents] Slider '{sliderID}': value={value}, normalized={normalized:F2}");
        }

        // ==================== Toggle Handlers ====================

        public void OnToggleValueChanged(bool isOn)
        {
            Debug.Log($"[UIComponents] Toggle is now: {(isOn ? "ON" : "OFF")}");
            
            // Example: Enable/disable a renderer
            if (targetRenderer != null)
            {
                targetRenderer.enabled = isOn;
            }
        }

        public void OnToggleValueChangedWithPrevious(bool current, bool previous)
        {
            Debug.Log($"[UIComponents] Toggle changed from {previous} to {current}");
        }

        public void OnToggleValueChangedWithID(string toggleID, bool current, bool previous)
        {
            Debug.Log($"[UIComponents] Toggle '{toggleID}' changed from {previous} to {current}");
        }

        // ==================== Advanced: Multi-Parameter Handler ====================

        /// <summary>
        /// Example of a method that receives Color and List parameters from an OmniButton.
        /// This demonstrates how OmniButton can trigger events with complex data types.
        /// </summary>
        public void HandleButtonWithComplexData(Color buttonColor, string buttonLabel)
        {
            Debug.Log($"[UIComponents] Button '{buttonLabel}' triggered with color: {buttonColor}");
            
            if (targetRenderer != null && targetRenderer.material != null)
            {
                targetRenderer.material.color = buttonColor;
            }
        }
    }
}

/* 
 * ============================================================================
 * INSPECTOR SETUP GUIDE FOR UI COMPONENTS
 * ============================================================================
 * 
 * SETUP 1: OmniButton Configuration
 * ----------------------------------
 * 1. Create a Canvas (GameObject > UI > Canvas)
 * 2. Add an OmniButton (Component > OmniEvent > UI > OmniButton)
 * 3. In the OmniButton Inspector:
 *    a) On Click event:
 *       - Add listener → Select this GameObject → UIComponentsExample.OnButtonClicked
 *    b) On Click With Position event:
 *       - Add listener → Select this GameObject → UIComponentsExample.OnButtonClickedWithPosition
 *    c) On Click With Name And Position event:
 *       - Add listener → Select this GameObject → UIComponentsExample.OnButtonClickedWithNameAndPosition
 *       - The button will automatically pass its name and click position
 * 
 * SETUP 2: OmniSlider Configuration
 * ----------------------------------
 * 1. Add an OmniSlider to the Canvas
 * 2. Configure the slider's min/max values (e.g., 0 to 100)
 * 3. In the OmniSlider Inspector:
 *    a) On Value Changed event:
 *       - Add listener → Select this GameObject → UIComponentsExample.OnSliderValueChanged
 *    b) On Value Changed With Normalized event:
 *       - Add listener → Select this GameObject → UIComponentsExample.OnSliderValueChangedWithNormalized
 *    c) Set Slider ID to "VolumeSlider" (or any identifier)
 *    d) On Value Changed With ID event:
 *       - Add listener → Select this GameObject → UIComponentsExample.OnSliderValueChangedWithID
 * 
 * SETUP 3: OmniToggle Configuration
 * ----------------------------------
 * 1. Add an OmniToggle to the Canvas
 * 2. In the OmniToggle Inspector:
 *    a) On Value Changed event:
 *       - Add listener → Select this GameObject → UIComponentsExample.OnToggleValueChanged
 *    b) Set Toggle ID to "SoundToggle"
 *    c) On Value Changed With ID event:
 *       - Add listener → Select this GameObject → UIComponentsExample.OnToggleValueChangedWithID
 * 
 * SETUP 4: Advanced - Button with Complex Data
 * ---------------------------------------------
 * 1. Add another OmniButton
 * 2. Create a custom OmniEvent<Color, string> on another component
 * 3. Configure the button to trigger that event
 * 4. Add a listener to call HandleButtonWithComplexData
 * 5. Set static values:
 *    - Color: Choose from color picker (e.g., Red)
 *    - String: Type a label (e.g., "Fire Button")
 * 
 * TESTING:
 * --------
 * 1. Enter Play mode
 * 2. Click the OmniButton → Check Console for "Button clicked!"
 * 3. Move the OmniSlider → Watch the target object scale
 * 4. Toggle the OmniToggle → Watch the target renderer enable/disable
 * 5. All events should log to the Console with their parameters
 * 
 * ============================================================================
 */
