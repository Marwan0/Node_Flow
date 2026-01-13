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
        // Static cache to persist editors across repaints
        private static System.Collections.Generic.Dictionary<int, UnityEditor.Editor> _editorCache = new System.Collections.Generic.Dictionary<int, UnityEditor.Editor>();
        private static bool _playModeState = false;

        static AnimationSequencerNodeInlineContent()
        {
            // Clear cache when play mode changes
            // Note: NodeGraphEditorWindow will handle refreshing all node inline content
            EditorApplication.playModeStateChanged += (state) =>
            {
                ClearEditorCache();
                _playModeState = EditorApplication.isPlaying;
            };
        }

        private static void ClearEditorCache()
        {
            foreach (var editor in _editorCache.Values)
            {
                if (editor != null)
                {
                    UnityEngine.Object.DestroyImmediate(editor);
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
            
            // Render the Animation Sequencer Controller inspector if component exists
            if (hasSequencer && sequencerComponent != null)
            {
                // Get or create editor for the sequencer component (cached)
                int instanceId = sequencerComponent.GetInstanceID();
                UnityEditor.Editor sequencerEditor;
                
                // Check if we need to recreate the editor (play mode change, target changed, or editor is null)
                bool needsNewEditor = !_editorCache.TryGetValue(instanceId, out sequencerEditor) || 
                                     sequencerEditor == null || 
                                     sequencerEditor.target == null ||
                                     sequencerEditor.target != sequencerComponent;
                
                if (needsNewEditor)
                {
                    // Clean up old editor if exists
                    if (_editorCache.TryGetValue(instanceId, out UnityEditor.Editor oldEditor) && oldEditor != null)
                    {
                        UnityEngine.Object.DestroyImmediate(oldEditor);
                    }
                    
                    sequencerEditor = UnityEditor.Editor.CreateEditor(sequencerComponent);
                    _editorCache[instanceId] = sequencerEditor;
                }

                // Create scrollable container for the inspector
                var scrollView = new ScrollView(ScrollViewMode.Vertical);
                scrollView.style.marginTop = 4;
                scrollView.style.marginBottom = 2;
                scrollView.style.minHeight = 300;
                scrollView.style.maxHeight = 800;
                scrollView.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.8f);
                
                // Store component reference for closure (will be used to get fresh editor)
                var componentRef = sequencerComponent;
                var instanceIdRef = instanceId;
                
                // Create IMGUI container to render the editor
                IMGUIContainer imguiContainer = null;
                imguiContainer = new IMGUIContainer(() =>
                {
                    if (componentRef == null) return;
                    
                    // Get or recreate editor (always get fresh from cache)
                    UnityEditor.Editor currentEditor = null;
                    if (_editorCache.TryGetValue(instanceIdRef, out currentEditor) && currentEditor != null && currentEditor.target == componentRef)
                    {
                        // Editor is valid, use it
                    }
                    else
                    {
                        // Recreate editor
                        if (_editorCache.TryGetValue(instanceIdRef, out var oldEditor) && oldEditor != null)
                        {
                            UnityEngine.Object.DestroyImmediate(oldEditor);
                        }
                        currentEditor = UnityEditor.Editor.CreateEditor(componentRef);
                        _editorCache[instanceIdRef] = currentEditor;
                    }
                    
                    if (currentEditor != null && currentEditor.target != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        currentEditor.OnInspectorGUI();
                        if (EditorGUI.EndChangeCheck())
                        {
                            MarkDirty();
                        }
                    }
                });
                
                // Always repaint to keep it updated (especially during play mode and preview)
                imguiContainer.schedule.Execute(() =>
                {
                    if (imguiContainer != null)
                    {
                        imguiContainer.MarkDirtyRepaint();
                    }
                }).Every(16); // ~60fps - repaint every frame
                
                // Let IMGUI container size itself based on content
                imguiContainer.style.minHeight = 200;
                imguiContainer.style.flexGrow = 1;
                
                scrollView.Add(imguiContainer);
                Container.Add(scrollView);
            }
            else
            {
                // Show button to add component or open sequencer
                if (currentTarget != null)
                {
                    var button = new Button(() =>
                    {
                        if (hasSequencer && sequencerComponent != null)
                        {
                            // Open in Inspector
                            Selection.activeObject = sequencerComponent;
                            EditorGUIUtility.PingObject(sequencerComponent);
                        }
                        else
                        {
                            // Add the component
                            var component = CreateAnimationSequencerComponent(currentTarget);
                            if (component != null)
                            {
                                // Record undo
                                Undo.RegisterCreatedObjectUndo(component, "Add Animation Sequencer Controller");
                                
                                // Mark scene dirty
                                EditorUtility.SetDirty(currentTarget);
                                if (!Application.isPlaying && currentTarget.scene.IsValid())
                                {
                                    UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                                }
                                
                                // Select and ping
                                Selection.activeObject = component;
                                EditorGUIUtility.PingObject(component);
                                
                                // Force immediate refresh to show embedded inspector
                                MarkDirty();
                                
                                // Use delayCall to ensure component is fully initialized
                                EditorApplication.delayCall += () =>
                                {
                                    RequestRefresh();
                                };
                            }
                            else
                            {
                                Debug.LogError("[AnimationSequencerNode] Failed to create Animation Sequencer Controller. Make sure the Animation Sequencer package is installed.");
                            }
                        }
                    })
                    {
                        text = hasSequencer ? "Open Sequencer" : "Add Component"
                    };
                    button.style.marginTop = 4;
                    button.style.marginBottom = 2;
                    button.style.height = 22;
                    button.style.fontSize = 10;
                    button.style.minWidth = 120;
                    Container.Add(button);
                }
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

