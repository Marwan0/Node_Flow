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
        
        private Canvas rootCanvas;

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

            // Clear hover state
            if (currentlyHoveredZone != null && currentlyHoveredZone.dropZoneObject != null)
            {
                Image img = currentlyHoveredZone.dropZoneObject.GetComponent<Image>();
                if (img != null) img.color = defaultZoneColor;
                currentlyHoveredZone = null;
            }

            // If it wasn't dropped into a valid zone (OnDrop would have set the pairing)
            if (!currentPairings.ContainsKey(itemUI.itemIndex))
            {
                if (snapBackOnInvalidDrop)
                {
                    itemUI.dragItem.AnimateToHome();
                }
                else
                {
                    itemUI.dragItem.DestroyPlaceholder();
                    LayoutElement le = itemUI.dragObject.GetComponent<LayoutElement>();
                    if (le != null) le.ignoreLayout = false;
                }
            }
        }

        private void OnDrop(DropZoneUI zoneUI, PointerEventData eventData)
        {
            if (currentlyDragging != null)
            {
                // Record the drop
                currentPairings[currentlyDragging.itemIndex] = zoneUI.zoneIndex;

                // Clean up placeholder and parent to the drop zone
                currentlyDragging.dragItem.DestroyPlaceholder();
                currentlyDragging.dragObject.transform.SetParent(zoneUI.dropZoneObject.transform, true);

                LayoutElement le = currentlyDragging.dragObject.GetComponent<LayoutElement>();
                if (le != null) le.ignoreLayout = false;

                // Restore raycast blocking so it can be picked up again
                CanvasGroup cg = currentlyDragging.dragObject.GetComponent<CanvasGroup>();
                if (cg != null) cg.blocksRaycasts = true;

                // Animate drop if smooth snapping enabled
                if (enableSmoothSnapping)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(zoneUI.dropZoneObject.GetComponent<RectTransform>());
                    
                    var layoutGroup = zoneUI.dropZoneObject.GetComponent<LayoutGroup>();
                    if (layoutGroup == null)
                    {
                        currentlyDragging.dragObject.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
                    }
                }
                else
                {
                    var layoutGroup = zoneUI.dropZoneObject.GetComponent<LayoutGroup>();
                    if (layoutGroup == null)
                    {
                        currentlyDragging.dragObject.transform.localPosition = Vector3.zero;
                    }
                }

                UpdateVisualFeedback();
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
                    newHoveredZone = zoneUI;
                    break;
                }
            }

            if (newHoveredZone != currentlyHoveredZone)
            {
                if (currentlyHoveredZone != null && currentlyHoveredZone.dropZoneObject != null)
                {
                    Image oldImg = currentlyHoveredZone.dropZoneObject.GetComponent<Image>();
                    if (oldImg != null) oldImg.color = defaultZoneColor;
                }
                
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
            if (submitButton != null)
                submitButton.interactable = false;

            var result = validator.ValidateAnswer(currentPairings);
            HandleValidationResult(result);
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
            currentPairings.Clear();
            HashSet<int> processedDrags = new HashSet<int>();
            foreach (var correctPairing in ddData.correctPairings)
            {
                if (processedDrags.Contains(correctPairing.dragIndex)) continue;
                processedDrags.Add(correctPairing.dragIndex);
                
                currentPairings[correctPairing.dragIndex] = correctPairing.dropIndex;

                DragItemUI itemUI = dragItemUIs.Find(x => x.itemIndex == correctPairing.dragIndex);
                DropZoneUI zoneUI = dropZoneUIs.Find(x => x.zoneIndex == correctPairing.dropIndex);

                if (itemUI != null && zoneUI != null)
                {
                    itemUI.dragItem.DestroyPlaceholder();
                    itemUI.dragObject.transform.SetParent(zoneUI.dropZoneObject.transform, false);
                    LayoutElement le = itemUI.dragObject.GetComponent<LayoutElement>();
                    if (le != null) le.ignoreLayout = false;
                    
                    Image itemImage = itemUI.dragObject.GetComponent<Image>();
                    Image zoneImage = zoneUI.dropZoneObject.GetComponent<Image>();
                    if (itemImage != null) itemImage.color = Color.green;
                    if (zoneImage != null) zoneImage.color = Color.green;
                }
            }
            UpdateVisualFeedback();
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
