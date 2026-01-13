using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Sets a variable in the graph for use with ConditionalNode
    /// </summary>
    [Serializable]
    public class SetVariableNode : NodeData
    {
        [SerializeField]
        public string variableName = "";
        
        [SerializeField]
        public VariableType variableType = VariableType.Bool;
        
        [SerializeField]
        public string value = "";

        public override string Name => "Set Variable";
        public override Color Color => new Color(0.5f, 0.4f, 0.7f); // Purple
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
                Debug.LogError("[SetVariableNode] No graph runner assigned!");
                Complete();
                return;
            }

            if (string.IsNullOrEmpty(variableName))
            {
                Debug.LogWarning("[SetVariableNode] No variable name specified");
                Complete();
                return;
            }

            var graph = Runner.Graph;
            var variable = graph.GetOrCreateVariable(variableName, variableType, value);

            // Set the value based on type
            switch (variableType)
            {
                case VariableType.Bool:
                    bool boolVal = value.ToLower() == "true" || value == "1";
                    variable.SetBoolValue(boolVal);
                    Debug.Log($"[SetVariableNode] Set {variableName} = {boolVal}");
                    break;
                    
                case VariableType.Int:
                    if (int.TryParse(value, out int intVal))
                    {
                        variable.SetIntValue(intVal);
                        Debug.Log($"[SetVariableNode] Set {variableName} = {intVal}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SetVariableNode] Invalid int value: {value}");
                    }
                    break;
                    
                case VariableType.Float:
                    if (float.TryParse(value, out float floatVal))
                    {
                        variable.SetFloatValue(floatVal);
                        Debug.Log($"[SetVariableNode] Set {variableName} = {floatVal}");
                    }
                    else
                    {
                        Debug.LogWarning($"[SetVariableNode] Invalid float value: {value}");
                    }
                    break;
                    
                case VariableType.String:
                    variable.SetStringValue(value);
                    Debug.Log($"[SetVariableNode] Set {variableName} = \"{value}\"");
                    break;
            }

            graph.Save();
            Complete();
        }
    }
}

