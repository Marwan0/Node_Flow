using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Routes execution to a specific output port based on the last answered point index.
    /// Connect to LoadQuestionNode's output ports (on_correct, correct, incorrect, etc.)
    /// to trigger unique actions per answer option or per step in multi-step questions.
    /// </summary>
    [Serializable]
    public class PointRouterNode : NodeData
    {
        [SerializeField]
        [Range(2, 10)]
        [Tooltip("Number of point-specific output ports (match your question's answer/step count).")]
        public int maxOutputs = 4;

        /// <summary>
        /// Set during OnExecute so the runner knows which single port to follow.
        /// </summary>
        [NonSerialized]
        public string SelectedPortId;

        public override string Name => "Point Router";
        public override Color Color => new Color(0.9f, 0.6f, 0.2f); // Orange
        public override string Category => "Quiz";

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
            for (int i = 0; i < maxOutputs; i++)
            {
                ports.Add(new PortData($"point_{i}", $"Point {i}", PortDirection.Output));
            }
            ports.Add(new PortData("default", "Default", PortDirection.Output));
            return ports;
        }

        protected override void OnExecute()
        {
            var state = QuizState.Instance;
            int pointIndex = state != null ? state.lastAnsweredPointIndex : -1;

            if (pointIndex >= 0 && pointIndex < maxOutputs)
            {
                SelectedPortId = $"point_{pointIndex}";
            }
            else
            {
                SelectedPortId = "default";
            }

            Debug.Log($"[PointRouterNode] Point index {pointIndex} → port '{SelectedPortId}'");
            Complete();
        }

        public override void Reset()
        {
            base.Reset();
            SelectedPortId = null;
        }
    }
}
