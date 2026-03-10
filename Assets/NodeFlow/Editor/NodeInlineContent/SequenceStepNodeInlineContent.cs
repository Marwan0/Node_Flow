#if UNITY_EDITOR
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class SequenceStepNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as SequenceStepNode;
            if (node == null) return;

            CreateIntField("Steps", node.stepCount, v =>
            {
                int clamped = UnityEngine.Mathf.Clamp(v, 2, 10);
                if (clamped != node.stepCount)
                {
                    node.stepCount = clamped;
                    RequestRefresh();
                }
            });

            CreateEnumField("After Last", node.wrapMode, (SequenceWrapMode v) =>
            {
                if (v != node.wrapMode)
                {
                    node.wrapMode = v;
                    RequestRefresh();
                }
            });
        }
    }
}
#endif
