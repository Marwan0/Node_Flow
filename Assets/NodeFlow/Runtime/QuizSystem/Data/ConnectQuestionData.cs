using System.Collections.Generic;
using UnityEngine;

namespace QuizSystem
{
    [CreateAssetMenu(fileName = "ConnectQuestion", menuName = "Quiz System/Connect Question")]
    public class ConnectQuestionData : QuestionData, ISerializationCallbackReceiver
    {
        [System.Serializable]
        public class ConnectItem
        {
            [Tooltip("Display text for the item")]
            public string label;
            
            [Tooltip("Optional sprite/image for the item")]
            public Sprite icon;
        }

        [Header("Left Column")]
        [Tooltip("Items in the left column that will be connected to right column items")]
        public List<ConnectItem> leftColumnItems = new List<ConnectItem>();

        [Header("Right Column")]
        [Tooltip("Items in the right column that will be connected to left column items")]
        public List<ConnectItem> rightColumnItems = new List<ConnectItem>();

        [Header("Correct Connections")]
        [Tooltip("Maps left column index to correct right column index")]
        [HideInInspector]
        public Dictionary<int, int> correctConnections = new Dictionary<int, int>();

        // Serializable backing fields for the dictionary
        [SerializeField] private List<int> _connectionKeys = new List<int>();
        [SerializeField] private List<int> _connectionValues = new List<int>();

        [Header("Connect Rules")]
        [Tooltip("Max attempts per connection before the correct answer is revealed")]
        [Range(2, 10)]
        public int maxAttemptsPerConnection = 3;

        [Tooltip("When true, the user can connect any pair in any order and from either side. When false, connections must follow the left-column order sequentially.")]
        public bool freeOrderMode = false;

        private void OnEnable()
        {
            questionType = QuestionType.Connect;
        }

        public void OnBeforeSerialize()
        {
            _connectionKeys.Clear();
            _connectionValues.Clear();
            foreach (var kvp in correctConnections)
            {
                _connectionKeys.Add(kvp.Key);
                _connectionValues.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            correctConnections = new Dictionary<int, int>();
            for (int i = 0; i < Mathf.Min(_connectionKeys.Count, _connectionValues.Count); i++)
            {
                if (!correctConnections.ContainsKey(_connectionKeys[i]))
                    correctConnections[_connectionKeys[i]] = _connectionValues[i];
            }
        }
    }
}
