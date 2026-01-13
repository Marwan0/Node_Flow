#if UNITY_EDITOR
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class ButtonActionNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as ButtonActionNode;
            if (node == null) return;

            // Button path
            CreateTextField(node.buttonPath, v => node.buttonPath = v, "Button GameObject path...");

            // Disable after click
            CreateToggle("Disable after click", node.disableAfterClick, v => node.disableAfterClick = v);
        }
    }
}
#endif

