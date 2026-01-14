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

        // Odin Inspector support removed - always use custom inline content
        
        // Copy/paste
        private List<NodeData> _copiedNodes = new List<NodeData>();
        private List<ConnectionData> _copiedConnections = new List<ConnectionData>();
        private Vector2 _copyCenter;
        private Vector2 _lastMousePosition;

        public NodeGraphView()
        {
            // Add background grid
            Insert(0, new GridBackground());

            // Add minimap
            var minimap = new MinimapView();
            Add(minimap);

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
        }

        private void OnGraphContextMenu(ContextualMenuPopulateEvent evt)
        {
            // Only show if clicking on empty space (not on a node)
            if (evt.target == this || evt.target is GridBackground)
            {
                // Group functionality removed due to persistent issues
            }
        }

        // Group functionality removed due to persistent serialization and restoration issues

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
                    // Refresh to sync with potential runtime changes
                    break;
                    
                case PlayModeStateChange.ExitingPlayMode:
                    // Reset all visual states when exiting play mode
                    ResetAllNodeVisualStates();
                    break;
            }
        }

        private void OnRuntimeNodeStarted(NodeGraphRunner runner, NodeData node)
        {
            // Only update if this is our graph
            if (Graph == null || runner.Graph != Graph) return;

            UpdateNodeVisualState(node.Guid, NodeState.Running);
        }

        private void OnRuntimeNodeCompleted(NodeGraphRunner runner, NodeData node)
        {
            // Only update if this is our graph
            if (Graph == null || runner.Graph != Graph) return;

            UpdateNodeVisualState(node.Guid, NodeState.Completed);
        }

        /// <summary>
        /// Update visual state of a node (handles threading and timing issues)
        /// </summary>
        private void UpdateNodeVisualState(string nodeGuid, NodeState state)
        {
            // Try immediate update first
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
            // Only update if this is our graph
            if (Graph == null || runner.Graph != Graph) return;

            // Reset all nodes to idle
            ResetAllNodeVisualStates();
        }

        private void OnRuntimeGraphEnded(NodeGraphRunner runner)
        {
            // Could add visual feedback for graph completion
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

            // Mark executed nodes as completed
            foreach (var guid in runner.ExecutionPath)
            {
                var nodeElement = GetNodeByGuid(guid);
                if (nodeElement is NodeView nodeView)
                {
                    if (runner.CurrentNode != null && runner.CurrentNode.Guid == guid)
                        nodeView.SetVisualState(NodeState.Running);
                    else
                        nodeView.SetVisualState(NodeState.Completed);
                }
#if ODIN_INSPECTOR
                else if (nodeElement is NodeViewOdin odinView)
                {
                    if (runner.CurrentNode != null && runner.CurrentNode.Guid == guid)
                        odinView.SetVisualState(NodeState.Running);
                    else
                        odinView.SetVisualState(NodeState.Completed);
                }
#endif
            }
        }

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

            // Ensure graph is fully loaded (this will restore sequence ports)
            // Access Nodes property to trigger EnsureLoaded and port restoration
            // Do this BEFORE checking counts to ensure data is loaded
            var nodes = graph.Nodes;
            var connections = graph.Connections;
            
            Debug.Log($"[NodeGraphView] Loading: {graph.graphName} - {nodes.Count} nodes, {connections.Count} connections");
            
            if (nodes.Count == 0 && connections.Count == 0)
            {
                Debug.LogWarning($"[NodeGraphView] Graph {graph.graphName} appears to be empty! Check if _jsonData is populated.");
            }

            // Create node views AFTER ports are restored
            // Track created GUIDs to prevent duplicates
            var createdGuids = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (node == null) continue;
                
                // Skip if we already created a view for this GUID
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
            
            
            // Clean up floating edges after port refresh (ports refresh disconnects edges)
            CleanupFloatingEdges();

            // Clean up any "addStep" connections from graph data (they shouldn't be persisted)
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

            // Clean up any floating edges after creating edges
            CleanupFloatingEdges();
            
            // Schedule another cleanup after a short delay to catch any timing issues
            schedule.Execute(() => CleanupFloatingEdges()).ExecuteLater(100);

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
                // Check if edge has valid ports
                if (edge.output == null || edge.input == null)
                {
                    edgesToRemove.Add(edge);
                    continue;
                }
                
                // Check if ports have valid nodes
                if (edge.output.node == null || edge.input.node == null)
                {
                    edgesToRemove.Add(edge);
                    continue;
                }
                
                var outputData = GetNodeData(edge.output.node);
                var inputData = GetNodeData(edge.input.node);
                
                // Check if nodes are valid
                if (outputData == null || inputData == null)
                {
                    edgesToRemove.Add(edge);
                    continue;
                }
                
                // Check if ports still exist on their nodes
                var outputNode = edge.output.node;
                var inputNode = edge.input.node;
                
                // Try to find the port on the output node
                bool outputPortExists = false;
                if (outputNode is NodeView outputNodeView)
                {
                    outputPortExists = outputNodeView.GetOutputPort(edge.output.name) != null;
                }
                
                // Try to find the port on the input node
                bool inputPortExists = false;
                if (inputNode is NodeView inputNodeView)
                {
                    inputPortExists = inputNodeView.GetInputPort(edge.input.name) != null;
                }
                
                // Remove edge if either port doesn't exist
                if (!outputPortExists || !inputPortExists)
                {
                    edgesToRemove.Add(edge);
                    continue;
                }
                
                // Check for duplicate edges (same connection)
                var connectionKey = (outputData.Guid, edge.output.name, inputData.Guid, edge.input.name);
                if (seenConnections.Contains(connectionKey))
                {
                    // Duplicate edge - remove it
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
            
            // Get all elements before deleting (to avoid modification during iteration)
            var allElements = graphElements.ToList();
            
            // Remove all edges first
            var allEdges = allElements.OfType<Edge>().ToList();
            foreach (var edge in allEdges)
            {
                RemoveElement(edge);
            }
            
            // Remove all nodes
            var allNodes = allElements.OfType<NodeView>().ToList();
            foreach (var node in allNodes)
            {
                RemoveElement(node);
            }
            
            // Remove comment nodes
            var allComments = allElements.OfType<CommentNodeView>().ToList();
            foreach (var comment in allComments)
            {
                RemoveElement(comment);
            }
            
            // Clear any remaining elements
            DeleteElements(graphElements.ToList());
            
            graphViewChanged = OnGraphViewChanged;
            
            Debug.Log($"[NodeGraphView] Cleared graph: removed {allNodes.Count} nodes, {allEdges.Count} edges");
        }

        /// <summary>
        /// Create a visual node
        /// </summary>
        public NodeView CreateNodeView(NodeData data)
        {
            if (data == null) return null;
            
            // Check if node view already exists for this GUID (prevent duplicates)
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
                return null; // Comment nodes don't use NodeView
            }

            // Always use regular NodeView with custom inline content
            var view = new NodeView(data);
            view.OnNodeSelected = OnNodeSelected;
            view.OnDataChanged = () => 
            {
                if (Graph != null)
                {
                    EditorUtility.SetDirty(Graph);
                }
            };
            AddElement(view);
            return view;
        }

        // Odin Inspector support removed

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

            // Check if edge already exists
            var existingEdge = edges.ToList().FirstOrDefault(e =>
                e.output == outputPort && e.input == inputPort
            );
            
            if (existingEdge != null)
            {
                // Edge already exists, don't create duplicate
                return;
            }

            var edge = outputPort.ConnectTo(inputPort);
            AddElement(edge);
        }

        /// <summary>
        /// Refresh all edges connected to a specific node (removes old edges and recreates from graph data)
        /// </summary>
        private void RefreshEdgesForNode(string nodeGuid)
        {
            if (Graph == null) return;

            // First, remove ALL "addStep" edges from the entire graph (they shouldn't exist)
            var addStepEdgesToRemove = edges.ToList().Where(e =>
            {
                var outputData = GetNodeData(e.output.node);
                return outputData != null && e.output.name == "addStep";
            }).ToList();

            foreach (var edge in addStepEdgesToRemove)
            {
                RemoveElement(edge);
            }

            // Remove all existing edges connected to this node
            var edgesToRemove = edges.ToList().Where(e =>
                (GetNodeData(e.output.node)?.Guid == nodeGuid) ||
                (GetNodeData(e.input.node)?.Guid == nodeGuid)
            ).ToList();

            foreach (var edge in edgesToRemove)
            {
                RemoveElement(edge);
            }

            // Recreate all edges from graph data that involve this node
            // Skip "addStep" connections as they are temporary and shouldn't be persisted
            foreach (var conn in Graph.Connections)
            {
                if (conn.outputPortId == "addStep") continue; // Skip temporary "addStep" connections
                
                if (conn.outputNodeGuid == nodeGuid || conn.inputNodeGuid == nodeGuid)
                {
                    CreateEdge(conn);
                }
            }
            
            // Clean up any floating edges that might have been created
            CleanupFloatingEdges();
        }

        /// <summary>
        /// Handle graph changes (adding/removing elements)
        /// </summary>
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
                        // Skip "addStep" connections (legacy from SequenceNode)
                        if (edge.output.name == "addStep")
                            {
                                // Mark for removal and remove visually
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
                }
                
                // Remove edges that shouldn't be created
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
                // Force reload graph data from JSON (undo restores the ScriptableObject state)
                Graph.ForceReload();
                // Reload the visual graph
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

        // === Copy/Paste/Duplicate ===

        /// <summary>
        /// Serialize selected elements for copy
        /// </summary>
        private string SerializeGraphElementsCallback(IEnumerable<GraphElement> elements)
        {
            _copiedNodes.Clear();
            _copiedConnections.Clear();

            var selectedNodes = elements.OfType<NodeView>().ToList();
            var selectedEdges = elements.OfType<Edge>().ToList();

            if (selectedNodes.Count == 0) return "";

            // Calculate center of selection for relative positioning
            _copyCenter = Vector2.zero;
            foreach (var nodeView in selectedNodes)
            {
                _copyCenter += nodeView.Data.Position;
            }
            _copyCenter /= selectedNodes.Count;

            // Copy nodes (deep clone)
            var guidMap = new Dictionary<string, string>(); // old guid -> new guid
            foreach (var nodeView in selectedNodes)
            {
                var original = nodeView.Data;
                var clone = CloneNode(original);
                guidMap[original.Guid] = clone.Guid;
                _copiedNodes.Add(clone);
            }

            // Copy connections between selected nodes
            foreach (var edge in selectedEdges)
            {
                var outputView = edge.output?.node as NodeView;
                var inputView = edge.input?.node as NodeView;

                if (outputView != null && inputView != null)
                {
                    // Only copy if both nodes are in selection
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

            // Return a marker string (actual data is stored in fields)
            return $"NodeGraph_Copy:{_copiedNodes.Count}";
        }

        /// <summary>
        /// Check if paste is possible
        /// </summary>
        private bool CanPasteSerializedDataCallback(string data)
        {
            return !string.IsNullOrEmpty(data) && data.StartsWith("NodeGraph_Copy:") && _copiedNodes.Count > 0;
        }

        /// <summary>
        /// Paste copied elements at mouse position
        /// </summary>
        private void UnserializeAndPasteCallback(string operationName, string data)
        {
            if (Graph == null || _copiedNodes.Count == 0) return;

            // Get paste position (mouse position in graph space)
            var pastePosition = contentViewContainer.WorldToLocal(_lastMousePosition);
            
            PasteNodesAt(pastePosition);
        }

        /// <summary>
        /// Paste nodes at a specific position
        /// </summary>
        private void PasteNodesAt(Vector2 position)
        {
            if (Graph == null || _copiedNodes.Count == 0) return;

            Undo.RecordObject(Graph, "Paste Nodes");

            // Clear selection
            ClearSelection();

            // Create new guid mapping for this paste operation
            var guidMap = new Dictionary<string, string>();
            var newNodeViews = new List<NodeView>();

            foreach (var copiedNode in _copiedNodes)
            {
                // Clone again for each paste (so we can paste multiple times)
                var newNode = CloneNode(copiedNode);
                var oldGuid = copiedNode.Guid;
                guidMap[oldGuid] = newNode.Guid;

                // Calculate relative position from copy center
                var relativePos = copiedNode.Position - _copyCenter;
                newNode.Position = position + relativePos;

                // Add to graph
                Graph.AddNode(newNode);

                // Create view
                var view = CreateNodeView(newNode);
                newNodeViews.Add(view);
            }

            // Recreate connections with new guids
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

            // Select pasted nodes
            foreach (var view in newNodeViews)
            {
                AddToSelection(view);
            }
        }

        /// <summary>
        /// Duplicate selected nodes
        /// </summary>
        public void DuplicateSelection()
        {
            var selectedNodes = selection.OfType<NodeView>().ToList();
            var selectedEdges = selection.OfType<Edge>().ToList();

            if (selectedNodes.Count == 0) return;

            // Temporarily store current copied data
            var tempNodes = new List<NodeData>(_copiedNodes);
            var tempConnections = new List<ConnectionData>(_copiedConnections);
            var tempCenter = _copyCenter;

            // Copy selected elements
            SerializeGraphElementsCallback(selection.OfType<GraphElement>());

            // Paste with offset
            var offset = new Vector2(50, 50);
            var centerPos = _copyCenter + offset;
            PasteNodesAt(centerPos);

            // Restore previous copied data
            _copiedNodes = tempNodes;
            _copiedConnections = tempConnections;
            _copyCenter = tempCenter;
        }

        /// <summary>
        /// Clone a node (deep copy)
        /// </summary>
        private NodeData CloneNode(NodeData original)
        {
            // Use JSON serialization for deep clone
            var json = JsonUtility.ToJson(original);
            var clone = (NodeData)Activator.CreateInstance(original.GetType());
            JsonUtility.FromJsonOverwrite(json, clone);
            
            // Assign new GUID
            clone.Guid = Guid.NewGuid().ToString();
            
            return clone;
        }
    }
}
#endif

