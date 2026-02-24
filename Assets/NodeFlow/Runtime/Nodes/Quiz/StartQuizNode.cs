using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Initializes quiz state for node-based quiz flow.
    /// Sets up tracking for questions, score, and progress.
    /// </summary>
    [Serializable]
    public class StartQuizNode : NodeData
    {
        [SerializeField]
        public int totalQuestions = 10;

        [SerializeField]
        public int maxScore = 100;

        [SerializeField]
        public bool startTimer = false;

        [SerializeField]
        public float timerDuration = 300f; // 5 minutes

        [SerializeField]
        public string quizManagerPath = "QuizManager";

        public override string Name => "Start Quiz";
        public override Color Color => new Color(0.2f, 0.8f, 0.3f); // Bright Green
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
                new PortData("output", "Next", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            // Initialize quiz state
            var state = QuizState.Instance;
            state.StartQuiz(totalQuestions, maxScore);

            // Start timer if enabled
            if (startTimer && timerDuration > 0)
            {
                state.StartTimer(timerDuration);
            }

            // Optionally initialize QuizManager
            if (!string.IsNullOrEmpty(quizManagerPath))
            {
                var managerObj = GameObject.Find(quizManagerPath);
                if (managerObj != null)
                {
                    var manager = managerObj.GetComponent<QuizManager>();
                    if (manager != null)
                    {
                        // Sync total questions from QuizManager if not manually set
                        if (totalQuestions <= 0 && manager.questions.Count > 0)
                        {
                            state.totalQuestions = manager.questions.Count;
                        }
                    }
                }
            }

            Debug.Log($"[StartQuizNode] Quiz initialized: {totalQuestions} questions, max score: {maxScore}");
            Complete();
        }
    }
}
