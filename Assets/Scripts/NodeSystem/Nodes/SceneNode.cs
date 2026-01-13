using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Scene operation types
    /// </summary>
    public enum SceneOperation
    {
        Load,
        Unload,
        LoadAdditive,
        SetActive
    }

    /// <summary>
    /// Loads or unloads scenes
    /// </summary>
    [Serializable]
    public class SceneNode : NodeData
    {
        [SerializeField]
        public SceneOperation operation = SceneOperation.Load;
        
        [SerializeField]
        public string sceneName = "";
        
        [SerializeField]
        public bool waitForCompletion = true;

        public override string Name => "Scene";
        public override Color Color => new Color(0.5f, 0.7f, 0.9f); // Light Blue
        public override string Category => "Scene";

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
                new PortData("output", "Next", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[SceneNode] No scene name specified");
                Complete();
                return;
            }

            Runner?.StartCoroutine(ExecuteSceneOperation());
        }

        private IEnumerator ExecuteSceneOperation()
        {
            AsyncOperation asyncOp = null;

            switch (operation)
            {
                case SceneOperation.Load:
                    Debug.Log($"[SceneNode] Loading scene: {sceneName}");
                    asyncOp = SceneManager.LoadSceneAsync(sceneName);
                    break;

                case SceneOperation.LoadAdditive:
                    Debug.Log($"[SceneNode] Loading scene additively: {sceneName}");
                    asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    break;

                case SceneOperation.Unload:
                    Debug.Log($"[SceneNode] Unloading scene: {sceneName}");
                    asyncOp = SceneManager.UnloadSceneAsync(sceneName);
                    break;

                case SceneOperation.SetActive:
                    var scene = SceneManager.GetSceneByName(sceneName);
                    if (scene.IsValid())
                    {
                        SceneManager.SetActiveScene(scene);
                        Debug.Log($"[SceneNode] Set active scene: {sceneName}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SceneNode] Scene not found: {sceneName}");
                    }
                    break;
            }

            if (asyncOp != null && waitForCompletion)
            {
                while (!asyncOp.isDone)
                {
                    yield return null;
                }
                Debug.Log($"[SceneNode] Scene operation complete: {sceneName}");
            }
            else if (asyncOp == null && operation == SceneOperation.SetActive)
            {
                // SetActive is synchronous
            }

            Complete();
        }
    }
}

