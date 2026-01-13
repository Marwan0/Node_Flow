using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Terminal node - ends graph execution
    /// </summary>
    [Serializable]
    public class EndNode : NodeData
    {
        [SerializeField]
        public string message = "Flow completed";

        public override string Name => "End";
        public override Color Color => new Color(0.8f, 0.2f, 0.2f); // Red
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
            return new List<PortData>(); // No outputs
        }

        protected override void OnExecute()
        {
            if (!string.IsNullOrEmpty(message))
            {
                Debug.Log($"[EndNode] {message}");
            }
            Complete();
        }
    }
}


