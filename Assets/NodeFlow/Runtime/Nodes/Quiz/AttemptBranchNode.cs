using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Routes execution to a specific output port based on the current question's attempt number.
    /// Each wrong attempt fires the corresponding port (Attempt 1, Attempt 2, ...).
    /// If the attempt exceeds the configured max, the Default port fires.
    /// </summary>
    [Serializable]
    public class AttemptBranchNode : NodeData
    {
        [SerializeField]
        [Range(2, 10)]
        [Tooltip("Number of attempt-specific output ports.")]
        public int maxOutputs = 3;

        /// <summary>
        /// Set during OnExecute so the runner knows which single port to follow.
        /// </summary>
        [NonSerialized]
        public string SelectedPortId;

        public override string Name => "Attempt Branch";
        public override Color Color => new Color(0.7f, 0.5f, 0.8f); // Purple
        public override string Category => "Quiz";
        public override string Description => "Routes execution to a specific output port based on the current question's attempt number.";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input, PortCapacity.Multi)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            var ports = new List<PortData>();
            for (int i = 1; i <= maxOutputs; i++)
            {
                ports.Add(new PortData($"attempt_{i}", $"Attempt {i}", PortDirection.Output));
            }
            ports.Add(new PortData("default", "Default", PortDirection.Output));
            return ports;
        }

        protected override void OnExecute()
        {
            var state = QuizState.Instance;
            int attempt = state != null ? state.currentQuestionAttempt : 0;

            if (attempt >= 1 && attempt <= maxOutputs)
            {
                SelectedPortId = $"attempt_{attempt}";
            }
            else
            {
                SelectedPortId = "default";
            }

            Debug.Log($"[AttemptBranchNode] Attempt {attempt} → port '{SelectedPortId}'");
            Complete();
        }

        public override void Reset()
        {
            base.Reset();
            SelectedPortId = null;
        }
    }
}
