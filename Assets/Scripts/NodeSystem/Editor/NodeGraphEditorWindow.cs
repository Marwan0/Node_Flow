#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// Main editor window for editing NodeGraphs
    /// </summary>
    public class NodeGraphEditorWindow : EditorWindow
    {
        // Keys for persisting state across play mode
        private const string PREF_KEY_GRAPH_PATH = "NodeGraphEditor_CurrentGraphPath";
        private const string PREF_KEY_VIEW_POSITION = "NodeGraphEditor_ViewPosition";
        private const string PREF_KEY_VIEW_SCALE = "NodeGraphEditor_ViewScale";

        private NodeGraphView _graphView;
        private ObjectField _graphField;
        private NodeGraph _currentGraph;
        private VisualElement _inspectorPanel;
        private Label _inspectorTitle;
        private VisualElement _inspectorContent;
        
        // Variable panel
        private VariablePanel _variablePanel;
        private VisualElement _variablePanelContainer;
        private bool _showVariablePanel = false;
        
        // Breadcrumb navigation
        private BreadcrumbView _breadcrumbView;
        private List<NodeGraph> _graphStack = new List<NodeGraph>();
        
        // Debug tools
        private DebugToolbar _debugToolbar;
        private VariableWatchPanel _variableWatchPanel;
        private VisualElement _debugPanelContainer;
        private VisualElement _debugResizer;
        private bool _showDebugPanel = false;
        private float _debugPanelWidth = 300f;
        
        // Runtime visualization
        private Label _runtimeStatusLabel;
        private VisualElement _runtimeIndicator;

        [MenuItem("Window/Node System/Graph Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<NodeGraphEditorWindow>();
            window.titleContent = new GUIContent("Node Graph");
            window.minSize = new Vector2(800, 600);
        }

        /// <summary>
        /// Open a specific graph
        /// </summary>
        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.InstanceIDToObject(instanceID);
            if (asset is NodeGraph graph)
            {
                OpenWindow();
                var window = GetWindow<NodeGraphEditorWindow>();
                window.LoadGraph(graph);
                return true;
            }
            return false;
        }

        private void OnEnable()
        {
            CreateUI();
            
            // Subscribe to runtime events
            NodeGraphRunner.OnNodeStarted += OnRuntimeNodeStarted;
            NodeGraphRunner.OnNodeCompleted += OnRuntimeNodeCompleted;
            NodeGraphRunner.OnGraphStarted += OnRuntimeGraphStarted;
            NodeGraphRunner.OnGraphEnded += OnRuntimeGraphEnded;
            
            // Refresh inline content when play mode changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Restore state after UI is created (so _graphField exists)
            RestoreState();
        }

        private void OnDisable()
        {
            SaveState();
            SaveCurrentGraph();
            
            // Unsubscribe from runtime events
            NodeGraphRunner.OnNodeStarted -= OnRuntimeNodeStarted;
            NodeGraphRunner.OnNodeCompleted -= OnRuntimeNodeCompleted;
            NodeGraphRunner.OnGraphStarted -= OnRuntimeGraphStarted;
            NodeGraphRunner.OnGraphEnded -= OnRuntimeGraphEnded;
            
            // Unsubscribe from play mode changes
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
        
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Refresh all node inline content when play mode changes
            // Use delayCall to ensure it happens after play mode transition completes
            EditorApplication.delayCall += () =>
            {
                if (_graphView != null)
                {
                    foreach (var element in _graphView.graphElements.ToList())
                    {
                        if (element is NodeView nodeView)
                        {
                            nodeView.RefreshInlineContent();
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Save editor state to persist across play mode
        /// </summary>
        private void SaveState()
        {
            // Save current graph path
            if (_currentGraph != null)
            {
                var path = AssetDatabase.GetAssetPath(_currentGraph);
                SessionState.SetString(PREF_KEY_GRAPH_PATH, path);
            }
            else
            {
                SessionState.EraseString(PREF_KEY_GRAPH_PATH);
            }

            // Save view position and scale
            if (_graphView != null)
            {
                var viewPos = _graphView.viewTransform.position;
                var viewScale = _graphView.viewTransform.scale;
                SessionState.SetString(PREF_KEY_VIEW_POSITION, $"{viewPos.x},{viewPos.y}");
                SessionState.SetFloat(PREF_KEY_VIEW_SCALE, viewScale.x);
            }
        }

        /// <summary>
        /// Restore editor state after play mode or domain reload
        /// </summary>
        private void RestoreState()
        {
            // Don't reload if we already have the same graph loaded
            var graphPath = SessionState.GetString(PREF_KEY_GRAPH_PATH, "");
            if (!string.IsNullOrEmpty(graphPath))
            {
                var graph = AssetDatabase.LoadAssetAtPath<NodeGraph>(graphPath);
                if (graph != null)
                {
                    // Always call LoadGraph to ensure _currentGraph and _graphField are set
                    // LoadGraph will handle the actual loading logic
                    LoadGraph(graph);

                    // Restore view position and scale (delayed to ensure graph is loaded)
                    EditorApplication.delayCall += () =>
                    {
                        if (_graphView == null) return;

                        var posStr = SessionState.GetString(PREF_KEY_VIEW_POSITION, "");
                        if (!string.IsNullOrEmpty(posStr))
                        {
                            var parts = posStr.Split(',');
                            if (parts.Length == 2 && 
                                float.TryParse(parts[0], out float x) && 
                                float.TryParse(parts[1], out float y))
                            {
                                _graphView.viewTransform.position = new Vector3(x, y, 0);
                            }
                        }

                        var scale = SessionState.GetFloat(PREF_KEY_VIEW_SCALE, 1f);
                        _graphView.viewTransform.scale = new Vector3(scale, scale, 1);
                    };
                }
            }
        }

        private void CreateUI()
        {
            // Root container
            var root = rootVisualElement;
            root.Clear();

            // Toolbar
            var toolbar = new Toolbar();
            
            // Graph selector
            _graphField = new ObjectField("Graph")
            {
                objectType = typeof(NodeGraph),
                allowSceneObjects = false
            };
            _graphField.style.minWidth = 200;
            _graphField.RegisterValueChangedCallback(evt => 
            {
                LoadGraph(evt.newValue as NodeGraph);
            });
            toolbar.Add(_graphField);

            // New button
            var newBtn = new ToolbarButton(() => CreateNewGraph()) { text = "New" };
            toolbar.Add(newBtn);

            // Save button
            var saveBtn = new ToolbarButton(() => SaveCurrentGraph()) { text = "Save" };
            toolbar.Add(saveBtn);

            // Spacer
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            toolbar.Add(spacer);

            // Runtime status indicator
            _runtimeIndicator = new VisualElement();
            _runtimeIndicator.style.width = 10;
            _runtimeIndicator.style.height = 10;
            _runtimeIndicator.style.borderTopLeftRadius = 5;
            _runtimeIndicator.style.borderTopRightRadius = 5;
            _runtimeIndicator.style.borderBottomLeftRadius = 5;
            _runtimeIndicator.style.borderBottomRightRadius = 5;
            _runtimeIndicator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            _runtimeIndicator.style.marginRight = 5;
            _runtimeIndicator.style.alignSelf = Align.Center;
            toolbar.Add(_runtimeIndicator);

            // Runtime status label
            _runtimeStatusLabel = new Label("Idle");
            _runtimeStatusLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _runtimeStatusLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            _runtimeStatusLabel.style.marginRight = 10;
            toolbar.Add(_runtimeStatusLabel);

            // Reset visual states button
            var resetBtn = new ToolbarButton(() => 
            {
                _graphView?.ResetAllNodeVisualStates();
                UpdateRuntimeStatus(null, "Reset");
            }) { text = "Reset States" };
            toolbar.Add(resetBtn);

            // Variables toggle
            var variablesToggle = new ToolbarToggle() { text = "Variables" };
            variablesToggle.value = _showVariablePanel;
            variablesToggle.RegisterValueChangedCallback(evt =>
            {
                _showVariablePanel = evt.newValue;
                UpdateVariablePanelVisibility();
            });
            toolbar.Add(variablesToggle);

            // Debug toggle
            var debugToggle = new ToolbarToggle() { text = "Debug" };
            debugToggle.value = _showDebugPanel;
            debugToggle.RegisterValueChangedCallback(evt =>
            {
                _showDebugPanel = evt.newValue;
                UpdateDebugPanelVisibility();
            });
            toolbar.Add(debugToggle);

            root.Add(toolbar);

            // Breadcrumb navigation
            _breadcrumbView = new BreadcrumbView(OnBreadcrumbClicked);
            root.Add(_breadcrumbView);

            // Main container (graph + inspector)
            var mainContainer = new VisualElement();
            mainContainer.style.flexGrow = 1;
            mainContainer.style.flexDirection = FlexDirection.Row;

            // Graph view
            _graphView = new NodeGraphView();
            _graphView.Initialize(this);
            _graphView.OnNodeSelected = OnNodeSelected;
            _graphView.style.flexGrow = 1;
            mainContainer.Add(_graphView);

            // Inspector panel (hidden by default - properties shown in nodes)
            _inspectorPanel = new VisualElement();
            _inspectorPanel.style.width = 0; // Hidden
            _inspectorPanel.style.display = DisplayStyle.None; // Hidden
            _inspectorPanel.style.minWidth = 200;
            _inspectorPanel.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            _inspectorPanel.style.borderLeftWidth = 1;
            _inspectorPanel.style.borderLeftColor = Color.black;

            _inspectorTitle = new Label("Node Properties");
            _inspectorTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _inspectorTitle.style.paddingLeft = 10;
            _inspectorTitle.style.paddingTop = 10;
            _inspectorPanel.Add(_inspectorTitle);

            _inspectorContent = new VisualElement();
            _inspectorContent.style.paddingLeft = 10;
            _inspectorContent.style.paddingRight = 10;
            _inspectorContent.style.paddingTop = 10;
            _inspectorPanel.Add(_inspectorContent);

            mainContainer.Add(_inspectorPanel);

            // Variable panel container
            _variablePanelContainer = new VisualElement();
            _variablePanelContainer.style.width = 300;
            _variablePanelContainer.style.minWidth = 250;
            _variablePanelContainer.style.maxWidth = 400;
            _variablePanelContainer.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            _variablePanelContainer.style.borderLeftWidth = 1;
            _variablePanelContainer.style.borderLeftColor = Color.black;
            _variablePanelContainer.style.display = DisplayStyle.None; // Hidden by default

            _variablePanel = new VariablePanel();
            _variablePanelContainer.Add(_variablePanel);

            mainContainer.Add(_variablePanelContainer);

            // Debug resizer (only visible when debug panel is shown)
            _debugResizer = new VisualElement();
            _debugResizer.style.width = 4;
            _debugResizer.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            _debugResizer.style.cursor = new StyleCursor(new UnityEngine.UIElements.Cursor());
            _debugResizer.style.display = DisplayStyle.None;
            // Add hover effect to show it's resizable
            _debugResizer.RegisterCallback<MouseEnterEvent>(evt =>
            {
                _debugResizer.style.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
            });
            _debugResizer.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                _debugResizer.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            });
            _debugResizer.RegisterCallback<MouseDownEvent>(OnDebugResizerMouseDown);
            mainContainer.Add(_debugResizer);

            // Debug panel container
            _debugPanelContainer = new VisualElement();
            _debugPanelContainer.style.width = _debugPanelWidth;
            _debugPanelContainer.style.minWidth = 200;
            _debugPanelContainer.style.maxWidth = 800;
            _debugPanelContainer.style.flexShrink = 0; // Don't shrink
            _debugPanelContainer.style.flexDirection = FlexDirection.Column;
            _debugPanelContainer.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            _debugPanelContainer.style.borderLeftWidth = 1;
            _debugPanelContainer.style.borderLeftColor = Color.black;
            _debugPanelContainer.style.display = DisplayStyle.None; // Hidden by default
            _debugPanelContainer.style.overflow = Overflow.Hidden; // Prevent content from overflowing

            // Debug toolbar (fixed height)
            _debugToolbar = new DebugToolbar();
            _debugToolbar.style.flexShrink = 0; // Don't shrink
            _debugPanelContainer.Add(_debugToolbar);

            // Variable watch panel (flexible, scrollable)
            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1;
            _variableWatchPanel = new VariableWatchPanel();
            scrollView.Add(_variableWatchPanel);
            _debugPanelContainer.Add(scrollView);

            mainContainer.Add(_debugPanelContainer);

            root.Add(mainContainer);
        }

        private void UpdateDebugPanelVisibility()
        {
            if (_debugPanelContainer != null)
            {
                _debugPanelContainer.style.display = _showDebugPanel ? DisplayStyle.Flex : DisplayStyle.None;
                _debugPanelContainer.style.width = _debugPanelWidth;
            }

            if (_debugResizer != null)
            {
                _debugResizer.style.display = _showDebugPanel ? DisplayStyle.Flex : DisplayStyle.None;
            }

            // Update variable watch panel graph
            if (_variableWatchPanel != null && _showDebugPanel)
            {
                _variableWatchPanel.SetGraph(_currentGraph);
            }
        }

        private void OnDebugResizerMouseDown(MouseDownEvent evt)
        {
            if (evt.button != 0) return; // Only left mouse button

            _debugResizer.CaptureMouse();
            evt.StopPropagation();

            var startMouseX = evt.mousePosition.x;
            var startPanelWidth = _debugPanelWidth;

            _debugResizer.RegisterCallback<MouseMoveEvent>(OnDebugResizerMouseMove);
            _debugResizer.RegisterCallback<MouseUpEvent>(OnDebugResizerMouseUp);

            void OnDebugResizerMouseMove(MouseMoveEvent moveEvt)
            {
                if (!_debugResizer.HasMouseCapture()) return;

                var deltaX = moveEvt.mousePosition.x - startMouseX;
                var newWidth = startPanelWidth - deltaX; // Invert because we're resizing from the left

                // Clamp to min/max
                newWidth = Mathf.Clamp(newWidth, 200f, 800f);
                _debugPanelWidth = newWidth;
                _debugPanelContainer.style.width = _debugPanelWidth;
            }

            void OnDebugResizerMouseUp(MouseUpEvent upEvt)
            {
                _debugResizer.ReleaseMouse();
                _debugResizer.UnregisterCallback<MouseMoveEvent>(OnDebugResizerMouseMove);
                _debugResizer.UnregisterCallback<MouseUpEvent>(OnDebugResizerMouseUp);
            }
        }

        private void LoadGraph(NodeGraph graph)
        {
            _currentGraph = graph;
            _graphView.LoadGraph(graph);
            
            if (_graphField.value != graph)
            {
                _graphField.SetValueWithoutNotify(graph);
            }

            titleContent = new GUIContent(graph != null ? $"Node Graph - {graph.name}" : "Node Graph");
            
            ClearInspector();
            
            // Update variable panel
            if (_variablePanel != null)
            {
                _variablePanel.SetGraph(graph);
            }

            // Update variable watch panel
            if (_variableWatchPanel != null && _showDebugPanel)
            {
                _variableWatchPanel.SetGraph(graph);
            }

            // Update breadcrumbs
            if (_graphStack.Count == 0 || _graphStack[_graphStack.Count - 1] != graph)
            {
                _graphStack.Clear();
                if (graph != null)
                {
                    _graphStack.Add(graph);
                }
            }
            UpdateBreadcrumbs();
        }

        private void OnBreadcrumbClicked(NodeGraph graph)
        {
            // Navigate to clicked graph
            int index = _graphStack.IndexOf(graph);
            if (index >= 0)
            {
                // Remove everything after this graph
                _graphStack.RemoveRange(index + 1, _graphStack.Count - index - 1);
                LoadGraph(graph);
            }
        }

        private void UpdateBreadcrumbs()
        {
            if (_breadcrumbView != null)
            {
                _breadcrumbView.SetGraphStack(_graphStack);
            }
        }

        public void OpenSubGraph(NodeGraph subGraph)
        {
            if (subGraph != null)
            {
                _graphStack.Add(subGraph);
                LoadGraph(subGraph);
            }
        }

        private void UpdateVariablePanelVisibility()
        {
            if (_variablePanelContainer != null)
            {
                _variablePanelContainer.style.display = _showVariablePanel ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void CreateNewGraph()
        {
            var path = EditorUtility.SaveFilePanelInProject(
                "Create Node Graph",
                "NewNodeGraph",
                "asset",
                "Choose location for new graph"
            );

            if (string.IsNullOrEmpty(path)) return;

            var graph = ScriptableObject.CreateInstance<NodeGraph>();
            graph.graphName = System.IO.Path.GetFileNameWithoutExtension(path);

            // Add default start node
            var startNode = new Nodes.StartNode();
            startNode.Position = new Vector2(100, 200);
            graph.AddNode(startNode);

            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();

            LoadGraph(graph);
        }

        private void SaveCurrentGraph()
        {
            if (_currentGraph != null)
            {
                _currentGraph.Save();
            }
        }

        private void OnNodeSelected(NodeView nodeView)
        {
            ClearInspector();

            if (nodeView == null) return;

            _inspectorTitle.text = nodeView.Data.Name;

            // Use custom inspector system
            DrawNodeProperties(nodeView.Data);
        }

        private void ClearInspector()
        {
            _inspectorContent.Clear();
            _inspectorTitle.text = "Node Properties";
        }

        private void DrawNodeProperties(NodeData node)
        {
            // Get custom inspector for this node type
            var inspector = NodeInspectorFactory.GetInspector(node);
            inspector.Initialize(node, _currentGraph, _inspectorContent);
            inspector.DrawInspector();
        }

        // === Runtime Event Handlers ===

        private void OnRuntimeNodeStarted(NodeGraphRunner runner, NodeData node)
        {
            if (_currentGraph == null || runner.Graph != _currentGraph) return;
            UpdateRuntimeStatus(node, "Running");
        }

        private void OnRuntimeNodeCompleted(NodeGraphRunner runner, NodeData node)
        {
            if (_currentGraph == null || runner.Graph != _currentGraph) return;
            UpdateRuntimeStatus(node, "Completed");
        }

        private void OnRuntimeGraphStarted(NodeGraphRunner runner)
        {
            if (_currentGraph == null || runner.Graph != _currentGraph) return;
            UpdateRuntimeStatus(null, "Graph Started");
            
            // Update indicator to running (green)
            if (_runtimeIndicator != null)
            {
                _runtimeIndicator.style.backgroundColor = new Color(0.3f, 0.9f, 0.4f);
            }
        }

        private void OnRuntimeGraphEnded(NodeGraphRunner runner)
        {
            if (_currentGraph == null || runner.Graph != _currentGraph) return;
            UpdateRuntimeStatus(null, "Graph Completed");
            
            // Update indicator to idle
            if (_runtimeIndicator != null)
            {
                _runtimeIndicator.style.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            }
        }

        private void UpdateRuntimeStatus(NodeData node, string status)
        {
            if (_runtimeStatusLabel == null) return;

            if (node != null)
            {
                _runtimeStatusLabel.text = $"{status}: {node.Name}";
                
                // Update indicator color based on status
                if (_runtimeIndicator != null)
                {
                    if (status == "Running")
                        _runtimeIndicator.style.backgroundColor = new Color(1f, 0.6f, 0f); // Orange (active)
                    else if (status == "Completed")
                        _runtimeIndicator.style.backgroundColor = new Color(0.3f, 0.7f, 0.3f); // Green
                }
            }
            else
            {
                _runtimeStatusLabel.text = status;
            }

            // Force repaint
            Repaint();
        }
    }
}
#endif

