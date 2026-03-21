using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        // Scene-specific events (allows referencing Scene Objects)
        [SerializeField]
        private List<NodeUnityEvent> _sceneEvents = new List<NodeUnityEvent>();

        private NodeData _currentNode;
        private bool _isRunning;
        private List<string> _executionPath = new List<string>();
        private NodeData _pendingNextNode; // For step mode
        private HashSet<string> _activeNodeGuids = new HashSet<string>(); // Track nodes currently executing
        
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


#if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-cleanup orphaned events in editor when component is loaded or modified
            if (!Application.isPlaying && _graph != null)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null) // Check if still valid after delay
                    {
                        CleanupSceneEvents();
                    }
                };
            }
        }
#endif

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
            _activeNodeGuids.Clear();

            // Re-resolve scene references (they become null after scene reload
            // because the old GameObjects were destroyed)
            _graph.ResolveSceneReferencesAtRuntime();

            // Initialize all nodes with runner reference
            foreach (var node in _graph.Nodes)
            {
                node.Runner = this;

                // Reset step counters for SequenceStepNodes on graph start
                if (node is Nodes.SequenceStepNode stepNode)
                    stepNode.ResetStepCounter();
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
            _activeNodeGuids.Clear();
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
        public void ExecuteNode(NodeData node)
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

            // WaitForAllNode is a synchronization point: multiple branches may try
            // to execute it. Once it's running (waiting) or already completed,
            // subsequent calls must be ignored — the node handles the rest via events.
            if (node is Nodes.WaitForAllNode && (node.State == NodeState.Running || node.State == NodeState.Completed))
                return;

            // Reset node state if it was previously completed (allows re-execution in parallel scenarios)
            if (node.State == NodeState.Completed || node.State == NodeState.Failed)
            {
                node.Reset();
            }

            // Track this node as active (for parallel execution tracking)
            _activeNodeGuids.Add(node.Guid);

            // Don't overwrite _currentNode if multiple nodes are executing in parallel
            // Only set it if it's null or if we're executing sequentially
            if (_currentNode == null)
            {
                _currentNode = node;
            }
            
            _executionPath.Add(node.Guid);

            if (_debugMode)
            {
                Debug.Log($"[NodeGraphRunner] Executing: {node.Name} (State before: {node.State}, Active nodes: {_activeNodeGuids.Count})");
            }

            // Fire event
            OnNodeStarted?.Invoke(this, node);

            // Subscribe to completion
            node.OnComplete = OnNodeComplete;

            // Execute
            node.Execute();
            
            if (_debugMode)
            {
                Debug.Log($"[NodeGraphRunner] After Execute call: {node.Name} (State after: {node.State})");
            }
        }

        /// <summary>
        /// Broadcast a signal by id. All ReceiveSignalNode instances in the graph with matching signalId will execute (wireless / antenna-style).
        /// </summary>
        public void BroadcastSignal(string signalId)
        {
            if (!_isRunning || _graph == null || string.IsNullOrEmpty(signalId)) return;
            foreach (var node in _graph.Nodes)
            {
                if (node is Nodes.ReceiveSignalNode recv && recv.signalId == signalId)
                    ExecuteNode(node);
            }
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
            }
        }

        // === Unity Event Support ===

        public UnityEngine.Events.UnityEvent GetUnityEvent(string nodeGuid)
        {
            var entry = _sceneEvents.FirstOrDefault(e => e.nodeGuid == nodeGuid);
            if (entry == null)
            {
                entry = new NodeUnityEvent { nodeGuid = nodeGuid, onEvent = new UnityEngine.Events.UnityEvent() };
                _sceneEvents.Add(entry);
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
            return entry.onEvent;
        }

        public void InvokeUnityEvent(string nodeGuid)
        {
            // 1. Try Scene Event first (Scene overrides Asset)
            var sceneEntry = _sceneEvents.FirstOrDefault(e => e.nodeGuid == nodeGuid);
            if (sceneEntry != null && sceneEntry.onEvent != null && sceneEntry.onEvent.GetPersistentEventCount() > 0)
            {
                sceneEntry.onEvent.Invoke();
                return;
            }

            // 2. Fallback to Graph Asset Event
            if (_graph != null)
            {
                _graph.InvokeUnityEvent(nodeGuid);
            }
        }

        /// <summary>
        /// Clean up scene events for nodes that no longer exist in the graph
        /// </summary>
        public void CleanupSceneEvents()
        {
            if (_graph == null) return;
            
            var validGuids = new HashSet<string>(_graph.Nodes.Select(n => n.Guid));
            int removed = _sceneEvents.RemoveAll(e => !validGuids.Contains(e.nodeGuid));
            
            if (removed > 0)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
                Debug.Log($"[NodeGraphRunner] Cleaned up {removed} orphaned scene events");
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
            else if (completedNode is Nodes.CompareVariableNode)
            {
                outputPort = completedNode.State == NodeState.Completed ? "true" : "false";
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.CooldownNode)
            {
                // Completed = forwarded (Next), Failed = throttled
                outputPort = completedNode.State == NodeState.Completed ? "output" : "throttled";
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.RetryNode)
            {
                // Failed = exhausted (max retries), Completed = jumped (no output port)
                outputPort = completedNode.State == NodeState.Failed ? "exhausted" : "output";
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.Quiz.StreakNode)
            {
                outputPort = completedNode.State == NodeState.Completed ? "above" : "below";
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.GateNode)
            {
                outputPort = completedNode.State == NodeState.Completed ? "through" : "blocked";
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.LoopNode)
            {
                // LoopNode uses "done" port when loop completes
                outputPort = "done";
            }
            else if (completedNode is Nodes.SubGraphNode)
            {
                // SubGraphNode uses "complete" port when sub-graph finishes
                outputPort = "complete";
            }
            // === Quiz Nodes with Branching ===
            else if (completedNode is Nodes.Quiz.QuizBranchNode)
            {
                // QuizBranchNode: Completed = true, Failed = false
                outputPort = completedNode.State == NodeState.Completed ? "true" : "false";
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.Quiz.ScoreNode scoreNode && scoreNode.branchOnThreshold)
            {
                // ScoreNode with branching: Completed = above, Failed = below
                outputPort = completedNode.State == NodeState.Completed ? "above" : "below";
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.Quiz.QuizProgressNode progressNode && progressNode.branchOnThreshold)
            {
                // QuizProgressNode with branching: Completed = above, Failed = below
                outputPort = completedNode.State == NodeState.Completed ? "above" : "below";
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.Quiz.QuizTimerNode timerNode && 
                     (timerNode.action == Nodes.Quiz.TimerAction.WaitForExpiry || timerNode.branchOnExpiry))
            {
                // QuizTimerNode: Completed = expired, Failed = still running
                outputPort = completedNode.State == NodeState.Completed ? "expired" : "running";
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.Quiz.EndQuizNode endNode && endNode.branchOnPerformance)
            {
                // EndQuizNode with performance branching: Completed = passed, Failed = failed
                outputPort = completedNode.State == NodeState.Completed ? "passed" : "failed";
                completedNode.State = NodeState.Completed;
            }
            else if (completedNode is Nodes.Quiz.LoadQuestionNode)
            {
                // Question nodes: Fire correct/incorrect based on answer, AND fire complete
                string resultPort = completedNode.State == NodeState.Failed ? "incorrect" : "correct";
                completedNode.State = NodeState.Completed;
                
                // First, execute nodes connected to the result port (correct/incorrect)
                var resultNodes = _graph.GetConnectedNodes(completedNode.Guid, resultPort);
                
                // Also get the "complete" port nodes so we can pre-register all of them
                outputPort = "complete";
                var completeNodes = _graph.GetConnectedNodes(completedNode.Guid, outputPort);
                
                // Pre-register ALL nodes (result + complete) before executing any,
                // to prevent synchronous nodes from prematurely stopping the graph.
                foreach (var nextNode in resultNodes)
                    _activeNodeGuids.Add(nextNode.Guid);
                foreach (var nextNode in completeNodes)
                    _activeNodeGuids.Add(nextNode.Guid);
                
                foreach (var nextNode in resultNodes)
                {
                    ExecuteNode(nextNode);
                }
                
                // outputPort is already set to "complete" for the main loop below
            }
            // Remove this node from active tracking
            _activeNodeGuids.Remove(completedNode.Guid);

            // Clear callback to prevent memory leaks
            completedNode.OnComplete = null;

            // RandomBranchNode: execute only the randomly selected node, not all connected nodes
            if (completedNode is Nodes.RandomBranchNode randomBranch && !string.IsNullOrEmpty(randomBranch.SelectedNodeGuid))
            {
                var selectedNode = _graph.GetNode(randomBranch.SelectedNodeGuid);
                if (selectedNode != null)
                {
                    ExecuteNode(selectedNode);
                }
                else
                {
                    Debug.LogWarning($"[NodeGraphRunner] RandomBranch selected node not found: {randomBranch.SelectedNodeGuid}");
                }
                return;
            }

            // SequenceStepNode: route to the selected step port
            if (completedNode is Nodes.SequenceStepNode sequenceStep && !string.IsNullOrEmpty(sequenceStep.SelectedPortId))
            {
                outputPort = sequenceStep.SelectedPortId;
            }

            // AttemptBranchNode: route to the single selected attempt port
            if (completedNode is Nodes.Quiz.AttemptBranchNode attemptBranch && !string.IsNullOrEmpty(attemptBranch.SelectedPortId))
            {
                outputPort = attemptBranch.SelectedPortId;
            }

            // PointRouterNode: route to the single selected point port
            if (completedNode is Nodes.Quiz.PointRouterNode pointRouter && !string.IsNullOrEmpty(pointRouter.SelectedPortId))
            {
                outputPort = pointRouter.SelectedPortId;
            }

            // Get connected nodes from the appropriate output port
            var nextNodes = _graph.GetConnectedNodes(completedNode.Guid, outputPort);

            if (nextNodes.Count == 0)
            {
                // This branch has no more nodes
                // Only stop the graph if NO nodes are currently executing
                if (_activeNodeGuids.Count == 0)
                {
                    // All branches have completed - graph complete
                    _isRunning = false;
                    _currentNode = null;
                    _pendingNextNode = null;
                    
                    if (ActiveRunner == this)
                        ActiveRunner = null;

                    // Fire event
                    OnGraphEnded?.Invoke(this);
                    
                    Debug.Log($"[NodeGraphRunner] Graph execution complete (all branches finished)");
                }
                else
                {
                    // Other branches are still running, just this branch ended
                    Debug.Log($"[NodeGraphRunner] Branch ended at {completedNode.Name}, but {_activeNodeGuids.Count} other nodes are still running");
                }
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
            // If there are multiple connections, execute ALL of them in parallel
            // All connected nodes will execute simultaneously when the current node completes
            // This enables parallel execution through one-to-many connections
            if (nextNodes.Count > 1)
            {
                if (_debugMode)
                {
                    Debug.Log($"[NodeGraphRunner] Executing {nextNodes.Count} nodes in parallel from {completedNode.Name}: {string.Join(", ", nextNodes.Select(n => n.Name))}");
                }
                
                // IMPORTANT: Pre-register ALL next nodes as active BEFORE executing any.
                // This prevents a synchronous node from completing and seeing an empty
                // _activeNodeGuids set, which would incorrectly stop the graph before
                // the remaining sibling nodes get a chance to execute.
                foreach (var nextNode in nextNodes)
                {
                    _activeNodeGuids.Add(nextNode.Guid);
                }
            }
            
            foreach (var nextNode in nextNodes)
            {
                if (_debugMode)
                {
                    Debug.Log($"[NodeGraphRunner] Executing node: {nextNode.Name} (Current State: {nextNode.State})");
                }
                ExecuteNode(nextNode);
            }
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


