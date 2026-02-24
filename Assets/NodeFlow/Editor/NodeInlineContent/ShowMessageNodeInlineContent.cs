#if UNITY_EDITOR
using NodeSystem.Nodes;
using UnityEngine;

namespace NodeSystem.Editor
{
    public class ShowMessageNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as ShowMessageNode;
            if (node == null) return;

            CreateMultilineTextField(node.message, v => node.message = v, "Message...");
            CreateFloatField("Duration", node.duration, v => node.duration = Mathf.Max(0.1f, v));
            CreateTextField(node.targetPath, v => node.targetPath = v, "Target GameObject path (optional)...");
        }
    }
}
#endif
