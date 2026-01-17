using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    public enum ScoreOperation
    {
        Add,
        Subtract,
        Set,
        Multiply,
        Reset
    }

    /// <summary>
    /// Modifies or checks the quiz score.
    /// Can branch based on score thresholds.
    /// </summary>
    [Serializable]
    public class ScoreNode : NodeData
    {
        [SerializeField]
        public ScoreOperation operation = ScoreOperation.Add;

        [SerializeField]
        public int value = 10;

        [SerializeField]
        public bool branchOnThreshold = false;

        [SerializeField]
        public int threshold = 50;

        [SerializeField]
        public bool usePercentage = false; // If true, threshold is percentage of max score

        public override string Name => "Score";
        public override Color Color => new Color(0.9f, 0.7f, 0.2f); // Gold
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
            var ports = new List<PortData>();

            if (branchOnThreshold)
            {
                ports.Add(new PortData("above", "Above Threshold", PortDirection.Output));
                ports.Add(new PortData("below", "Below Threshold", PortDirection.Output));
            }
            else
            {
                ports.Add(new PortData("output", "Next", PortDirection.Output));
            }

            return ports;
        }

        protected override void OnExecute()
        {
            var state = QuizState.Instance;

            // Apply operation
            switch (operation)
            {
                case ScoreOperation.Add:
                    state.AddScore(value);
                    break;
                case ScoreOperation.Subtract:
                    state.AddScore(-value);
                    break;
                case ScoreOperation.Set:
                    state.SetScore(value);
                    break;
                case ScoreOperation.Multiply:
                    state.SetScore(state.currentScore * value);
                    break;
                case ScoreOperation.Reset:
                    state.SetScore(0);
                    break;
            }

            // Check threshold for branching
            if (branchOnThreshold)
            {
                float compareValue;
                if (usePercentage)
                {
                    compareValue = state.ScorePercentage;
                }
                else
                {
                    compareValue = state.currentScore;
                }

                bool aboveThreshold = compareValue >= threshold;
                State = aboveThreshold ? NodeState.Completed : NodeState.Failed;
                Debug.Log($"[ScoreNode] Score: {state.currentScore} ({state.ScorePercentage:F1}%), Threshold: {threshold}, Above: {aboveThreshold}");
            }

            Complete();
        }
    }
}
