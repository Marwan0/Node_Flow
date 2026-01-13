using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem
{
    /// <summary>
    /// Runtime executor for node graphs.
    /// Attach to a GameObject and assign a NodeGraph to execute it.
    /// </summary>
    public class NodeGraphRunner : MonoBehaviour
    {
        [Header("Graph")]
        [SerializeField]
        private NodeGraph _graph;

        [Header("Settings")]
        [SerializeField]
        private bool _runOnStart = true;

        [Header("Debug")]
        [SerializeField]
        private bool _debugMode = false;
        
        [SerializeField]
        private bool _isPaused = false;
        
        [SerializeField]
        private bool _stepMode = false;

        private NodeData _currentNode;
        private bool _isRunning;
        private List<string> _executionPath = new List<string>();
        private NodeData _pendingNextNode; // For step mode
        
        // Debug events
        public static event System.Action<NodeGraphRunner> OnPaused;
        public static event System.Action<NodeGraphRunner> OnResumed;
        public static event System.Action<NodeGraphRunner, NodeData> OnBreakpointHit;

        public bool IsRunning => _isRunning;
        public NodeData CurrentNode => _currentNode;
        public NodeGraph Graph => _graph;
        public IReadOnlyList<string> ExecutionPath => _executionPath;

        // === Static Events for Editor Visualization ===
        
        /// <summary>Event fired when a node starts executing</summary>
        public static event Action<NodeGraphRunner, NodeData> OnNodeStarted;
        
        /// <summary>Event fired when a node completes</summary>
        public static event Action<NodeGraphRunner, NodeData> OnNodeCompleted;
        
        /// <summary>Event fired when graph execution starts</summary>
        public static event Action<NodeGraphRunner> OnGraphStarted;
        
        /// <summary>Event fired when graph execution ends</summary>
        public static event Action<NodeGraphRunner> OnGraphEnded;
        
        /// <summary>Active runner instance (for editor visualization)</summary>
        public static NodeGraphRunner ActiveRunner { get; private set; }

        /// <summary>
        /// Broadcast that a node started (for parallel nodes to trigger visual feedback)
        /// </summary>
        public static void BroadcastNodeStarted(NodeGraphRunner runner, NodeData node)
        {
            OnNodeStarted?.Invoke(runner, node);
        }

        /// <summary>
        /// Broadcast that a node completed (for parallel nodes to trigger visual feedback)
        /// </summary>
        public static void BroadcastNodeCompleted(NodeGraphRunner runner, NodeData node)
        {
            OnNodeCompleted?.Invoke(runner, node);
        }

        private void Start()
        {
            if (_runOnStart && _graph != null)
            {
                Run();
            }
        }

        /// <summary>
        /// Start executing the graph
        /// </summary>
        public void Run()
        {
            if (_graph == null)
            {
                Debug.LogError("[NodeGraphRunner] No graph assigned!");
                return;
            }

            // Validate
            var errors = _graph.Validate();
            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    Debug.LogError($"[NodeGraphRunner] Validation error: {error}");
                }
                return;
            }

            // Reset all nodes
            _graph.ResetAllNodes();
            _executionPath.Clear();

            // Initialize all nodes with runner reference
            foreach (var node in _graph.Nodes)
            {
                node.Runner = this;
            }

            // Start from entry node
            var entry = _graph.GetEntryNode();
            if (entry == null)
            {
                Debug.LogError("[NodeGraphRunner] No entry node found!");
                return;
            }

            _isRunning = true;
            ActiveRunner = this;
            
            if (_debugMode)
                Debug.Log("[NodeGraphRunner] Starting graph execution");

            // Fire event
            OnGraphStarted?.Invoke(this);

            ExecuteNode(entry);
        }

        /// <summary>
        /// Stop graph execution
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            _currentNode = null;
            _pendingNextNode = null;
            _isPaused = false;
            _stepMode = false;
            StopAllCoroutines();
            
            if (ActiveRunner == this)
                ActiveRunner = null;

            // Fire event
            OnGraphEnded?.Invoke(this);
            
            if (_debugMode)
                Debug.Log("[NodeGraphRunner] Stopped");
        }

        /// <summary>
        /// Execute a specific node
        /// </summary>
        private void ExecuteNode(NodeData node)
        {
            if (!_isRunning) return;
            if (node == null) return;

            // Check if paused (but not from step mode - step mode handles its own flow)
            if (_isPaused && !_stepMode)
            {
                // Store the node to execute when resumed
                _pendingNextNode = node;
                return;
            }

            // Check for breakpoint - pause BEFORE executing
            if (node.hasBreakpoint)
            {
                Debug.Log($"[NodeGraphRunner] Breakpoint hit at: {node.Name}");
                _isPaused = true;
                _pendingNextNode = node; // Store node to execute when resumed
                OnBreakpointHit?.Invoke(this, node);
                OnPaused?.Invoke(this);
                return; // Don't execute yet, wait for resume
            }

            // Execute the node
            ExecuteNodeInternal(node);
        }

        /// <summary>
        /// Internal method to actually execute a node
        /// </summary>
        private void ExecuteNodeInternal(NodeData node)
        {
            if (!_isRunning) return;
            if (node == null) return;

            _currentNode = node;
            _executionPath.Add(node.Guid);

            if (_debugMode)
                Debug.Log($"[NodeGraphRunner] Executing: {node.Name}");

            // Fire event
            OnNodeStarted?.Invoke(this, node);

            // Subscribe to completion
            node.OnComplete = OnNodeComplete;

            // Execute
            node.Execute();
        }

        /// <summary>
        /// Pause execution
        /// </summary>
        public void Pause()
        {
            if (_isRunning && !_isPaused)
            {
                _isPaused = true;
                OnPaused?.Invoke(this);
                Debug.Log("[NodeGraphRunner] Paused");
            }
        }

        /// <summary>
        /// Resume execution
        /// </summary>
        public void Resume()
        {
            if (_isPaused)
            {
                _isPaused = false;
                _stepMode = false;
                OnResumed?.Invoke(this);
                Debug.Log("[NodeGraphRunner] Resumed");
                
                // If there's a pending next node, execute it
                if (_pendingNextNode != null)
                {
                    var nextNode = _pendingNextNode;
                    _pendingNextNode = null;
                    ExecuteNodeInternal(nextNode);
                }
            }
        }

        /// <summary>
        /// Step to next node
        /// </summary>
        public void Step()
        {
            if (_isPaused)
            {
                _stepMode = true;
                _isPaused = false;
                OnResumed?.Invoke(this);
                Debug.Log("[NodeGraphRunner] Stepping");
                
                // If there's a pending next node, execute it
                if (_pendingNextNode != null)
                {
                    var nextNode = _pendingNextNode;
                    _pendingNextNode = null;
                    ExecuteNodeInternal(nextNode);
                }
            }
        }

        public bool IsPaused => _isPaused;

        /// <summary>
        /// Called when a node completes
        /// </summary>
        private void OnNodeComplete(NodeData completedNode)
        {
            if (!_isRunning) return;

            if (_debugMode)
                Debug.Log($"[NodeGraphRunner] Completed: {completedNode.Name}");

            // Fire event
            OnNodeCompleted?.Invoke(this, completedNode);

            // Handle special node types with multiple outputs
            string outputPort = "output";
            
            if (completedNode is Nodes.ConditionalNode)
            {
                // ConditionalNode uses State to indicate branch:
                // Completed = true branch, Failed = false branch
                outputPort = completedNode.State == NodeState.Completed ? "true" : "false";
                
                // Reset state for next execution
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.ParallelNode)
            {
                // ParallelNode uses "done" port after all parallel nodes complete
                outputPort = "done";
            }
            else if (completedNode is Nodes.LoopNode)
            {
                // LoopNode uses "done" port when loop completes
                outputPort = "done";
            }
            else if (completedNode is Nodes.SequenceNode)
            {
                // SequenceNode uses "done" port when all sequence steps complete
                outputPort = "done";
            }
            else if (completedNode is Nodes.SubGraphNode)
            {
                // SubGraphNode uses "complete" port when sub-graph finishes
                outputPort = "complete";
            }

            // Get connected nodes from the appropriate output port
            var nextNodes = _graph.GetConnectedNodes(completedNode.Guid, outputPort);

            if (nextNodes.Count == 0)
            {
                // No more nodes - graph complete
                _isRunning = false;
                _currentNode = null;
                _pendingNextNode = null;
                
                if (ActiveRunner == this)
                    ActiveRunner = null;

                // Fire event
                OnGraphEnded?.Invoke(this);
                
                if (_debugMode)
                    Debug.Log("[NodeGraphRunner] Graph execution complete");
                return;
            }

            // If in step mode, pause here and store the next node
            if (_stepMode)
            {
                _pendingNextNode = nextNodes[0];
                _isPaused = true;
                _stepMode = false; // Reset step mode - user needs to click Step again
                OnPaused?.Invoke(this);
                Debug.Log("[NodeGraphRunner] Paused after step (click Step to continue)");
                return; // Don't continue to next node yet
            }

            // If paused (but not step mode), wait for resume before executing next node
            if (_isPaused)
            {
                _pendingNextNode = nextNodes[0];
                return;
            }

            // Execute next node(s)
            // For simplicity, execute first connected node
            // (For parallel execution, you'd execute all)
            // Use ExecuteNode (not ExecuteNodeInternal) to check for breakpoints
            ExecuteNode(nextNodes[0]);
        }

        /// <summary>
        /// Set graph at runtime (for sub-graphs)
        /// </summary>
        public void SetGraph(NodeGraph graph)
        {
            _graph = graph;
        }

        /// <summary>
        /// Helper to start coroutines from nodes
        /// </summary>
        public new Coroutine StartCoroutine(IEnumerator routine)
        {
            return base.StartCoroutine(routine);
        }
    }
}


