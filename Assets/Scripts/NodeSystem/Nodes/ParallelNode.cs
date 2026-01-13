using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Executes all connected nodes simultaneously and waits for ALL to complete
    /// before proceeding to the next node.
    /// </summary>
    [Serializable]
    public class ParallelNode : NodeData
    {
        [NonSerialized]
        private int _pendingCount;
        
        [NonSerialized]
        private int _completedCount;
        
        [NonSerialized]
        private List<NodeData> _parallelNodes;
        
        [NonSerialized]
        private bool _allCompleted;

        public override string Name => "Parallel";
        public override Color Color => new Color(0.2f, 0.6f, 0.8f); // Blue
        public override string Category => "Flow";

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
                new PortData("parallel", "Parallel (Multi)", PortDirection.Output, PortCapacity.Multi),
                new PortData("done", "All Done", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (Runner == null)
            {
                Debug.LogError("[ParallelNode] No runner assigned!");
                Complete();
                return;
            }

            // Get all nodes connected to the "parallel" output
            var graph = Runner.Graph;
            _parallelNodes = graph.GetConnectedNodes(Guid, "parallel");

            if (_parallelNodes.Count == 0)
            {
                Debug.LogWarning("[ParallelNode] No parallel nodes connected! Connect nodes to 'Parallel (Multi)' port.");
                Complete();
                return;
            }

            _pendingCount = _parallelNodes.Count;
            _completedCount = 0;
            _allCompleted = false;

            Debug.Log($"[ParallelNode] ====== Starting {_pendingCount} parallel nodes ======");
            foreach (var node in _parallelNodes)
            {
                Debug.Log($"[ParallelNode]   - {node.Name} ({node.GetType().Name})");
            }

            // Execute all parallel nodes simultaneously
            foreach (var node in _parallelNodes)
            {
                node.Runner = Runner;
                node.OnComplete = OnParallelNodeComplete;
                
                // Broadcast that this node is starting (for visual feedback)
                NodeGraphRunner.BroadcastNodeStarted(Runner, node);
                
                node.Execute();
            }
        }

        private void OnParallelNodeComplete(NodeData completedNode)
        {
            _completedCount++;
            Debug.Log($"[ParallelNode] Node completed: {completedNode.Name} ({_completedCount}/{_pendingCount})");

            // Broadcast that this node completed (for visual feedback)
            NodeGraphRunner.BroadcastNodeCompleted(Runner, completedNode);

            // Check if all parallel nodes are done
            if (_completedCount >= _pendingCount && !_allCompleted)
            {
                _allCompleted = true;
                Debug.Log("[ParallelNode] ====== All parallel nodes completed! ======");
                
                // Signal completion - runner will use "done" port
                Complete();
            }
        }

        public override void Reset()
        {
            base.Reset();
            _pendingCount = 0;
            _completedCount = 0;
            _parallelNodes = null;
        }
    }
}

