using UnityEngine;
using UnityEngine.EventSystems;

namespace QuizSystem
{
    public class OrderingContainerDropZone : MonoBehaviour, IDropHandler
    {
        private OrderingUI orderingUI;

        public void Init(OrderingUI ui)
        {
            orderingUI = ui;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (orderingUI == null) return;

            var dragItem = eventData.pointerDrag?.GetComponent<QuizDragItem>();
            if (dragItem == null) return;

            // Ignore drops from items whose drag was cancelled by the lock
            if (!dragItem.dragEnabled) return;

            // Forward the drop to the current active slot in OrderingUI
            orderingUI.TryDropItemIntoCurrentSlot(dragItem);
        }
    }
}
