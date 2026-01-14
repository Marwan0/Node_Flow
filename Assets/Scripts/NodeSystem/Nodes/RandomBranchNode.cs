using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Randomly selects one of multiple output paths and waits for it to complete
    /// </summary>
    [Serializable]
    public class RandomBranchNode : NodeData
    {
        [SerializeField]
        public int outputCount = 2;
        
        [SerializeField]
        public bool waitForBranch = true; // If true, waits for branch to complete before completing

        [NonSerialized]
        private bool _branchCompleted;
        
        [NonSerialized]
        private string _selectedPort;

        public override string Name => "Random Branch";
        public override Color Color => new Color(0.8f, 0.5f, 0.2f); // Orange
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
            var ports = new List<PortData>();
            for (int i = 0; i < outputCount; i++)
            {
                ports.Add(new PortData($"output{i}", $"Option {i + 1}", PortDirection.Output));
            }
            return ports;
        }

        protected override void OnExecute()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogError("[RandomBranchNode] No runner assigned!");
                Complete();
                return;
            }

            // Pick random output
            int randomIndex = UnityEngine.Random.Range(0, outputCount);
            _selectedPort = $"output{randomIndex}";
            _branchCompleted = false;

            Debug.Log($"[RandomBranchNode] Selected random branch: Option {randomIndex + 1}");

            // Get connected nodes for selected port
            var nextNodes = Runner.Graph.GetConnectedNodes(Guid, _selectedPort);
            if (nextNodes.Count > 0)
            {
                if (waitForBranch)
                {
                    // Wait for branch to complete (needed for Parallel)
                    Runner.StartCoroutine(ExecuteBranchAndWait(nextNodes[0]));
                }
                else
                {
                    // Fire and forget - let runner handle continuation
                    nextNodes[0].Runner = Runner;
                    nextNodes[0].Execute();
                    Complete();
                }
            }
            else
            {
                Debug.Log($"[RandomBranchNode] No nodes connected to {_selectedPort}");
                Complete();
            }
        }

        private IEnumerator ExecuteBranchAndWait(NodeData branchNode)
        {
            // Execute the branch node and wait for its entire chain to complete
            branchNode.Runner = Runner;
            branchNode.OnComplete = OnBranchNodeComplete;
            
            // Broadcast start for visual feedback
            NodeGraphRunner.BroadcastNodeStarted(Runner, branchNode);
            
            branchNode.Execute();

            // Wait for branch chain to complete
            while (!_branchCompleted && Runner.IsRunning)
            {
                yield return null;
            }

            Debug.Log("[RandomBranchNode] Branch completed");
            Complete();
        }

        private void OnBranchNodeComplete(NodeData completedNode)
        {
            if (!Runner.IsRunning) return;
            
            // Broadcast completion for visual feedback
            NodeGraphRunner.BroadcastNodeCompleted(Runner, completedNode);
            
            // Get output port for this node type
            string outputPort = GetOutputPortForNode(completedNode);
            
            // Get next nodes in branch chain
            var nextNodes = Runner.Graph.GetConnectedNodes(completedNode.Guid, outputPort);
            
            if (nextNodes.Count == 0)
            {
                // Branch chain complete
                _branchCompleted = true;
                return;
            }
            
            // Continue executing branch chain
            var nextNode = nextNodes[0];
            nextNode.Runner = Runner;
            nextNode.OnComplete = OnBranchNodeComplete;
            
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

        public override void Reset()
        {
            base.Reset();
            _branchCompleted = false;
            _selectedPort = null;
        }
    }
}

