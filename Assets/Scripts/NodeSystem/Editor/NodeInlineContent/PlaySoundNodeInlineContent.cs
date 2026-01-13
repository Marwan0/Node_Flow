#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class PlaySoundNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as PlaySoundNode;
            if (node == null) return;

            // Audio source path - editable
            CreateTextField(node.audioSourcePath, v => node.audioSourcePath = v, "AudioSource path (optional)...");

            // Clip name (Resources path)
            CreateTextField(node.clipName, v => node.clipName = v, "Clip name (Resources)...");

            // Volume slider
            CreateSlider("Volume", node.volume, 0f, 1f, v => node.volume = v);

            // Wait for completion toggle
            CreateToggle("Wait for completion", node.waitForCompletion, v => node.waitForCompletion = v);
        }
    }
}
#endif

