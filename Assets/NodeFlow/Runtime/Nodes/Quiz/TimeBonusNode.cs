using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Adds score based on remaining quiz time (e.g. points per second). Call when timer stops or on correct answer.
    /// </summary>
    [Serializable]
    public class TimeBonusNode : NodeData
    {
        [SerializeField]
        [Tooltip("Points awarded per second of remaining time")]
        public float pointsPerSecond = 1f;

        [SerializeField]
        [Tooltip("Cap the bonus at this many points (0 = no cap)")]
        public int maxBonus = 0;

        public override string Name => "Time Bonus";
        public override Color Color => new Color(0.3f, 0.8f, 0.6f);
        public override string Category => "Quiz";
        public override string Description => "Adds bonus score based on the remaining quiz time (points per second).";

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
            if (QuizState.Instance == null)
            {
                Complete();
                return;
            }

            float remaining = QuizState.Instance.timerRemaining;
            int bonus = Mathf.RoundToInt(remaining * pointsPerSecond);
            if (maxBonus > 0 && bonus > maxBonus)
                bonus = maxBonus;
            if (bonus > 0)
                QuizState.Instance.AddScore(bonus);
            Complete();
        }
    }
}
