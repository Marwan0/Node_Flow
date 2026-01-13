using Sirenix.OdinInspector;
using Sirenix.Serialization;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "OrderingQuestion", menuName = "Quiz System/Ordering Question")]
    public class OrderingQuestionData : QuestionData
    {
        [BoxGroup("Items")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true, IsReadOnly = true)]
        [InfoBox("Items will be displayed in the order shown below. Users must arrange them correctly.")]
        [Tooltip("Items to be arranged in order (displayed in correct order)")]
        public List<string> items = new List<string>();

        [BoxGroup("Settings")]
        [Tooltip("Shuffle items when question is displayed")]
        public bool shuffleItems = true;

        [BoxGroup("Settings")]
        [Tooltip("Allow partial credit for partially correct ordering")]
        public bool allowPartialCredit = false;

        private void OnEnable()
        {
            questionType = QuestionType.Ordering;
        }

        [Button("Add Item")]
        [BoxGroup("Items")]
        private void AddItem()
        {
            items.Add("New Item");
        }

        [Button("Shuffle Items (Preview)")]
        [BoxGroup("Items")]
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

        public bool IsOrderCorrect(List<int> userOrder)
        {
            if (userOrder == null || userOrder.Count != items.Count)
                return false;

            for (int i = 0; i < userOrder.Count; i++)
            {
                if (userOrder[i] != i)
                    return false;
            }

            return true;
        }

        public float GetPartialCredit(List<int> userOrder)
        {
            if (userOrder == null || userOrder.Count != items.Count)
                return 0f;

            int correctPositions = 0;
            for (int i = 0; i < userOrder.Count; i++)
            {
                if (userOrder[i] == i)
                    correctPositions++;
            }

            return (float)correctPositions / items.Count;
        }
    }
}

