using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "ConnectQuestion", menuName = "Quiz System/Connect Question")]
    public class ConnectQuestionData : QuestionData
    {
        [System.Serializable]
        public class ConnectItem
        {
            [Tooltip("Display text for the item")]
            public string label;
            
            [Tooltip("Optional sprite/image for the item")]
            public Sprite icon;
        }

        [BoxGroup("Left Column")]
        [InfoBox("Items in the left column that will be connected to right column items")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        [Tooltip("Items in the left column")]
        public List<ConnectItem> leftColumnItems = new List<ConnectItem>();

        [BoxGroup("Right Column")]
        [InfoBox("Items in the right column that will be connected to left column items")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        [Tooltip("Items in the right column")]
        public List<ConnectItem> rightColumnItems = new List<ConnectItem>();

        [BoxGroup("Correct Connections")]
        [InfoBox("Maps left column index to correct right column index. Key = left index, Value = right index")]
        [DictionaryDrawerSettings(KeyLabel = "Left Item", ValueLabel = "Right Item")]
        [Tooltip("Dictionary mapping left column indices to their correct right column indices")]
        public Dictionary<int, int> correctConnections = new Dictionary<int, int>();

        private void OnEnable()
        {
            questionType = QuestionType.Connect;
        }

        [Button("Validate Connections")]
        [BoxGroup("Correct Connections")]
        private void ValidateConnections()
        {
            if (correctConnections == null || correctConnections.Count == 0)
            {
                Debug.LogWarning($"{name}: No correct connections defined!");
                return;
            }

            foreach (var connection in correctConnections)
            {
                if (connection.Key < 0 || connection.Key >= leftColumnItems.Count)
                {
                    Debug.LogWarning($"{name}: Invalid left column index: {connection.Key}");
                }
                if (connection.Value < 0 || connection.Value >= rightColumnItems.Count)
                {
                    Debug.LogWarning($"{name}: Invalid right column index: {connection.Value}");
                }
            }

            Debug.Log($"{name}: Connection validation complete!");
        }
    }
}

