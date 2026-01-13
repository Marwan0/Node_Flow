#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class LoopNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as LoopNode;
            if (node == null) return;

            // Loop type
            CreateEnumField("Type", node.loopType, (LoopType v) => 
            {
                node.loopType = v;
                MarkDirty();
                RequestRefresh();
            });

            // Show different fields based on loop type
            switch (node.loopType)
            {
                case LoopType.Count:
                    // Loop count
                    CreateIntField("Count", node.loopCount, v => node.loopCount = Mathf.Max(1, v));
                    break;
                    
                case LoopType.Condition:
                    // Condition variable
                    CreateTextField(node.conditionVariable, v => node.conditionVariable = v, "Variable name...");
                    // Condition value (True/False)
                    CreateDropdown("While", node.conditionValue ? 0 : 1, new[] { "True", "False" },
                        i => node.conditionValue = i == 0);
                    break;
                    
                case LoopType.Infinite:
                    // No additional fields needed
                    CreateLabel("⚠️ Loop until stopped", new Color(1f, 0.8f, 0.4f));
                    break;
            }
        }
    }
}
#endif

