#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class RandomBranchNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as RandomBranchNode;
            if (node == null) return;

            // Output count (number of random branches)
            CreateIntField("Outputs", node.outputCount, v => 
            {
                node.outputCount = Mathf.Clamp(v, 2, 10);
                MarkDirty();
            });
            
            // Wait for branch to complete (important for Parallel)
            CreateToggle("Wait for branch", node.waitForBranch, v => node.waitForBranch = v);
            
            CreateLabel($"🎲 Random 1 of {node.outputCount}", new Color(0.8f, 0.6f, 0.3f));
        }
    }
}
#endif

