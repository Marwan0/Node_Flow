using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NodeSystem
{
    /// <summary>
    /// UI-friendly scene reload helper.
    /// Attach to a GameObject and wire public methods to a Button OnClick event.
    /// </summary>
    [AddComponentMenu("Node System/UI/Scene Reload Button Action")]
    public class SceneReloadButtonAction : MonoBehaviour
    {
        [Header("Optional Defaults")]
        [Tooltip("If enabled, ReloadConfiguredScene() reloads this scene name. If disabled or empty, it reloads the active scene.")]
        [SerializeField] private bool useConfiguredSceneName;

        [Tooltip("Scene name used by ReloadConfiguredScene() when enabled.")]
        [SerializeField] private string sceneName = "";

        [Header("Loading")]
        [Tooltip("If enabled, scenes are loaded asynchronously.")]
        [SerializeField] private bool loadAsync = true;

        private bool _isLoading;

        /// <summary>
        /// Reloads the currently active scene.
        /// </summary>
        public void ReloadActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid())
            {
                Debug.LogWarning("[SceneReloadButtonAction] Active scene is not valid.");
                return;
            }

            LoadScene(activeScene.name);
        }

        /// <summary>
        /// Reloads the configured scene name, or active scene if configuration is disabled/empty.
        /// </summary>
        public void ReloadConfiguredScene()
        {
            if (useConfiguredSceneName && !string.IsNullOrWhiteSpace(sceneName))
            {
                ReloadSceneByName(sceneName);
                return;
            }

            ReloadActiveScene();
        }

        /// <summary>
        /// Reloads a scene by name. Use this with Button OnClick(string) dynamic parameter if needed.
        /// </summary>
        public void ReloadSceneByName(string targetSceneName)
        {
            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogWarning("[SceneReloadButtonAction] Scene name is empty.");
                return;
            }

            LoadScene(targetSceneName.Trim());
        }

        private void LoadScene(string targetSceneName)
        {
            if (_isLoading)
            {
                Debug.LogWarning("[SceneReloadButtonAction] Scene load already in progress.");
                return;
            }

            if (loadAsync)
            {
                StartCoroutine(LoadSceneAsyncRoutine(targetSceneName));
                return;
            }

            _isLoading = true;
            SceneManager.LoadScene(targetSceneName);
            _isLoading = false;
        }

        private IEnumerator LoadSceneAsyncRoutine(string targetSceneName)
        {
            _isLoading = true;

            AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
            if (operation == null)
            {
                Debug.LogWarning($"[SceneReloadButtonAction] Failed to start async load for scene: {targetSceneName}");
                _isLoading = false;
                yield break;
            }

            while (!operation.isDone)
            {
                yield return null;
            }

            _isLoading = false;
        }
    }
}
