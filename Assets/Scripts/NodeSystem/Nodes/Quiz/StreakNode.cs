using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Branches on current correct-answer streak (from QuizState). Outputs "above" when streak >= threshold, "below" otherwise.
    /// </summary>
    [Serializable]
    public class StreakNode : NodeData
    {
        [SerializeField]
        [Tooltip("Streak must be >= this to output Above")]
        public int threshold = 3;

        public override string Name => "Streak";
        public override Color Color => new Color(0.9f, 0.6f, 0.2f);
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
                new PortData("above", "Above", PortDirection.Output),
                new PortData("below", "Below", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            int streak = QuizState.Instance != null ? QuizState.Instance.consecutiveCorrect : 0;
            State = streak >= threshold ? NodeState.Completed : NodeState.Failed;
            OnComplete?.Invoke(this);
        }
    }
}
