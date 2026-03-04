using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Writes the finalized quiz answer order into a graph string variable.
    /// Example output: "RRRWWRRRRR" (custom tokens/separator supported).
    /// </summary>
    [Serializable]
    public class AnswerOrderToVariableNode : NodeData
    {
        [SerializeField]
        [Tooltip("Graph variable to write the answer order string into.")]
        public string outputVariableName = "answer_order";

        [SerializeField]
        [Tooltip("Token used for a correct answer.")]
        public string correctToken = "R";

        [SerializeField]
        [Tooltip("Token used for a wrong answer.")]
        public string wrongToken = "W";

        [SerializeField]
        [Tooltip("Optional separator between tokens. Leave empty for compact output.")]
        public string separator = "";

        public override string Name => "Answer Order To Variable";
        public override Color Color => new Color(0.85f, 0.55f, 0.25f);
        public override string Category => "Quiz";

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
                Debug.LogWarning("[AnswerOrderToVariableNode] No graph runner.");
                Complete();
                return;
            }

            if (string.IsNullOrEmpty(outputVariableName))
            {
                Debug.LogWarning("[AnswerOrderToVariableNode] Output variable name is empty.");
                Complete();
                return;
            }

            var state = QuizState.Instance;
            string value = state != null
                ? state.GetAnswerOrderString(correctToken, wrongToken, separator)
                : string.Empty;

            var variable = Runner.Graph.GetOrCreateVariable(outputVariableName, VariableType.String, value);
            variable.SetStringValue(value);
            Runner.Graph.Save();

            Debug.Log($"[AnswerOrderToVariableNode] Set {outputVariableName} = \"{value}\"");
            Complete();
        }
    }
}

