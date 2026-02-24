#if UNITY_EDITOR
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class DelayNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as DelayNode;
            if (node == null) return;

            CreateSlider("", node.delaySeconds, 0f, 10f, v => node.delaySeconds = v);
        }
    }
}
#endif

