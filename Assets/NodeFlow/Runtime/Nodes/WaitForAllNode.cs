using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Synchronization node: waits for ALL upstream nodes connected to its
    /// single multi-capacity input port to complete before firing output.
    /// 
    /// Use this to rejoin parallel branches — connect every branch's last node
    /// into the "Inputs" port, and the output fires only when all of them are done.
    /// </summary>
    [Serializable]
    public class WaitForAllNode : NodeData
    {
        [NonSerialized]
        private HashSet<string> _pendingGuids = new HashSet<string>();

        [NonSerialized]
        private HashSet<string> _completedGuids = new HashSet<string>();

        [NonSerialized]
        private bool _isListening;

        public override string Name => "Wait For All";
        public override Color Color => new Color(0.3f, 0.7f, 0.5f);
        public override string Category => "Flow";
        public override string Description => "A synchronization node. It waits until *all* upstream nodes connected directly to its input have fired before it proceeds. Essential for rejoining parallel execution paths.";

        // --- Ports ---

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                // Single port that accepts unlimited connections
                new PortData("input", "Inputs", PortDirection.Input, PortCapacity.Multi)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "All Complete", PortDirection.Output)
            };
        }

        // --- Execution ---

        protected override void OnExecute()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogError("[WaitForAllNode] No runner assigned!");
                Complete();
                return;
            }

            // Discover every node whose output connects to our "input" port
            _pendingGuids.Clear();
            _completedGuids.Clear();

            foreach (var conn in Runner.Graph.Connections)
            {
                if (conn.inputNodeGuid == Guid && conn.inputPortId == "input")
                {
                    _pendingGuids.Add(conn.outputNodeGuid);
                }
            }

            if (_pendingGuids.Count == 0)
            {
                Debug.Log("[WaitForAllNode] No inputs connected, completing immediately.");
                Complete();
                return;
            }

            // Check if any are already completed (they may have finished before we started)
            foreach (var guid in _pendingGuids.ToList())
            {
                if (Runner.ExecutionPath.Contains(guid))
                {
                    var node = Runner.Graph.GetNode(guid);
                    if (node != null && node.State == NodeState.Completed)
                    {
                        _completedGuids.Add(guid);
                    }
                }
            }

            if (_completedGuids.Count >= _pendingGuids.Count)
            {
                Debug.Log("[WaitForAllNode] All inputs already completed!");
                Complete();
                return;
            }

            Debug.Log($"[WaitForAllNode] Waiting for {_pendingGuids.Count - _completedGuids.Count} " +
                      $"of {_pendingGuids.Count} inputs to complete.");

            // Subscribe to future completions
            StartListening();
        }

        // --- Event-driven completion tracking ---

        private void StartListening()
        {
            if (_isListening) return;
            _isListening = true;
            NodeGraphRunner.OnNodeCompleted += OnUpstreamNodeCompleted;
        }

        private void StopListening()
        {
            if (!_isListening) return;
            _isListening = false;
            NodeGraphRunner.OnNodeCompleted -= OnUpstreamNodeCompleted;
        }

        private void OnUpstreamNodeCompleted(NodeGraphRunner runner, NodeData completedNode)
        {
            // Ignore events from a different runner
            if (runner != Runner) return;

            if (_pendingGuids.Contains(completedNode.Guid))
            {
                _completedGuids.Add(completedNode.Guid);

                Debug.Log($"[WaitForAllNode] Input '{completedNode.Name}' completed " +
                          $"({_completedGuids.Count}/{_pendingGuids.Count})");

                if (_completedGuids.Count >= _pendingGuids.Count)
                {
                    StopListening();
                    Debug.Log("[WaitForAllNode] All inputs completed!");
                    Complete();
                }
            }
        }

        // --- Cleanup ---

        public override void Reset()
        {
            base.Reset();
            StopListening();
            _pendingGuids.Clear();
            _completedGuids.Clear();
        }
    }
}
