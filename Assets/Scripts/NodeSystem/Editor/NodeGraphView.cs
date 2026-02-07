#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace NodeSystem.Editor
{
    /// <summary>
    /// The main graph view for editing NodeGraphs
    /// </summary>
    public class NodeGraphView : GraphView
    {
        public NodeGraph Graph { get; private set; }
        public Action<NodeView> OnNodeSelected;

        private NodeSearchWindow _searchWindow;
        private bool _isSubscribedToRuntime;
        private MinimapView _minimap;

        // Copy/paste
        private List<NodeData> _copiedNodes = new List<NodeData>();
        private List<ConnectionData> _copiedConnections = new List<ConnectionData>();
        private Vector2 _copyCenter;
        private Vector2 _lastMousePosition;
        
        // --- UX Enhancements (inspired by Doozy Nody) ---
        
        // Edge execution highlighting
        private HashSet<string> _executedNodeGuids = new HashSet<string>();
        private string _currentRunningNodeGuid;
        
        // Zoom-based LOD
        private float _currentZoom = 1f;
        private const float ZoomLodThreshold = 0.6f;
        
        // Delete preview (Alt+Hover)
        private bool _isAltHeld = false;
        private NodeView _deletePreviewNode;

        public NodeGraphView()
        {
            // Add background grid
            Insert(0, new GridBackground());

            // Minimap will be added after graph loads to prevent zoom flicker on play mode entry
            _minimap = new MinimapView();

            // Add manipulators
            this.AddManipulator(new ContentZoomer());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            // Load stylesheet
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Scripts/NodeSystem/Editor/NodeGraphStyles.uss");
            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }

            // Handle graph changes
            graphViewChanged = OnGraphViewChanged;

            // Set up Undo
            Undo.undoRedoPerformed += OnUndoRedo;

            // Subscribe to play mode changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            
            // Subscribe to runtime events
            SubscribeToRuntimeEvents();

            // Track mouse position for paste operations
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            
            // Handle keyboard shortcuts
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<KeyUpEvent>(OnKeyUp);
            
            // Also handle via IMGUI for undo/redo (more reliable)
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            
            // Enable copy/paste callbacks
            serializeGraphElements = SerializeGraphElementsCallback;
            canPasteSerializedData = CanPasteSerializedDataCallback;
            unserializeAndPaste = UnserializeAndPasteCallback;

            // Add context menu
            RegisterCallback<ContextualMenuPopulateEvent>(OnContextMenu);

            // Add group creation to context menu
            RegisterCallback<ContextualMenuPopulateEvent>(OnGraphContextMenu);
            
            // --- Zoom LOD: listen for viewTransform changes ---
            viewTransformChanged += OnViewTransformChanged;
        }

        private void OnGraphContextMenu(ContextualMenuPopulateEvent evt)
        {
            // Only show if clicking on empty space (not on a node)
            if (evt.target == this || evt.target is GridBackground)
            {
                // Group functionality removed due to persistent issues
            }
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            var hasSelection = selection.OfType<NodeView>().Any();
            var hasCopied = _copiedNodes.Count > 0;

            if (hasSelection)
            {
                evt.menu.AppendAction("Copy", _ => CopySelection(), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Duplicate", _ => DuplicateSelection(), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Delete", _ => DeleteSelection(), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendSeparator();
            }

            if (hasCopied)
            {
                evt.menu.AppendAction("Paste", _ => 
                {
                    var pos = contentViewContainer.WorldToLocal(_lastMousePosition);
                    PasteNodesAt(pos);
                }, DropdownMenuAction.AlwaysEnabled);
            }
        }

        /// <summary>
        /// Copy selected nodes
        /// </summary>
        public void CopySelection()
        {
            SerializeGraphElementsCallback(selection.OfType<GraphElement>());
        }

        /// <summary>
        /// Delete selected elements
        /// </summary>
        public void DeleteSelection()
        {
            DeleteElements(selection.OfType<GraphElement>().ToList());
        }

        private void OnMouseMove(MouseMoveEvent evt)
        {
            _lastMousePosition = evt.localMousePosition;
            
            // --- Delete preview: update hover state when Alt is held ---
            if (_isAltHeld)
            {
                UpdateDeletePreview(evt.localMousePosition);
            }
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            // Only handle if GraphView has focus and graph is loaded
            if (Graph == null) return;
            
            // Don't intercept if a text field is focused
            if (evt.target is TextField || evt.target is TextInputBaseField<char>)
            {
                return;
            }
            
            // Track Alt key for delete preview
            if (evt.keyCode == KeyCode.LeftAlt || evt.keyCode == KeyCode.RightAlt)
            {
                _isAltHeld = true;
                return;
            }
            
            // Ctrl+D for duplicate
            if (evt.ctrlKey && evt.keyCode == KeyCode.D)
            {
                DuplicateSelection();
                evt.StopPropagation();
                return;
            }
            
            // Ctrl+Z for undo
            if (evt.ctrlKey && evt.keyCode == KeyCode.Z && !evt.shiftKey)
            {
                Undo.PerformUndo();
                evt.StopPropagation();
                return;
            }
            
            // Ctrl+Y for redo (Windows)
            if (evt.ctrlKey && evt.keyCode == KeyCode.Y)
            {
                Undo.PerformRedo();
                evt.StopPropagation();
                return;
            }
            
            // Ctrl+Shift+Z for redo (Mac/alternative)
            if (evt.ctrlKey && evt.shiftKey && evt.keyCode == KeyCode.Z)
            {
                Undo.PerformRedo();
                evt.StopPropagation();
                return;
            }
        }

        /// <summary>
        /// Handle key up - clears delete preview when Alt is released
        /// </summary>
        private void OnKeyUp(KeyUpEvent evt)
        {
            if (evt.keyCode == KeyCode.LeftAlt || evt.keyCode == KeyCode.RightAlt)
            {
                _isAltHeld = false;
                ClearDeletePreview();
            }
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            // Make sure GraphView can receive keyboard focus
            focusable = true;
        }

        ~NodeGraphView()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            UnsubscribeFromRuntimeEvents();
        }

        // ============================================================
        //  RUNTIME EVENT SUBSCRIPTION
        // ============================================================

        private void SubscribeToRuntimeEvents()
        {
            if (_isSubscribedToRuntime) return;
            
            NodeGraphRunner.OnNodeStarted += OnRuntimeNodeStarted;
            NodeGraphRunner.OnNodeCompleted += OnRuntimeNodeCompleted;
            NodeGraphRunner.OnGraphStarted += OnRuntimeGraphStarted;
            NodeGraphRunner.OnGraphEnded += OnRuntimeGraphEnded;
            _isSubscribedToRuntime = true;
        }

        private void UnsubscribeFromRuntimeEvents()
        {
            if (!_isSubscribedToRuntime) return;
            
            NodeGraphRunner.OnNodeStarted -= OnRuntimeNodeStarted;
            NodeGraphRunner.OnNodeCompleted -= OnRuntimeNodeCompleted;
            NodeGraphRunner.OnGraphStarted -= OnRuntimeGraphStarted;
            NodeGraphRunner.OnGraphEnded -= OnRuntimeGraphEnded;
            _isSubscribedToRuntime = false;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    break;
                    
                case PlayModeStateChange.ExitingPlayMode:
                    ResetAllNodeVisualStates();
                    ClearEdgeHighlights();
                    break;

                // Safety net: if ExitingPlayMode didn't fully clean up
                // (UI can be in a transitional state), clean up again.
                case PlayModeStateChange.EnteredEditMode:
                    ResetAllNodeVisualStates();
                    ClearEdgeHighlights();
                    _executedNodeGuids.Clear();
                    _currentRunningNodeGuid = null;
                    break;
            }
        }

        private void OnRuntimeNodeStarted(NodeGraphRunner runner, NodeData node)
        {
            if (Graph == null || runner.Graph != Graph) return;

            _currentRunningNodeGuid = node.Guid;
            UpdateNodeVisualState(node.Guid, NodeState.Running);
            
            // Highlight edges leading into this node (active execution path)
            HighlightEdgesForActiveNode(node.Guid);
        }

        private void OnRuntimeNodeCompleted(NodeGraphRunner runner, NodeData node)
        {
            if (Graph == null || runner.Graph != Graph) return;

            _executedNodeGuids.Add(node.Guid);
            _currentRunningNodeGuid = null;
            UpdateNodeVisualState(node.Guid, NodeState.Completed);
            
            // Mark outgoing edges from this node as executed
            MarkEdgesAsExecuted(node.Guid);
        }

        /// <summary>
        /// Update visual state of a node (handles threading and timing issues)
        /// </summary>
        private void UpdateNodeVisualState(string nodeGuid, NodeState state)
        {
            var nodeElement = GetNodeByGuid(nodeGuid);
            if (nodeElement != null)
            {
                if (nodeElement is NodeView nodeView)
                {
                    nodeView.SetVisualState(state);
                    return;
                }
#if ODIN_INSPECTOR
                else if (nodeElement is NodeViewOdin odinView)
                {
                    odinView.SetVisualState(state);
                    return;
                }
#endif
            }

            // If immediate update failed, schedule for next frame
            var graphRef = Graph;
            EditorApplication.delayCall += () =>
            {
                if (graphRef == null || Graph != graphRef) return;

                var delayedNodeElement = GetNodeByGuid(nodeGuid);
                if (delayedNodeElement is NodeView delayedNodeView)
                {
                    delayedNodeView.SetVisualState(state);
                }
#if ODIN_INSPECTOR
                else if (delayedNodeElement is NodeViewOdin delayedOdinView)
                {
                    delayedOdinView.SetVisualState(state);
                }
#endif
            };
        }

        private void OnRuntimeGraphStarted(NodeGraphRunner runner)
        {
            if (Graph == null || runner.Graph != Graph) return;

            // Reset tracking
            _executedNodeGuids.Clear();
            _currentRunningNodeGuid = null;
            
            ResetAllNodeVisualStates();
            ClearEdgeHighlights();
        }

        private void OnRuntimeGraphEnded(NodeGraphRunner runner)
        {
            // Graph execution finished - edges stay highlighted to show the full path taken
        }

        /// <summary>
        /// Reset all node visual states to idle
        /// </summary>
        public void ResetAllNodeVisualStates()
        {
            foreach (var element in graphElements.ToList())
            {
                if (element is NodeView nodeView)
                {
                    nodeView.ResetVisualState();
                }
#if ODIN_INSPECTOR
                else if (element is NodeViewOdin odinView)
                {
                    odinView.ResetVisualState();
                }
#endif
            }
        }

        /// <summary>
        /// Sync visual states with runtime state (useful when opening editor during play)
        /// </summary>
        public void SyncWithRuntimeState()
        {
            if (!EditorApplication.isPlaying) return;
            if (Graph == null) return;

            var runner = NodeGraphRunner.ActiveRunner;
            if (runner == null || runner.Graph != Graph) return;

            // Rebuild our tracking from the runner's execution path
            _executedNodeGuids.Clear();
            _currentRunningNodeGuid = null;

            foreach (var guid in runner.ExecutionPath)
            {
                var nodeElement = GetNodeByGuid(guid);
                if (nodeElement is NodeView nodeView)
                {
                    if (runner.CurrentNode != null && runner.CurrentNode.Guid == guid)
                    {
                        nodeView.SetVisualState(NodeState.Running);
                        _currentRunningNodeGuid = guid;
                    }
                    else
                    {
                        nodeView.SetVisualState(NodeState.Completed);
                        _executedNodeGuids.Add(guid);
                    }
                }
#if ODIN_INSPECTOR
                else if (nodeElement is NodeViewOdin odinView)
                {
                    if (runner.CurrentNode != null && runner.CurrentNode.Guid == guid)
                    {
                        odinView.SetVisualState(NodeState.Running);
                        _currentRunningNodeGuid = guid;
                    }
                    else
                    {
                        odinView.SetVisualState(NodeState.Completed);
                        _executedNodeGuids.Add(guid);
                    }
                }
#endif
            }
            
            // Sync edge highlights
            SyncEdgeHighlights();
        }

        // ============================================================
        //  EDGE EXECUTION HIGHLIGHTING (inspired by Doozy's curve colors)
        // ============================================================
        
        /// <summary>
        /// Highlight OUTGOING edges from the currently running node.
        /// The traveling dot shows "where execution will go next",
        /// giving the user a forward-looking view of the execution flow.
        /// Also converts any previously-active edges into "executed".
        /// </summary>
        private void HighlightEdgesForActiveNode(string nodeGuid)
        {
            // Find the running node directly (same lookup that node glow uses – proven to work)
            // then walk its output ports → connected edges to set them active.
            //
            // NOTE: We intentionally do NOT convert other active edges to executed here.
            // That transition is handled by MarkEdgesAsExecuted() when a node completes.
            // Converting all active edges here would break parallel execution
            // (a later node start would wipe an earlier running node's active edges).
            var nodeElement = GetNodeByGuid(nodeGuid);
            if (nodeElement == null) return;

            var outputPorts = nodeElement.outputContainer.Query<Port>().ToList();
            foreach (var port in outputPorts)
            {
                foreach (var edge in port.connections)
                {
                    edge.RemoveFromClassList("edge-executed");
                    edge.AddToClassList("edge-active");
                    (edge.edgeControl as DoozyStyleEdgeControl)?.SetRuntimeState(
                        DoozyStyleEdgeControl.EdgeRuntimeState.Active);
                }
            }
        }
        
        /// <summary>
        /// Mark outgoing edges from a completed node as "executed".
        /// These get a green tint to trace the path that was already taken.
        /// </summary>
        private void MarkEdgesAsExecuted(string nodeGuid)
        {
            var nodeElement = GetNodeByGuid(nodeGuid);
            if (nodeElement == null) return;

            // Mark all outgoing edges from this node as executed
            var outputPorts = nodeElement.outputContainer.Query<Port>().ToList();
            foreach (var port in outputPorts)
            {
                foreach (var edge in port.connections)
                {
                    edge.RemoveFromClassList("edge-active");
                    edge.AddToClassList("edge-executed");
                    (edge.edgeControl as DoozyStyleEdgeControl)?.SetRuntimeState(
                        DoozyStyleEdgeControl.EdgeRuntimeState.Executed);
                }
            }

            // Also convert any active incoming edges to executed
            var inputPorts = nodeElement.inputContainer.Query<Port>().ToList();
            foreach (var port in inputPorts)
            {
                foreach (var edge in port.connections)
                {
                    if (edge.ClassListContains("edge-active"))
                    {
                        edge.RemoveFromClassList("edge-active");
                        edge.AddToClassList("edge-executed");
                        (edge.edgeControl as DoozyStyleEdgeControl)?.SetRuntimeState(
                            DoozyStyleEdgeControl.EdgeRuntimeState.Executed);
                    }
                }
            }
        }
        
        /// <summary>
        /// Rebuild edge highlights from tracked execution state
        /// (used when syncing with an already-running graph)
        /// </summary>
        private void SyncEdgeHighlights()
        {
            ClearEdgeHighlights();

            // Highlight executed edges by walking executed nodes' output ports
            foreach (var guid in _executedNodeGuids)
            {
                var nodeElement = GetNodeByGuid(guid);
                if (nodeElement == null) continue;

                var outputPorts = nodeElement.outputContainer.Query<Port>().ToList();
                foreach (var port in outputPorts)
                {
                    foreach (var edge in port.connections)
                    {
                        // Only mark as executed if the target was also executed or is the current running node
                        var targetData = GetNodeData(edge.input?.node);
                        if (targetData == null) continue;
                        var targetGuid = targetData.Guid;
                        if (_executedNodeGuids.Contains(targetGuid) || targetGuid == _currentRunningNodeGuid)
                        {
                            edge.AddToClassList("edge-executed");
                            (edge.edgeControl as DoozyStyleEdgeControl)?.SetRuntimeState(
                                DoozyStyleEdgeControl.EdgeRuntimeState.Executed);
                        }
                    }
                }
            }

            // Highlight active edges from the currently running node
            if (!string.IsNullOrEmpty(_currentRunningNodeGuid))
            {
                var runningNode = GetNodeByGuid(_currentRunningNodeGuid);
                if (runningNode != null)
                {
                    var outputPorts = runningNode.outputContainer.Query<Port>().ToList();
                    foreach (var port in outputPorts)
                    {
                        foreach (var edge in port.connections)
                        {
                            edge.RemoveFromClassList("edge-executed");
                            edge.AddToClassList("edge-active");
                            (edge.edgeControl as DoozyStyleEdgeControl)?.SetRuntimeState(
                                DoozyStyleEdgeControl.EdgeRuntimeState.Active);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Remove all execution highlighting from edges
        /// </summary>
        private void ClearEdgeHighlights()
        {
            foreach (var edge in edges.ToList())
            {
                edge.RemoveFromClassList("edge-active");
                edge.RemoveFromClassList("edge-executed");
                (edge.edgeControl as DoozyStyleEdgeControl)?.ResetState();
            }
        }

        // ============================================================
        //  ZOOM-BASED LOD (inspired by Doozy's NodyWindowDrawViewGraph LOD)
        // ============================================================
        
        /// <summary>
        /// Called whenever the view transform (pan/zoom) changes.
        /// Updates zoom-based detail level on all nodes.
        /// </summary>
        private void OnViewTransformChanged(GraphView graphView)
        {
            float newZoom = viewTransform.scale.x;
            
            // Only update if zoom changed meaningfully (avoid unnecessary work)
            if (Mathf.Abs(newZoom - _currentZoom) < 0.01f) return;
            _currentZoom = newZoom;
            
            // Update all node LOD levels
            foreach (var element in graphElements.ToList())
            {
                if (element is NodeView nodeView)
                {
                    nodeView.SetZoomDetailLevel(newZoom);
                }
            }
        }

        // ============================================================
        //  DELETE PREVIEW (inspired by Doozy's delete mode)
        // ============================================================
        
        /// <summary>
        /// Find the node under the mouse cursor and apply delete-preview styling.
        /// Active only while Alt is held.
        /// </summary>
        private void UpdateDeletePreview(Vector2 localMousePos)
        {
            // Convert to content space
            var worldPos = this.LocalToWorld(localMousePos);
            
            // Find node under cursor by checking all node views
            NodeView hoveredNode = null;
            foreach (var element in graphElements.ToList())
            {
                if (element is NodeView nv && nv.worldBound.Contains(worldPos))
                {
                    hoveredNode = nv;
                    break;
                }
            }
            
            // If the hovered node changed, update preview
            if (hoveredNode != _deletePreviewNode)
            {
                // Clear old preview
                _deletePreviewNode?.SetDeletePreview(false);
                
                // Set new preview
                _deletePreviewNode = hoveredNode;
                _deletePreviewNode?.SetDeletePreview(true);
            }
        }
        
        /// <summary>
        /// Remove delete-preview from all nodes (when Alt is released)
        /// </summary>
        private void ClearDeletePreview()
        {
            _deletePreviewNode?.SetDeletePreview(false);
            _deletePreviewNode = null;
        }

        // ============================================================
        //  INITIALIZATION & GRAPH LOADING
        // ============================================================

        /// <summary>
        /// Initialize with search window
        /// </summary>
        public void Initialize(EditorWindow window)
        {
            _searchWindow = ScriptableObject.CreateInstance<NodeSearchWindow>();
            _searchWindow.Initialize(this, window);
            
            nodeCreationRequest = ctx =>
            {
                SearchWindow.Open(new SearchWindowContext(ctx.screenMousePosition), _searchWindow);
            };
        }

        /// <summary>
        /// Load a graph for editing
        /// </summary>
        public void LoadGraph(NodeGraph graph)
        {
            // Always clear first to prevent duplicates
            ClearGraph();
            
            Graph = graph;

            if (graph == null) return;

            // Ensure graph is fully loaded
            var nodes = graph.Nodes;
            var connections = graph.Connections;
            
            Debug.Log($"[NodeGraphView] Loading: {graph.graphName} - {nodes.Count} nodes, {connections.Count} connections");
            
            if (nodes.Count == 0 && connections.Count == 0)
            {
                Debug.LogWarning($"[NodeGraphView] Graph {graph.graphName} appears to be empty! Check if _jsonData is populated.");
            }

            // Create node views - track GUIDs to prevent duplicates
            var createdGuids = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (node == null) continue;
                
                if (createdGuids.Contains(node.Guid))
                {
                    Debug.LogWarning($"[NodeGraphView] Duplicate node GUID in graph data: {node.Guid} ({node.Name}). Skipping.");
                    continue;
                }
                
                var view = CreateNodeView(node);
                if (view != null || node is Nodes.CommentNode)
                {
                    createdGuids.Add(node.Guid);
                }
            }
            
            // Clean up floating edges after port refresh
            CleanupFloatingEdges();

            // Clean up any "addStep" connections from graph data
            var addStepConnections = graph.Connections.Where(c => c.outputPortId == "addStep").ToList();
            foreach (var conn in addStepConnections)
            {
                graph.RemoveConnection(conn);
            }

            // Create edges
            foreach (var conn in graph.Connections)
            {
                CreateEdge(conn);
            }

            // Clean up floating edges
            CleanupFloatingEdges();
            schedule.Execute(() => CleanupFloatingEdges()).ExecuteLater(100);

            // Add minimap after graph content is loaded
            if (_minimap == null || _minimap.parent == null)
            {
                if (_minimap == null)
                {
                    _minimap = new MinimapView();
                }
                Add(_minimap);
            }

            // Apply current zoom LOD to newly created nodes
            foreach (var element in graphElements.ToList())
            {
                if (element is NodeView nv)
                {
                    nv.SetZoomDetailLevel(_currentZoom);
                }
            }

            // Sync with runtime state if in play mode
            if (EditorApplication.isPlaying)
            {
                SyncWithRuntimeState();
            }
        }

        /// <summary>
        /// Clean up any floating edges (edges with invalid or missing ports, or duplicates)
        /// </summary>
        private void CleanupFloatingEdges()
        {
            var allEdges = edges.ToList();
            var edgesToRemove = new HashSet<Edge>();
            var seenConnections = new HashSet<(string outputGuid, string outputPort, string inputGuid, string inputPort)>();
            
            foreach (var edge in allEdges)
            {
                if (edge.output == null || edge.input == null)
                {
                    edgesToRemove.Add(edge);
                    continue;
                }
                
                if (edge.output.node == null || edge.input.node == null)
                {
                    edgesToRemove.Add(edge);
                    continue;
                }
                
                var outputData = GetNodeData(edge.output.node);
                var inputData = GetNodeData(edge.input.node);
                
                if (outputData == null || inputData == null)
                {
                    edgesToRemove.Add(edge);
                    continue;
                }
                
                bool outputPortExists = false;
                if (edge.output.node is NodeView outputNodeView)
                {
                    outputPortExists = outputNodeView.GetOutputPort(edge.output.name) != null;
                }
                
                bool inputPortExists = false;
                if (edge.input.node is NodeView inputNodeView)
                {
                    inputPortExists = inputNodeView.GetInputPort(edge.input.name) != null;
                }
                
                if (!outputPortExists || !inputPortExists)
                {
                    edgesToRemove.Add(edge);
                    continue;
                }
                
                var connectionKey = (outputData.Guid, edge.output.name, inputData.Guid, edge.input.name);
                if (seenConnections.Contains(connectionKey))
                {
                    edgesToRemove.Add(edge);
                }
                else
                {
                    seenConnections.Add(connectionKey);
                }
            }

            foreach (var edge in edgesToRemove)
            {
                RemoveElement(edge);
            }
            
            if (edgesToRemove.Count > 0)
            {
                Debug.Log($"[NodeGraphView] Cleaned up {edgesToRemove.Count} floating/duplicate edges");
            }
        }

        /// <summary>
        /// Clear all elements
        /// </summary>
        private void ClearGraph()
        {
            graphViewChanged = null;
            
            var allElements = graphElements.ToList();
            
            var allEdges = allElements.OfType<Edge>().ToList();
            foreach (var edge in allEdges)
            {
                RemoveElement(edge);
            }
            
            var allNodes = allElements.OfType<NodeView>().ToList();
            foreach (var node in allNodes)
            {
                RemoveElement(node);
            }
            
            var allComments = allElements.OfType<CommentNodeView>().ToList();
            foreach (var comment in allComments)
            {
                RemoveElement(comment);
            }
            
            DeleteElements(graphElements.ToList());
            
            graphViewChanged = OnGraphViewChanged;
            
            // Clear execution tracking
            _executedNodeGuids.Clear();
            _currentRunningNodeGuid = null;
            
            Debug.Log($"[NodeGraphView] Cleared graph: removed {allNodes.Count} nodes, {allEdges.Count} edges");
        }

        /// <summary>
        /// Create a visual node
        /// </summary>
        public NodeView CreateNodeView(NodeData data)
        {
            if (data == null) return null;
            
            var existingNode = GetNodeByGuid(data.Guid);
            if (existingNode != null)
            {
                Debug.LogWarning($"[NodeGraphView] Node view already exists for GUID {data.Guid} ({data.Name}). Skipping duplicate creation.");
                return existingNode as NodeView;
            }
            
            // Special handling for comment nodes
            if (data is Nodes.CommentNode commentNode)
            {
                var commentView = new CommentNodeView(commentNode, this);
                AddElement(commentView);
                return null;
            }

            var view = new NodeView(data);
            view.OnNodeSelected = OnNodeSelected;
            view.OnDataChanged = () => 
            {
                if (Graph != null)
                {
                    Graph.SaveToJson();
                    EditorUtility.SetDirty(Graph);
                }
            };
            
            // Apply current zoom level to new node
            view.SetZoomDetailLevel(_currentZoom);
            
            AddElement(view);
            return view;
        }

        /// <summary>
        /// Create a node at position
        /// </summary>
        public void CreateNode(Type nodeType, Vector2 position)
        {
            if (Graph == null)
            {
                Debug.LogError("[NodeGraphView] No graph loaded!");
                return;
            }

            var node = (NodeData)Activator.CreateInstance(nodeType);
            node.Position = position;

            Undo.RecordObject(Graph, "Add Node");
            Graph.AddNode(node);
            Graph.Save();

            CreateNodeView(node);
        }

        /// <summary>
        /// Get NodeData from a node (supports both NodeView and NodeViewOdin)
        /// </summary>
        private NodeData GetNodeData(UnityEditor.Experimental.GraphView.Node node)
        {
            if (node is NodeView nodeView)
                return nodeView.Data;
#if ODIN_INSPECTOR
            if (node is NodeViewOdin odinView)
                return odinView.Data;
#endif
            return null;
        }

        /// <summary>
        /// Get a port from a node (supports both NodeView and NodeViewOdin)
        /// </summary>
        private Port GetPortFromNode(UnityEditor.Experimental.GraphView.Node node, string portId, bool isInput)
        {
            if (node is NodeView nodeView)
            {
                return isInput ? nodeView.GetInputPort(portId) : nodeView.GetOutputPort(portId);
            }
#if ODIN_INSPECTOR
            else if (node is NodeViewOdin odinView)
            {
                return isInput ? odinView.GetInputPort(portId) : odinView.GetOutputPort(portId);
            }
#endif
            return null;
        }

        /// <summary>
        /// Create an edge from connection data (checks for duplicates first)
        /// </summary>
        private void CreateEdge(ConnectionData conn)
        {
            var outputNode = GetNodeByGuid(conn.outputNodeGuid);
            var inputNode = GetNodeByGuid(conn.inputNodeGuid);

            if (outputNode == null || inputNode == null) return;

            var outputPort = GetPortFromNode(outputNode, conn.outputPortId, false);
            var inputPort = GetPortFromNode(inputNode, conn.inputPortId, true);

            if (outputPort == null || inputPort == null) return;

            var existingEdge = edges.ToList().FirstOrDefault(e =>
                e.output == outputPort && e.input == inputPort
            );
            
            if (existingEdge != null) return;

            var edge = new DoozyStyleEdge();
            edge.output = outputPort;
            edge.input = inputPort;
            outputPort.Connect(edge);
            inputPort.Connect(edge);
            AddElement(edge);
        }

        /// <summary>
        /// Refresh all edges connected to a specific node
        /// </summary>
        private void RefreshEdgesForNode(string nodeGuid)
        {
            if (Graph == null) return;

            var addStepEdgesToRemove = edges.ToList().Where(e =>
            {
                var outputData = GetNodeData(e.output.node);
                return outputData != null && e.output.name == "addStep";
            }).ToList();

            foreach (var edge in addStepEdgesToRemove)
            {
                RemoveElement(edge);
            }

            var edgesToRemove = edges.ToList().Where(e =>
                (GetNodeData(e.output.node)?.Guid == nodeGuid) ||
                (GetNodeData(e.input.node)?.Guid == nodeGuid)
            ).ToList();

            foreach (var edge in edgesToRemove)
            {
                RemoveElement(edge);
            }

            foreach (var conn in Graph.Connections)
            {
                if (conn.outputPortId == "addStep") continue;
                
                if (conn.outputNodeGuid == nodeGuid || conn.inputNodeGuid == nodeGuid)
                {
                    CreateEdge(conn);
                }
            }
            
            CleanupFloatingEdges();
        }

        // ============================================================
        //  GRAPH CHANGE HANDLING
        // ============================================================

        private GraphViewChange OnGraphViewChanged(GraphViewChange change)
        {
            if (Graph == null) return change;

            // Handle removed elements
            if (change.elementsToRemove != null)
            {
                foreach (var elem in change.elementsToRemove)
                {
                    NodeData nodeData = null;
                    
                    if (elem is NodeView nodeView)
                    {
                        nodeData = nodeView.Data;
                    }
#if ODIN_INSPECTOR
                    else if (elem is NodeViewOdin odinView)
                    {
                        nodeData = odinView.Data;
                    }
#endif
                    else if (elem is CommentNodeView commentView)
                    {
                        nodeData = commentView.Data;
                    }
                    
                    if (nodeData != null)
                    {
                        Undo.RecordObject(Graph, "Remove Node");
                        Graph.RemoveNode(nodeData);
                    }
                    else if (elem is Edge edge)
                    {
                        var outputData = GetNodeData(edge.output.node);
                        var inputData = GetNodeData(edge.input.node);

                        if (outputData != null && inputData != null)
                        {
                            var conn = new ConnectionData(
                                outputData.Guid,
                                edge.output.name,
                                inputData.Guid,
                                edge.input.name
                            );
                            
                            Undo.RecordObject(Graph, "Remove Connection");
                            Graph.RemoveConnection(conn);
                        }
                    }
                }
                Graph.Save();
            }

            // Handle created edges
            if (change.edgesToCreate != null)
            {
                var edgesToRemove = new List<Edge>();
                
                foreach (var edge in change.edgesToCreate)
                {
                    var outputData = GetNodeData(edge.output.node);
                    var inputData = GetNodeData(edge.input.node);

                    if (outputData != null && inputData != null)
                    {
                        if (edge.output.name == "addStep")
                        {
                            edgesToRemove.Add(edge);
                            RemoveElement(edge);
                            continue;
                        }
                            
                        var conn = new ConnectionData(
                            outputData.Guid,
                            edge.output.name,
                            inputData.Guid,
                            edge.input.name
                        );
                            
                        Undo.RecordObject(Graph, "Add Connection");
                        Graph.AddConnection(conn);
                    }
                }
                
                foreach (var edge in edgesToRemove)
                {
                    change.edgesToCreate.Remove(edge);
                }
                
                Graph.Save();
            }

            // Handle moved elements
            if (change.movedElements != null)
            {
                foreach (var elem in change.movedElements)
                {
                    if (elem is NodeView nodeView)
                    {
                        nodeView.Data.Position = nodeView.GetPosition().position;
                    }
                }
                EditorUtility.SetDirty(Graph);
                Graph.Save();
            }

            return change;
        }

        /// <summary>
        /// Get compatible ports for connections
        /// </summary>
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList().Where(endPort =>
                endPort.direction != startPort.direction &&
                endPort.node != startPort.node
            ).ToList();
        }

        /// <summary>
        /// Handle undo/redo
        /// </summary>
        private void OnUndoRedo()
        {
            if (Graph != null)
            {
                Graph.ForceReload();
                LoadGraph(Graph);
            }
        }

        /// <summary>
        /// Convert screen position to graph position
        /// </summary>
        public Vector2 ScreenToGraphPosition(Vector2 screenPos)
        {
            var worldPos = screenPos - new Vector2(worldBound.x, worldBound.y);
            return contentViewContainer.WorldToLocal(worldPos);
        }

        // ============================================================
        //  COPY / PASTE / DUPLICATE
        // ============================================================

        private string SerializeGraphElementsCallback(IEnumerable<GraphElement> elements)
        {
            _copiedNodes.Clear();
            _copiedConnections.Clear();

            var selectedNodes = elements.OfType<NodeView>().ToList();
            var selectedEdges = elements.OfType<Edge>().ToList();

            if (selectedNodes.Count == 0) return "";

            _copyCenter = Vector2.zero;
            foreach (var nodeView in selectedNodes)
            {
                _copyCenter += nodeView.Data.Position;
            }
            _copyCenter /= selectedNodes.Count;

            var guidMap = new Dictionary<string, string>();
            foreach (var nodeView in selectedNodes)
            {
                var original = nodeView.Data;
                var clone = CloneNode(original);
                guidMap[original.Guid] = clone.Guid;
                _copiedNodes.Add(clone);
            }

            foreach (var edge in selectedEdges)
            {
                var outputView = edge.output?.node as NodeView;
                var inputView = edge.input?.node as NodeView;

                if (outputView != null && inputView != null)
                {
                    if (guidMap.ContainsKey(outputView.Data.Guid) && guidMap.ContainsKey(inputView.Data.Guid))
                    {
                        var conn = new ConnectionData(
                            guidMap[outputView.Data.Guid],
                            edge.output.name,
                            guidMap[inputView.Data.Guid],
                            edge.input.name
                        );
                        _copiedConnections.Add(conn);
                    }
                }
            }

            return $"NodeGraph_Copy:{_copiedNodes.Count}";
        }

        private bool CanPasteSerializedDataCallback(string data)
        {
            return !string.IsNullOrEmpty(data) && data.StartsWith("NodeGraph_Copy:") && _copiedNodes.Count > 0;
        }

        private void UnserializeAndPasteCallback(string operationName, string data)
        {
            if (Graph == null || _copiedNodes.Count == 0) return;

            var pastePosition = contentViewContainer.WorldToLocal(_lastMousePosition);
            PasteNodesAt(pastePosition);
        }

        private void PasteNodesAt(Vector2 position)
        {
            if (Graph == null || _copiedNodes.Count == 0) return;

            Undo.RecordObject(Graph, "Paste Nodes");
            ClearSelection();

            var guidMap = new Dictionary<string, string>();
            var newNodeViews = new List<NodeView>();

            foreach (var copiedNode in _copiedNodes)
            {
                var newNode = CloneNode(copiedNode);
                var oldGuid = copiedNode.Guid;
                guidMap[oldGuid] = newNode.Guid;

                var relativePos = copiedNode.Position - _copyCenter;
                newNode.Position = position + relativePos;

                Graph.AddNode(newNode);
                var view = CreateNodeView(newNode);
                newNodeViews.Add(view);
            }

            foreach (var conn in _copiedConnections)
            {
                if (guidMap.TryGetValue(conn.outputNodeGuid, out var newOutputGuid) &&
                    guidMap.TryGetValue(conn.inputNodeGuid, out var newInputGuid))
                {
                    var newConn = new ConnectionData(
                        newOutputGuid,
                        conn.outputPortId,
                        newInputGuid,
                        conn.inputPortId
                    );
                    Graph.AddConnection(newConn);
                    CreateEdge(newConn);
                }
            }

            Graph.Save();

            foreach (var view in newNodeViews)
            {
                AddToSelection(view);
            }
        }

        public void DuplicateSelection()
        {
            var selectedNodes = selection.OfType<NodeView>().ToList();
            var selectedEdges = selection.OfType<Edge>().ToList();

            if (selectedNodes.Count == 0) return;

            var tempNodes = new List<NodeData>(_copiedNodes);
            var tempConnections = new List<ConnectionData>(_copiedConnections);
            var tempCenter = _copyCenter;

            SerializeGraphElementsCallback(selection.OfType<GraphElement>());

            var offset = new Vector2(50, 50);
            var centerPos = _copyCenter + offset;
            PasteNodesAt(centerPos);

            _copiedNodes = tempNodes;
            _copiedConnections = tempConnections;
            _copyCenter = tempCenter;
        }

        private NodeData CloneNode(NodeData original)
        {
            var json = JsonUtility.ToJson(original);
            var clone = (NodeData)Activator.CreateInstance(original.GetType());
            JsonUtility.FromJsonOverwrite(json, clone);
            clone.Guid = Guid.NewGuid().ToString();
            return clone;
        }
    }
}
#endif
