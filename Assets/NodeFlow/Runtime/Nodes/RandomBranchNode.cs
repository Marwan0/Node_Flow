using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Weight entry mapping a connected node GUID to a probability weight.
    /// </summary>
    [Serializable]
    public class BranchWeight
    {
        public string nodeGuid;
        public float weight = 1f;
    }

    /// <summary>
    /// Randomly selects ONE of the nodes connected to its multi-capacity output
    /// port using configurable weights. Connect as many branches as you want —
    /// each gets a weight that controls how likely it is to be chosen.
    /// </summary>
    [Serializable]
    public class RandomBranchNode : NodeData
    {
        [SerializeField]
        public List<BranchWeight> weights = new List<BranchWeight>();

        /// <summary>
        /// After execution, holds the GUID of the randomly chosen next node.
        /// The runner reads this to know which single node to execute.
        /// </summary>
        [NonSerialized]
        public string SelectedNodeGuid;

        public override string Name => "Random Branch";
        public override Color Color => new Color(0.8f, 0.5f, 0.2f); // Orange
        public override string Category => "Flow";
        public override string Description => "Randomly selects one of its active output ports based on configured probability weights.";

        // --- Ports ---

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
                new PortData("output", "Random Out", PortDirection.Output, PortCapacity.Multi)
            };
        }

        // --- Weight helpers ---

        /// <summary>
        /// Get the weight for a connected node (defaults to 1.0 if not set).
        /// </summary>
        public float GetWeight(string nodeGuid)
        {
            var entry = weights.FirstOrDefault(w => w.nodeGuid == nodeGuid);
            return entry?.weight ?? 1f;
        }

        /// <summary>
        /// Set the weight for a connected node. Creates the entry if it doesn't exist.
        /// </summary>
        public void SetWeight(string nodeGuid, float value)
        {
            var entry = weights.FirstOrDefault(w => w.nodeGuid == nodeGuid);
            if (entry != null)
            {
                entry.weight = Mathf.Max(0f, value);
            }
            else
            {
                weights.Add(new BranchWeight { nodeGuid = nodeGuid, weight = Mathf.Max(0f, value) });
            }
        }

        /// <summary>
        /// Remove weight entries for nodes that are no longer connected.
        /// </summary>
        public void CleanupWeights(IEnumerable<string> connectedGuids)
        {
            var connectedSet = new HashSet<string>(connectedGuids);
            weights.RemoveAll(w => !connectedSet.Contains(w.nodeGuid));
        }

        // --- Execution ---

        protected override void OnExecute()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogError("[RandomBranchNode] No runner assigned!");
                Complete();
                return;
            }

            var candidates = Runner.Graph.GetConnectedNodes(Guid, "output");

            if (candidates.Count == 0)
            {
                Debug.Log("[RandomBranchNode] No outputs connected, completing immediately.");
                SelectedNodeGuid = null;
                Complete();
                return;
            }

            // Build weighted list
            float totalWeight = 0f;
            var weightedCandidates = new List<(NodeData node, float weight)>();
            foreach (var candidate in candidates)
            {
                float w = GetWeight(candidate.Guid);
                weightedCandidates.Add((candidate, w));
                totalWeight += w;
            }

            // Weighted random pick
            if (totalWeight <= 0f)
            {
                // All weights zero — fall back to uniform random
                int pick = UnityEngine.Random.Range(0, candidates.Count);
                SelectedNodeGuid = candidates[pick].Guid;
            }
            else
            {
                float roll = UnityEngine.Random.Range(0f, totalWeight);
                float cumulative = 0f;
                SelectedNodeGuid = weightedCandidates.Last().node.Guid; // fallback

                foreach (var (node, w) in weightedCandidates)
                {
                    cumulative += w;
                    if (roll <= cumulative)
                    {
                        SelectedNodeGuid = node.Guid;
                        break;
                    }
                }
            }

            var selected = candidates.FirstOrDefault(c => c.Guid == SelectedNodeGuid);
            float selectedPct = totalWeight > 0 ? (GetWeight(SelectedNodeGuid) / totalWeight * 100f) : (100f / candidates.Count);
            Debug.Log($"[RandomBranchNode] Picked '{selected?.Name}' ({selectedPct:F0}% chance) from {candidates.Count} branches");

            Complete();
        }

        public override void Reset()
        {
            base.Reset();
            SelectedNodeGuid = null;
        }
    }
}
