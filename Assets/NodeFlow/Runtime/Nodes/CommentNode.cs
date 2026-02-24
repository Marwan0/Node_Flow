using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Comment/sticky note node for documentation
    /// </summary>
    [Serializable]
    public class CommentNode : NodeData
    {
        [SerializeField]
        public string comment = "Add your comment here...";
        
        [SerializeField]
        public Color commentColor = new Color(1f, 1f, 0.4f, 0.9f); // Yellow

        public override string Name => "Comment";
        public override Color Color => commentColor;
        public override string Category => "Documentation";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>(); // No ports
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>(); // No ports
        }

        protected override void OnExecute()
        {
            // Comment nodes don't execute
            Complete();
        }
    }
}

