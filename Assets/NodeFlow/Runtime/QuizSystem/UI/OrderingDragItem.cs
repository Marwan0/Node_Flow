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
        [HideInInspector] public OrderingDropSlot currentSlot;

        private Canvas rootCanvas;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Transform originalParent;
        private Vector2 homePosition;
        private int homeSiblingIndex;
        private OrderingUI orderingUI;

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
            SaveHomePosition();

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
            canvasGroup.blocksRaycasts = true;

            // If not placed in a slot by OrderingDropSlot.OnDrop, snap back
            if (currentSlot == null)
            {
                AnimateToHome();
            }
        }

        // ──────────────── placement ────────────────

        /// <summary>Smoothly animate into a drop slot.</summary>
        public void PlaceInSlot(OrderingDropSlot slot)
        {
            // Vacate previous slot
            if (currentSlot != null)
                currentSlot.Clear();

            currentSlot = slot;
            slot.Accept(this);

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            transform.SetParent(slot.transform, true);
            rectTransform.DOAnchorPos(Vector2.zero, DropDuration).SetEase(Ease.OutQuad);
        }

        /// <summary>Smoothly animate back to the source container.</summary>
        public void AnimateToHome()
        {
            if (currentSlot != null)
            {
                currentSlot.Clear();
                currentSlot = null;
            }

            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(homeSiblingIndex);
            rectTransform.DOAnchorPos(homePosition, SnapBackDuration).SetEase(Ease.OutBack);

            orderingUI?.OnItemReturnedToSource(this);
        }

        /// <summary>Instantly place back home (no animation).</summary>
        public void ReturnHomeImmediate()
        {
            if (currentSlot != null)
            {
                currentSlot.Clear();
                currentSlot = null;
            }

            transform.SetParent(originalParent, true);
            transform.SetSiblingIndex(homeSiblingIndex);
            rectTransform.anchoredPosition = homePosition;
        }
    }
}
