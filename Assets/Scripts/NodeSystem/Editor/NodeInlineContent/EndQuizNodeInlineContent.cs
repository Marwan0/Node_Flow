#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class EndQuizNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as EndQuizNode;
            if (node == null) return;

            // Action dropdown
            CreateEnumField("Action", node.action, v => 
            {
                node.action = v;
                MarkDirty();
            });

            // Performance branching
            CreateToggle("Branch on Performance", node.branchOnPerformance, v => 
            {
                node.branchOnPerformance = v;
                RequestRefresh();
            });

            if (node.branchOnPerformance)
            {
                CreateFloatField("Passing %", node.passingPercentage, v => 
                {
                    node.passingPercentage = Mathf.Clamp(v, 0f, 100f);
                });
            }
        }
    }
}
#endif
