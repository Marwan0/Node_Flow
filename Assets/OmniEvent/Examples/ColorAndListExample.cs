using System.Collections.Generic;
using UnityEngine;
using OmniEvent;

namespace OmniEvent.Examples
{
    /// <summary>
    /// Example demonstrating OmniEvent with Color and List<string> parameters.
    /// This shows how to configure OmniEvents in the Inspector to pass both static and dynamic data.
    /// </summary>
    public class ColorAndListExample : MonoBehaviour
    {
        [Header("OmniEvent Configuration")]
        [Tooltip("Event that passes a Color and List of strings")]
        public OmniEvent<Color, List<string>> onColorAndListEvent = new OmniEvent<Color, List<string>>();

        [Header("Test Data")]
        [SerializeField] private Color testColor = Color.red;
        [SerializeField] private List<string> testList = new List<string> { "Item1", "Item2", "Item3" };

        [Header("Test Button")]
        [Tooltip("Press this button in the Inspector to test the event")]
        [SerializeField] private bool triggerEvent = false;

        private void OnValidate()
        {
            if (triggerEvent)
            {
                triggerEvent = false;
                if (Application.isPlaying)
                {
                    TriggerTestEvent();
                }
            }
        }

        /// <summary>
        /// Trigger the event with test data.
        /// Call this from an OmniButton or other UI component.
        /// </summary>
        public void TriggerTestEvent()
        {
            Debug.Log($"[ColorAndListExample] Triggering event with Color: {testColor} and {testList.Count} items");
            onColorAndListEvent?.Invoke(testColor, testList);
        }

        /// <summary>
        /// Example method that receives Color and List<string> parameters.
        /// Configure this in the Inspector as a listener to the OmniEvent.
        /// </summary>
        public void HandleColorAndList(Color color, List<string> items)
        {
            Debug.Log($"[ColorAndListExample] Received Color: {color}");
            Debug.Log($"[ColorAndListExample] Received {items.Count} items:");
            
            for (int i = 0; i < items.Count; i++)
            {
                Debug.Log($"  [{i}] {items[i]}");
            }

            // Example: Apply the color to a material
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.color = color;
                Debug.Log($"[ColorAndListExample] Applied color to material");
            }
        }

        /// <summary>
        /// Example method with only Color parameter (for testing single parameter events).
        /// </summary>
        public void HandleColorOnly(Color color)
        {
            Debug.Log($"[ColorAndListExample] Received Color only: {color}");
        }

        /// <summary>
        /// Example method with only List parameter (for testing single parameter events).
        /// </summary>
        public void HandleListOnly(List<string> items)
        {
            Debug.Log($"[ColorAndListExample] Received List only with {items.Count} items");
        }
    }
}

/* 
 * ============================================================================
 * INSPECTOR SETUP GUIDE
 * ============================================================================
 * 
 * 1. ADD THE COMPONENT:
 *    - Create a GameObject in your scene
 *    - Add the ColorAndListExample component
 * 
 * 2. CONFIGURE THE OMNIEVENT (Static Data):
 *    - In the Inspector, find the "On Color And List Event" field
 *    - Click the "+" button to add a listener
 *    - Drag the same GameObject into the object field
 *    - Select "ColorAndListExample > HandleColorAndList" from the dropdown
 *    - You'll see two parameter fields:
 *      • Color parameter: Use the color picker to set a static color (e.g., Blue)
 *      • List<string> parameter: Click to expand and add items
 *        - Set Size to 3
 *        - Element 0: "Static Item 1"
 *        - Element 1: "Static Item 2"
 *        - Element 2: "Static Item 3"
 * 
 * 3. CONFIGURE WITH DYNAMIC DATA:
 *    - Add another listener to the same event
 *    - For the Color parameter, instead of using the color picker:
 *      • Click the small circle icon next to the color field
 *      • Select a GameObject with a Renderer component
 *      • Choose "Renderer > material > color" to pass the material's color dynamically
 *    - For the List parameter, you can reference a list from another component
 * 
 * 4. TRIGGER WITH OMNIBUTTON:
 *    - Create a Canvas with an OmniButton (Component > OmniEvent > UI > OmniButton)
 *    - In the OmniButton's "On Click" event:
 *      • Add a listener
 *      • Drag the ColorAndListExample GameObject
 *      • Select "ColorAndListExample > TriggerTestEvent"
 *    - Enter Play mode and click the button
 * 
 * 5. TEST:
 *    - Enter Play mode
 *    - Click the OmniButton or check the "Trigger Event" checkbox
 *    - Check the Console for output showing the received Color and List items
 * 
 * ============================================================================
 */
