using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Condition types for branching
    /// </summary>
    public enum ConditionType
    {
        BoolVariable,
        IntComparison,
        FloatComparison,
        StringEquals,
        GameObjectExists,
        GameObjectActive
    }

    /// <summary>
    /// Comparison operators
    /// </summary>
    public enum ComparisonOperator
    {
        Equals,
        NotEquals,
        GreaterThan,
        LessThan,
        GreaterOrEqual,
        LessOrEqual
    }

    /// <summary>
    /// Branch execution based on conditions
    /// </summary>
    [Serializable]
    public class ConditionalNode : NodeData
    {
        [SerializeField]
        public ConditionType conditionType = ConditionType.BoolVariable;
        
        [SerializeField]
        public string variableName = "";
        
        [SerializeField]
        public ComparisonOperator comparison = ComparisonOperator.Equals;
        
        [SerializeField]
        public string compareValue = "";
        
        [SerializeField]
        public string gameObjectPath = "";

        public override string Name => "Conditional";
        public override Color Color => new Color(0.9f, 0.7f, 0.2f); // Yellow/Orange
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
                new PortData("true", "True", PortDirection.Output),
                new PortData("false", "False", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            bool result = EvaluateCondition();
            Debug.Log($"[ConditionalNode] Condition evaluated to: {result}");
            
            // The runner will need to handle which output port to follow
            // We'll store the result and let the runner check it
            State = result ? NodeState.Completed : NodeState.Failed;
            OnComplete?.Invoke(this);
        }

        private bool EvaluateCondition()
        {
            switch (conditionType)
            {
                case ConditionType.BoolVariable:
                    return EvaluateBoolVariable();
                    
                case ConditionType.IntComparison:
                    return EvaluateIntComparison();
                    
                case ConditionType.FloatComparison:
                    return EvaluateFloatComparison();
                    
                case ConditionType.StringEquals:
                    return EvaluateStringEquals();
                    
                case ConditionType.GameObjectExists:
                    return GameObject.Find(gameObjectPath) != null;
                    
                case ConditionType.GameObjectActive:
                    var go = GameObject.Find(gameObjectPath);
                    return go != null && go.activeInHierarchy;
            }
            
            return false;
        }

        private bool EvaluateBoolVariable()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogWarning("[ConditionalNode] No graph runner assigned!");
                return false;
            }

            var variable = Runner.Graph.GetVariable(variableName);
            if (variable == null)
            {
                Debug.LogWarning($"[ConditionalNode] Variable '{variableName}' not found");
                return false;
            }

            bool boolVal = variable.GetBoolValue();
            bool compareVal = compareValue.ToLower() == "true" || compareValue == "1";
            
            return comparison == ComparisonOperator.Equals ? boolVal == compareVal : boolVal != compareVal;
        }

        private bool EvaluateIntComparison()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogWarning("[ConditionalNode] No graph runner assigned!");
                return false;
            }

            var variable = Runner.Graph.GetVariable(variableName);
            if (variable == null)
            {
                Debug.LogWarning($"[ConditionalNode] Variable '{variableName}' not found");
                return false;
            }

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

        private bool EvaluateFloatComparison()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogWarning("[ConditionalNode] No graph runner assigned!");
                return false;
            }

            var variable = Runner.Graph.GetVariable(variableName);
            if (variable == null)
            {
                Debug.LogWarning($"[ConditionalNode] Variable '{variableName}' not found");
                return false;
            }

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

        private bool EvaluateStringEquals()
        {
            if (Runner == null || Runner.Graph == null)
            {
                Debug.LogWarning("[ConditionalNode] No graph runner assigned!");
                return false;
            }

            var variable = Runner.Graph.GetVariable(variableName);
            if (variable == null)
            {
                Debug.LogWarning($"[ConditionalNode] Variable '{variableName}' not found");
                return false;
            }

            string val = variable.GetStringValue();
            return comparison == ComparisonOperator.Equals 
                ? val.Equals(compareValue, StringComparison.OrdinalIgnoreCase)
                : !val.Equals(compareValue, StringComparison.OrdinalIgnoreCase);
        }
    }
}

