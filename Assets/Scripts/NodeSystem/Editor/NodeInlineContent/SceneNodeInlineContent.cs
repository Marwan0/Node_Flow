#if UNITY_EDITOR
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class SceneNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as SceneNode;
            if (node == null) return;

            // Operation type
            CreateEnumField("Operation", node.operation, (SceneOperation v) => node.operation = v);

            // Scene name
            CreateTextField(node.sceneName, v => node.sceneName = v, "Scene name...");

            // Wait for completion
            CreateToggle("Wait for completion", node.waitForCompletion, v => node.waitForCompletion = v);
        }
    }
}
#endif

