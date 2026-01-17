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
            // Convert path to GameObject if possible (including disabled objects)
            GameObject currentTarget = null;
            if (!string.IsNullOrEmpty(node.targetPath))
            {
                currentTarget = FindGameObjectByPath(node.targetPath);
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

        /// <summary>
        /// Find a GameObject by hierarchy path, including disabled objects
        /// </summary>
        private GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // First try the fast method (only works for active objects)
            var found = GameObject.Find(path);
            if (found != null) return found;

            // Search through all root GameObjects in loaded scenes (includes disabled)
            string[] pathParts = path.Split('/');
            string rootName = pathParts[0];

            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    if (rootGo.name == rootName)
                    {
                        if (pathParts.Length == 1)
                            return rootGo;

                        Transform current = rootGo.transform;
                        for (int j = 1; j < pathParts.Length; j++)
                        {
                            current = current.Find(pathParts[j]);
                            if (current == null) break;
                        }

                        if (current != null)
                            return current.gameObject;
                    }
                }
            }

            // Fallback: search by name only
            string targetName = pathParts[pathParts.Length - 1];
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    var result = FindInHierarchy(rootGo.transform, targetName);
                    if (result != null) return result;
                }
            }

            return null;
        }

        private GameObject FindInHierarchy(Transform parent, string name)
        {
            if (parent.name == name) return parent.gameObject;
            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindInHierarchy(parent.GetChild(i), name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
#endif

