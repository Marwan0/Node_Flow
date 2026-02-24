#if UNITY_EDITOR
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class DebugLogNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as DebugLogNode;
            if (node == null) return;

            // Log type
            CreateEnumField("Type", node.logType, (DebugLogNode.LogType v) => node.logType = v);

            // Message
            CreateMultilineTextField(node.message, v => node.message = v, "Message...");
        }
    }
}
#endif
