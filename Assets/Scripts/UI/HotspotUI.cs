using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace QuizSystem
{
    public class HotspotUI : QuestionUI
    {
        [Header("Hotspot UI")]
        [SerializeField] private Image imageDisplay;
        [SerializeField] private RectTransform imageRectTransform;
        [SerializeField] private Button submitButton;

        private HotspotQuestionData hotspotData;
        private int? selectedHotspotIndex = null;

        protected override void SetupQuestion()
        {
            hotspotData = currentQuestion as HotspotQuestionData;
            if (hotspotData == null) return;

            if (imageDisplay != null && hotspotData.image != null)
            {
                imageDisplay.sprite = hotspotData.image;
            }

            // Add click handler to image
            if (imageRectTransform != null)
            {
                EventTrigger trigger = imageRectTransform.GetComponent<EventTrigger>();
                if (trigger == null)
                {
                    trigger = imageRectTransform.gameObject.AddComponent<EventTrigger>();
                }

                // Clear existing triggers
                trigger.triggers.Clear();

                EventTrigger.Entry entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.PointerClick;
                entry.callback.AddListener((data) => { OnImageClicked((PointerEventData)data); });
                trigger.triggers.Add(entry);
            }

            selectedHotspotIndex = null;

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
                submitButton.interactable = false;
            }
        }

        private void OnImageClicked(PointerEventData eventData)
        {
            if (imageRectTransform == null || hotspotData == null) return;

            // Convert click position to normalized coordinates (0-1)
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                imageRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );

            Rect rect = imageRectTransform.rect;
            Vector2 normalizedPoint = new Vector2(
                (localPoint.x - rect.x) / rect.width,
                (localPoint.y - rect.y) / rect.height
            );

            // Check which hotspot was clicked
            for (int i = 0; i < hotspotData.hotspotRegions.Count; i++)
            {
                if (hotspotData.IsPointInHotspot(normalizedPoint, i))
                {
                    selectedHotspotIndex = i;
                    Debug.Log($"Hotspot {i} clicked at normalized position: {normalizedPoint}");

                    if (submitButton != null)
                        submitButton.interactable = true;

                    // Visual feedback could be added here
                    break;
                }
            }
        }

        private void OnSubmitClicked()
        {
            if (selectedHotspotIndex.HasValue)
            {
                var result = validator.ValidateAnswer(selectedHotspotIndex.Value);
                HandleValidationResult(result);

                if (result.IsCorrect || result.ShouldAutoCorrect)
                {
                    if (submitButton != null)
                        submitButton.interactable = false;
                }
            }
            else
            {
                ShowHint("Please click on the image to select a hotspot.");
            }
        }

        public override void OnAnswerSubmitted()
        {
            OnSubmitClicked();
        }

        protected override string GetCorrectAnswerDisplay()
        {
            if (hotspotData != null && hotspotData.correctHotspotIndex >= 0 && 
                hotspotData.correctHotspotIndex < hotspotData.hotspotRegions.Count)
            {
                return hotspotData.hotspotRegions[hotspotData.correctHotspotIndex].name;
            }
            return "";
        }
    }
}

