using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Waits for all input connections to complete before proceeding
    /// </summary>
    [Serializable]
    public class WaitForAllNode : NodeData
    {
        [NonSerialized]
        private HashSet<string> _pendingInputs = new HashSet<string>();
        
        [NonSerialized]
        private HashSet<string> _completedInputs = new HashSet<string>();

        public override string Name => "Wait For All";
        public override Color Color => new Color(0.3f, 0.7f, 0.5f); // Green
        public override string Category => "Flow";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input1", "Input 1", PortDirection.Input),
                new PortData("input2", "Input 2", PortDirection.Input),
                new PortData("input3", "Input 3", PortDirection.Input),
                new PortData("input4", "Input 4", PortDirection.Input)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "All Complete", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogError("[WaitForAllNode] No runner assigned!");
                Complete();
                return;
            }

            // Find all nodes connected to our inputs
            _pendingInputs.Clear();
            _completedInputs.Clear();

            var inputPorts = GetInputPorts();
            foreach (var port in inputPorts)
            {
                var connectedNodes = Runner.Graph.GetConnectedNodes(Guid, port.id);
                if (connectedNodes.Count > 0)
                {
                    _pendingInputs.Add(port.id);
                }
            }

            if (_pendingInputs.Count == 0)
            {
                Debug.Log("[WaitForAllNode] No inputs connected, completing immediately");
                Complete();
                return;
            }

            Debug.Log($"[WaitForAllNode] Waiting for {_pendingInputs.Count} inputs to complete");
            
            // Start monitoring
            Runner.StartCoroutine(WaitForAllInputs());
        }

        private IEnumerator WaitForAllInputs()
        {
            // Wait until all inputs are complete
            while (_completedInputs.Count < _pendingInputs.Count && Runner.IsRunning)
            {
                yield return null;
            }

            if (Runner.IsRunning)
            {
                Debug.Log("[WaitForAllNode] All inputs completed!");
                Complete();
            }
        }

        /// <summary>
        /// Called when an input completes (via connection tracking)
        /// This is a simplified version - in a full implementation, you'd track node completions
        /// </summary>
        public void OnInputComplete(string portId)
        {
            if (_pendingInputs.Contains(portId))
            {
                _completedInputs.Add(portId);
                Debug.Log($"[WaitForAllNode] Input {portId} completed ({_completedInputs.Count}/{_pendingInputs.Count})");
            }
        }

        public override void Reset()
        {
            base.Reset();
            _pendingInputs.Clear();
            _completedInputs.Clear();
        }
    }
}

