using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using NodeSystem;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Builds a string from a template and graph variables (e.g. "Score: {score}") and writes to an output variable.
    /// Use with SetTextNode or DebugLog by reading the output variable.
    /// </summary>
    [Serializable]
    public class StringFormatNode : NodeData
    {
        [SerializeField]
        [Tooltip("Template with {varName} placeholders. Example: \"Score: {score} / {max}\"")]
        public string template = "Score: {score}";

        [SerializeField]
        [GraphVariable(true)]
        [Tooltip("Variable to write the result string to")]
        public string outputVariableName = "formattedText";

        public override string Name => "String Format";
        public override Color Color => new Color(0.55f, 0.5f, 0.7f);
        public override string Category => "Variables";

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
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogWarning("[StringFormatNode] No graph runner.");
                Complete();
                return;
            }

            if (string.IsNullOrEmpty(outputVariableName))
            {
                Debug.LogWarning("[StringFormatNode] No output variable name.");
                Complete();
                return;
            }

            string result = ReplacePlaceholders(template ?? "");
            var graph = Runner.Graph;
            var variable = graph.GetOrCreateVariable(outputVariableName, VariableType.String, "");
            variable.SetStringValue(result);
            graph.Save();
            Complete();
        }

        private string ReplacePlaceholders(string text)
        {
            if (Runner?.Graph == null) return text;
            // Replace {varName} with graph variable value
            return Regex.Replace(text, @"\{(\w+)\}", m =>
            {
                string name = m.Groups[1].Value;
                var v = Runner.Graph.GetVariable(name);
                if (v != null)
                    return v.GetStringValue();
                return m.Value;
            });
        }
    }
}
