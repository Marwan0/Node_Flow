using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Executes connected nodes one by one in sequence
    /// Dynamic ports: Each connection creates a new port below
    /// </summary>
    [Serializable]
    public class SequenceNode : NodeData
    {
        [SerializeField]
        private List<string> _sequencePorts = new List<string>(); // Port IDs: sequence0, sequence1, etc.

        [NonSerialized]
        private int _currentIndex = 0;

        public override string Name => "Sequence";
        public override Color Color => new Color(0.4f, 0.6f, 0.8f); // Blue
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
            
            // Only add default port if we truly have no ports (not during loading)
            // During loading, RestorePortsFromConnections() will populate _sequencePorts
            if (_sequencePorts.Count == 0)
            {
                // Check if we're in a graph context - if so, don't add default yet
                // The restore method will handle it
                _sequencePorts.Add("sequence0");
            }
            
            // Add sequence ports (ordered)
            for (int i = 0; i < _sequencePorts.Count; i++)
            {
                ports.Add(new PortData(_sequencePorts[i], $"Step {i + 1}", PortDirection.Output, PortCapacity.Single));
            }
            
            // Add "Add Step" port (always last) - this will create a new port when connected
            ports.Add(new PortData("addStep", "+ Add Step", PortDirection.Output, PortCapacity.Single));
            
            // Add "All Done" port
            ports.Add(new PortData("done", "All Done", PortDirection.Output));
            
            return ports;
        }

        /// <summary>
        /// Add a new sequence port (called when a connection is made to "addStep")
        /// </summary>
        public void AddSequencePort()
        {
            string newPortId = $"sequence{_sequencePorts.Count}";
            _sequencePorts.Add(newPortId);
        }

        /// <summary>
        /// Get the ordered list of sequence ports (excluding "addStep" and "done")
        /// </summary>
        public List<string> GetSequencePorts()
        {
            return new List<string>(_sequencePorts);
        }

        /// <summary>
        /// Restore sequence ports from connections (called after graph load)
        /// This ensures ports are restored even if _sequencePorts wasn't serialized properly
        /// </summary>
        public void RestorePortsFromConnections(NodeGraph graph)
        {
            if (graph == null) return;

            // Find all unique sequence port IDs from connections
            var usedPorts = new HashSet<string>();
            foreach (var conn in graph.Connections)
            {
                if (conn.outputNodeGuid == Guid && 
                    conn.outputPortId.StartsWith("sequence") && 
                    conn.outputPortId != "addStep" && 
                    conn.outputPortId != "done")
                {
                    usedPorts.Add(conn.outputPortId);
                }
            }

            // If we found ports in connections, restore them
            if (usedPorts.Count > 0)
            {
                // Sort ports by their index (sequence0, sequence1, sequence2, etc.)
                var sortedPorts = usedPorts.OrderBy(p =>
                {
                    if (int.TryParse(p.Replace("sequence", ""), out int index))
                        return index;
                    return int.MaxValue;
                }).ToList();

                _sequencePorts = sortedPorts;
                Debug.Log($"[SequenceNode] Restored {_sequencePorts.Count} sequence ports from connections: {string.Join(", ", _sequencePorts)}");
            }
        }

        protected override void OnExecute()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogError("[SequenceNode] No runner assigned!");
                Complete();
                return;
            }

            _currentIndex = 0;
            
            // Get nodes in port order (sequence0, sequence1, sequence2, etc.)
            var sequenceNodes = new List<NodeData>();
            foreach (var portId in _sequencePorts)
            {
                var nodes = Runner.Graph.GetConnectedNodes(Guid, portId);
                if (nodes.Count > 0)
                {
                    sequenceNodes.Add(nodes[0]); // Each port has single capacity
                }
            }

            if (sequenceNodes.Count == 0)
            {
                Debug.Log("[SequenceNode] No sequence nodes connected");
                Complete();
                return;
            }

            Debug.Log($"[SequenceNode] Starting sequence of {sequenceNodes.Count} nodes (in port order)");
            for (int i = 0; i < sequenceNodes.Count; i++)
            {
                Debug.Log($"  [{i + 1}] {sequenceNodes[i].Name} (port: {_sequencePorts[i]})");
            }
            
            Runner.StartCoroutine(ExecuteSequence(sequenceNodes));
        }

        private IEnumerator ExecuteSequence(List<NodeData> sequenceNodes)
        {
            foreach (var node in sequenceNodes)
            {
                _currentIndex++;
                Debug.Log($"[SequenceNode] Executing step {_currentIndex}/{sequenceNodes.Count}: {node.Name}");

                bool completed = false;
                
                // Fire visual start event
                NodeGraphRunner.BroadcastNodeStarted(Runner, node);
                
                // Set up completion handler that tracks completion AND fires visual event
                node.OnComplete = (n) =>
                {
                    completed = true;
                    // Fire visual completion event
                    NodeGraphRunner.BroadcastNodeCompleted(Runner, node);
                };
                
                node.Runner = Runner;
                node.Execute();

                // Wait for completion
                while (!completed && Runner.IsRunning)
                {
                    yield return null;
                }

                if (!Runner.IsRunning) break;
            }

            Debug.Log("[SequenceNode] Sequence complete");
            Complete();
        }

        public override void Reset()
        {
            base.Reset();
            _currentIndex = 0;
        }
    }
}

