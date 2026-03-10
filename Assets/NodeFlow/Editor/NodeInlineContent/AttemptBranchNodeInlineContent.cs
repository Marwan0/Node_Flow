#if UNITY_EDITOR
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class AttemptBranchNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as AttemptBranchNode;
            if (node == null) return;

            CreateIntField("Attempts", node.maxOutputs, v =>
            {
                int clamped = UnityEngine.Mathf.Clamp(v, 2, 10);
                if (clamped != node.maxOutputs)
                {
                    node.maxOutputs = clamped;
                    RequestRefresh();
                }
            });
        }
    }
}
#endif
