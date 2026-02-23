using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Sends a named signal (like an antenna). All Receive Signal nodes with the same
    /// Signal Id will run when this executes. This node has no output — flow continues only from the receiver(s).
    /// </summary>
    [Serializable]
    public class SendSignalNode : NodeData
    {
        [SerializeField]
        [Tooltip("Channel name. Receive Signal nodes with the same Id will run when this sends.")]
        public string signalId = "signal";

        public override string Name => "Send Signal";
        public override Color Color => new Color(0.6f, 0.4f, 0.9f); // Purple
        public override string Category => "Flow";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input,PortCapacity.Multi)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>(); // No output — antenna only sends; flow continues from Receive Signal(s)
        }

        protected override void OnExecute()
        {
            if (Runner != null && !string.IsNullOrEmpty(signalId))
                Runner.BroadcastSignal(signalId);
            Complete();
        }
    }
}
