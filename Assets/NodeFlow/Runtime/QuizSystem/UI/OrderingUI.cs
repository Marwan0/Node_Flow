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

        [Header("Ordering UI — Visuals")]
        [Tooltip("Color applied to an item when placed correctly.")]
        [SerializeField] private Color correctItemColor = new Color(0.3f, 0.9f, 0.3f, 1f);

        private Canvas rootCanvas;
        private OrderingQuestionData orderingData;
        private List<OrderingDragItem> dragItems = new List<OrderingDragItem>();
        private List<OrderingDragItem> placedItems = new List<OrderingDragItem>();

        public int currentSlotIndex { get; private set; } = 0;
        private int attemptsForCurrentSlot = 0;
        private int starsCollected = 0;
        private int totalSlots = 0;

        // All valid orders that are still consistent with placements so far
        private List<List<int>> candidateOrders = new List<List<int>>();

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

            // NOTE: We do NOT destroy other children of slotsContainer so
            // manually-placed visuals/VFX GameObjects are preserved.
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

            // Start with all valid orders as candidates
            candidateOrders = orderingData.GetAllValidOrders();
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
                var validOrders = orderingData.GetAllValidOrders();
                int attempts = 0;
                const int maxAttempts = 50;

                do
                {
                    // Fisher-Yates shuffle
                    for (int i = 0; i < indices.Count; i++)
                    {
                        int j = Random.Range(i, indices.Count);
                        (indices[i], indices[j]) = (indices[j], indices[i]);
                    }
                    attempts++;
                }
                // Keep reshuffling while the result matches ANY valid order
                while (attempts < maxAttempts && IsValidOrder(indices, validOrders));
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
            if (candidateOrders == null || candidateOrders.Count == 0) return;

            // Check if this item is valid for currentSlotIndex across ANY remaining candidate order
            bool isCorrect = false;
            foreach (var order in candidateOrders)
            {
                if (currentSlotIndex < order.Count && order[currentSlotIndex] == item.originalIndex)
                {
                    isCorrect = true;
                    break;
                }
            }

            if (isCorrect)
            {
                // Narrow down candidates to only those that have this item at this position
                candidateOrders = candidateOrders.FindAll(
                    o => currentSlotIndex < o.Count && o[currentSlotIndex] == item.originalIndex
                );

                // Correct!
                item.LockInContainer(slotsContainer);
                item.TintImage(correctItemColor);
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

                // Lock and notify feedback — unlock immediately (no finalize yet, user retries)
                LockUI();
                QuizState.Instance?.NotifyWrongAttempt();
                bool hasFeedbackListeners = QuizState.Instance != null &&
                    QuizState.Instance.NotifyWrongAnswerFeedback();
                if (!hasFeedbackListeners)
                    UnlockUI();

                // Snap item back home automatically
                item.AnimateToHome();

                int maxAttempts = orderingData.maxAttemptsPerSlot > 0 ? orderingData.maxAttemptsPerSlot : 3;
                if (attemptsForCurrentSlot >= maxAttempts)
                {
                    // Auto-correct: pick from first remaining candidate order
                    int expectedOriginalIndex = candidateOrders[0][currentSlotIndex];
                    var correctItem = dragItems.Find(d => d.originalIndex == expectedOriginalIndex);
                    if (correctItem != null)
                    {
                        // Narrow candidates to the chosen auto-correct path
                        candidateOrders = candidateOrders.FindAll(
                            o => currentSlotIndex < o.Count && o[currentSlotIndex] == expectedOriginalIndex
                        );

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
            FinalizeQuestion(allCorrect, points);
        }

        private int GetEarnedRawQuestionPoints()
        {
            if (totalSlots <= 0 || currentQuestion == null) return 0;
            float normalized = Mathf.Clamp01((float)starsCollected / totalSlots);
            return Mathf.RoundToInt(currentQuestion.points * normalized);
        }

        /// <summary>
        /// Returns true if the given index list exactly matches any of the valid orders.
        /// Used by CreateDragItems to reject shuffles that are already in a correct order.
        /// </summary>
        private static bool IsValidOrder(List<int> indices, List<List<int>> validOrders)
        {
            foreach (var order in validOrders)
            {
                if (order.Count != indices.Count) continue;
                bool match = true;
                for (int i = 0; i < indices.Count; i++)
                {
                    if (indices[i] != order[i]) { match = false; break; }
                }
                if (match) return true;
            }
            return false;
        }

        public override void OnAnswerSubmitted()
        {
            // Sequential ordering validates on each drop; no full-question submit.
        }

        public override void LockUI()
        {
            base.LockUI(); // handles CanvasGroup on root (blocks non-drag UI elements)
            // Also disable drag directly on each item — they may be reparented to the root
            // canvas during OnBeginDrag and would escape the root CanvasGroup lock.
            foreach (var item in dragItems)
            {
                if (item != null) item.dragEnabled = false;
            }
        }

        public override void UnlockUI()
        {
            base.UnlockUI(); // restores root CanvasGroup
            foreach (var item in dragItems)
            {
                // Only re-enable drag on items that aren't permanently locked into a slot
                if (item != null && !item.isLocked)
                    item.dragEnabled = true;
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

            // Items locked via LockInContainer already have blocksRaycasts = false
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
