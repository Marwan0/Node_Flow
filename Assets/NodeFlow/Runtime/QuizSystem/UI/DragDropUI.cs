using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace QuizSystem
{
    public class DragDropUI : QuestionUI
    {
        [System.Serializable]
        public class DragItemUI
        {
            public GameObject dragObject;
            public QuizDragItem dragItem;
            public int itemIndex;
        }

        [System.Serializable]
        public class DropZoneUI
        {
            public GameObject dropZoneObject;
            public int zoneIndex;
        }

        [Header("Drag & Drop UI")]
        [SerializeField] private Transform dragItemsContainer;
        [SerializeField] private Transform dropZonesContainer;
        [SerializeField] private GameObject dragItemPrefab;
        [SerializeField] private GameObject dropZonePrefab;

        [Header("Drag & Drop UI - Settings")]
        [Header("Manual Layout Option")]
        [Tooltip("Optional: Assign pre-placed Drag Item GameObjects here instead of spawning prefabs.")]
        public List<GameObject> preplacedDragItems = new List<GameObject>();
        [Tooltip("Optional: Assign pre-placed Drop Zone GameObjects here instead of spawning prefabs.")]
        public List<GameObject> preplacedDropZones = new List<GameObject>();
        [SerializeField] private bool enableSmoothSnapping = true;
        [SerializeField] private bool enableHoverHighlighting = true;
        [SerializeField] private bool snapBackOnInvalidDrop = true;
        
        [Header("Drag & Drop UI - Visuals")]
        [SerializeField] private Color defaultZoneColor = Color.white;
        [SerializeField] private Color hoverZoneColor = new Color(0.8f, 0.9f, 1f, 1f);

        private DragDropQuestionData ddData;
        private List<DragItemUI> dragItemUIs = new List<DragItemUI>();
        private List<DropZoneUI> dropZoneUIs = new List<DropZoneUI>();
        private Dictionary<int, int> currentPairings = new Dictionary<int, int>(); // drag item index -> drop zone index
        private DragItemUI currentlyDragging = null;
        private DropZoneUI currentlyHoveredZone = null;
        private bool _lastDropWasWrong = false;
        private HashSet<int> _correctZoneIndices = new HashSet<int>(); // zones with correct items
        
        private Canvas rootCanvas;

        // Live scoring state
        private int correctCount = 0;
        private int totalItems = 0;
        private Dictionary<int, int> _wrongAttemptsPerItem = new Dictionary<int, int>(); // dragIndex -> wrong drop count

        protected override void SetupQuestion()
        {
            ddData = currentQuestion as DragDropQuestionData;
            if (ddData == null)
            {
                Debug.LogError("Question is not a DragDropQuestionData!");
                return;
            }

            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null)
                rootCanvas = rootCanvas.rootCanvas;

            ClearUI();
            CreateDropZones();
            CreateDragItems();
            currentPairings.Clear();
            currentlyDragging = null;
            currentlyHoveredZone = null;
            correctCount = 0;
            totalItems = ddData.dragItems.Count;
            _correctZoneIndices.Clear();
            _wrongAttemptsPerItem.Clear();

            // No submit button needed — validation happens live on each drop
            if (submitButton != null)
                submitButton.gameObject.SetActive(false);
        }

        private void ClearUI()
        {
            // Process drag items — clean up placeholders owned by QuizDragItem, handle preplaced items
            foreach (var item in dragItemUIs)
            {
                if (item.dragObject != null)
                {
                    // Clean up any placeholder the QuizDragItem might have
                    if (item.dragItem != null)
                        item.dragItem.DestroyPlaceholder();

                    if (preplacedDragItems.Contains(item.dragObject))
                    {
                        // Safely return preplaced items to their original parent and disable
                        if (item.dragItem != null && item.dragItem.HomeParent != null)
                            item.dragObject.transform.SetParent(item.dragItem.HomeParent, false);
                        item.dragObject.SetActive(false);
                    }
                    else
                    {
                        SafeDestroy(item.dragObject);
                    }
                }
            }
            dragItemUIs.Clear();

            foreach (var zone in dropZoneUIs)
            {
                if (zone.dropZoneObject != null)
                {
                    if (preplacedDropZones.Contains(zone.dropZoneObject))
                    {
                        zone.dropZoneObject.SetActive(false);
                    }
                    else
                    {
                        SafeDestroy(zone.dropZoneObject);
                    }
                }
            }
            dropZoneUIs.Clear();
        }

        /// <summary>Destroys a GameObject safely in the Editor by deselecting it first to avoid Inspector exceptions.</summary>
        private static void SafeDestroy(GameObject obj)
        {
            if (obj == null) return;
#if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject == obj)
            {
                UnityEditor.Selection.activeGameObject = null;
            }
            else
            {
                var selected = new List<Object>(UnityEditor.Selection.objects);
                if (selected.Contains(obj))
                {
                    selected.Remove(obj);
                    UnityEditor.Selection.objects = selected.ToArray();
                }
            }
#endif
            Destroy(obj);
        }

        private void CreateDragItems()
        {
            bool usePreplaced = preplacedDragItems != null && preplacedDragItems.Count > 0;
            if (!usePreplaced && (dragItemsContainer == null || dragItemPrefab == null)) return;

            for (int i = 0; i < ddData.dragItems.Count; i++)
            {
                GameObject itemObj;
                if (usePreplaced && i < preplacedDragItems.Count && preplacedDragItems[i] != null)
                {
#if UNITY_EDITOR
                    if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(preplacedDragItems[i]))
                    {
                        Debug.LogError($"[DragDropUI] Error: 'Preplaced Drag Items' contains a Prefab Asset from the Project window ({preplacedDragItems[i].name}). You must drag instances from the Scene Hierarchy instead!");
                        continue;
                    }
#endif
                    itemObj = preplacedDragItems[i];
                    itemObj.SetActive(true);
                }
                else if (!usePreplaced)
                {
                    itemObj = Instantiate(dragItemPrefab, dragItemsContainer);
                }
                else
                {
                    continue; // Skip if we are out of preplaced objects
                }

                // Setup UI
                TextMeshProUGUI text = itemObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = ddData.dragItems[i].label;

                Image image = itemObj.GetComponentInChildren<Image>();
                if (image != null && ddData.dragItems[i].icon != null)
                    image.sprite = ddData.dragItems[i].icon;

                // Ensure CanvasGroup exists for raycast blocking during drag
                CanvasGroup canvasGroup = itemObj.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = itemObj.AddComponent<CanvasGroup>();

                // Ensure LayoutElement exists
                LayoutElement layoutElement = itemObj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                    layoutElement = itemObj.AddComponent<LayoutElement>();

                // Attach the unified QuizDragItem component
                var dragItem = itemObj.GetComponent<QuizDragItem>();
                if (dragItem == null)
                    dragItem = itemObj.AddComponent<QuizDragItem>();

                dragItem.Init(i, rootCanvas);

                DragItemUI itemUI = new DragItemUI { dragObject = itemObj, dragItem = dragItem, itemIndex = i };

                // Wire up callbacks — QuizDragItem handles all drag mechanics
                dragItem.OnDragStarted = (item) => OnDragStarted(itemUI);
                dragItem.OnDragMoved = (item, eventData) => OnDragMoved(itemUI, eventData);
                dragItem.OnItemDropped = (item, eventData) => OnItemDropped(itemUI, eventData);

                dragItemUIs.Add(itemUI);
            }

            // Let layout calculate, then snapshot home positions
            if (dragItemsContainer != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(dragItemsContainer as RectTransform);
                Canvas.ForceUpdateCanvases();
            }
            foreach (var item in dragItemUIs)
                item.dragItem.SaveHomePosition();
        }

        private void CreateDropZones()
        {
            bool usePreplaced = preplacedDropZones != null && preplacedDropZones.Count > 0;
            if (!usePreplaced && (dropZonesContainer == null || dropZonePrefab == null)) return;

            for (int i = 0; i < ddData.dropZones.Count; i++)
            {
                GameObject zoneObj;
                if (usePreplaced && i < preplacedDropZones.Count && preplacedDropZones[i] != null)
                {
#if UNITY_EDITOR
                    if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(preplacedDropZones[i]))
                    {
                        Debug.LogError($"[DragDropUI] Error: 'Preplaced Drop Zones' contains a Prefab Asset from the Project window ({preplacedDropZones[i].name}). You must drag instances from the Scene Hierarchy instead!");
                        continue;
                    }
#endif
                    zoneObj = preplacedDropZones[i];
                    zoneObj.SetActive(true);
                }
                else if (!usePreplaced)
                {
                    zoneObj = Instantiate(dropZonePrefab, dropZonesContainer);
                }
                else
                {
                    continue; // Skip if out of preplaced objects
                }
                
                DropZoneUI zoneUI = new DropZoneUI { dropZoneObject = zoneObj, zoneIndex = i };

                // Setup UI
                TextMeshProUGUI text = zoneObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                    text.text = ddData.dropZones[i].label;

                // Store original color
                Image zoneImage = zoneObj.GetComponentInChildren<Image>();
                if (zoneImage != null)
                {
                    if (ddData.dropZones[i].icon != null)
                        zoneImage.sprite = ddData.dropZones[i].icon;
                    defaultZoneColor = zoneImage.color;
                }

                // Add drop handler via EventTrigger (drop zones don't reparent, so EventTrigger is fine)
                EventTrigger trigger = zoneObj.GetComponent<EventTrigger>();
                if (trigger == null)
                    trigger = zoneObj.AddComponent<EventTrigger>();
                
                // Clear existing triggers if preplaced to avoid duplicates
                if (usePreplaced) trigger.triggers.Clear();

                AddDropHandlers(trigger, zoneUI);
                dropZoneUIs.Add(zoneUI);
            }
        }

        private void AddDropHandlers(EventTrigger trigger, DropZoneUI zoneUI)
        {
            EventTrigger.Entry drop = new EventTrigger.Entry();
            drop.eventID = EventTriggerType.Drop;
            drop.callback.AddListener((data) => { OnDrop(zoneUI, (PointerEventData)data); });
            trigger.triggers.Add(drop);
        }

        // ──────────────── Callbacks from QuizDragItem ────────────────

        private void OnDragStarted(DragItemUI itemUI)
        {
            currentlyDragging = itemUI;

            // Remove from current pairings if picked out of a zone
            if (currentPairings.ContainsKey(itemUI.itemIndex))
            {
                currentPairings.Remove(itemUI.itemIndex);
                UpdateVisualFeedback();
            }
        }

        private void OnItemDropped(DragItemUI itemUI, PointerEventData eventData)
        {
            currentlyDragging = null;

            // Clear hover state — but don't reset correct zones
            if (currentlyHoveredZone != null && currentlyHoveredZone.dropZoneObject != null)
            {
                if (!_correctZoneIndices.Contains(currentlyHoveredZone.zoneIndex))
                {
                    Image img = currentlyHoveredZone.dropZoneObject.GetComponent<Image>();
                    if (img != null) img.color = defaultZoneColor;
                }
                currentlyHoveredZone = null;
            }

            // If it wasn't dropped into a correct zone, snap back and fire feedback
            if (!currentPairings.ContainsKey(itemUI.itemIndex))
            {
                // Always snap back first (this works even while UI is locked)
                itemUI.dragItem.AnimateToHome();

                // If this was an actual drop onto a wrong zone (not just released in empty space),
                // lock the UI and fire wrong-answer feedback
                if (_lastDropWasWrong)
                {
                    _lastDropWasWrong = false;

                    // Track wrong attempts per item
                    if (!_wrongAttemptsPerItem.ContainsKey(itemUI.itemIndex))
                        _wrongAttemptsPerItem[itemUI.itemIndex] = 0;
                    _wrongAttemptsPerItem[itemUI.itemIndex]++;

                    LockUI();
                    QuizState.Instance?.NotifyWrongAttempt();
                    bool hasFeedbackListeners = QuizState.Instance != null &&
                        QuizState.Instance.NotifyWrongAnswerFeedback();
                    if (!hasFeedbackListeners)
                        UnlockUI();

                    // Auto-place after max wrong attempts
                    int maxAttempts = ddData.maxAttempts > 0 ? ddData.maxAttempts : 3;
                    if (_wrongAttemptsPerItem[itemUI.itemIndex] >= maxAttempts)
                    {
                        AutoPlaceItem(itemUI);
                    }
                }
            }
        }

        private void OnDrop(DropZoneUI zoneUI, PointerEventData eventData)
        {
            if (currentlyDragging == null) return;

            int dragIndex = currentlyDragging.itemIndex;
            int dropIndex = zoneUI.zoneIndex;

            // Check if this is a correct pairing
            bool isCorrect = false;
            foreach (var cp in ddData.correctPairings)
            {
                if (cp.dragIndex == dragIndex && cp.dropIndex == dropIndex)
                {
                    isCorrect = true;
                    break;
                }
            }

            if (isCorrect)
            {
                // Record the correct pairing
                currentPairings[dragIndex] = dropIndex;

                // Mark as locked so OnEndDrag won't snap it back
                currentlyDragging.dragItem.isLocked = true;

                // Parent to the drop zone (keep world pos so it doesn't jump)
                currentlyDragging.dragObject.transform.SetParent(zoneUI.dropZoneObject.transform, true);

                LayoutElement le = currentlyDragging.dragObject.GetComponent<LayoutElement>();
                if (le != null) le.ignoreLayout = false;

                CanvasGroup cg = currentlyDragging.dragObject.GetComponent<CanvasGroup>();
                if (cg != null) { cg.blocksRaycasts = false; cg.alpha = 1f; }

                // Animate to the zone's center, then let layout take over
                LayoutRebuilder.ForceRebuildLayoutImmediate(zoneUI.dropZoneObject.GetComponent<RectTransform>());
                var layoutGroup = zoneUI.dropZoneObject.GetComponent<LayoutGroup>();
                if (layoutGroup == null)
                {
                    currentlyDragging.dragObject.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
                }

                // Visual feedback
                Image itemImage = currentlyDragging.dragObject.GetComponent<Image>();
                Image zoneImage = zoneUI.dropZoneObject.GetComponent<Image>();
                if (itemImage != null) itemImage.color = Color.green;
                if (zoneImage != null) zoneImage.color = Color.green;

                // Score and progress
                correctCount++;
                _correctZoneIndices.Add(dropIndex);
                
                // Only notify success for manual drops
                QuizState.Instance?.NotifyCorrectAttempt();
                QuizState.Instance?.NotifyStepResult(true);
                quizManager?.UpdateQuestionProgress(currentQuestion, correctCount, totalItems);

                // Disable zone if it has received all its correct items
                CheckAndDisableZone(zoneUI);

                // Check if all items are correctly placed
                if (correctCount >= totalItems)
                {
                    FinalizeQuestion(true, currentQuestion.points);
                }
            }
            else
            {
                // Wrong drop — flag it so OnItemDropped can fire feedback after snap-back
                _lastDropWasWrong = true;
            }
        }

        // ──────────────── Hover Highlighting (via QuizDragItem.OnDragMoved) ────────────────

        private void OnDragMoved(DragItemUI itemUI, PointerEventData eventData)
        {
            if (!enableHoverHighlighting) return;
            HandleHoverFeedback(eventData);
        }

        private void HandleHoverFeedback(PointerEventData eventData)
        {
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            
            DropZoneUI newHoveredZone = null;
            
            foreach (var result in results)
            {
                var zoneUI = dropZoneUIs.Find(z => z.dropZoneObject == result.gameObject || result.gameObject.transform.IsChildOf(z.dropZoneObject.transform));
                if (zoneUI != null)
                {
                    // Don't highlight if the zone is disabled
                    CanvasGroup cg = zoneUI.dropZoneObject.GetComponent<CanvasGroup>();
                    if (cg != null && !cg.blocksRaycasts) continue;

                    newHoveredZone = zoneUI;
                    break;
                }
            }

            if (newHoveredZone != currentlyHoveredZone)
            {
                // Restore previous zone color (but not if it's a correct zone — keep it green)
                if (currentlyHoveredZone != null && currentlyHoveredZone.dropZoneObject != null)
                {
                    if (!_correctZoneIndices.Contains(currentlyHoveredZone.zoneIndex))
                    {
                        Image oldImg = currentlyHoveredZone.dropZoneObject.GetComponent<Image>();
                        if (oldImg != null) oldImg.color = defaultZoneColor;
                    }
                }
                
                // Highlight new zone (even correct zones get hover feedback)
                if (newHoveredZone != null && newHoveredZone.dropZoneObject != null)
                {
                    Image newImg = newHoveredZone.dropZoneObject.GetComponent<Image>();
                    if (newImg != null) newImg.color = hoverZoneColor;
                }
                
                currentlyHoveredZone = newHoveredZone;
            }
        }

        // ──────────────── Visual Feedback ────────────────

        private void UpdateVisualFeedback()
        {
            foreach (var pairing in currentPairings)
            {
                DragItemUI itemUI = dragItemUIs.Find(x => x.itemIndex == pairing.Key);
                DropZoneUI zoneUI = dropZoneUIs.Find(x => x.zoneIndex == pairing.Value);

                if (itemUI != null && zoneUI != null)
                {
                    bool isCorrect = false;
                    foreach (var correctPairing in ddData.correctPairings)
                    {
                        if (correctPairing.dragIndex == pairing.Key && correctPairing.dropIndex == pairing.Value)
                        {
                            isCorrect = true;
                            break;
                        }
                    }

                    Image itemImage = itemUI.dragObject.GetComponent<Image>();
                    Image zoneImage = zoneUI.dropZoneObject.GetComponent<Image>();

                    if (itemImage != null)
                        itemImage.color = isCorrect ? Color.green : Color.yellow;
                    if (zoneImage != null)
                        zoneImage.color = isCorrect ? Color.green : Color.yellow;
                }
            }
        }

        // ──────────────── Answer Handling ────────────────

        public override void OnAnswerSubmitted()
        {
            // Live validation handles scoring per-drop; no batch submit needed.
        }

        protected override void OnWrongAnswer()
        {
            base.OnWrongAnswer();
            foreach (var itemUI in dragItemUIs)
            {
                Image img = itemUI.dragObject.GetComponent<Image>();
                if (img != null)
                    img.color = Color.white;
            }
            foreach (var zoneUI in dropZoneUIs)
            {
                Image img = zoneUI.dropZoneObject.GetComponent<Image>();
                if (img != null)
                    img.color = defaultZoneColor;
            }

            if (submitButton != null)
                submitButton.interactable = true;
        }

        protected override void OnAutoCorrect()
        {
            base.OnAutoCorrect();
            AutoPlaceAllRemaining();
        }

        /// <summary>Auto-place a single item into its correct zone after max wrong attempts.</summary>
        private void AutoPlaceItem(DragItemUI itemUI)
        {
            // Find the correct pairing for this drag item
            var pairing = ddData.correctPairings.Find(cp => cp.dragIndex == itemUI.itemIndex);
            if (pairing == null) return;

            DropZoneUI zoneUI = dropZoneUIs.Find(z => z.zoneIndex == pairing.dropIndex);
            if (zoneUI == null) return;

            // Record pairing and place
            currentPairings[itemUI.itemIndex] = pairing.dropIndex;

            // Kill any running tween (e.g. AnimateToHome) so its OnComplete won't snap back
            itemUI.dragObject.transform.DOKill();
            
            itemUI.dragItem.CreatePlaceholderAndLift();
            
            // Lock handles state, but we manually reparent and animate to avoid jumping
            itemUI.dragItem.isLocked = true;
            itemUI.dragObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
            
            itemUI.dragObject.transform.SetParent(zoneUI.dropZoneObject.transform, true);
            
            LayoutElement le = itemUI.dragObject.GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = false;
            
            LayoutRebuilder.ForceRebuildLayoutImmediate(zoneUI.dropZoneObject.GetComponent<RectTransform>());
            var layoutGroup = zoneUI.dropZoneObject.GetComponent<LayoutGroup>();
            if (layoutGroup == null)
            {
                itemUI.dragObject.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
            }

            // Visual feedback
            Image itemImage = itemUI.dragObject.GetComponent<Image>();
            Image zoneImage = zoneUI.dropZoneObject.GetComponent<Image>();
            if (itemImage != null) itemImage.color = Color.green;
            if (zoneImage != null) zoneImage.color = Color.green;

            correctCount++;
            _correctZoneIndices.Add(pairing.dropIndex);
            
            // Auto-placed = wrong step result (user didn't earn it)
            QuizState.Instance?.NotifyStepResult(false);
            // NOTE: We do NOT call NotifyCorrectAttempt here because this is auto-placement.
            // We also skip progress update if the user didn't earn it, but keep internal count.
            
            CheckAndDisableZone(zoneUI);

            if (correctCount >= totalItems)
            {
                FinalizeQuestion(false, 0); // auto-corrected = no points
            }
        }

        /// <summary>Auto-place all remaining items (for full OnAutoCorrect from base).</summary>
        private void AutoPlaceAllRemaining()
        {
            HashSet<int> processedDrags = new HashSet<int>(currentPairings.Keys);
            foreach (var cp in ddData.correctPairings)
            {
                if (processedDrags.Contains(cp.dragIndex)) continue;
                processedDrags.Add(cp.dragIndex);

                DragItemUI itemUI = dragItemUIs.Find(x => x.itemIndex == cp.dragIndex);
                DropZoneUI zoneUI = dropZoneUIs.Find(x => x.zoneIndex == cp.dropIndex);

                if (itemUI != null && zoneUI != null)
                {
                    currentPairings[cp.dragIndex] = cp.dropIndex;

                    // Kill any running tween (e.g. AnimateToHome) so its OnComplete won't snap back
                    itemUI.dragObject.transform.DOKill();
                    
                    itemUI.dragItem.CreatePlaceholderAndLift();
                    
                    itemUI.dragItem.isLocked = true;
                    itemUI.dragObject.GetComponent<CanvasGroup>().blocksRaycasts = false;
                    
                    itemUI.dragObject.transform.SetParent(zoneUI.dropZoneObject.transform, true);

                    LayoutElement le = itemUI.dragObject.GetComponent<LayoutElement>();
                    if (le != null) le.ignoreLayout = false;
                    
                    LayoutRebuilder.ForceRebuildLayoutImmediate(zoneUI.dropZoneObject.GetComponent<RectTransform>());
                    var layoutGroup = zoneUI.dropZoneObject.GetComponent<LayoutGroup>();
                    if (layoutGroup == null)
                    {
                        itemUI.dragObject.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
                    }

                    Image itemImage = itemUI.dragObject.GetComponent<Image>();
                    Image zoneImage = zoneUI.dropZoneObject.GetComponent<Image>();
                    if (itemImage != null) itemImage.color = Color.green;
                    if (zoneImage != null) zoneImage.color = Color.green;

                    CheckAndDisableZone(zoneUI);
                }
            }
        }

        private void CheckAndDisableZone(DropZoneUI zoneUI)
        {
            if (zoneUI == null || zoneUI.dropZoneObject == null) return;

            // Count how many items SHOULD be in this zone
            int requiredCount = 0;
            foreach (var cp in ddData.correctPairings)
                if (cp.dropIndex == zoneUI.zoneIndex) requiredCount++;

            // Count how many correct items are CURRENTLY in this zone
            int currentCountInZone = 0;
            foreach (var p in currentPairings)
                if (p.Value == zoneUI.zoneIndex) currentCountInZone++;

            if (currentCountInZone >= requiredCount)
            {
                // Disable the zone so it doesn't accept more highlights or drops
                CanvasGroup cg = zoneUI.dropZoneObject.GetComponent<CanvasGroup>();
                if (cg == null) cg = zoneUI.dropZoneObject.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
            }
        }

        protected override string GetCorrectAnswerDisplay()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (var pairing in ddData.correctPairings)
            {
                if (pairing.dragIndex < ddData.dragItems.Count && pairing.dropIndex < ddData.dropZones.Count)
                {
                    sb.AppendLine($"{ddData.dragItems[pairing.dragIndex].label} → {ddData.dropZones[pairing.dropIndex].label}");
                }
            }
            return sb.ToString();
        }

        // ──────────────── Lock / Unlock ────────────────

        public override void LockUI()
        {
            base.LockUI();
            foreach (var item in dragItemUIs)
            {
                if (item.dragItem != null)
                    item.dragItem.dragEnabled = false;
            }
        }

        public override void UnlockUI()
        {
            base.UnlockUI();
            foreach (var item in dragItemUIs)
            {
                if (item.dragItem != null && !item.dragItem.isLocked)
                    item.dragItem.dragEnabled = true;
            }
        }
    }
}
