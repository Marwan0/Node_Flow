using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    public enum BranchCondition
    {
        LastAnswerCorrect,
        LastAnswerWrong,
        ScoreAbove,
        ScoreBelow,
        CorrectPercentageAbove,
        CorrectPercentageBelow,
        ConsecutiveCorrectAbove,
        ConsecutiveWrongAbove,
        QuizComplete,
        TimerExpired,
        AllQuestionsAnswered
    }

    /// <summary>
    /// Branch node specifically for quiz logic.
    /// Provides common quiz branching conditions.
    /// </summary>
    [Serializable]
    public class QuizBranchNode : NodeData
    {
        [SerializeField]
        public BranchCondition condition = BranchCondition.LastAnswerCorrect;

        [SerializeField]
        public int thresholdValue = 50;

        public override string Name => "Quiz Branch";
        public override Color Color => new Color(0.7f, 0.5f, 0.8f); // Purple
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
                new PortData("true", "True", PortDirection.Output),
                new PortData("false", "False", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            var state = QuizState.Instance;
            bool conditionMet = false;

            switch (condition)
            {
                case BranchCondition.LastAnswerCorrect:
                    conditionMet = state.lastAnswerWasCorrect;
                    break;

                case BranchCondition.LastAnswerWrong:
                    conditionMet = !state.lastAnswerWasCorrect && state.lastQuestionIndex >= 0;
                    break;

                case BranchCondition.ScoreAbove:
                    conditionMet = state.currentScore >= thresholdValue;
                    break;

                case BranchCondition.ScoreBelow:
                    conditionMet = state.currentScore < thresholdValue;
                    break;

                case BranchCondition.CorrectPercentageAbove:
                    conditionMet = state.CorrectPercentage >= thresholdValue;
                    break;

                case BranchCondition.CorrectPercentageBelow:
                    conditionMet = state.CorrectPercentage < thresholdValue;
                    break;

                case BranchCondition.ConsecutiveCorrectAbove:
                    conditionMet = state.consecutiveCorrect >= thresholdValue;
                    break;

                case BranchCondition.ConsecutiveWrongAbove:
                    conditionMet = state.consecutiveWrong >= thresholdValue;
                    break;

                case BranchCondition.QuizComplete:
                    conditionMet = state.quizCompleted;
                    break;

                case BranchCondition.TimerExpired:
                    conditionMet = state.TimerExpired;
                    break;

                case BranchCondition.AllQuestionsAnswered:
                    conditionMet = state.questionsAnswered >= state.totalQuestions && state.totalQuestions > 0;
                    break;
            }

            Debug.Log($"[QuizBranchNode] Condition: {condition}, Threshold: {thresholdValue}, Result: {conditionMet}");
            State = conditionMet ? NodeState.Completed : NodeState.Failed;
            Complete();
        }
    }
}
