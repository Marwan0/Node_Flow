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

        public int currentSlotIndex { get; private set; } = 0;
        private int attemptsForCurrentSlot = 0;
        private int starsCollected = 0;
        private int totalSlots = 0;

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
                submitButton.gameObject.SetActive(false); // No longer needed for sequential
            }

            if (resetButton != null)
            {
                resetButton.gameObject.SetActive(false); // No longer needed for sequential
            }
            
            SetupHintButton();
        }

        private void SetupHintButton()
        {
            if (hintButton == null) return;
            hintButton.onClick.RemoveAllListeners();

            if (HintsEnabled)
            {
                hintButton.onClick.AddListener(OnHintButtonClicked);
            }

            hintButton.gameObject.SetActive(false);
        }

        protected override void OnHintButtonClicked()
        {
            if (hintPanel == null) return;

            if (hintPanel.activeSelf)
            {
                HideHint();
            }
            else
            {
                string hint = GetCurrentHintText();
                if (!string.IsNullOrEmpty(hint))
                    ShowHint(hint);
            }
        }

        private string GetCurrentHintText()
        {
            if (currentQuestion == null || currentQuestion.hints == null || currentQuestion.hints.Length == 0)
                return null;

            int threshold = currentQuestion.showHintAfterAttempt;
            if (threshold <= 0) return null;

            int hintIndex = attemptsForCurrentSlot - threshold;
            if (hintIndex < 0) return null;
            hintIndex = Mathf.Min(hintIndex, currentQuestion.hints.Length - 1);

            string hint = currentQuestion.hints[hintIndex];
            return string.IsNullOrEmpty(hint) ? null : hint;
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

            // Ensure the container itself can catch drops in the empty padding/spacing areas
            var bgGraphic = slotsContainer.GetComponent<Graphic>();
            if (bgGraphic == null)
            {
                bgGraphic = slotsContainer.gameObject.AddComponent<Image>();
                bgGraphic.color = new Color(0, 0, 0, 0); // Transparent
            }
            bgGraphic.raycastTarget = true;

            // Add global drop zone to the container itself
            var containerDropZone = slotsContainer.gameObject.GetComponent<OrderingContainerDropZone>();
            if (containerDropZone == null)
            {
                containerDropZone = slotsContainer.gameObject.AddComponent<OrderingContainerDropZone>();
            }
            containerDropZone.Init(this);

            for (int i = 0; i < orderingData.items.Count; i++)
            {
                GameObject slotObj = Instantiate(dropSlotPrefab, slotsContainer);
                var slot = slotObj.GetComponent<OrderingDropSlot>();
                if (slot == null)
                    slot = slotObj.AddComponent<OrderingDropSlot>();

                slot.Init(i, this);

                // Hide the slot number text completely
                var label = slotObj.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = "";
                    label.enabled = false;
                }

                // Hide the slot background completely so it's strictly an invisible layout gap
                var img = slotObj.GetComponent<Image>();
                if (img != null)
                {
                    img.enabled = false;
                }

                // Ensure the empty slot has a physical width/height for the LayoutGroup
                var le = slotObj.GetComponent<LayoutElement>();
                if (le == null) le = slotObj.AddComponent<LayoutElement>();
                
                if (dragItemPrefab != null)
                {
                    var prefabRt = dragItemPrefab.GetComponent<RectTransform>();
                    if (prefabRt != null)
                    {
                        le.preferredWidth = prefabRt.rect.width;
                        le.preferredHeight = prefabRt.rect.height;
                    }
                }

                dropSlots.Add(slot);
            }
            totalSlots = orderingData.items.Count;
            currentSlotIndex = 0;
            attemptsForCurrentSlot = 0;
            starsCollected = 0;

            // Force layout rebuild so slots arrange themselves instantly before play begins
            if (slotsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(slotsContainer as RectTransform);
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

        /// <summary>Called when a drop occurs anywhere on the slots container.</summary>
        public void TryDropItemIntoCurrentSlot(OrderingDragItem item)
        {
            if (currentSlotIndex >= 0 && currentSlotIndex < dropSlots.Count)
            {
                var targetSlot = dropSlots[currentSlotIndex];
                
                // Simulate an OnDrop on the active slot
                if (targetSlot.occupant != null && targetSlot.occupant != item)
                    targetSlot.occupant.AnimateToHome();

                item.PlaceInSlot(targetSlot);
                OnItemPlacedInSlot(item, targetSlot);
            }
            else
            {
                item.AnimateToHome();
            }
        }

        /// <summary>Called by OrderingDropSlot when an item is dropped directly into a slot.</summary>
        public void OnItemPlacedInSlot(OrderingDragItem item, OrderingDropSlot slot)
        {
            if (slot.slotIndex != currentSlotIndex)
            {
                item.AnimateToHome();
                return;
            }

            ValidateAndApplyPlacement(item, slot);
        }

        private void ValidateAndApplyPlacement(OrderingDragItem item, OrderingDropSlot slot)
        {
            var expectedOrder = orderingData.GetExpectedOrder();
            if (expectedOrder == null || currentSlotIndex >= expectedOrder.Count) return;

            int expectedOriginalIndex = expectedOrder[currentSlotIndex];

            if (item.originalIndex == expectedOriginalIndex)
            {
                // Correct!
                slot.SetHighlight(slotCorrectColor);
                slot.isLocked = true;
                item.GetComponent<CanvasGroup>().blocksRaycasts = false; // Disable dragging

                starsCollected++;
                QuizState.Instance?.NotifyCorrectAttempt();
                quizManager?.UpdateQuestionProgress(currentQuestion, starsCollected, totalSlots);

                HideHint();
                if (hintButton != null) hintButton.gameObject.SetActive(false);

                currentSlotIndex++;
                attemptsForCurrentSlot = 0;

                if (currentSlotIndex >= totalSlots)
                {
                    CompleteQuestion(true, GetEarnedRawQuestionPoints());
                }
            }
            else
            {
                // Wrong!
                attemptsForCurrentSlot++;
                QuizState.Instance?.NotifyWrongAttempt();

                // Snap item back home automatically
                item.AnimateToHome();
                slot.Clear();

                int maxAttempts = orderingData.maxAttemptsPerSlot > 0 ? orderingData.maxAttemptsPerSlot : 3;
                if (attemptsForCurrentSlot >= maxAttempts)
                {
                    // Auto-correct
                    var correctItem = dragItems.Find(d => d.originalIndex == expectedOriginalIndex);
                    if (correctItem != null)
                    {
                        correctItem.PlaceInSlot(slot);
                        slot.SetHighlight(slotCorrectColor);
                        slot.isLocked = true;
                        correctItem.GetComponent<CanvasGroup>().blocksRaycasts = false;
                    }

                    if (HintsEnabled)
                    {
                        // Add hint logic if desired
                    }

                    if (hintButton != null) hintButton.gameObject.SetActive(false);
                    currentSlotIndex++;
                    attemptsForCurrentSlot = 0;

                    if (currentSlotIndex >= totalSlots)
                    {
                        CompleteQuestion(false, GetEarnedRawQuestionPoints());
                    }
                }
                else
                {
                    string hint = GetCurrentHintText();
                    if (HintsEnabled && !string.IsNullOrEmpty(hint))
                    {
                        ShowHint(hint);
                        if (hintButton != null) hintButton.gameObject.SetActive(true);
                    }
                }
            }
        }

        private void CompleteQuestion(bool allCorrect, int points)
        {
            quizManager?.OnQuestionAnswered(allCorrect, points, currentQuestion);
        }

        private int GetEarnedRawQuestionPoints()
        {
            if (totalSlots <= 0 || currentQuestion == null) return 0;
            float normalized = Mathf.Clamp01((float)starsCollected / totalSlots);
            return Mathf.RoundToInt(currentQuestion.points * normalized);
        }

        /// <summary>Called by OrderingDragItem when an item snaps back to source.</summary>
        public void OnItemReturnedToSource(OrderingDragItem item)
        {
            // Nothing extra needed; slot is already cleared in AnimateToHome
        }

        public override void OnAnswerSubmitted()
        {
            // Sequential ordering validates on each drop; no full-question submit.
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
