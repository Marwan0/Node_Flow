using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace QuizSystem
{
    public class OrderingUI : QuestionUI
    {
        [Header("Ordering UI")]
        [SerializeField] private Transform itemsContainer;
        [SerializeField] private GameObject itemPrefab;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button resetButton;

        private List<OrderingItem> orderingItems = new List<OrderingItem>();
        private OrderingQuestionData orderingData;
        private List<int> currentOrder = new List<int>();

        private class OrderingItem
        {
            public GameObject gameObject;
            public TextMeshProUGUI label;
            public Button button;
            public int originalIndex;
            public int currentIndex;
        }

        protected override void SetupQuestion()
        {
            orderingData = currentQuestion as OrderingQuestionData;
            if (orderingData == null) return;

            // Clear existing items
            foreach (var item in orderingItems)
            {
                if (item.gameObject != null) Destroy(item.gameObject);
            }
            orderingItems.Clear();
            currentOrder.Clear();

            // Create item list
            List<int> indices = new List<int>();
            for (int i = 0; i < orderingData.items.Count; i++)
            {
                indices.Add(i);
            }

            // Shuffle if needed
            if (orderingData.shuffleItems)
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    int temp = indices[i];
                    int randomIndex = UnityEngine.Random.Range(i, indices.Count);
                    indices[i] = indices[randomIndex];
                    indices[randomIndex] = temp;
                }
            }

            // Create UI items
            if (itemsContainer != null && itemPrefab != null)
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    int originalIndex = indices[i];
                    GameObject itemObj = Instantiate(itemPrefab, itemsContainer);
                    TextMeshProUGUI label = itemObj.GetComponentInChildren<TextMeshProUGUI>();
                    Button button = itemObj.GetComponent<Button>();

                    if (label != null && button != null)
                    {
                        label.text = orderingData.items[originalIndex];
                        int capturedIndex = i;
                        button.onClick.AddListener(() => OnItemClicked(capturedIndex));

                        orderingItems.Add(new OrderingItem
                        {
                            gameObject = itemObj,
                            label = label,
                            button = button,
                            originalIndex = originalIndex,
                            currentIndex = capturedIndex
                        });

                        currentOrder.Add(originalIndex);
                    }
                }
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
                submitButton.interactable = true;
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(ResetOrder);
            }
        }

        private void OnItemClicked(int clickedIndex)
        {
            // Simple implementation: move item to end or swap with next
            // You could implement drag-and-drop here for better UX
            int currentPos = currentOrder.IndexOf(clickedIndex);
            if (currentPos >= 0 && currentPos < currentOrder.Count - 1)
            {
                // Swap with next
                int temp = currentOrder[currentPos];
                currentOrder[currentPos] = currentOrder[currentPos + 1];
                currentOrder[currentPos + 1] = temp;

                UpdateItemPositions();
            }
        }

        private void UpdateItemPositions()
        {
            for (int i = 0; i < orderingItems.Count; i++)
            {
                if (orderingItems[i].gameObject != null)
                {
                    orderingItems[i].gameObject.transform.SetSiblingIndex(i);
                }
            }
        }

        private void ResetOrder()
        {
            SetupQuestion();
        }

        private void OnSubmitClicked()
        {
            var result = validator.ValidateAnswer(currentOrder);
            HandleValidationResult(result);

            if (result.IsCorrect || result.ShouldAutoCorrect)
            {
                if (submitButton != null)
                    submitButton.interactable = false;
            }
        }

        public override void OnAnswerSubmitted()
        {
            OnSubmitClicked();
        }

        protected override string GetCorrectAnswerDisplay()
        {
            if (orderingData != null)
            {
                return string.Join(" → ", orderingData.items);
            }
            return "";
        }
    }
}

