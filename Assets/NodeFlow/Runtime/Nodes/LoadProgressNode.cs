using System;
using System.Collections.Generic;
using UnityEngine;
using NodeSystem;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Loads saved graph variables from PlayerPrefs (key: graphName_variableName). Creates variables if missing.
    /// </summary>
    [Serializable]
    public class LoadProgressNode : NodeData
    {
        [SerializeField]
        [Tooltip("Variable names to load (comma-separated). Variables are created as string if not present; use Set Variable or logic to coerce types if needed.")]
        public string variableNames = "score,level";

        public override string Name => "Load Progress";
        public override Color Color => new Color(0.4f, 0.5f, 0.65f);
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

            var graph = Runner.Graph;
            string prefix = graph.graphName + "_";
            string[] names = variableNames.Split(new[] { ',', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string name in names)
            {
                string key = prefix + name.Trim();
                if (PlayerPrefs.HasKey(key))
                {
                    string value = PlayerPrefs.GetString(key);
                    var v = graph.GetOrCreateVariable(name.Trim(), VariableType.String, value);
                    v.SetStringValue(value);
                }
            }
            graph.Save();
            Complete();
        }
    }
}
