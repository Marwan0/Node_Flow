#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class SubGraphNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as SubGraphNode;
            if (node == null) return;

            // Sub-graph reference
            CreateObjectField<NodeGraph>("Graph", node.subGraph, g => node.subGraph = g);

            // Show sub-graph info
            if (node.subGraph != null)
            {
                int nodeCount = node.subGraph.Nodes?.Count ?? 0;
                CreateLabel($"📊 {nodeCount} nodes", new Color(0.6f, 0.8f, 0.6f));
            }
            else
            {
                CreateLabel("⚠️ No graph assigned", new Color(1f, 0.5f, 0.5f));
            }
        }
    }
}
#endif

