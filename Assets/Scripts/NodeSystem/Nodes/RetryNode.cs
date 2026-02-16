using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Jumps flow back to a target node (e.g. for "Try again"). Optionally limits retries and outputs "exhausted" when exceeded.
    /// </summary>
    [Serializable]
    public class RetryNode : NodeData
    {
        [SerializeField]
        [Tooltip("GUID of the node to jump to. Set in graph editor or via inline content that lists nodes.")]
        public string targetNodeGuid = "";

        [SerializeField]
        [Tooltip("Max retries before outputting Exhausted. 0 = unlimited.")]
        public int maxRetries = 0;

        [NonSerialized]
        private int _retryCount;

        public override string Name => "Retry";
        public override Color Color => new Color(0.75f, 0.5f, 0.25f);
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
            var list = new List<PortData>();
            if (maxRetries > 0)
                list.Add(new PortData("exhausted", "Exhausted", PortDirection.Output));
            return list;
        }

        protected override void OnExecute()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Complete();
                return;
            }

            if (maxRetries > 0 && _retryCount >= maxRetries)
            {
                State = NodeState.Failed;
                OnComplete?.Invoke(this);
                return;
            }

            _retryCount++;
            var target = Runner.Graph.GetNode(targetNodeGuid);
            if (target != null)
                Runner.ExecuteNode(target);
            else
                Debug.LogWarning($"[RetryNode] Target node not found: {targetNodeGuid}");
            Complete();
        }
    }
}
