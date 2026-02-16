using System;
using System.Collections.Generic;
using UnityEngine;
using NodeSystem;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Compares one graph variable to a literal (or second variable) and outputs true/false.
    /// Simpler than ConditionalNode when you only need variable comparison.
    /// </summary>
    [Serializable]
    public class CompareVariableNode : NodeData
    {
        [SerializeField]
        [GraphVariable]
        public string variableName = "";

        [SerializeField]
        public VariableType variableType = VariableType.Int;

        [SerializeField]
        public ComparisonOperator comparison = ComparisonOperator.Equals;

        [SerializeField]
        public string compareValue = "";

        public override string Name => "Compare Variable";
        public override Color Color => new Color(0.5f, 0.45f, 0.75f);
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
                new PortData("true", "True", PortDirection.Output),
                new PortData("false", "False", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            bool result = Evaluate();
            State = result ? NodeState.Completed : NodeState.Failed;
            OnComplete?.Invoke(this);
        }

        private bool Evaluate()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogWarning("[CompareVariableNode] No graph runner assigned!");
                return false;
            }

            var variable = Runner.Graph.GetVariable(variableName);
            if (variable == null)
            {
                Debug.LogWarning($"[CompareVariableNode] Variable '{variableName}' not found");
                return false;
            }

            switch (variableType)
            {
                case VariableType.Bool:
                    return EvalBool(variable);
                case VariableType.Int:
                    return EvalInt(variable);
                case VariableType.Float:
                    return EvalFloat(variable);
                case VariableType.String:
                    return EvalString(variable);
            }
            return false;
        }

        private bool EvalBool(GraphVariable variable)
        {
            bool val = variable.GetBoolValue();
            bool compareVal = compareValue?.ToLower() == "true" || compareValue == "1";
            return comparison == ComparisonOperator.Equals ? val == compareVal : val != compareVal;
        }

        private bool EvalInt(GraphVariable variable)
        {
            int val = variable.GetIntValue();
            if (!int.TryParse(compareValue, out int compareVal))
                return false;
            switch (comparison)
            {
                case ComparisonOperator.Equals: return val == compareVal;
                case ComparisonOperator.NotEquals: return val != compareVal;
                case ComparisonOperator.GreaterThan: return val > compareVal;
                case ComparisonOperator.LessThan: return val < compareVal;
                case ComparisonOperator.GreaterOrEqual: return val >= compareVal;
                case ComparisonOperator.LessOrEqual: return val <= compareVal;
            }
            return false;
        }

        private bool EvalFloat(GraphVariable variable)
        {
            float val = variable.GetFloatValue();
            if (!float.TryParse(compareValue, out float compareVal))
                return false;
            switch (comparison)
            {
                case ComparisonOperator.Equals: return Mathf.Approximately(val, compareVal);
                case ComparisonOperator.NotEquals: return !Mathf.Approximately(val, compareVal);
                case ComparisonOperator.GreaterThan: return val > compareVal;
                case ComparisonOperator.LessThan: return val < compareVal;
                case ComparisonOperator.GreaterOrEqual: return val >= compareVal;
                case ComparisonOperator.LessOrEqual: return val <= compareVal;
            }
            return false;
        }

        private bool EvalString(GraphVariable variable)
        {
            string val = variable.GetStringValue();
            return comparison == ComparisonOperator.Equals
                ? string.Equals(val, compareValue, StringComparison.OrdinalIgnoreCase)
                : !string.Equals(val, compareValue, StringComparison.OrdinalIgnoreCase);
        }
    }
}
