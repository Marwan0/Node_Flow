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
        [Header("Ordering UI — Optional")]
        [SerializeField] private Button resetButton;

        private Canvas rootCanvas;
        private OrderingQuestionData orderingData;
        private List<OrderingDragItem> dragItems = new List<OrderingDragItem>();
        private List<OrderingDragItem> placedItems = new List<OrderingDragItem>();

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
            placedItems.Clear();

            // Destroy any leftover children in slotsContainer
            if (slotsContainer != null)
            {
                foreach (Transform child in slotsContainer)
                {
                    if (child != null && child.gameObject != null)
                        Destroy(child.gameObject);
                }
            }
        }

        // ──────────────── slot creation ────────────────

        private void CreateDropSlots()
        {
            if (slotsContainer == null) return;

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

            totalSlots = orderingData.items.Count;
            currentSlotIndex = 0;
            attemptsForCurrentSlot = 0;
            starsCollected = 0;
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
            ValidateAndApplyPlacement(item);
        }

        private void ValidateAndApplyPlacement(OrderingDragItem item)
        {
            var expectedOrder = orderingData.GetExpectedOrder();
            if (expectedOrder == null || currentSlotIndex >= expectedOrder.Count) return;

            int expectedOriginalIndex = expectedOrder[currentSlotIndex];

            if (item.originalIndex == expectedOriginalIndex)
            {
                // Correct!
                item.LockInContainer(slotsContainer);
                placedItems.Add(item);

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

                int maxAttempts = orderingData.maxAttemptsPerSlot > 0 ? orderingData.maxAttemptsPerSlot : 3;
                if (attemptsForCurrentSlot >= maxAttempts)
                {
                    // Auto-correct
                    var correctItem = dragItems.Find(d => d.originalIndex == expectedOriginalIndex);
                    if (correctItem != null)
                    {
                        correctItem.LockInContainer(slotsContainer);
                        placedItems.Add(correctItem);
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
            placedItems.Clear();
        }

        // ──────────────── auto-correct display ────────────────

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
            placedItems.Clear();

            // Place items into correct slots container sequentially
            for (int i = 0; i < expected.Count; i++)
            {
                int origIdx = expected[i];
                var item = dragItems.Find(d => d.originalIndex == origIdx);
                if (item != null)
                {
                    item.LockInContainer(slotsContainer);
                    placedItems.Add(item);
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
