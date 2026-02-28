using System.Collections.Generic;
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

        [Header("Drag Items")]
        [Tooltip("List of items that can be dragged")]
        public List<DragItem> dragItems = new List<DragItem>();

        [Header("Drop Zones")]
        [Tooltip("List of drop zones")]
        public List<DropZone> dropZones = new List<DropZone>();

        [System.Serializable]
        public class Pairing
        {
            [Tooltip("Index of the Drag Item")]
            public int dragIndex;
            [Tooltip("Index of the Drop Zone")]
            public int dropIndex;
        }

        [Header("Correct Pairings")]
        [Tooltip("List of valid drag-to-drop pairings")]
        public List<Pairing> correctPairings = new List<Pairing>();

        private void OnEnable()
        {
            questionType = QuestionType.DragDrop;
        }
    }
}
