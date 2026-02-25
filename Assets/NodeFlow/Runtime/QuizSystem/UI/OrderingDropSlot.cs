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
            var dragItem = eventData.pointerDrag?.GetComponent<OrderingDragItem>();
            if (dragItem == null) return;

            // In sequential mode, dropping on ANY slot (even locked ones) 
            // should just evaluate the item for the *current active slot*.
            if (orderingUI != null)
            {
                orderingUI.TryDropItemIntoCurrentSlot(dragItem);
            }
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
