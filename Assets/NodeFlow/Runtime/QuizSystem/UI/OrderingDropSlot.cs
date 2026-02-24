using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuizSystem
{
    public class OrderingDropSlot : MonoBehaviour, IDropHandler
    {
        [HideInInspector] public int slotIndex;
        [HideInInspector] public OrderingDragItem occupant;

        private OrderingUI orderingUI;
        private Image slotImage;
        private Color defaultColor;
        [HideInInspector] public bool isLocked = false;

        public bool IsOccupied => occupant != null;

        public void Init(int index, OrderingUI ui)
        {
            slotIndex = index;
            orderingUI = ui;
            slotImage = GetComponent<Image>();
            if (slotImage != null)
                defaultColor = slotImage.color;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (isLocked) return;

            var dragItem = eventData.pointerDrag?.GetComponent<OrderingDragItem>();
            if (dragItem == null) return;

            // Only allow drops into the current active slot
            if (orderingUI != null && orderingUI.currentSlotIndex != slotIndex)
            {
                // Let the item snap back; do not accept it
                return;
            }

            // If this slot already has an item, send it home first
            if (occupant != null && occupant != dragItem)
                occupant.AnimateToHome();

            dragItem.PlaceInSlot(this);
            orderingUI?.OnItemPlacedInSlot(dragItem, this);
        }

        public void Accept(OrderingDragItem item)
        {
            occupant = item;
        }

        public void Clear()
        {
            occupant = null;
        }

        public void SetHighlight(Color color)
        {
            if (slotImage != null)
                slotImage.color = color;
        }

        public void ResetHighlight()
        {
            if (slotImage != null)
                slotImage.color = defaultColor;
        }
    }
}
