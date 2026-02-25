using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

namespace QuizSystem
{
    [RequireComponent(typeof(CanvasGroup))]
    public class OrderingDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [HideInInspector] public int originalIndex;
        [HideInInspector] public bool isLocked = false;

        private Canvas rootCanvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Transform originalParent;
        private Vector2 homePosition;
        private int homeSiblingIndex;
        private OrderingUI orderingUI;

        private GameObject placeholder;
        private Tweener snapTween;

        private const float SnapBackDuration = 0.3f;
        private const float DropDuration = 0.15f;
        private const float DragAlpha = 0.7f;

        public void Init(int index, OrderingUI ui, Canvas canvas)
        {
            originalIndex = index;
            orderingUI = ui;
            rootCanvas = canvas;

            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void SaveHomePosition()
        {
            originalParent = transform.parent;
            homePosition = rectTransform.anchoredPosition;
            homeSiblingIndex = transform.GetSiblingIndex();
        }

        // ──────────────── drag callbacks ────────────────

        public void OnBeginDrag(PointerEventData eventData)
        {
            snapTween?.Kill();

            if (placeholder == null)
            {
                placeholder = new GameObject("OrderingPlaceholder");
                var rt = placeholder.AddComponent<RectTransform>();
                rt.SetParent(originalParent, false);
                rt.SetSiblingIndex(homeSiblingIndex);
                rt.sizeDelta = rectTransform.rect.size;

                var le = placeholder.AddComponent<LayoutElement>();
                le.preferredWidth = rectTransform.rect.width;
                le.preferredHeight = rectTransform.rect.height;
            }

            canvasGroup.alpha = DragAlpha;
            canvasGroup.blocksRaycasts = false;

            // Reparent to canvas root so it renders on top of everything
            transform.SetParent(rootCanvas.transform, true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            canvasGroup.alpha = 1f;

            if (!isLocked)
            {
                canvasGroup.blocksRaycasts = true;
                AnimateToHome();
            }
            else
            {
                canvasGroup.blocksRaycasts = false;
            }
        }

        // ──────────────── placement ────────────────

        /// <summary>Locks the item into its correct sequential spot in the container.</summary>
        public void LockInContainer(Transform container)
        {
            snapTween?.Kill();

            isLocked = true;
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;

            transform.SetParent(container, false);
            
            // Unity's layout group will automatically format the child since it's now parented
        }

        /// <summary>Smoothly animate back to the source container.</summary>
        public void AnimateToHome()
        {
            snapTween?.Kill();

            isLocked = false;
            
            // Re-enable raycasts after it gets home
            canvasGroup.blocksRaycasts = true;

            // Animate in root canvas to avoid LayoutGroup interference
            transform.SetParent(rootCanvas.transform, true);
            
            Vector3 targetWorldPos = placeholder != null ? placeholder.transform.position : originalParent.TransformPoint(homePosition);

            snapTween = transform.DOMove(targetWorldPos, SnapBackDuration).SetEase(Ease.OutBack).OnComplete(() =>
            {
                if (placeholder != null)
                {
                    Destroy(placeholder);
                    placeholder = null;
                }
                transform.SetParent(originalParent, true);
                transform.SetSiblingIndex(homeSiblingIndex);
                rectTransform.anchoredPosition = homePosition;
                orderingUI?.OnItemReturnedToSource(this);
            });
        }

        /// <summary>Instantly place back home (no animation).</summary>
        public void ReturnHomeImmediate()
        {
            snapTween?.Kill();

            isLocked = false;
            canvasGroup.blocksRaycasts = true;

            if (placeholder != null)
            {
                Destroy(placeholder);
                placeholder = null;
            }

            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(homeSiblingIndex);
            rectTransform.anchoredPosition = homePosition;
        }
    }
}
