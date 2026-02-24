using System;
using System.Collections.Generic;
using UnityEngine;
using NodeSystem;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Saves listed graph variables to PlayerPrefs (key: graphName_variableName). Use with Load Progress to persist across sessions.
    /// </summary>
    [Serializable]
    public class SaveProgressNode : NodeData
    {
        [SerializeField]
        [Tooltip("Variable names to save (comma-separated or one per line)")]
        public string variableNames = "score,level";

        public override string Name => "Save Progress";
        public override Color Color => new Color(0.35f, 0.6f, 0.4f);
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
            return new List<PortData>
            {
                new PortData("output", "Next", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (Runner?.Graph == null)
            {
                Complete();
                return;
            }

            string prefix = Runner.Graph.graphName + "_";
            string[] names = variableNames.Split(new[] { ',', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string name in names)
            {
                var v = Runner.Graph.GetVariable(name.Trim());
                if (v != null)
                    PlayerPrefs.SetString(prefix + name.Trim(), v.Value);
            }
            PlayerPrefs.Save();
            Complete();
        }
    }
}
