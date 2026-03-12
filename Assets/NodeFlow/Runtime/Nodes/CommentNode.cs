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
        public string commentTitle = "Note";

        [SerializeField]
        public Vector2 commentSize = new Vector2(200, 160);

        [SerializeField]
        public int theme = 0; // Maps to StickyNoteTheme enum (0 = Classic)

        [SerializeField]
        public int fontSize = 0; // Maps to StickyNoteFontSize enum (0 = Small)

        [SerializeField]
        public Color commentColor = new Color(1f, 1f, 0.4f, 0.9f); // Legacy, kept for compat

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

