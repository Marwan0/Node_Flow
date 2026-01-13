using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace QuizSystem
{
    public class ConnectUI : QuestionUI
    {
        [System.Serializable]
        public class ConnectItemUI
        {
            public GameObject itemObject;
            public int itemIndex;
            public bool isLeftColumn;
        }

        [Header("Connect UI")]
        [SerializeField] private Transform leftColumnContainer;
        [SerializeField] private Transform rightColumnContainer;
        [SerializeField] private GameObject connectItemPrefab;
        [SerializeField] private LineRenderer lineRendererPrefab;
        [SerializeField] private Canvas canvas;

        private ConnectQuestionData connectData;
        private List<ConnectItemUI> leftItemUIs = new List<ConnectItemUI>();
        private List<ConnectItemUI> rightItemUIs = new List<ConnectItemUI>();
        private List<LineRenderer> connectionLines = new List<LineRenderer>();
        private Dictionary<int, int> currentConnections = new Dictionary<int, int>(); // left index -> right index
        private ConnectItemUI selectedLeftItem = null;

        protected override void SetupQuestion()
        {
            connectData = currentQuestion as ConnectQuestionData;
            if (connectData == null)
            {
                Debug.LogError("Question is not a ConnectQuestionData!");
                return;
            }

            ClearUI();
            CreateLeftColumnItems();
            CreateRightColumnItems();
            currentConnections.Clear();
        }

        private void ClearUI()
        {
            foreach (var item in leftItemUIs)
            {
                if (item.itemObject != null)
                    Destroy(item.itemObject);
            }
            leftItemUIs.Clear();

            foreach (var item in rightItemUIs)
            {
                if (item.itemObject != null)
                    Destroy(item.itemObject);
            }
            rightItemUIs.Clear();

            foreach (var line in connectionLines)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }
            connectionLines.Clear();
        }

        private void CreateLeftColumnItems()
        {
            if (leftColumnContainer == null || connectItemPrefab == null) return;

            for (int i = 0; i < connectData.leftColumnItems.Count; i++)
            {
                GameObject itemObj = Instantiate(connectItemPrefab, leftColumnContainer);
                ConnectItemUI itemUI = new ConnectItemUI { itemObject = itemObj, itemIndex = i, isLeftColumn = true };

                // Setup UI
                TextMeshProUGUI text = itemObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = connectData.leftColumnItems[i].label;

                Image image = itemObj.GetComponentInChildren<Image>();
                if (image != null && connectData.leftColumnItems[i].icon != null)
                    image.sprite = connectData.leftColumnItems[i].icon;

                // Add click handler
                Button button = itemObj.GetComponent<Button>();
                if (button == null)
                    button = itemObj.AddComponent<Button>();

                int index = i; // Capture for closure
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnLeftItemClicked(itemUI));

                leftItemUIs.Add(itemUI);
            }
        }

        private void CreateRightColumnItems()
        {
            if (rightColumnContainer == null || connectItemPrefab == null) return;

            for (int i = 0; i < connectData.rightColumnItems.Count; i++)
            {
                GameObject itemObj = Instantiate(connectItemPrefab, rightColumnContainer);
                ConnectItemUI itemUI = new ConnectItemUI { itemObject = itemObj, itemIndex = i, isLeftColumn = false };

                // Setup UI
                TextMeshProUGUI text = itemObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = connectData.rightColumnItems[i].label;

                Image image = itemObj.GetComponentInChildren<Image>();
                if (image != null && connectData.rightColumnItems[i].icon != null)
                    image.sprite = connectData.rightColumnItems[i].icon;

                // Add click handler
                Button button = itemObj.GetComponent<Button>();
                if (button == null)
                    button = itemObj.AddComponent<Button>();

                int index = i; // Capture for closure
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnRightItemClicked(itemUI));

                rightItemUIs.Add(itemUI);
            }
        }

        private void OnLeftItemClicked(ConnectItemUI itemUI)
        {
            // Deselect previous selection
            if (selectedLeftItem != null)
            {
                UpdateItemVisual(selectedLeftItem, false);
            }

            selectedLeftItem = itemUI;
            UpdateItemVisual(selectedLeftItem, true);
        }

        private void OnRightItemClicked(ConnectItemUI itemUI)
        {
            if (selectedLeftItem == null) return;

            // Remove old connection if exists
            if (currentConnections.ContainsKey(selectedLeftItem.itemIndex))
            {
                RemoveConnection(selectedLeftItem.itemIndex);
            }

            // Create new connection
            currentConnections[selectedLeftItem.itemIndex] = itemUI.itemIndex;
            CreateConnectionLine(selectedLeftItem, itemUI);

            // Deselect
            UpdateItemVisual(selectedLeftItem, false);
            selectedLeftItem = null;

            UpdateVisualFeedback();
        }

        private void CreateConnectionLine(ConnectItemUI leftItem, ConnectItemUI rightItem)
        {
            if (lineRendererPrefab == null || canvas == null) return;

            LineRenderer line = Instantiate(lineRendererPrefab, canvas.transform);
            line.positionCount = 2;

            Vector3 leftPos = leftItem.itemObject.transform.position;
            Vector3 rightPos = rightItem.itemObject.transform.position;

            // Convert to world space if needed
            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                line.SetPosition(0, leftPos);
                line.SetPosition(1, rightPos);
            }
            else
            {
                line.SetPosition(0, leftItem.itemObject.transform.position);
                line.SetPosition(1, rightItem.itemObject.transform.position);
            }

            connectionLines.Add(line);
        }

        private void RemoveConnection(int leftIndex)
        {
            // Find and remove the line for this connection
            for (int i = connectionLines.Count - 1; i >= 0; i--)
            {
                // This is a simplified check - you might want to store line-to-connection mapping
                if (currentConnections.ContainsKey(leftIndex))
                {
                    Destroy(connectionLines[i].gameObject);
                    connectionLines.RemoveAt(i);
                    break;
                }
            }
        }

        private void UpdateItemVisual(ConnectItemUI itemUI, bool selected)
        {
            Image img = itemUI.itemObject.GetComponent<Image>();
            if (img != null)
            {
                img.color = selected ? Color.cyan : Color.white;
            }
        }

        private void UpdateVisualFeedback()
        {
            // Update line colors based on correctness
            int lineIndex = 0;
            foreach (var connection in currentConnections)
            {
                if (lineIndex < connectionLines.Count)
                {
                    bool isCorrect = connectData.correctConnections.ContainsKey(connection.Key) &&
                                    connectData.correctConnections[connection.Key] == connection.Value;

                    connectionLines[lineIndex].startColor = isCorrect ? Color.green : Color.red;
                    connectionLines[lineIndex].endColor = isCorrect ? Color.green : Color.red;
                }
                lineIndex++;
            }
        }

        public override void OnAnswerSubmitted()
        {
            if (submitButton != null)
                submitButton.interactable = false;

            var result = validator.ValidateAnswer(currentConnections);
            HandleValidationResult(result);
        }

        protected override void OnWrongAnswer()
        {
            base.OnWrongAnswer();
            // Reset line colors
            foreach (var line in connectionLines)
            {
                if (line != null)
                {
                    line.startColor = Color.white;
                    line.endColor = Color.white;
                }
            }

            if (submitButton != null)
                submitButton.interactable = true;
        }

        protected override void OnAutoCorrect()
        {
            base.OnAutoCorrect();
            // Show correct connections
            currentConnections.Clear();
            foreach (var line in connectionLines)
            {
                if (line != null)
                    Destroy(line.gameObject);
            }
            connectionLines.Clear();

            foreach (var correctConnection in connectData.correctConnections)
            {
                currentConnections[correctConnection.Key] = correctConnection.Value;

                ConnectItemUI leftItem = leftItemUIs.Find(x => x.itemIndex == correctConnection.Key);
                ConnectItemUI rightItem = rightItemUIs.Find(x => x.itemIndex == correctConnection.Value);

                if (leftItem != null && rightItem != null)
                {
                    CreateConnectionLine(leftItem, rightItem);
                }
            }
            UpdateVisualFeedback();
        }

        protected override string GetCorrectAnswerDisplay()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var connection in connectData.correctConnections)
            {
                if (connection.Key < connectData.leftColumnItems.Count && connection.Value < connectData.rightColumnItems.Count)
                {
                    sb.AppendLine($"{connectData.leftColumnItems[connection.Key].label} → {connectData.rightColumnItems[connection.Value].label}");
                }
            }
            return sb.ToString();
        }
    }
}

