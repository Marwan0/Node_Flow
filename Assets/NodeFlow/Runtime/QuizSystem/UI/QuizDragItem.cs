using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

namespace QuizSystem
{
    /// <summary>
    /// A reusable, SOLID drag component that any question type can attach to its items.
    /// Handles all shared drag mechanics: placeholder, reparent-to-canvas, snap-back, lock/unlock.
    /// Question-specific logic is handled via OnDragStarted / OnItemDropped callbacks.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class QuizDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ──────────────── public state ────────────────

        /// <summary>Index of this item within the question data (e.g. drag item 0, 1, 2...).</summary>
        [HideInInspector] public int itemIndex;

        /// <summary>Whether the item has been locked into a valid drop target.</summary>
        [HideInInspector] public bool isLocked = false;

        /// <summary>Set to false to prevent the user from starting new drags (e.g. during feedback).</summary>
        [HideInInspector] public bool dragEnabled = true;

        // ──────────────── callbacks ────────────────

        /// <summary>Fired when the user begins dragging this item.</summary>
        public System.Action<QuizDragItem> OnDragStarted;

        /// <summary>Fired every frame during drag. Use for hover highlighting etc.</summary>
        public System.Action<QuizDragItem, PointerEventData> OnDragMoved;

        /// <summary>
        /// Fired when the user releases the item.
        /// The question UI should validate the drop and call LockInPlace() or AnimateToHome() accordingly.
        /// If not handled, the item will automatically snap back home.
        /// </summary>
        public System.Action<QuizDragItem, PointerEventData> OnItemDropped;

        // ──────────────── private ────────────────

        private Canvas rootCanvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Transform homeParent;
        private Vector2 homeAnchoredPosition;
        private int homeSiblingIndex;

        private GameObject placeholder;
        private Tweener snapTween;
        private bool isDragCancelled;

        private const float SnapBackDuration = 0.3f;
        private const float DragAlpha = 0.7f;

        // ──────────────── initialisation ────────────────

        /// <summary>Initialise the drag item. Must be called once after instantiation.</summary>
        public void Init(int index, Canvas canvas)
        {
            itemIndex = index;
            rootCanvas = canvas;

            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>
        /// Saves the current parent, anchored position and sibling index as the "home" position.
        /// Call this after the layout system has positioned all items (typically end of frame).
        /// </summary>
        public void SaveHomePosition()
        {
            homeParent = transform.parent;
            homeAnchoredPosition = rectTransform.anchoredPosition;
            homeSiblingIndex = transform.GetSiblingIndex();
        }

        /// <summary>The saved home parent container.</summary>
        public Transform HomeParent => homeParent;

        // ──────────────── drag callbacks ────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!dragEnabled || isLocked)
            {
                isDragCancelled = true;
                return;
            }

            isDragCancelled = false;
            snapTween?.Kill();

            // Create placeholder to prevent layout collapse
            if (placeholder == null)
            {
                placeholder = new GameObject("QuizPlaceholder");
                var rt = placeholder.AddComponent<RectTransform>();
                rt.SetParent(homeParent, false);
                rt.SetSiblingIndex(homeSiblingIndex);
                rt.sizeDelta = rectTransform.rect.size;

                var le = placeholder.AddComponent<LayoutElement>();
                LayoutElement itemLe = GetComponent<LayoutElement>();
                if (itemLe != null)
                {
                    le.preferredWidth = itemLe.preferredWidth >= 0 ? itemLe.preferredWidth : rectTransform.rect.width;
                    le.preferredHeight = itemLe.preferredHeight >= 0 ? itemLe.preferredHeight : rectTransform.rect.height;
                    le.minWidth = itemLe.minWidth;
                    le.minHeight = itemLe.minHeight;
                    le.flexibleWidth = itemLe.flexibleWidth;
                    le.flexibleHeight = itemLe.flexibleHeight;
                }
                else
                {
                    le.preferredWidth = rectTransform.rect.width;
                    le.preferredHeight = rectTransform.rect.height;
                }
            }

            canvasGroup.alpha = DragAlpha;
            canvasGroup.blocksRaycasts = false;

            // Ignore layout so item moves freely
            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement != null) layoutElement.ignoreLayout = true;

            // Reparent to canvas root so it renders on top of everything
            transform.SetParent(rootCanvas.transform, true);
            transform.SetAsLastSibling();

            OnDragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isDragCancelled) return;

            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    rectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPoint))
            {
                rectTransform.position = worldPoint;
            }

            OnDragMoved?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (isDragCancelled)
            {
                isDragCancelled = false;
                return;
            }

            canvasGroup.alpha = 1f;

            // If the item was already locked during this drag (e.g. by IDropHandler.OnDrop
            // which fires before OnEndDrag), don't do anything — it's already placed.
            if (isLocked)
            {
                canvasGroup.blocksRaycasts = false;
                return;
            }

            // Let the question UI decide what to do
            if (OnItemDropped != null)
            {
                OnItemDropped.Invoke(this, eventData);
            }
            else
            {
                // Default: no handler means snap back
                AnimateToHome();
            }
        }

        // ──────────────── public API ────────────────

        /// <summary>
        /// Creates a placeholder at the item's home slot so the source container
        /// doesn't resize, then marks the item as ignoring layout.
        /// Used by auto-correct flows that move items without a user drag.
        /// </summary>
        public void CreatePlaceholderAndLift()
        {
            if (placeholder == null)
            {
                placeholder = new GameObject("QuizPlaceholder");
                var rt = placeholder.AddComponent<RectTransform>();
                rt.SetParent(homeParent, false);
                rt.SetSiblingIndex(homeSiblingIndex);
                rt.sizeDelta = rectTransform.rect.size;

                var le = placeholder.AddComponent<LayoutElement>();
                LayoutElement itemLe = GetComponent<LayoutElement>();
                if (itemLe != null)
                {
                    le.preferredWidth = itemLe.preferredWidth >= 0 ? itemLe.preferredWidth : rectTransform.rect.width;
                    le.preferredHeight = itemLe.preferredHeight >= 0 ? itemLe.preferredHeight : rectTransform.rect.height;
                    le.minWidth = itemLe.minWidth;
                    le.minHeight = itemLe.minHeight;
                    le.flexibleWidth = itemLe.flexibleWidth;
                    le.flexibleHeight = itemLe.flexibleHeight;
                }
                else
                {
                    le.preferredWidth = rectTransform.rect.width;
                    le.preferredHeight = rectTransform.rect.height;
                }
            }

            // Ignore layout so the item can be reparented freely
            var layoutElement = GetComponent<LayoutElement>();
            if (layoutElement != null) layoutElement.ignoreLayout = true;
        }

        /// <summary>Smoothly animate back to the home position in the source container.</summary>
        public void AnimateToHome()
        {
            snapTween?.Kill();

            isLocked = false;
            canvasGroup.blocksRaycasts = true;

            // Ensure we are in root canvas for the animation to avoid LayoutGroup interference
            transform.SetParent(rootCanvas.transform, true);

            Vector3 targetWorldPos = placeholder != null
                ? placeholder.transform.position
                : homeParent.TransformPoint(homeAnchoredPosition);

            snapTween = transform.DOMove(targetWorldPos, SnapBackDuration).SetEase(Ease.OutBack).OnComplete(() =>
            {
                // Reparent into the home container at the placeholder's slot
                transform.SetParent(homeParent, true);
                transform.SetSiblingIndex(homeSiblingIndex);

                // Re-enable layout participation
                var le = GetComponent<LayoutElement>();
                if (le != null) le.ignoreLayout = false;

                // Clean up placeholder (now that the real item occupies the slot)
                DestroyPlaceholder();

                rectTransform.anchoredPosition = homeAnchoredPosition;
                LayoutRebuilder.ForceRebuildLayoutImmediate(homeParent.GetComponent<RectTransform>());
            });
        }

        /// <summary>Instantly place back home (no animation).</summary>
        public void ReturnHomeImmediate()
        {
            snapTween?.Kill();

            isLocked = false;
            canvasGroup.blocksRaycasts = true;

            DestroyPlaceholder();

            transform.SetParent(homeParent, true);
            transform.SetSiblingIndex(homeSiblingIndex);
            
            var le = GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = false;
            
            rectTransform.anchoredPosition = homeAnchoredPosition;
        }

        /// <summary>
        /// Locks the item into a container (e.g. a drop zone or ordering slot).
        /// The item will no longer be draggable until unlocked.
        /// </summary>
        public void LockInPlace(Transform container)
        {
            snapTween?.Kill();

            isLocked = true;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;

            transform.SetParent(container, false);
            
            var le = GetComponent<LayoutElement>();
            if (le != null) le.ignoreLayout = false;
        }

        /// <summary>Unlocks the item so the user can drag it again.</summary>
        public void Unlock()
        {
            isLocked = false;
            canvasGroup.blocksRaycasts = true;
        }

        /// <summary>Tints the item's Image component with the given color.</summary>
        public void TintImage(Color color)
        {
            var img = GetComponent<Image>();
            if (img != null) img.color = color;
        }

        /// <summary>Destroys the placeholder safely.</summary>
        public void DestroyPlaceholder()
        {
            if (placeholder != null)
            {
                SafeDestroy(placeholder);
                placeholder = null;
            }
        }

        /// <summary>Whether a placeholder currently exists for this item.</summary>
        public bool HasPlaceholder => placeholder != null;

        // ──────────────── internal ────────────────

        /// <summary>Destroys a GameObject safely in the Editor by deselecting it first.</summary>
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
                var selected = new System.Collections.Generic.List<Object>(UnityEditor.Selection.objects);
                if (selected.Contains(obj))
                {
                    selected.Remove(obj);
                    UnityEditor.Selection.objects = selected.ToArray();
                }
            }
#endif
            Destroy(obj);
        }
    }
}
