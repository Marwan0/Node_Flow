using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    public enum QuizEndAction
    {
        Complete,
        Reset,
        ShowResults
    }

    /// <summary>
    /// Ends the quiz and can perform final actions.
    /// Can branch based on final score/performance.
    /// </summary>
    [Serializable]
    public class EndQuizNode : NodeData
    {
        [SerializeField]
        public QuizEndAction action = QuizEndAction.Complete;

        [SerializeField]
        public bool branchOnPerformance = false;

        [SerializeField]
        public float passingPercentage = 70f;

        public override string Name => "End Quiz";
        public override Color Color => new Color(0.8f, 0.2f, 0.2f); // Red
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
            if (branchOnPerformance)
            {
                return new List<PortData>
                {
                    new PortData("passed", "Passed", PortDirection.Output),
                    new PortData("failed", "Failed", PortDirection.Output)
                };
            }
            else
            {
                return new List<PortData>
                {
                    new PortData("output", "Next", PortDirection.Output)
                };
            }
        }

        protected override void OnExecute()
        {
            var state = QuizState.Instance;

            switch (action)
            {
                case QuizEndAction.Complete:
                    state.CompleteQuiz();
                    break;

                case QuizEndAction.Reset:
                    state.ResetState();
                    break;

                case QuizEndAction.ShowResults:
                    state.CompleteQuiz();
                    // Results display would be handled by UI listening to OnQuizCompleted
                    break;
            }

            // Branch based on performance
            if (branchOnPerformance)
            {
                bool passed = state.ScorePercentage >= passingPercentage;
                Debug.Log($"[EndQuizNode] Score: {state.ScorePercentage:F1}%, Passing: {passingPercentage}%, Passed: {passed}");
                State = passed ? NodeState.Completed : NodeState.Failed;
            }

            Complete();
        }
    }
}
