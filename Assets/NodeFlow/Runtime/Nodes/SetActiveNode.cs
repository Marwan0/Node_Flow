using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NodeSystem.Nodes
{
    public enum SetActiveMode
    {
        Single,
        List,
        Random
    }

    /// <summary>
    /// Node to activate or deactivate GameObjects.
    /// Supports single target, a list of targets, or a random pick from a list.
    /// </summary>
    [Serializable]
    public class SetActiveNode : NodeData
    {
        [SerializeField]
        public SetActiveMode mode = SetActiveMode.Single;

        [SerializeField]
        public string targetPath = "";

        [SerializeField]
        public bool setActive = true;

        [SerializeField]
        public List<string> targetPaths = new List<string>();

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
            switch (mode)
            {
                case SetActiveMode.Single:
                    ExecuteSingle();
                    break;
                case SetActiveMode.List:
                    ExecuteList();
                    break;
                case SetActiveMode.Random:
                    ExecuteRandom();
                    break;
            }

            Complete();
        }

        private void ExecuteSingle()
        {
            SetActiveByPath(targetPath);
        }

        private void ExecuteList()
        {
            if (targetPaths == null || targetPaths.Count == 0)
            {
                Debug.LogWarning("[SetActiveNode] No targets in list");
                return;
            }

            foreach (var path in targetPaths)
            {
                SetActiveByPath(path);
            }
        }

        private void ExecuteRandom()
        {
            if (targetPaths == null || targetPaths.Count == 0)
            {
                Debug.LogWarning("[SetActiveNode] No targets in list for random selection");
                return;
            }

            int randomIndex = UnityEngine.Random.Range(0, targetPaths.Count);
            string chosenPath = targetPaths[randomIndex];

            // Deactivate all others, activate the chosen one
            for (int i = 0; i < targetPaths.Count; i++)
            {
                if (string.IsNullOrEmpty(targetPaths[i])) continue;

                GameObject go = FindGameObject(targetPaths[i]);
                if (go == null) continue;

                bool shouldBeActive = (i == randomIndex) ? setActive : !setActive;
                ClearEditorSelectionIfNeeded(go, shouldBeActive);
                go.SetActive(shouldBeActive);
            }
        }

        private void SetActiveByPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("[SetActiveNode] No target specified");
                return;
            }

            GameObject target = FindGameObject(path);

            if (target == null)
            {
                Debug.LogError($"[SetActiveNode] Target not found: {path}");
                return;
            }

            ClearEditorSelectionIfNeeded(target, setActive);
            target.SetActive(setActive);
        }

        private void ClearEditorSelectionIfNeeded(GameObject target, bool willBeActive)
        {
#if UNITY_EDITOR
            if (!willBeActive)
            {
                var sel = Selection.activeGameObject;
                if (sel != null && (sel == target || sel.transform.IsChildOf(target.transform)))
                {
                    Selection.activeGameObject = null;
                }
            }
#endif
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
