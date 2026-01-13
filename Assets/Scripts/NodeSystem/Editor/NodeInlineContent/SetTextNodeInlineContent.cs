#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class SetTextNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as SetTextNode;
            if (node == null) return;

            // Target path - editable
            CreateTextField(node.targetPath, v => node.targetPath = v, "Target GameObject path...");

            // Text content
            CreateTextField(node.text, v => node.text = v, "Text content...");

            // Typewriter effect toggle
            CreateToggle("Typewriter", node.typewriterEffect, v => 
            {
                node.typewriterEffect = v;
                MarkDirty();
                RequestRefresh();
            });

            // Typewriter speed - only shown when typewriter is enabled
            if (node.typewriterEffect)
            {
                CreateFloatField("Speed", node.typewriterSpeed, v => node.typewriterSpeed = Mathf.Max(0.001f, v));
            }
        }
    }
}
#endif

