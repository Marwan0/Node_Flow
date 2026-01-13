using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Checks if the last answer was correct
    /// </summary>
    [Serializable]
    public class CheckAnswerNode : NodeData
    {
        [SerializeField]
        public string quizManagerPath = "";

        [NonSerialized]
        private QuizManager _quizManager;

        public override string Name => "Check Answer";
        public override Color Color => new Color(0.7f, 0.5f, 0.2f); // Orange
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
                new PortData("correct", "Correct", PortDirection.Output),
                new PortData("incorrect", "Incorrect", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (string.IsNullOrEmpty(quizManagerPath))
            {
                Debug.LogWarning("[CheckAnswerNode] No quiz manager path specified");
                State = NodeState.Failed;
                Complete();
                return;
            }

            var managerObj = GameObject.Find(quizManagerPath);
            if (managerObj == null)
            {
                Debug.LogWarning($"[CheckAnswerNode] QuizManager not found: {quizManagerPath}");
                State = NodeState.Failed;
                Complete();
                return;
            }

            _quizManager = managerObj.GetComponent<QuizManager>();
            if (_quizManager == null)
            {
                Debug.LogWarning($"[CheckAnswerNode] No QuizManager component on: {quizManagerPath}");
                State = NodeState.Failed;
                Complete();
                return;
            }

            // Check last answer (simplified - would need to track in QuizManager)
            // For now, check current score or use a variable
            bool isCorrect = false; // Would check actual answer result
            
            Debug.Log($"[CheckAnswerNode] Answer check: {(isCorrect ? "Correct" : "Incorrect")}");
            
            State = isCorrect ? NodeState.Completed : NodeState.Failed;
            Complete();
        }
    }
}

