using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Node to activate or deactivate a GameObject
    /// </summary>
    [Serializable]
    public class SetActiveNode : NodeData
    {
        [SerializeField]
        public string targetPath = "";
        
        [SerializeField]
        public bool setActive = true;

        public override string Name => "Set Active";
        public override Color Color => new Color(0.4f, 0.8f, 0.4f); // Green
        public override string Category => "UI";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "On Complete", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(targetPath))
            {
                Debug.LogWarning("[SetActiveNode] No target specified");
                Complete();
                return;
            }

            // Find the target GameObject (including disabled objects)
            GameObject target = FindGameObject(targetPath);

            if (target == null)
            {
                Debug.LogError($"[SetActiveNode] Target not found: {targetPath}");
                Complete();
                return;
            }

            // Set active state
            target.SetActive(setActive);
            
            Complete();
        }

        /// <summary>
        /// Find GameObject by path, including disabled objects
        /// </summary>
        private GameObject FindGameObject(string path)
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
