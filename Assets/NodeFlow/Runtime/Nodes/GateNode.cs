using System;
using System.Collections.Generic;
using UnityEngine;
using NodeSystem;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Lets flow through only when a graph variable (open flag) is true. Outputs "through" or "blocked".
    /// Open the gate by setting the variable to true (e.g. via Set Variable or Send Signal to another flow that sets it).
    /// Optionally close after one pass (oneShot).
    /// </summary>
    [Serializable]
    public class GateNode : NodeData
    {
        [SerializeField]
        [GraphVariable]
        [Tooltip("Variable that must be true to pass through. Set elsewhere (e.g. Set Variable) to open.")]
        public string variableName = "gateOpen";

        [SerializeField]
        [Tooltip("If true, close the gate after one pass (set variable to false)")]
        public bool oneShot = true;

        public override string Name => "Gate";
        public override Color Color => new Color(0.5f, 0.55f, 0.6f);
        public override string Category => "Flow";
        public override string Description => "Acts as a tollbooth. If the specified boolean graph variable is true, execution passes quickly through the through port. If false, it exits via blocked.";

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
                new PortData("through", "Through", PortDirection.Output),
                new PortData("blocked", "Blocked", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (Runner == null || Runner.Graph == null)
            {
                State = NodeState.Failed;
                OnComplete?.Invoke(this);
                return;
            }

            var variable = Runner.Graph.GetVariable(variableName);
            bool open = variable != null && variable.GetBoolValue();

            if (open)
            {
                if (oneShot && variable != null)
                    variable.SetBoolValue(false);
                State = NodeState.Completed;
            }
            else
                State = NodeState.Failed;

            OnComplete?.Invoke(this);
        }
    }
}
