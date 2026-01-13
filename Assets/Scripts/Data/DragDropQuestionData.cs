using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "DragDropQuestion", menuName = "Quiz System/Drag & Drop Question")]
    public class DragDropQuestionData : QuestionData
    {
        [System.Serializable]
        public class DragItem
        {
            [Tooltip("Display text or identifier for the drag item")]
            public string label;
            
            [Tooltip("Optional sprite/image for the drag item")]
            public Sprite icon;
        }

        [System.Serializable]
        public class DropZone
        {
            [Tooltip("Display text or identifier for the drop zone")]
            public string label;
            
            [Tooltip("Optional sprite/image for the drop zone")]
            public Sprite icon;
        }

        [BoxGroup("Drag Items")]
        [InfoBox("Items that can be dragged to drop zones")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        [Tooltip("List of items that can be dragged")]
        public List<DragItem> dragItems = new List<DragItem>();

        [BoxGroup("Drop Zones")]
        [InfoBox("Zones where drag items can be dropped")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        [Tooltip("List of drop zones")]
        public List<DropZone> dropZones = new List<DropZone>();

        [BoxGroup("Correct Pairings")]
        [InfoBox("Maps drag item index to correct drop zone index. Key = drag item index, Value = drop zone index")]
        [DictionaryDrawerSettings(KeyLabel = "Drag Item", ValueLabel = "Drop Zone")]
        [Tooltip("Dictionary mapping drag item indices to their correct drop zone indices")]
        public Dictionary<int, int> correctPairings = new Dictionary<int, int>();

        private void OnEnable()
        {
            questionType = QuestionType.DragDrop;
        }

        [Button("Validate Pairings")]
        [BoxGroup("Correct Pairings")]
        private void ValidatePairings()
        {
            if (correctPairings == null || correctPairings.Count == 0)
            {
                Debug.LogWarning($"{name}: No correct pairings defined!");
                return;
            }

            foreach (var pairing in correctPairings)
            {
                if (pairing.Key < 0 || pairing.Key >= dragItems.Count)
                {
                    Debug.LogWarning($"{name}: Invalid drag item index: {pairing.Key}");
                }
                if (pairing.Value < 0 || pairing.Value >= dropZones.Count)
                {
                    Debug.LogWarning($"{name}: Invalid drop zone index: {pairing.Value}");
                }
            }

            Debug.Log($"{name}: Pairing validation complete!");
        }
    }
}

