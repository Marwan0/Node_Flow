using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Listens for a named signal (wireless receiver). Has no input port — it runs when
    /// a Send Signal node with the same Signal Id broadcasts. Flow continues from Next output.
    /// Can be triggered many times by multiple senders.
    /// </summary>
    [Serializable]
    public class ReceiveSignalNode : NodeData
    {
        [SerializeField]
        [Tooltip("Channel name. Must match the Send Signal node(s) that should trigger this.")]
        public string signalId = "signal";

        public override string Name => "Receive Signal";
        public override Color Color => new Color(0.4f, 0.6f, 0.9f); // Blue
        public override string Category => "Flow";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>(); // No inputs — triggered only by BroadcastSignal
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
