using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    public enum MathOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Modulo
    }

    public enum OperandType
    {
        Constant,
        Variable
    }

    /// <summary>
    /// Performs math operations on variables
    /// </summary>
    [Serializable]
    public class MathOperationNode : NodeData
    {
        [SerializeField]
        [GraphVariable(true)]
        public string resultVariable = "Result";

        [SerializeField]
        [GraphVariable]
        public string variableA = "VariableA";

        [SerializeField]
        public MathOperation operation = MathOperation.Add;

        [SerializeField]
        public OperandType operandType = OperandType.Constant;

        [SerializeField]
        [GraphVariable]
        public string variableB = "";

        [SerializeField]
        public float constantValue = 0f;

        public override string Name => "Math Operation";
        public override Color Color => new Color(0.3f, 0.5f, 0.7f); // Blueish
        public override string Category => "Math";

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
                Debug.LogError("[MathOperationNode] No graph runner assigned!");
                Complete();
                return;
            }

            var graph = Runner.Graph;
            var varA = graph.GetVariable(variableA);
            
            if (varA == null)
            {
                Debug.LogWarning($"[MathOperationNode] Variable A '{variableA}' not found");
                Complete();
                return;
            }

            float valA = varA.GetFloatValue();
            float valB = 0f;

            if (operandType == OperandType.Constant)
            {
                valB = constantValue;
            }
            else
            {
                var varB = graph.GetVariable(variableB);
                if (varB == null)
                {
                    Debug.LogWarning($"[MathOperationNode] Variable B '{variableB}' not found");
                    Complete();
                    return;
                }
                valB = varB.GetFloatValue();
            }

            float result = 0f;
            switch (operation)
            {
                case MathOperation.Add:
                    result = valA + valB;
                    break;
                case MathOperation.Subtract:
                    result = valA - valB;
                    break;
                case MathOperation.Multiply:
                    result = valA * valB;
                    break;
                case MathOperation.Divide:
                    if (Mathf.Abs(valB) < 0.0001f)
                    {
                        Debug.LogError("[MathOperationNode] Division by zero");
                        result = 0f;
                    }
                    else
                    {
                        result = valA / valB;
                    }
                    break;
                case MathOperation.Modulo:
                     if (Mathf.Abs(valB) < 0.0001f)
                    {
                        Debug.LogError("[MathOperationNode] Modulo by zero");
                        result = 0f;
                    }
                    else
                    {
                        result = valA % valB;
                    }
                    break;
            }

            // Store result
            var resultVar = graph.GetOrCreateVariable(resultVariable, VariableType.Float, result.ToString());
            
            // Should likely match the type of the result variable if it already exists and is Int
            if (resultVar.Type == VariableType.Int)
            {
                 resultVar.SetIntValue(Mathf.RoundToInt(result));
            }
            else
            {
                 resultVar.SetFloatValue(result);
            }

            Debug.Log($"[MathOperationNode] {valA} {operation} {valB} = {result} -> {resultVariable}");

            graph.Save();
            Complete();
        }
    }
}
