using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Portal (junction) node: can be triggered by many nodes and more than once.
    /// Use a multi-capacity input so multiple nodes connect into it; each trigger
    /// passes through immediately to the output. No logic, no state.
    /// </summary>
    [Serializable]
    public class PortalNode : NodeData
    {
        public override string Name => "Portal";
        public override Color Color => new Color(0.2f, 0.7f, 0.7f); // Teal
        public override string Category => "Flow";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input, PortCapacity.Multi)
            };
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
            Complete();
        }
    }
}
