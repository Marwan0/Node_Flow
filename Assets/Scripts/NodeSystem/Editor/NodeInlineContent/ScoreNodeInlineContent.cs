#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class ScoreNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as ScoreNode;
            if (node == null) return;

            // Operation dropdown
            CreateEnumField("Operation", node.operation, v => 
            {
                node.operation = v;
                RequestRefresh();
            });

            // Value (except for Reset)
            if (node.operation != ScoreOperation.Reset)
            {
                CreateIntField("Value", node.value, v => node.value = v);
            }

            // Threshold branching
            CreateToggle("Branch on Threshold", node.branchOnThreshold, v => 
            {
                node.branchOnThreshold = v;
                RequestRefresh();
            });

            if (node.branchOnThreshold)
            {
                CreateIntField("Threshold", node.threshold, v => node.threshold = v);
                CreateToggle("Use Percentage", node.usePercentage, v => node.usePercentage = v);
            }
        }
    }
}
#endif
