#if UNITY_EDITOR
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class ButtonActivationNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as ButtonActivationNode;
            if (node == null) return;

            // Button path
            CreateTextField(node.buttonPath, v => node.buttonPath = v, "Button GameObject path...");

            // Set interactable
            CreateToggle("Interactable", node.setInteractable, v => node.setInteractable = v);
        }
    }
}
#endif

