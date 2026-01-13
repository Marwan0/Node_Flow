#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class AnimationNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as AnimationNode;
            if (node == null) return;

            // Target GameObject - use ObjectField for better UX
            // Convert path to GameObject if possible, otherwise show as text
            GameObject currentTarget = null;
            if (!string.IsNullOrEmpty(node.targetPath))
            {
                // Try to find the GameObject by path
                currentTarget = GameObject.Find(node.targetPath);
                // If not found by name, try to find in scene by hierarchy path
                if (currentTarget == null)
                {
                    var parts = node.targetPath.Split('/');
                    if (parts.Length > 0)
                    {
                        currentTarget = GameObject.Find(parts[parts.Length - 1]);
                    }
                }
            }
            
            CreateObjectField<GameObject>("Target", currentTarget, (GameObject go) =>
            {
                if (go != null)
                {
                    // Convert GameObject to hierarchy path
                    node.targetPath = GetGameObjectPath(go);
                }
                else
                {
                    node.targetPath = "";
                }
                MarkDirty();
            });

            // Animation type
            CreateEnumField("Type", node.animationType, (AnimationType v) => 
            {
                node.animationType = v;
                MarkDirty();
                RequestRefresh();
            });

            // Slide direction - only for slide animations
            if (node.animationType == AnimationType.SlideIn || node.animationType == AnimationType.SlideOut)
            {
                CreateEnumField("Direction", node.slideDirection, (SlideDirection v) => node.slideDirection = v);
            }

            // Duration
            CreateFloatField("Duration", node.duration, v => node.duration = Mathf.Max(0, v));

            // Delay
            CreateFloatField("Delay", node.delay, v => node.delay = Mathf.Max(0, v));
        }
        
        /// <summary>
        /// Get the full hierarchy path of a GameObject
        /// </summary>
        private string GetGameObjectPath(GameObject go)
        {
            if (go == null) return "";
            
            string path = go.name;
            Transform current = go.transform.parent;
            
            while (current != null)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            
            return path;
        }
    }
}
#endif

