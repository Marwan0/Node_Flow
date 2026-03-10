using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Changes the sprite and/or color of a UI Image or SpriteRenderer
    /// </summary>
    [Serializable]
    public class ChangeSpriteNode : NodeData
    {
        [SerializeField]
        public string targetPath = "";

        [SerializeField]
        public Sprite sprite;

        [SerializeField]
        public bool changeColor = false;

        [SerializeField]
        public Color targetColor = Color.white;

        public override string Name => "Change Sprite";
        public override Color Color => new Color(0.7f, 0.4f, 0.8f); // Purple
        public override string Category => "UI";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input, PortCapacity.Multi)
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
            try
            {
                if (string.IsNullOrEmpty(targetPath))
                {
                    Debug.LogWarning("[ChangeSpriteNode] No target path specified");
                    Complete();
                    return;
                }

                GameObject target = FindGameObject(targetPath);
                if (target == null)
                {
                    Debug.LogWarning($"[ChangeSpriteNode] Target not found: {targetPath}");
                    Complete();
                    return;
                }

                // Try Image (UI) first, then SpriteRenderer (2D)
                var image = target.GetComponent<Image>();
                if (image != null)
                {
                    if (sprite != null)
                        image.sprite = sprite;
                    if (changeColor)
                        image.color = targetColor;
                    Complete();
                    return;
                }

                var spriteRenderer = target.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    if (sprite != null)
                        spriteRenderer.sprite = sprite;
                    if (changeColor)
                        spriteRenderer.color = targetColor;
                    Complete();
                    return;
                }

                Debug.LogWarning($"[ChangeSpriteNode] No Image or SpriteRenderer on: {targetPath}");
                Complete();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ChangeSpriteNode] Error: {ex.Message}");
                Complete();
            }
        }

        private GameObject FindGameObject(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Fast path (active objects only)
            var found = GameObject.Find(path);
            if (found != null) return found;

            // Search through all root GameObjects (includes disabled)
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
