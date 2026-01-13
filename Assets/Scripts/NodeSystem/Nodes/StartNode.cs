using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Entry point node - starts graph execution
    /// </summary>
    [Serializable]
    public class StartNode : NodeData
    {
        public override string Name => "Start";
        public override Color Color => new Color(0.2f, 0.8f, 0.3f); // Green
        public override string Category => "Flow";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>(); // No inputs
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "Next", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            Debug.Log("[StartNode] Graph started");
            Complete();
        }
    }
}


