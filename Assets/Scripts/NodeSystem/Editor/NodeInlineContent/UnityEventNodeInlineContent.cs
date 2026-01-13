#if UNITY_EDITOR
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class UnityEventNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as UnityEventNode;
            if (node == null) return;

            // Event holder path
            CreateTextField(node.eventHolderPath, v => node.eventHolderPath = v, "EventHolder GameObject path...");

            // Event name
            CreateTextField(node.eventName, v => node.eventName = v, "Event name...");
        }
    }
}
#endif

