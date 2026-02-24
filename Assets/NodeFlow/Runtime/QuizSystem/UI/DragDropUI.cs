using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace QuizSystem
{
    public class DragDropUI : QuestionUI
    {
        [System.Serializable]
        public class DragItemUI
        {
            public GameObject dragObject;
            public int itemIndex;
        }

        [System.Serializable]
        public class DropZoneUI
        {
            public GameObject dropZoneObject;
            public int zoneIndex;
        }

        [Header("Drag & Drop UI")]
        [SerializeField] private Transform dragItemsContainer;
        [SerializeField] private Transform dropZonesContainer;
        [SerializeField] private GameObject dragItemPrefab;
        [SerializeField] private GameObject dropZonePrefab;

        private DragDropQuestionData ddData;
        private List<DragItemUI> dragItemUIs = new List<DragItemUI>();
        private List<DropZoneUI> dropZoneUIs = new List<DropZoneUI>();
        private Dictionary<int, int> currentPairings = new Dictionary<int, int>(); // drag item index -> drop zone index
        private DragItemUI currentlyDragging = null;

        protected override void SetupQuestion()
        {
            ddData = currentQuestion as DragDropQuestionData;
            if (ddData == null)
            {
                Debug.LogError("Question is not a DragDropQuestionData!");
                return;
            }

            ClearUI();
            CreateDragItems();
            CreateDropZones();
            currentPairings.Clear();
        }

        private void ClearUI()
        {
            foreach (var item in dragItemUIs)
            {
                if (item.dragObject != null)
                    Destroy(item.dragObject);
            }
            dragItemUIs.Clear();

            foreach (var zone in dropZoneUIs)
            {
                if (zone.dropZoneObject != null)
                    Destroy(zone.dropZoneObject);
            }
            dropZoneUIs.Clear();
        }

        private void CreateDragItems()
        {
            if (dragItemsContainer == null || dragItemPrefab == null) return;

            for (int i = 0; i < ddData.dragItems.Count; i++)
            {
                GameObject itemObj = Instantiate(dragItemPrefab, dragItemsContainer);
                DragItemUI itemUI = new DragItemUI { dragObject = itemObj, itemIndex = i };

                // Setup UI
                TextMeshProUGUI text = itemObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = ddData.dragItems[i].label;

                Image image = itemObj.GetComponentInChildren<Image>();
                if (image != null && ddData.dragItems[i].icon != null)
                    image.sprite = ddData.dragItems[i].icon;

                // Add drag handler
                EventTrigger trigger = itemObj.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = itemObj.AddComponent<EventTrigger>();

                AddDragHandlers(trigger, itemUI);
                dragItemUIs.Add(itemUI);
            }
        }

        private void CreateDropZones()
        {
            if (dropZonesContainer == null || dropZonePrefab == null) return;

            for (int i = 0; i < ddData.dropZones.Count; i++)
            {
                GameObject zoneObj = Instantiate(dropZonePrefab, dropZonesContainer);
                DropZoneUI zoneUI = new DropZoneUI { dropZoneObject = zoneObj, zoneIndex = i };

                // Setup UI
                TextMeshProUGUI text = zoneObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = ddData.dropZones[i].label;

                Image image = zoneObj.GetComponentInChildren<Image>();
                if (image != null && ddData.dropZones[i].icon != null)
                    image.sprite = ddData.dropZones[i].icon;

                // Add drop handler
                EventTrigger trigger = zoneObj.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = zoneObj.AddComponent<EventTrigger>();

                AddDropHandlers(trigger, zoneUI);
                dropZoneUIs.Add(zoneUI);
            }
        }

        private void AddDragHandlers(EventTrigger trigger, DragItemUI itemUI)
        {
            // Begin drag
            EventTrigger.Entry beginDrag = new EventTrigger.Entry();
            beginDrag.eventID = EventTriggerType.BeginDrag;
            beginDrag.callback.AddListener((data) => { OnBeginDrag(itemUI); });
            trigger.triggers.Add(beginDrag);

            // Drag
            EventTrigger.Entry drag = new EventTrigger.Entry();
            drag.eventID = EventTriggerType.Drag;
            drag.callback.AddListener((data) => { OnDrag(itemUI, (PointerEventData)data); });
            trigger.triggers.Add(drag);

            // End drag
            EventTrigger.Entry endDrag = new EventTrigger.Entry();
            endDrag.eventID = EventTriggerType.EndDrag;
            endDrag.callback.AddListener((data) => { OnEndDrag(itemUI, (PointerEventData)data); });
            trigger.triggers.Add(endDrag);
        }

        private void AddDropHandlers(EventTrigger trigger, DropZoneUI zoneUI)
        {
            // Drop
            EventTrigger.Entry drop = new EventTrigger.Entry();
            drop.eventID = EventTriggerType.Drop;
            drop.callback.AddListener((data) => { OnDrop(zoneUI, (PointerEventData)data); });
            trigger.triggers.Add(drop);
        }

        private void OnBeginDrag(DragItemUI itemUI)
        {
            currentlyDragging = itemUI;
        }

        private void OnDrag(DragItemUI itemUI, PointerEventData eventData)
        {
            if (itemUI.dragObject != null)
            {
                itemUI.dragObject.transform.position = eventData.position;
            }
        }

        private void OnEndDrag(DragItemUI itemUI, PointerEventData eventData)
        {
            currentlyDragging = null;
            // Reset position if not dropped on a zone
            // Position will be set by OnDrop if successful
        }

        private void OnDrop(DropZoneUI zoneUI, PointerEventData eventData)
        {
            if (currentlyDragging != null)
            {
                // Remove old pairing if exists
                if (currentPairings.ContainsKey(currentlyDragging.itemIndex))
                {
                    currentPairings.Remove(currentlyDragging.itemIndex);
                }

                // Add new pairing
                currentPairings[currentlyDragging.itemIndex] = zoneUI.zoneIndex;

                // Update visual position
                if (currentlyDragging.dragObject != null && zoneUI.dropZoneObject != null)
                {
                    currentlyDragging.dragObject.transform.position = zoneUI.dropZoneObject.transform.position;
                }

                UpdateVisualFeedback();
            }
        }

        private void UpdateVisualFeedback()
        {
            // Update visual feedback for correct/incorrect pairings
            foreach (var pairing in currentPairings)
            {
                DragItemUI itemUI = dragItemUIs.Find(x => x.itemIndex == pairing.Key);
                DropZoneUI zoneUI = dropZoneUIs.Find(x => x.zoneIndex == pairing.Value);

                if (itemUI != null && zoneUI != null)
                {
                    bool isCorrect = ddData.correctPairings.ContainsKey(pairing.Key) &&
                                    ddData.correctPairings[pairing.Key] == pairing.Value;

                    // Update colors
                    Image itemImage = itemUI.dragObject.GetComponent<Image>();
                    Image zoneImage = zoneUI.dropZoneObject.GetComponent<Image>();

                    if (itemImage != null)
                        itemImage.color = isCorrect ? Color.green : Color.yellow;
                    if (zoneImage != null)
                        zoneImage.color = isCorrect ? Color.green : Color.yellow;
                }
            }
        }

        public override void OnAnswerSubmitted()
        {
            if (submitButton != null)
                submitButton.interactable = false;

            var result = validator.ValidateAnswer(currentPairings);
            HandleValidationResult(result);
        }

        protected override void OnWrongAnswer()
        {
            base.OnWrongAnswer();
            // Reset visual feedback
            foreach (var itemUI in dragItemUIs)
            {
                Image img = itemUI.dragObject.GetComponent<Image>();
                if (img != null)
                    img.color = Color.white;
            }
            foreach (var zoneUI in dropZoneUIs)
            {
                Image img = zoneUI.dropZoneObject.GetComponent<Image>();
                if (img != null)
                    img.color = Color.white;
            }

            if (submitButton != null)
                submitButton.interactable = true;
        }

        protected override void OnAutoCorrect()
        {
            base.OnAutoCorrect();
            // Show correct pairings
            currentPairings.Clear();
            foreach (var correctPairing in ddData.correctPairings)
            {
                currentPairings[correctPairing.Key] = correctPairing.Value;

                DragItemUI itemUI = dragItemUIs.Find(x => x.itemIndex == correctPairing.Key);
                DropZoneUI zoneUI = dropZoneUIs.Find(x => x.zoneIndex == correctPairing.Value);

                if (itemUI != null && zoneUI != null)
                {
                    itemUI.dragObject.transform.position = zoneUI.dropZoneObject.transform.position;
                    Image itemImage = itemUI.dragObject.GetComponent<Image>();
                    Image zoneImage = zoneUI.dropZoneObject.GetComponent<Image>();
                    if (itemImage != null) itemImage.color = Color.green;
                    if (zoneImage != null) zoneImage.color = Color.green;
                }
            }
            UpdateVisualFeedback();
        }

        protected override string GetCorrectAnswerDisplay()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var pairing in ddData.correctPairings)
            {
                if (pairing.Key < ddData.dragItems.Count && pairing.Value < ddData.dropZones.Count)
                {
                    sb.AppendLine($"{ddData.dragItems[pairing.Key].label} → {ddData.dropZones[pairing.Value].label}");
                }
            }
            return sb.ToString();
        }
    }
}

