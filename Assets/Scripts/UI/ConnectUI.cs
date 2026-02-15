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
        [Tooltip("Prefab with UILineRenderer (renders in Canvas space). Leave empty to use built-in default line.")]
        [SerializeField] private UILineRenderer uiLinePrefab;
        [SerializeField] private RectTransform lineContainer;
        [SerializeField] private Canvas canvas;

        private ConnectQuestionData connectData;
        private List<ConnectItemUI> leftItemUIs = new List<ConnectItemUI>();
        private List<ConnectItemUI> rightItemUIs = new List<ConnectItemUI>();
        private List<GameObject> connectionLineObjects = new List<GameObject>();
        private List<int> connectionLeftIndices = new List<int>();
        private const float LineThickness = 4f;
        private static Sprite _whiteSprite;

        private static Sprite GetWhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            return _whiteSprite;
        }
        private Dictionary<int, int> currentConnections = new Dictionary<int, int>(); // left index -> right index
        private ConnectItemUI selectedLeftItem = null;

        private ConnectItemUI _dragStartItem;
        private GameObject _previewLine;
        private RectTransform _dragContainer;
        private bool _isDragging;
        private Vector2 _pointerDownPos;

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

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmitClicked);
                submitButton.interactable = true;
            }

            if (lineContainer != null)
            {
                var graphic = lineContainer.GetComponent<Graphic>();
                if (graphic != null)
                    graphic.raycastTarget = false;
            }
        }

        private void OnSubmitClicked()
        {
            OnAnswerSubmitted();
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

            foreach (var go in connectionLineObjects)
            {
                if (go != null)
                    Destroy(go);
            }
            connectionLineObjects.Clear();
            connectionLeftIndices.Clear();

            if (_previewLine != null)
            {
                Destroy(_previewLine);
                _previewLine = null;
            }
            _dragStartItem = null;
            _isDragging = false;
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

                SetupItemInteraction(itemObj, itemUI);

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

                SetupItemInteraction(itemObj, itemUI);

                rightItemUIs.Add(itemUI);
            }
        }

        private void SetupItemInteraction(GameObject itemObj, ConnectItemUI itemUI)
        {
            Button button = itemObj.GetComponent<Button>();
            if (button == null)
                button = itemObj.AddComponent<Button>();
            button.onClick.RemoveAllListeners();

            var dragHandler = itemObj.GetComponent<ConnectItemDragHandler>();
            if (dragHandler == null)
                dragHandler = itemObj.AddComponent<ConnectItemDragHandler>();
            dragHandler.Setup(this, itemUI);
        }

        private void OnLeftItemClicked(ConnectItemUI itemUI)
        {
            if (selectedLeftItem != null)
                UpdateItemVisual(selectedLeftItem, false);
            selectedLeftItem = itemUI;
            UpdateItemVisual(selectedLeftItem, true);
        }

        private void OnRightItemClicked(ConnectItemUI itemUI)
        {
            if (selectedLeftItem == null) return;
            if (currentConnections.ContainsKey(selectedLeftItem.itemIndex))
                RemoveConnection(selectedLeftItem.itemIndex);
            currentConnections[selectedLeftItem.itemIndex] = itemUI.itemIndex;
            CreateConnectionLine(selectedLeftItem, itemUI);
            UpdateItemVisual(selectedLeftItem, false);
            selectedLeftItem = null;
            UpdateVisualFeedback();
        }

        public void StartDrag(ConnectItemUI item, Vector2 screenPos)
        {
            _dragStartItem = item;
            _pointerDownPos = screenPos;
            _isDragging = false;
            RectTransform container = lineContainer != null ? lineContainer : GetComponent<RectTransform>();
            if (container == null && canvas != null)
                container = canvas.GetComponent<RectTransform>();
            _dragContainer = container;
        }

        public void UpdateDrag(Vector2 screenPos)
        {
            if (_dragStartItem == null || _dragContainer == null) return;
            if (!_isDragging)
            {
                _isDragging = true;
                CreatePreviewLine();
            }
            UpdatePreviewLine(screenPos);
        }

        public void EndDrag(Vector2 screenPos)
        {
            if (_dragStartItem == null)
                return;

            if (_isDragging && _previewLine != null)
            {
                ConnectItemUI target = GetItemAt(screenPos);
                if (target != null && target != _dragStartItem &&
                    ((_dragStartItem.isLeftColumn && !target.isLeftColumn) || (!_dragStartItem.isLeftColumn && target.isLeftColumn)))
                {
                    int leftIdx = _dragStartItem.isLeftColumn ? _dragStartItem.itemIndex : target.itemIndex;
                    int rightIdx = _dragStartItem.isLeftColumn ? target.itemIndex : _dragStartItem.itemIndex;
                    if (currentConnections.ContainsKey(leftIdx))
                        RemoveConnection(leftIdx);
                    currentConnections[leftIdx] = rightIdx;
                    CreateConnectionLine(
                        leftItemUIs.Find(x => x.itemIndex == leftIdx),
                        rightItemUIs.Find(x => x.itemIndex == rightIdx));
                    UpdateVisualFeedback();
                }
                Destroy(_previewLine);
                _previewLine = null;
            }
            else if (!_isDragging)
            {
                ConnectItemUI target = GetItemAt(screenPos);
                if (target != null)
                {
                    if (target.isLeftColumn)
                        OnLeftItemClicked(target);
                    else if (selectedLeftItem != null)
                        OnRightItemClicked(target);
                }
            }

            _dragStartItem = null;
            _isDragging = false;
        }

        private void CreatePreviewLine()
        {
            if (_dragContainer == null || _dragStartItem == null) return;
            Vector2 startLocal = GetItemCenterInContainerLocal(_dragContainer, _dragStartItem.itemObject);
            _previewLine = CreateLineWithImage(_dragContainer, startLocal, startLocal);
            var img = _previewLine.GetComponent<Image>();
            if (img != null)
                img.color = new Color(1f, 1f, 1f, 0.6f);
        }

        private void UpdatePreviewLine(Vector2 screenPos)
        {
            if (_previewLine == null || _dragContainer == null || _dragStartItem == null) return;
            Vector2 startLocal = GetItemCenterInContainerLocal(_dragContainer, _dragStartItem.itemObject);
            Vector2 endLocal = ScreenToContainerLocal(_dragContainer, screenPos);
            UpdateLineRect(_previewLine.GetComponent<RectTransform>(), startLocal, endLocal);
        }

        private static void UpdateLineRect(RectTransform rect, Vector2 localStart, Vector2 localEnd)
        {
            if (rect == null) return;
            Vector2 mid = (localStart + localEnd) * 0.5f;
            Vector2 dir = localEnd - localStart;
            float length = dir.magnitude;
            if (length < 1f) length = 1f;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rect.anchoredPosition = mid;
            rect.sizeDelta = new Vector2(length, LineThickness);
            rect.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private Vector2 ScreenToContainerLocal(RectTransform container, Vector2 screenPos)
        {
            Canvas c = container.GetComponentInParent<Canvas>();
            if (c == null) return Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPos, c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera, out Vector2 local);
            return local;
        }

        private ConnectItemUI GetItemAt(Vector2 screenPos)
        {
            var eventData = new PointerEventData(EventSystem.current) { position = screenPos };
            var results = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            foreach (var r in results)
            {
                var go = r.gameObject;
                foreach (var item in leftItemUIs)
                    if (item.itemObject == go) return item;
                foreach (var item in rightItemUIs)
                    if (item.itemObject == go) return item;
            }
            return null;
        }

        private void CreateConnectionLine(ConnectItemUI leftItem, ConnectItemUI rightItem)
        {
            // Prefer ConnectUI's RectTransform so the line is in the same coordinate space as the items
            RectTransform container = lineContainer != null ? lineContainer : GetComponent<RectTransform>();
            if (container == null && canvas != null)
                container = canvas.GetComponent<RectTransform>();
            if (container == null) return;

            Vector2 localStart = GetItemCenterInContainerLocal(container, leftItem.itemObject);
            Vector2 localEnd = GetItemCenterInContainerLocal(container, rightItem.itemObject);

            // Always use Image-based line so it reliably renders (no material/vertex issues)
            GameObject lineGo = CreateLineWithImage(container, localStart, localEnd);

            connectionLineObjects.Add(lineGo);
            connectionLeftIndices.Add(leftItem.itemIndex);
        }

        /// <summary>
        /// Renders the connection line as a stretched, rotated UI Image (white sprite) between the two points.
        /// This always renders because it uses Unity's standard Image component, not a custom mesh.
        /// </summary>
        private static GameObject CreateLineWithImage(RectTransform container, Vector2 localStart, Vector2 localEnd)
        {
            Vector2 mid = (localStart + localEnd) * 0.5f;
            Vector2 dir = localEnd - localStart;
            float length = dir.magnitude;
            if (length < 1f) length = 1f;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            var go = new GameObject("ConnectionLine");
            go.transform.SetParent(container, false);
            go.SetActive(true);

            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = mid;
            rect.sizeDelta = new Vector2(length, LineThickness);
            rect.localRotation = Quaternion.Euler(0, 0, angle);
            rect.SetAsLastSibling();

            Image img = go.AddComponent<Image>();
            img.sprite = GetWhiteSprite();
            img.color = Color.white;
            img.raycastTarget = false;

            return go;
        }

        private static Vector2 GetItemCenterInContainerLocal(RectTransform container, GameObject item)
        {
            if (container == null || item == null) return Vector2.zero;
            RectTransform itemRect = item.GetComponent<RectTransform>();
            Vector3 worldPos = itemRect != null
                ? itemRect.TransformPoint(itemRect.rect.center)
                : item.transform.position;
            return WorldToContainerLocal(container, worldPos);
        }

        private static Vector2 WorldToContainerLocal(RectTransform container, Vector3 worldOrScreenPos)
        {
            Canvas c = container.GetComponentInParent<Canvas>();
            if (c == null) return Vector2.zero;
            Vector2 screenPoint = c.renderMode == RenderMode.ScreenSpaceOverlay
                ? worldOrScreenPos
                : (Vector2)(c.worldCamera != null ? c.worldCamera.WorldToScreenPoint(worldOrScreenPos) : worldOrScreenPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(container, screenPoint, c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera, out Vector2 local);
            return local;
        }

        private void RemoveConnection(int leftIndex)
        {
            int idx = connectionLeftIndices.IndexOf(leftIndex);
            if (idx < 0) return;
            if (idx < connectionLineObjects.Count && connectionLineObjects[idx] != null)
                Destroy(connectionLineObjects[idx]);
            connectionLineObjects.RemoveAt(idx);
            connectionLeftIndices.RemoveAt(idx);
        }

        private void UpdateItemVisual(ConnectItemUI itemUI, bool selected)
        {
            Image img = itemUI.itemObject.GetComponent<Image>();
            if (img != null)
            {
                img.color = selected ? Color.cyan : Color.white;
            }
        }

        private void SetLineColor(GameObject lineGo, Color c)
        {
            if (lineGo == null) return;
            var ul = lineGo.GetComponent<UILineRenderer>();
            if (ul != null) { ul.SetColors(c, c); return; }
            var img = lineGo.GetComponent<Image>();
            if (img != null) img.color = c;
        }

        private void UpdateVisualFeedback()
        {
            for (int i = 0; i < connectionLineObjects.Count; i++)
            {
                if (connectionLineObjects[i] == null) continue;
                int leftIdx = connectionLeftIndices[i];
                bool isCorrect = connectData.correctConnections.TryGetValue(leftIdx, out int rightIdx) &&
                                 currentConnections.TryGetValue(leftIdx, out int userRight) && userRight == rightIdx;
                SetLineColor(connectionLineObjects[i], isCorrect ? Color.green : Color.red);
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
            foreach (var go in connectionLineObjects)
                SetLineColor(go, Color.white);

            if (submitButton != null)
                submitButton.interactable = true;
        }

        protected override void OnAutoCorrect()
        {
            base.OnAutoCorrect();
            currentConnections.Clear();
            foreach (var go in connectionLineObjects)
            {
                if (go != null)
                    Destroy(go);
            }
            connectionLineObjects.Clear();
            connectionLeftIndices.Clear();

            foreach (var correctConnection in connectData.correctConnections)
            {
                currentConnections[correctConnection.Key] = correctConnection.Value;
                ConnectItemUI leftItem = leftItemUIs.Find(x => x.itemIndex == correctConnection.Key);
                ConnectItemUI rightItem = rightItemUIs.Find(x => x.itemIndex == correctConnection.Value);
                if (leftItem != null && rightItem != null)
                    CreateConnectionLine(leftItem, rightItem);
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

    public class ConnectItemDragHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private ConnectUI _connectUI;
        private ConnectUI.ConnectItemUI _itemUI;

        public void Setup(ConnectUI connectUI, ConnectUI.ConnectItemUI itemUI)
        {
            _connectUI = connectUI;
            _itemUI = itemUI;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_connectUI != null)
                _connectUI.StartDrag(_itemUI, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_connectUI != null)
                _connectUI.UpdateDrag(eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_connectUI != null)
                _connectUI.EndDrag(eventData.position);
        }
    }
}

