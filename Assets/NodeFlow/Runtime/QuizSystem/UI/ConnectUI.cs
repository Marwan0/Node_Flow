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

        [Header("Connect Audio (optional)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip correctClip;
        [SerializeField] private AudioClip wrongClip;
        [Tooltip("Played when user scores a point (correct connection). Leave empty to use correctClip.")]
        [SerializeField] private AudioClip pointClip;

        [Header("Progress (optional)")]
        [SerializeField] private TextMeshProUGUI connectionProgressText;
        [SerializeField] private TextMeshProUGUI attemptProgressText;

        private ConnectQuestionData connectData;
        private List<ConnectItemUI> leftItemUIs = new List<ConnectItemUI>();
        private List<ConnectItemUI> rightItemUIs = new List<ConnectItemUI>();
        private List<GameObject> connectionLineObjects = new List<GameObject>();
        private List<int> connectionLeftIndices = new List<int>();
        private const float LineThickness = 4f;
        private static Sprite _whiteSprite;

        private int currentConnectionIndex;
        private int attemptsForCurrentConnection;
        private int starsCollected;
        private int totalConnections;

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
        private ConnectItemUI selectedItem = null; // can be from either column

        // Track which items are already connected (used in free-order mode)
        private HashSet<int> connectedLeftIndices = new HashSet<int>();
        private HashSet<int> connectedRightIndices = new HashSet<int>();
        private bool IsFreeOrder => connectData != null && connectData.freeOrderMode;

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
            connectedLeftIndices.Clear();
            connectedRightIndices.Clear();

            // Register hover effects
            ClearRegisteredHoverEffects();
            foreach (var item in leftItemUIs)
            {
                if (item.itemObject != null) RegisterHoverEffect(item.itemObject);
            }
            foreach (var item in rightItemUIs)
            {
                if (item.itemObject != null) RegisterHoverEffect(item.itemObject);
            }

            currentConnectionIndex = 0;
            attemptsForCurrentConnection = 0;
            starsCollected = 0;
            totalConnections = connectData.correctConnections.Count;
            if (totalConnections <= 0)
                totalConnections = connectData.leftColumnItems.Count;

            // Submit button is not needed for connect questions (each connection auto-validates)
            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.gameObject.SetActive(false);
            }

            // Setup hint button to toggle hint visibility on click
            SetupHintButton();

            if (lineContainer != null)
            {
                var graphic = lineContainer.GetComponent<Graphic>();
                if (graphic != null)
                    graphic.raycastTarget = false;
            }

            RefreshItemInteractability();
            UpdateProgressText();
        }

        private void SetupHintButton()
        {
            if (hintButton == null) return;
            hintButton.onClick.RemoveAllListeners();

            if (HintsEnabled)
            {
                hintButton.onClick.AddListener(OnHintButtonClicked);
            }

            // Start hidden until first wrong attempt (or always hidden if hints disabled)
            hintButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Override: ConnectUI uses per-connection attempts, not the validator's counter.
        /// </summary>
        protected override void OnHintButtonClicked()
        {
            if (hintPanel == null) return;

            if (hintPanel.activeSelf)
            {
                HideHint();
            }
            else
            {
                string hint = GetCurrentHintText();
                if (!string.IsNullOrEmpty(hint))
                    ShowHint(hint);
            }
        }

        /// <summary>
        /// Returns the appropriate hint for the current connection attempt,
        /// respecting the per-question showHintAfterAttempt threshold.
        /// ConnectUI tracks attempts per connection (not per question), so the
        /// threshold is compared against attemptsForCurrentConnection.
        /// </summary>
        private string GetCurrentHintText()
        {
            if (currentQuestion == null || currentQuestion.hints == null || currentQuestion.hints.Length == 0)
                return null;

            int threshold = currentQuestion.showHintAfterAttempt;
            if (threshold <= 0) return null; // 0 = never show hints for this question

            // Remap so hints[0] is shown when the threshold attempt is reached
            int hintIndex = attemptsForCurrentConnection - threshold;
            if (hintIndex < 0) return null;
            hintIndex = Mathf.Min(hintIndex, currentQuestion.hints.Length - 1);

            string hint = currentQuestion.hints[hintIndex];
            return string.IsNullOrEmpty(hint) ? null : hint;
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

        private void RefreshItemInteractability()
        {
            if (IsFreeOrder)
            {
                // Free-order: all unconnected items are interactable
                for (int i = 0; i < leftItemUIs.Count; i++)
                {
                    var item = leftItemUIs[i];
                    bool connected = connectedLeftIndices.Contains(i);
                    SetItemInteractable(item, !connected);
                    UpdateItemVisual(item, false);
                    if (connected) SetItemLockedAppearance(item, true);
                }
                for (int i = 0; i < rightItemUIs.Count; i++)
                {
                    var item = rightItemUIs[i];
                    bool connected = connectedRightIndices.Contains(i);
                    SetItemInteractable(item, !connected);
                    UpdateItemVisual(item, false);
                    if (connected) SetItemLockedAppearance(item, true);
                }
            }
            else
            {
                // Sequential: only current left item + all unconnected right items
                for (int i = 0; i < leftItemUIs.Count; i++)
                {
                    var item = leftItemUIs[i];
                    bool isCurrent = (i == currentConnectionIndex);
                    SetItemInteractable(item, isCurrent);
                    UpdateItemVisual(item, false);
                    if (!isCurrent)
                        SetItemLockedAppearance(item, i < currentConnectionIndex);
                }
                for (int i = 0; i < rightItemUIs.Count; i++)
                {
                    var item = rightItemUIs[i];
                    bool connected = connectedRightIndices.Contains(i);
                    SetItemInteractable(item, !connected);
                    UpdateItemVisual(item, false);
                    if (connected) SetItemLockedAppearance(item, true);
                }
            }
        }

        private void SetItemInteractable(ConnectItemUI itemUI, bool interactable)
        {
            if (itemUI?.itemObject == null) return;
            var button = itemUI.itemObject.GetComponent<Button>();
            if (button != null) button.interactable = interactable;
            var dragHandler = itemUI.itemObject.GetComponent<ConnectItemDragHandler>();
            if (dragHandler != null) dragHandler.enabled = interactable;
        }

        private void SetItemLockedAppearance(ConnectItemUI itemUI, bool locked)
        {
            if (itemUI?.itemObject == null) return;
            var img = itemUI.itemObject.GetComponent<Image>();
            if (img != null)
                img.color = locked ? new Color(0.7f, 0.9f, 0.7f) : new Color(0.6f, 0.6f, 0.6f);
        }

        private void UpdateProgressText()
        {
            int maxAttempts = GetMaxAttemptsPerConnection();
            int completedCount = IsFreeOrder ? connectedLeftIndices.Count : currentConnectionIndex;
            if (connectionProgressText != null)
                connectionProgressText.text = $"Connection {completedCount + 1} / {totalConnections}";
            if (attemptProgressText != null)
                attemptProgressText.text = $"Try {attemptsForCurrentConnection + 1} / {maxAttempts}";
            if (attemptCounterText != null)
                attemptCounterText.text = $"Stars: {starsCollected} / {totalConnections}";
        }

        private void PlayCorrectAudio()
        {
            if (audioSource != null && correctClip != null)
                audioSource.PlayOneShot(correctClip);
        }

        private void PlayWrongAudio()
        {
            if (audioSource != null && wrongClip != null)
                audioSource.PlayOneShot(wrongClip);
        }

        private void PlayPointAudio()
        {
            AudioClip clip = pointClip != null ? pointClip : correctClip;
            if (audioSource != null && clip != null)
                audioSource.PlayOneShot(clip);
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

        private void OnItemClicked(ConnectItemUI itemUI)
        {
            // First click: select the item
            if (selectedItem == null)
            {
                if (!IsItemAvailableForSelection(itemUI)) return;
                selectedItem = itemUI;
                UpdateItemVisual(selectedItem, true);
                return;
            }

            // Second click on same item: deselect
            if (selectedItem == itemUI)
            {
                UpdateItemVisual(selectedItem, false);
                selectedItem = null;
                return;
            }

            // Second click: must be from opposite column
            if (selectedItem.isLeftColumn == itemUI.isLeftColumn)
            {
                // Same column — switch selection
                UpdateItemVisual(selectedItem, false);
                if (!IsItemAvailableForSelection(itemUI)) return;
                selectedItem = itemUI;
                UpdateItemVisual(selectedItem, true);
                return;
            }

            // Opposite columns — normalize to (leftIdx, rightIdx) and validate
            int leftIdx, rightIdx;
            if (selectedItem.isLeftColumn)
            {
                leftIdx = selectedItem.itemIndex;
                rightIdx = itemUI.itemIndex;
            }
            else
            {
                leftIdx = itemUI.itemIndex;
                rightIdx = selectedItem.itemIndex;
            }

            ValidateAndApplyConnection(leftIdx, rightIdx);
            UpdateItemVisual(selectedItem, false);
            selectedItem = null;
        }

        /// <summary>
        /// Checks whether an item can be selected right now (not already connected,
        /// and in sequential mode the left item must be the current one).
        /// </summary>
        private bool IsItemAvailableForSelection(ConnectItemUI itemUI)
        {
            if (itemUI.isLeftColumn)
            {
                if (connectedLeftIndices.Contains(itemUI.itemIndex)) return false;
                if (!IsFreeOrder && itemUI.itemIndex != currentConnectionIndex) return false;
            }
            else
            {
                if (connectedRightIndices.Contains(itemUI.itemIndex)) return false;
            }
            return true;
        }

        private void ValidateAndApplyConnection(int leftIdx, int rightIdx)
        {
            var connectValidator = validator as ConnectValidator;
            if (connectValidator == null) return;

            // In sequential mode, only allow connections for the current left item
            if (!IsFreeOrder && leftIdx != currentConnectionIndex) return;

            if (connectValidator.IsConnectionCorrect(leftIdx, rightIdx))
            {
                currentConnections[leftIdx] = rightIdx;
                connectedLeftIndices.Add(leftIdx);
                connectedRightIndices.Add(rightIdx);

                ConnectItemUI leftItem = leftItemUIs.Find(x => x.itemIndex == leftIdx);
                ConnectItemUI rightItem = rightItemUIs.Find(x => x.itemIndex == rightIdx);
                if (leftItem != null && rightItem != null)
                {
                    CreateConnectionLine(leftItem, rightItem);
                    SetLineColor(connectionLineObjects[connectionLineObjects.Count - 1], Color.green);
                }
                starsCollected++;
                PlayPointAudio();
                PlayCorrectAudio();
                QuizState.Instance?.NotifyCorrectAttempt(leftIdx);
                QuizState.Instance?.NotifyStepResult(true);
                quizManager?.UpdateQuestionProgress(currentQuestion, starsCollected, totalConnections);

                // Hide hint from previous wrong attempt and reset for next connection
                HideHint();
                if (hintButton != null) hintButton.gameObject.SetActive(false);

                if (!IsFreeOrder) currentConnectionIndex++;
                attemptsForCurrentConnection = 0;
                RefreshItemInteractability();
                UpdateProgressText();

                int completedCount = connectedLeftIndices.Count;
                if (completedCount >= totalConnections)
                    CompleteQuestion(true, GetEarnedRawQuestionPoints());
            }
            else
            {
                attemptsForCurrentConnection++;
                PlayWrongAudio();

                // Lock UI and fire feedback chain; unlock when done so user can retry
                LockUI();
                QuizState.Instance?.NotifyWrongAttempt();
                bool hasFeedbackListeners = QuizState.Instance != null &&
                    QuizState.Instance.NotifyWrongAnswerFeedback();
                if (!hasFeedbackListeners)
                    UnlockUI();

                UpdateProgressText();

                if (attemptsForCurrentConnection >= GetMaxAttemptsPerConnection())
                {
                    // Auto-correct: show the correct answer and move on
                    AutoCorrectConnection(leftIdx);
                }
                else
                {
                    // Show hint for wrong attempt (if enabled and threshold met)
                    string hint = GetCurrentHintText(); // returns null if threshold not met or disabled
                    if (HintsEnabled && !string.IsNullOrEmpty(hint))
                    {
                        ShowHint(hint);
                        // Also show the hint button so user can toggle it
                        if (hintButton != null) hintButton.gameObject.SetActive(true);
                    }
                }
            }
        }

        /// <summary>
        /// Auto-corrects a connection when the user has exhausted their attempts.
        /// Works for both sequential and free-order modes.
        /// </summary>
        private void AutoCorrectConnection(int leftIdx)
        {
            if (connectData.correctConnections.TryGetValue(leftIdx, out int correctRight))
            {
                currentConnections[leftIdx] = correctRight;
                connectedLeftIndices.Add(leftIdx);
                connectedRightIndices.Add(correctRight);

                ConnectItemUI leftItem = leftItemUIs.Find(x => x.itemIndex == leftIdx);
                ConnectItemUI rightItem = rightItemUIs.Find(x => x.itemIndex == correctRight);
                if (leftItem != null && rightItem != null)
                {
                    CreateConnectionLine(leftItem, rightItem);
                    SetLineColor(connectionLineObjects[connectionLineObjects.Count - 1], Color.green);
                }

                // Show the correct answer as hint (if hints enabled)
                if (HintsEnabled)
                {
                    string correctLabel = correctRight < connectData.rightColumnItems.Count
                        ? connectData.rightColumnItems[correctRight].label : "?";
                    string leftLabel = leftIdx < connectData.leftColumnItems.Count
                        ? connectData.leftColumnItems[leftIdx].label : "?";
                    ShowHint($"{leftLabel} → {correctLabel}");
                }
            }

            QuizState.Instance?.NotifyStepResult(false);
            if (hintButton != null) hintButton.gameObject.SetActive(false);
            if (!IsFreeOrder) currentConnectionIndex++;
            attemptsForCurrentConnection = 0;
            RefreshItemInteractability();
            UpdateProgressText();

            int completedCount = connectedLeftIndices.Count;
            if (completedCount >= totalConnections)
                CompleteQuestion(false, GetEarnedRawQuestionPoints());
        }

        private int GetMaxAttemptsPerConnection()
        {
            if (connectData == null) return 3;
            return Mathf.Max(1, connectData.maxAttemptsPerConnection);
        }

        private void CompleteQuestion(bool allCorrect, int points)
        {
            if (submitButton != null)
                submitButton.gameObject.SetActive(false);
            FinalizeQuestion(allCorrect, points);
        }

        private int GetEarnedRawQuestionPoints()
        {
            if (totalConnections <= 0 || currentQuestion == null) return 0;
            float normalized = Mathf.Clamp01((float)starsCollected / totalConnections);
            return Mathf.RoundToInt(currentQuestion.points * normalized);
        }

        public void StartDrag(ConnectItemUI item, Vector2 screenPos)
        {
            if (!IsItemAvailableForSelection(item))
                return;
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
                    _dragStartItem.isLeftColumn != target.isLeftColumn) // opposite columns
                {
                    // Normalize to (leftIdx, rightIdx)
                    int leftIdx, rightIdx;
                    if (_dragStartItem.isLeftColumn)
                    {
                        leftIdx = _dragStartItem.itemIndex;
                        rightIdx = target.itemIndex;
                    }
                    else
                    {
                        leftIdx = target.itemIndex;
                        rightIdx = _dragStartItem.itemIndex;
                    }
                    ValidateAndApplyConnection(leftIdx, rightIdx);
                }
                Destroy(_previewLine);
                _previewLine = null;
            }
            else if (!_isDragging)
            {
                // Tap / click — use the unified click handler
                ConnectItemUI target = GetItemAt(screenPos);
                if (target != null)
                {
                    OnItemClicked(target);
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
            // Sequential connect validates on each connection; no full-question submit.
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

