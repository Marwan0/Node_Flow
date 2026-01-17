#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using NodeSystem.Nodes;
namespace NodeSystem.Editor
{
    public class AnimationSequencerNodeInlineContent : NodeInlineContentBase
    {
        // Cache editors for the embedded inspector
        private static System.Collections.Generic.Dictionary<int, UnityEditor.Editor> _editorCache 
            = new System.Collections.Generic.Dictionary<int, UnityEditor.Editor>();

        static AnimationSequencerNodeInlineContent()
        {
            EditorApplication.playModeStateChanged += (state) =>
            {
                ClearEditorCache();
            };
        }
        
        private static void ClearEditorCache()
        {
            foreach (var editor in _editorCache.Values)
            {
                if (editor != null)
                {
                    try { UnityEngine.Object.DestroyImmediate(editor); } catch { }
                }
            }
            _editorCache.Clear();
        }

        public override void Draw()
        {
            var node = Node as AnimationSequencerNode;
            if (node == null) return;
            
            if (Container == null) return; // Safety check

            // Target GameObject - use ObjectField for better UX
            GameObject currentTarget = null;
            if (!string.IsNullOrEmpty(node.targetPath))
            {
                // Find GameObject by path - including disabled objects
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

            // Auto-create option
            CreateToggle("Auto Create If Missing", node.autoCreateIfMissing, v => 
            {
                node.autoCreateIfMissing = v;
                MarkDirty();
                RequestRefresh(); // Refresh to update status messages
            });

            // Show info about Animation Sequencer - always show something
            Component sequencerComponent = null;
            bool hasSequencer = false;
            
            if (currentTarget != null)
            {
                sequencerComponent = GetAnimationSequencerComponent(currentTarget);
                hasSequencer = sequencerComponent != null;
            }
            
            // Always show status - ensure something is always visible
            if (currentTarget == null)
            {
                CreateLabel("📌 Select a target GameObject", new Color(0.7f, 0.7f, 0.7f));
            }
            else if (!hasSequencer)
            {
                if (node.autoCreateIfMissing)
                {
                    CreateLabel("⚠️ Will create on play", new Color(1f, 0.8f, 0f));
                }
                else
                {
                    CreateLabel("⚠️ No Animation Sequencer found", new Color(1f, 0.6f, 0f));
                }
            }
            else
            {
                // Get step count
                int stepCount = GetAnimationStepCount(sequencerComponent);
                
                // Always show at least step count
                if (stepCount == 0)
                {
                    CreateLabel("⚠️ No steps configured", new Color(1f, 0.6f, 0f));
                    CreateLabel("Click button to add steps", new Color(0.8f, 0.8f, 0.8f));
                }
                else
                {
                    CreateLabel($"✓ {stepCount} step{(stepCount == 1 ? "" : "s")} configured", new Color(0f, 0.8f, 0f));
                }
                
                // Show sequencer configuration details (if available)
                try
                {
                    var configInfo = GetSequencerConfiguration(sequencerComponent);
                    if (!string.IsNullOrEmpty(configInfo))
                    {
                        CreateLabel(configInfo, new Color(0.65f, 0.75f, 0.9f));
                    }
                }
                catch
                {
                    // If reflection fails, just skip config info
                }
            }
            
            // Draw the full Animation Sequencer editor inside the node
            if (hasSequencer && sequencerComponent != null)
            {
                int instanceId = sequencerComponent.GetInstanceID();
                
                // Get or create Editor using our safe wrapper
                if (!_editorCache.TryGetValue(instanceId, out UnityEditor.Editor sequencerEditor) || 
                    sequencerEditor == null || sequencerEditor.target != sequencerComponent)
                {
                    // Clean up old editor
                    if (_editorCache.TryGetValue(instanceId, out var oldEditor) && oldEditor != null)
                    {
                        try { UnityEngine.Object.DestroyImmediate(oldEditor); } catch { }
                        _editorCache.Remove(instanceId);
                    }
                    
                    try
                    {
                        // Create editor - this will use our SafeAnimationSequencerEditor
                        sequencerEditor = UnityEditor.Editor.CreateEditor(sequencerComponent);
                        _editorCache[instanceId] = sequencerEditor;
                    }
                    catch
                    {
                        sequencerEditor = null;
                    }
                }

                if (sequencerEditor != null)
                {
                    // Create scrollable container
                    var scrollView = new ScrollView(ScrollViewMode.Vertical);
                    scrollView.style.marginTop = 4;
                    scrollView.style.marginBottom = 2;
                    scrollView.style.minHeight = 200;
                    scrollView.style.maxHeight = 600;
                    scrollView.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);

                    var editorRef = sequencerEditor;
                    var componentRef = sequencerComponent;

                    var imguiContainer = new IMGUIContainer(() =>
                    {
                        if (componentRef == null || editorRef == null || editorRef.target == null) return;
                        
                        try
                        {
                            EditorGUI.BeginChangeCheck();
                            editorRef.OnInspectorGUI();
                            if (EditorGUI.EndChangeCheck())
                            {
                                MarkDirty();
                            }
                        }
                        catch { }
                    });

                    imguiContainer.style.minHeight = 150;
                    imguiContainer.style.flexGrow = 1;
                    
                    scrollView.Add(imguiContainer);
                    Container.Add(scrollView);
                }
            }
            else if (currentTarget != null)
            {
                // Show button to add component if no sequencer exists
                var button = new Button(() =>
                {
                    var component = CreateAnimationSequencerComponent(currentTarget);
                    if (component != null)
                    {
                        Undo.RegisterCreatedObjectUndo(component, "Add Animation Sequencer Controller");
                        EditorUtility.SetDirty(currentTarget);
                        if (!Application.isPlaying && currentTarget.scene.IsValid())
                        {
                            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                        }
                        Selection.activeObject = component;
                        EditorGUIUtility.PingObject(component);
                        MarkDirty();
                        EditorApplication.delayCall += () => RequestRefresh();
                    }
                    else
                    {
                        Debug.LogError("[AnimationSequencerNode] Failed to create Animation Sequencer Controller.");
                    }
                })
                {
                    text = "Add Animation Sequencer"
                };
                button.style.marginTop = 4;
                button.style.height = 24;
                button.style.fontSize = 11;
                Container.Add(button);
            }
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

            // Search all loaded scenes
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    if (rootGo.name == rootName)
                    {
                        // Found root, now traverse the path
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

            // Fallback: search by name only (last part of path)
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

        /// <summary>
        /// Recursively search for a GameObject by name in hierarchy (includes disabled)
        /// </summary>
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

        /// <summary>
        /// Get Animation Sequencer component from GameObject
        /// </summary>
        private Component GetAnimationSequencerComponent(GameObject go)
        {
            if (go == null) return null;

            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            Type sequencerType = null;

            foreach (var assembly in assemblies)
            {
                // Try correct capitalization first
                sequencerType = assembly.GetType("BrunoMikoski.AnimationSequencer.AnimationSequencerController");
                if (sequencerType != null)
                    break;
                    
                // Fallback to lowercase
                sequencerType = assembly.GetType("Brunomikoski.AnimationSequencer.AnimationSequencerController");
                if (sequencerType != null)
                    break;
            }

            if (sequencerType == null)
                return null;

            return go.GetComponent(sequencerType);
        }

        /// <summary>
        /// Create Animation Sequencer component on GameObject
        /// </summary>
        private Component CreateAnimationSequencerComponent(GameObject go)
        {
            if (go == null) return null;

            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            Type sequencerType = null;

            // Try to find the Animation Sequencer Controller type
            foreach (var assembly in assemblies)
            {
                // Try full namespace (correct capitalization)
                sequencerType = assembly.GetType("BrunoMikoski.AnimationSequencer.AnimationSequencerController");
                if (sequencerType != null)
                    break;
                    
                // Try with lowercase (fallback)
                sequencerType = assembly.GetType("Brunomikoski.AnimationSequencer.AnimationSequencerController");
                if (sequencerType != null)
                    break;
                    
                // Try without namespace (in case it's different)
                sequencerType = assembly.GetType("AnimationSequencerController");
                if (sequencerType != null)
                    break;
            }

            if (sequencerType == null)
            {
                Debug.LogError("[AnimationSequencerNode] Animation Sequencer Controller type not found. Please ensure the Animation Sequencer package is installed and the assembly is loaded.");
                return null;
            }

            // Check if already exists
            var existing = go.GetComponent(sequencerType);
            if (existing != null)
                return existing;

            // Add component using Undo
            try
            {
                var component = go.AddComponent(sequencerType);
                Undo.RegisterCreatedObjectUndo(component, "Add Animation Sequencer Controller");
                return component;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[AnimationSequencerNode] Failed to add Animation Sequencer Controller: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get the number of animation steps configured in the sequencer
        /// </summary>
        private int GetAnimationStepCount(Component sequencerComponent)
        {
            if (sequencerComponent == null) return 0;

            try
            {
                // Get AnimationSteps property
                var animationStepsProperty = sequencerComponent.GetType().GetProperty("AnimationSteps");
                if (animationStepsProperty != null)
                {
                    var steps = animationStepsProperty.GetValue(sequencerComponent);
                    if (steps != null)
                    {
                        // Try to get length/count
                        var array = steps as Array;
                        if (array != null)
                        {
                            return array.Length;
                        }
                        
                        // Try as ICollection
                        var collection = steps as System.Collections.ICollection;
                        if (collection != null)
                        {
                            return collection.Count;
                        }
                    }
                }
            }
            catch
            {
                // If reflection fails, just return 0
            }

            return 0;
        }

        /// <summary>
        /// Get a summary of the sequencer's configuration
        /// </summary>
        private string GetSequencerConfiguration(Component sequencerComponent)
        {
            if (sequencerComponent == null) return "";

            try
            {
                var configParts = new System.Collections.Generic.List<string>();

                // Get AutoplayMode
                var autoplayModeProperty = sequencerComponent.GetType().GetProperty("autoplayMode", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (autoplayModeProperty == null)
                {
                    // Try public field
                    var autoplayModeField = sequencerComponent.GetType().GetField("autoplayMode", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (autoplayModeField != null)
                    {
                        var autoplayValue = autoplayModeField.GetValue(sequencerComponent);
                        if (autoplayValue != null && autoplayValue.ToString() != "Nothing")
                        {
                            configParts.Add($"Auto: {autoplayValue}");
                        }
                    }
                }
                else
                {
                    var autoplayValue = autoplayModeProperty.GetValue(sequencerComponent);
                    if (autoplayValue != null && autoplayValue.ToString() != "Nothing")
                    {
                        configParts.Add($"Auto: {autoplayValue}");
                    }
                }

                // Get Loops
                var loopsField = sequencerComponent.GetType().GetField("loops", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (loopsField != null)
                {
                    var loopsValue = loopsField.GetValue(sequencerComponent);
                    if (loopsValue != null)
                    {
                        int loops = Convert.ToInt32(loopsValue);
                        if (loops != 0)
                        {
                            configParts.Add($"Loops: {(loops == -1 ? "∞" : loops.ToString())}");
                        }
                    }
                }

                // Get PlaybackSpeed
                var playbackSpeedProperty = sequencerComponent.GetType().GetProperty("PlaybackSpeed");
                if (playbackSpeedProperty != null)
                {
                    var speedValue = playbackSpeedProperty.GetValue(sequencerComponent);
                    if (speedValue != null)
                    {
                        float speed = Convert.ToSingle(speedValue);
                        if (speed != 1f)
                        {
                            configParts.Add($"Speed: {speed:F1}x");
                        }
                    }
                }

                // Get AutoKill
                var autoKillField = sequencerComponent.GetType().GetField("autoKill", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (autoKillField != null)
                {
                    var autoKillValue = autoKillField.GetValue(sequencerComponent);
                    if (autoKillValue != null && !Convert.ToBoolean(autoKillValue))
                    {
                        configParts.Add("No AutoKill");
                    }
                }

                if (configParts.Count > 0)
                {
                    return string.Join(" • ", configParts);
                }
            }
            catch
            {
                // If reflection fails, return empty
            }

            return "";
        }

        /// <summary>
        /// Get details about the animation steps
        /// </summary>
        private string GetStepDetails(Component sequencerComponent)
        {
            if (sequencerComponent == null) return "";

            try
            {
                var animationStepsProperty = sequencerComponent.GetType().GetProperty("AnimationSteps");
                if (animationStepsProperty != null)
                {
                    var steps = animationStepsProperty.GetValue(sequencerComponent) as Array;
                    if (steps != null && steps.Length > 0)
                    {
                        var stepNames = new System.Collections.Generic.List<string>();
                        for (int i = 0; i < Math.Min(steps.Length, 3); i++) // Show first 3 steps
                        {
                            var step = steps.GetValue(i);
                            if (step != null)
                            {
                                // Try to get DisplayName property
                                var displayNameProperty = step.GetType().GetProperty("DisplayName");
                                if (displayNameProperty != null)
                                {
                                    var name = displayNameProperty.GetValue(step)?.ToString();
                                    if (!string.IsNullOrEmpty(name))
                                    {
                                        stepNames.Add(name);
                                    }
                                }
                                else
                                {
                                    // Fallback to type name
                                    var typeName = step.GetType().Name;
                                    stepNames.Add(typeName.Replace("DOTween", "").Replace("Action", ""));
                                }
                            }
                        }
                        
                        if (stepNames.Count > 0)
                        {
                            var result = string.Join(", ", stepNames);
                            if (steps.Length > 3)
                            {
                                result += $" +{steps.Length - 3} more";
                            }
                            return $"Steps: {result}";
                        }
                    }
                }
            }
            catch
            {
                // If reflection fails, return empty
            }

            return "";
        }
    }
}
#endif

