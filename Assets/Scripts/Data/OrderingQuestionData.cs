using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "OrderingQuestion", menuName = "Quiz System/Ordering Question")]
    public class OrderingQuestionData : QuestionData
    {
        public enum OrderValidationMode
        {
            NaturalIndexOrder,
            CustomOrder
        }

        [Header("Items")]
        [Tooltip("Items to be arranged in order (displayed in correct order)")]
        public List<string> items = new List<string>();

        [Header("Settings")]
        [Tooltip("Shuffle items when question is displayed")]
        public bool shuffleItems = true;

        [Tooltip("Allow partial credit for partially correct ordering")]
        public bool allowPartialCredit = false;

        [Header("Validation")]
        [Tooltip("NaturalIndexOrder expects [0,1,2,...]. CustomOrder uses the list below.")]
        public OrderValidationMode validationMode = OrderValidationMode.NaturalIndexOrder;

        [Tooltip("Expected original-item indices in required order. Must contain each item index exactly once.")]
        public List<int> correctOrder = new List<int>();

        [Tooltip("Require the arranged sequence to start from this item index.")]
        public bool enforceStartIndex = false;

        [Min(0)]
        [Tooltip("First index expected in the arranged sequence when start enforcement is enabled.")]
        public int requiredStartIndex = 0;

        private void OnEnable()
        {
            questionType = QuestionType.Ordering;
        }

        [ContextMenu("Add Item")]
        private void AddItem()
        {
            items.Add("New Item");
        }

        [ContextMenu("Shuffle Items (Preview)")]
        private void PreviewShuffle()
        {
            var shuffled = new List<string>(items);
            for (int i = 0; i < shuffled.Count; i++)
            {
                string temp = shuffled[i];
                int randomIndex = Random.Range(i, shuffled.Count);
                shuffled[i] = shuffled[randomIndex];
                shuffled[randomIndex] = temp;
            }
            Debug.Log($"Shuffled order: {string.Join(" -> ", shuffled)}");
        }

        [ContextMenu("Build Custom Order From Start Index")]
        private void BuildCustomOrderFromStartIndex()
        {
            int count = items != null ? items.Count : 0;
            if (count <= 0)
            {
                correctOrder = new List<int>();
                return;
            }

            int start = Mathf.Clamp(requiredStartIndex, 0, count - 1);
            var generated = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                generated.Add((start + i) % count);
            }
            correctOrder = generated;
        }

        public List<int> GetExpectedOrder()
        {
            int count = items != null ? items.Count : 0;
            var naturalOrder = new List<int>(count);
            for (int i = 0; i < count; i++)
                naturalOrder.Add(i);

            if (validationMode != OrderValidationMode.CustomOrder)
                return naturalOrder;

            if (!IsValidCustomOrder(correctOrder, count))
                return naturalOrder;

            return new List<int>(correctOrder);
        }

        public bool IsValidCustomOrder(IReadOnlyList<int> order, int itemCount)
        {
            if (order == null || order.Count != itemCount)
                return false;

            var seen = new HashSet<int>();
            for (int i = 0; i < order.Count; i++)
            {
                int index = order[i];
                if (index < 0 || index >= itemCount)
                    return false;
                if (!seen.Add(index))
                    return false;
            }
            return true;
        }

        public bool HasRequiredStart(List<int> userOrder)
        {
            if (!enforceStartIndex)
                return true;
            if (userOrder == null || userOrder.Count == 0)
                return false;
            return userOrder[0] == requiredStartIndex;
        }

        public bool IsOrderCorrect(List<int> userOrder)
        {
            if (userOrder == null || userOrder.Count != items.Count)
                return false;
            if (!HasRequiredStart(userOrder))
                return false;

            var expectedOrder = GetExpectedOrder();
            for (int i = 0; i < userOrder.Count; i++)
            {
                if (userOrder[i] != expectedOrder[i])
                    return false;
            }

            return true;
        }

        public float GetPartialCredit(List<int> userOrder)
        {
            if (userOrder == null || userOrder.Count != items.Count)
                return 0f;

            if (enforceStartIndex && !HasRequiredStart(userOrder))
                return 0f;

            var expectedOrder = GetExpectedOrder();
            int correctPositions = 0;
            for (int i = 0; i < userOrder.Count; i++)
            {
                if (userOrder[i] == expectedOrder[i])
                    correctPositions++;
            }

            return (float)correctPositions / items.Count;
        }
    }
}

