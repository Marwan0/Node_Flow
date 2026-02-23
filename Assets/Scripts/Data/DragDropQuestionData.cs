using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "DragDropQuestion", menuName = "Quiz System/Drag & Drop Question")]
    public class DragDropQuestionData : QuestionData, ISerializationCallbackReceiver
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

        [Header("Drag Items")]
        [Tooltip("List of items that can be dragged")]
        public List<DragItem> dragItems = new List<DragItem>();

        [Header("Drop Zones")]
        [Tooltip("List of drop zones")]
        public List<DropZone> dropZones = new List<DropZone>();

        [Header("Correct Pairings")]
        [Tooltip("Maps drag item index to correct drop zone index")]
        [HideInInspector]
        public Dictionary<int, int> correctPairings = new Dictionary<int, int>();

        // Serializable backing fields for the dictionary
        [SerializeField] private List<int> _pairingKeys = new List<int>();
        [SerializeField] private List<int> _pairingValues = new List<int>();

        private void OnEnable()
        {
            questionType = QuestionType.DragDrop;
        }

        public void OnBeforeSerialize()
        {
            _pairingKeys.Clear();
            _pairingValues.Clear();
            foreach (var kvp in correctPairings)
            {
                _pairingKeys.Add(kvp.Key);
                _pairingValues.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            correctPairings = new Dictionary<int, int>();
            for (int i = 0; i < Mathf.Min(_pairingKeys.Count, _pairingValues.Count); i++)
            {
                if (!correctPairings.ContainsKey(_pairingKeys[i]))
                    correctPairings[_pairingKeys[i]] = _pairingValues[i];
            }
        }
    }
}
