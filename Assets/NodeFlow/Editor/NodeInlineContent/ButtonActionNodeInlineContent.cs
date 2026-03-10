#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class ButtonActionNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as ButtonActionNode;
            if (node == null) return;

            // Button GameObject - use ObjectField for drag-and-drop from hierarchy
            GameObject currentTarget = null;
            if (!string.IsNullOrEmpty(node.buttonPath))
            {
                currentTarget = FindGameObjectByPath(node.buttonPath);
            }

            CreateObjectField<GameObject>("", currentTarget, (GameObject go) =>
            {
                if (go != null)
                {
                    node.buttonPath = GetGameObjectPath(go);
                }
                else
                {
                    node.buttonPath = "";
                }
                MarkDirty();
            });

            // Disable after click
            CreateToggle("Disable after click", node.disableAfterClick, v => node.disableAfterClick = v);
        }

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

        private GameObject FindGameObjectByPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            var found = GameObject.Find(path);
            if (found != null) return found;

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
