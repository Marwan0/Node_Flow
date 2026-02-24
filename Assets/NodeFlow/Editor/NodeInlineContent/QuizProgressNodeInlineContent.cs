#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class QuizProgressNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as QuizProgressNode;
            if (node == null) return;

            // Check type dropdown
            CreateEnumField("Check", node.checkType, v => 
            {
                node.checkType = v;
                MarkDirty();
            });

            // Threshold
            CreateIntField("Threshold", node.threshold, v => node.threshold = v);

            // Branch option
            CreateToggle("Branch on Result", node.branchOnThreshold, v => 
            {
                node.branchOnThreshold = v;
                RequestRefresh();
            });
        }
    }
}
#endif
