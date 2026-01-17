using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    public enum ProgressCheckType
    {
        QuestionsAnswered,
        CorrectAnswers,
        WrongAnswers,
        ConsecutiveCorrect,
        ConsecutiveWrong,
        ProgressPercentage,
        CorrectPercentage
    }

    /// <summary>
    /// Checks quiz progress and can branch based on thresholds.
    /// Useful for adaptive quiz flows.
    /// </summary>
    [Serializable]
    public class QuizProgressNode : NodeData
    {
        [SerializeField]
        public ProgressCheckType checkType = ProgressCheckType.QuestionsAnswered;

        [SerializeField]
        public int threshold = 5;

        [SerializeField]
        public bool branchOnThreshold = true;

        public override string Name => "Quiz Progress";
        public override Color Color => new Color(0.4f, 0.6f, 0.8f); // Blue
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
            if (branchOnThreshold)
            {
                return new List<PortData>
                {
                    new PortData("above", "Above/Equal", PortDirection.Output),
                    new PortData("below", "Below", PortDirection.Output)
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
            float currentValue = 0;

            switch (checkType)
            {
                case ProgressCheckType.QuestionsAnswered:
                    currentValue = state.questionsAnswered;
                    break;
                case ProgressCheckType.CorrectAnswers:
                    currentValue = state.correctAnswers;
                    break;
                case ProgressCheckType.WrongAnswers:
                    currentValue = state.wrongAnswers;
                    break;
                case ProgressCheckType.ConsecutiveCorrect:
                    currentValue = state.consecutiveCorrect;
                    break;
                case ProgressCheckType.ConsecutiveWrong:
                    currentValue = state.consecutiveWrong;
                    break;
                case ProgressCheckType.ProgressPercentage:
                    currentValue = state.ProgressPercentage;
                    break;
                case ProgressCheckType.CorrectPercentage:
                    currentValue = state.CorrectPercentage;
                    break;
            }

            bool meetsThreshold = currentValue >= threshold;
            Debug.Log($"[QuizProgressNode] {checkType}: {currentValue}, Threshold: {threshold}, Meets: {meetsThreshold}");

            if (branchOnThreshold)
            {
                State = meetsThreshold ? NodeState.Completed : NodeState.Failed;
            }

            Complete();
        }
    }
}
