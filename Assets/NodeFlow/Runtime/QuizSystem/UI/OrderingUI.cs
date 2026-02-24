using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace QuizSystem
{
    public class OrderingUI : QuestionUI
    {
        [Header("Ordering UI — Containers")]
        [Tooltip("Source container where shuffled items start (e.g. VerticalLayoutGroup)")]
        [SerializeField] private Transform itemsSourceContainer;

        [Tooltip("Drop-slot container where the player builds the answer (e.g. VerticalLayoutGroup)")]
        [SerializeField] private Transform slotsContainer;

        [Header("Ordering UI — Prefabs")]
        [Tooltip("Prefab for a draggable item (needs Button/Image + child TMP_Text + CanvasGroup)")]
        [SerializeField] private GameObject dragItemPrefab;

        [Tooltip("Prefab for a drop slot placeholder (needs Image + CanvasGroup)")]
        [SerializeField] private GameObject dropSlotPrefab;

        [Header("Ordering UI — Optional")]
        [SerializeField] private Button resetButton;

        [Header("Ordering UI — Visuals")]
        [SerializeField] private Color slotDefaultColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color slotOccupiedColor = new Color(0.6f, 0.85f, 1f, 0.6f);
        [SerializeField] private Color slotCorrectColor = new Color(0.3f, 0.9f, 0.3f, 0.7f);
        [SerializeField] private Color slotWrongColor = new Color(0.9f, 0.3f, 0.3f, 0.7f);

        private Canvas rootCanvas;
        private OrderingQuestionData orderingData;
        private List<OrderingDragItem> dragItems = new List<OrderingDragItem>();
        private List<OrderingDropSlot> dropSlots = new List<OrderingDropSlot>();

        // ──────────────── setup ────────────────

        protected override void SetupQuestion()
        {
            orderingData = currentQuestion as OrderingQuestionData;
            if (orderingData == null) return;

            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
                rootCanvas = rootCanvas.rootCanvas;

            ClearUI();
            CreateDropSlots();
            CreateDragItems();

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
                submitButton.interactable = true;
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(ResetAll);
            }
        }

        private void ClearUI()
        {
            foreach (var item in dragItems)
            {
                if (item != null && item.gameObject != null)
                    Destroy(item.gameObject);
            }
            dragItems.Clear();

            foreach (var slot in dropSlots)
            {
                if (slot != null && slot.gameObject != null)
                    Destroy(slot.gameObject);
            }
            dropSlots.Clear();
        }

        // ──────────────── slot creation ────────────────

        private void CreateDropSlots()
        {
            if (slotsContainer == null || dropSlotPrefab == null) return;

            for (int i = 0; i < orderingData.items.Count; i++)
            {
                GameObject slotObj = Instantiate(dropSlotPrefab, slotsContainer);
                var slot = slotObj.GetComponent<OrderingDropSlot>();
                if (slot == null)
                    slot = slotObj.AddComponent<OrderingDropSlot>();

                slot.Init(i, this);

                // Show the slot number
                var label = slotObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = $"{i + 1}.";

                // Apply default slot color
                var img = slotObj.GetComponent<Image>();
                if (img != null)
                    img.color = slotDefaultColor;

                dropSlots.Add(slot);
            }
        }

        // ──────────────── drag item creation ────────────────

        private void CreateDragItems()
        {
            if (itemsSourceContainer == null || dragItemPrefab == null) return;

            // Build shuffled index list
            var indices = new List<int>();
            for (int i = 0; i < orderingData.items.Count; i++)
                indices.Add(i);

            if (orderingData.shuffleItems)
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    int j = Random.Range(i, indices.Count);
                    (indices[i], indices[j]) = (indices[j], indices[i]);
                }
            }

            foreach (int origIdx in indices)
            {
                GameObject itemObj = Instantiate(dragItemPrefab, itemsSourceContainer);

                var dragItem = itemObj.GetComponent<OrderingDragItem>();
                if (dragItem == null)
                    dragItem = itemObj.AddComponent<OrderingDragItem>();

                dragItem.Init(origIdx, this, rootCanvas);

                var label = itemObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = orderingData.items[origIdx];

                dragItems.Add(dragItem);
            }

            // Let layout calculate, then snapshot home positions
            LayoutRebuilder.ForceRebuildLayoutImmediate(itemsSourceContainer as RectTransform);
            Canvas.ForceUpdateCanvases();
            foreach (var item in dragItems)
                item.SaveHomePosition();
        }

        // ──────────────── callbacks from drag/drop components ────────────────

        /// <summary>Called by OrderingDropSlot when an item is dropped into a slot.</summary>
        public void OnItemPlacedInSlot(OrderingDragItem item, OrderingDropSlot slot)
        {
            slot.SetHighlight(slotOccupiedColor);
        }

        /// <summary>Called by OrderingDragItem when an item snaps back to source.</summary>
        public void OnItemReturnedToSource(OrderingDragItem item)
        {
            // Nothing extra needed; slot is already cleared in AnimateToHome
        }

        // ──────────────── submit / reset ────────────────

        private void OnSubmitClicked()
        {
            var userOrder = BuildCurrentOrder();
            if (userOrder == null)
            {
                Debug.LogWarning("OrderingUI: Not all slots are filled.");
                HighlightEmptySlots();
                return;
            }

            var result = validator.ValidateAnswer(userOrder);
            HandleValidationResult(result);

            if (result.IsCorrect)
            {
                HighlightSlotsCorrect();
                DisableAllDrag();
                if (submitButton != null) submitButton.interactable = false;
            }
            else if (result.ShouldAutoCorrect)
            {
                if (submitButton != null) submitButton.interactable = false;
                DisableAllDrag();
            }
            else
            {
                HighlightWrongSlots(userOrder);
            }
        }

        public override void OnAnswerSubmitted()
        {
            OnSubmitClicked();
        }

        private void ResetAll()
        {
            foreach (var item in dragItems)
            {
                if (item != null)
                    item.ReturnHomeImmediate();
            }

            foreach (var slot in dropSlots)
            {
                if (slot != null)
                    slot.ResetHighlight();
            }
        }

        // ──────────────── order building ────────────────

        /// <summary>
        /// Reads the current slot occupants into a List of original item indices.
        /// Returns null if any slot is empty.
        /// </summary>
        private List<int> BuildCurrentOrder()
        {
            var order = new List<int>(dropSlots.Count);
            foreach (var slot in dropSlots)
            {
                if (!slot.IsOccupied)
                    return null;
                order.Add(slot.occupant.originalIndex);
            }
            return order;
        }

        // ──────────────── visual feedback ────────────────

        private void HighlightEmptySlots()
        {
            foreach (var slot in dropSlots)
            {
                if (!slot.IsOccupied)
                {
                    slot.SetHighlight(slotWrongColor);
                    if (enableFeedbackAnimations)
                    {
                        var rt = slot.GetComponent<RectTransform>();
                        if (rt != null)
                            rt.DOShakeAnchorPos(0.4f, 8f, 12, 90f, false, true);
                    }
                }
            }
        }

        private void HighlightSlotsCorrect()
        {
            foreach (var slot in dropSlots)
                slot.SetHighlight(slotCorrectColor);
        }

        private void HighlightWrongSlots(List<int> userOrder)
        {
            var allValid = orderingData.GetAllValidOrders();

            // Find the best-matching valid order
            List<int> bestOrder = allValid[0];
            int bestMatches = 0;
            foreach (var valid in allValid)
            {
                int matches = 0;
                for (int i = 0; i < valid.Count && i < userOrder.Count; i++)
                {
                    if (valid[i] == userOrder[i]) matches++;
                }
                if (matches > bestMatches)
                {
                    bestMatches = matches;
                    bestOrder = valid;
                }
            }

            // Highlight each slot as correct or wrong compared to best match
            for (int i = 0; i < dropSlots.Count; i++)
            {
                bool correct = i < userOrder.Count && i < bestOrder.Count && userOrder[i] == bestOrder[i];
                dropSlots[i].SetHighlight(correct ? slotCorrectColor : slotWrongColor);
            }
        }

        private void DisableAllDrag()
        {
            foreach (var item in dragItems)
            {
                if (item != null)
                {
                    var cg = item.GetComponent<CanvasGroup>();
                    if (cg != null) cg.blocksRaycasts = false;
                }
            }
        }

        // ──────────────── auto-correct display ────────────────

        protected override void OnAutoCorrect()
        {
            base.OnAutoCorrect();
            ShowCorrectOrderInSlots();
        }

        private void ShowCorrectOrderInSlots()
        {
            var expected = orderingData.GetExpectedOrder();

            // Return all items home first (instant)
            foreach (var item in dragItems)
                item.ReturnHomeImmediate();

            // Place items into correct slots with staggered animation
            for (int i = 0; i < expected.Count && i < dropSlots.Count; i++)
            {
                int origIdx = expected[i];
                var item = dragItems.Find(d => d.originalIndex == origIdx);
                if (item != null)
                {
                    float delay = i * 0.12f;
                    var slot = dropSlots[i];
                    DOVirtual.DelayedCall(delay, () =>
                    {
                        item.PlaceInSlot(slot);
                        slot.SetHighlight(slotCorrectColor);
                    });
                }
            }

            DisableAllDrag();
        }

        // ──────────────── correct answer text ────────────────

        protected override string GetCorrectAnswerDisplay()
        {
            if (orderingData == null) return "";

            var allOrders = orderingData.GetAllValidOrders();
            var lines = new List<string>(allOrders.Count);

            foreach (var order in allOrders)
            {
                var orderedItems = new List<string>(order.Count);
                for (int i = 0; i < order.Count; i++)
                {
                    int itemIndex = order[i];
                    if (itemIndex >= 0 && itemIndex < orderingData.items.Count)
                        orderedItems.Add(orderingData.items[itemIndex]);
                }
                lines.Add(string.Join(" -> ", orderedItems));
            }

            return string.Join("\nor: ", lines);
        }
    }
}
