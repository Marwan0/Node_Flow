using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Node that references and executes another NodeGraph (sub-graph)
    /// </summary>
    [Serializable]
    public class SubGraphNode : NodeData
    {
        [SerializeField]
        public NodeGraph subGraph;

        [SerializeField]
        public string inputPortMapping = ""; // JSON mapping of input ports

        [SerializeField]
        public string outputPortMapping = ""; // JSON mapping of output ports

        [NonSerialized]
        private NodeGraph _originalGraph;
        
        [NonSerialized]
        private bool _subGraphCompleted;

        public override string Name => subGraph != null ? $"Sub: {subGraph.name}" : "Sub Graph";
        public override Color Color => new Color(0.8f, 0.6f, 0.4f); // Brown/Orange
        public override string Category => "Flow";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("execute", "Execute", PortDirection.Input)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("complete", "Complete", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (subGraph == null)
            {
                Debug.LogError("[SubGraphNode] No sub-graph assigned!");
                Complete();
                return;
            }

            if (Runner == null)
            {
                Debug.LogError("[SubGraphNode] No runner assigned!");
                Complete();
                return;
            }

            Debug.Log($"[SubGraphNode] Executing sub-graph: {subGraph.name}");
            
            _subGraphCompleted = false;
            Runner.StartCoroutine(ExecuteSubGraph());
        }

        private IEnumerator ExecuteSubGraph()
        {
            // Store original graph to restore later
            _originalGraph = Runner.Graph;
            
            // Reset and initialize sub-graph nodes
            subGraph.ResetAllNodes();
            foreach (var node in subGraph.Nodes)
            {
                node.Runner = Runner;
            }
            
            // Get entry node of sub-graph
            var entry = subGraph.GetEntryNode();
            if (entry == null)
            {
                Debug.LogError("[SubGraphNode] Sub-graph has no entry node!");
                Complete();
                yield break;
            }
            
            // Subscribe to graph end event
            NodeGraphRunner.OnGraphEnded += OnSubGraphEnded;
            
            // Temporarily switch to sub-graph
            Runner.SetGraph(subGraph);
            
            // Execute sub-graph entry node
            entry.OnComplete = OnSubGraphNodeComplete;
            entry.Execute();
            
            // Fire start event for sub-graph
            NodeGraphRunner.BroadcastNodeStarted(Runner, entry);
            
            // Wait for sub-graph to complete
            while (!_subGraphCompleted && Runner.IsRunning)
            {
                yield return null;
            }
            
            // Unsubscribe
            NodeGraphRunner.OnGraphEnded -= OnSubGraphEnded;
            
            // CRITICAL: Restore original graph BEFORE calling Complete()
            // This ensures the runner's OnNodeComplete can find connections in the correct graph
            if (_originalGraph != null)
            {
                Runner.SetGraph(_originalGraph);
                Debug.Log($"[SubGraphNode] Restored original graph: {_originalGraph.name}");
            }
            
            Debug.Log($"[SubGraphNode] Sub-graph complete: {subGraph.name}");
            
            // Now complete - runner will check for next nodes in the restored graph
            Complete();
        }

        private void OnSubGraphNodeComplete(NodeData completedNode)
        {
            if (!Runner.IsRunning) return;
            
            // Fire completion event
            NodeGraphRunner.BroadcastNodeCompleted(Runner, completedNode);
            
            // Get next nodes in sub-graph
            string outputPort = GetOutputPortForNode(completedNode);
            var nextNodes = subGraph.GetConnectedNodes(completedNode.Guid, outputPort);
            
            if (nextNodes.Count == 0)
            {
                // No more nodes - sub-graph complete
                _subGraphCompleted = true;
                return;
            }
            
            // Execute next node in sub-graph
            var nextNode = nextNodes[0];
            nextNode.OnComplete = OnSubGraphNodeComplete;
            NodeGraphRunner.BroadcastNodeStarted(Runner, nextNode);
            nextNode.Execute();
        }

        private string GetOutputPortForNode(NodeData node)
        {
            // Handle special node types
            if (node is ConditionalNode)
                return node.State == NodeState.Completed ? "true" : "false";
            if (node is LoopNode)
                return "done";
            return "output";
        }

        private void OnSubGraphEnded(NodeGraphRunner runner)
        {
            if (runner == Runner && runner.Graph == subGraph)
            {
                _subGraphCompleted = true;
            }
        }

        public override void Reset()
        {
            base.Reset();
            _subGraphCompleted = false;
            _originalGraph = null;
        }
    }
}

