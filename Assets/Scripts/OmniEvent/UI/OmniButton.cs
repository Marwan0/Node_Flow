using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace OmniEvent.UI
{
    /// <summary>
    /// Enhanced Button component with multi-argument OmniEvent support.
    /// Replaces standard Unity Button for use with the OmniEvent system.
    /// </summary>
    [AddComponentMenu("OmniEvent/UI/OmniButton")]
    [RequireComponent(typeof(Image))]
    public class OmniButton : Selectable, IPointerClickHandler, ISubmitHandler
    {
        [Header("OmniEvent Configuration")]
        [Tooltip("Event triggered when button is clicked (no parameters)")]
        public OmniEvent onClick = new OmniEvent();

        [Tooltip("Event triggered with click position in screen space")]
        public OmniEvent<Vector2> onClickWithPosition = new OmniEvent<Vector2>();

        [Tooltip("Event triggered with button name and click position")]
        public OmniEvent<string, Vector2> onClickWithNameAndPosition = new OmniEvent<string, Vector2>();

        [Header("Button Settings")]
        [Tooltip("Custom identifier for this button")]
        public string buttonID = "";

        private void Press()
        {
            if (!IsActive() || !IsInteractable())
                return;

            // Invoke all configured events
            onClick?.Invoke();
            
            // Get click position (use center of button if not from pointer)
            Vector2 clickPosition = transform.position;
            onClickWithPosition?.Invoke(clickPosition);
            
            // Pass button ID and position
            string id = string.IsNullOrEmpty(buttonID) ? gameObject.name : buttonID;
            onClickWithNameAndPosition?.Invoke(id, clickPosition);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Press();
        }

        public void OnSubmit(BaseEventData eventData)
        {
            Press();

            // If we get here, we are navigating with keyboard/controller
            if (!IsActive() || !IsInteractable())
                return;

            DoStateTransition(SelectionState.Pressed, false);
            StartCoroutine(OnFinishSubmit());
        }

        private System.Collections.IEnumerator OnFinishSubmit()
        {
            var fadeTime = colors.fadeDuration;
            var elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.unscaledDeltaTime;
                yield return null;
            }

            DoStateTransition(currentSelectionState, false);
        }

        /// <summary>
        /// Programmatically trigger the button click.
        /// </summary>
        public void SimulateClick()
        {
            Press();
        }

        /// <summary>
        /// Programmatically trigger the button click with a custom position.
        /// </summary>
        public void SimulateClick(Vector2 position)
        {
            if (!IsActive() || !IsInteractable())
                return;

            onClick?.Invoke();
            onClickWithPosition?.Invoke(position);
            
            string id = string.IsNullOrEmpty(buttonID) ? gameObject.name : buttonID;
            onClickWithNameAndPosition?.Invoke(id, position);
        }
    }
}
