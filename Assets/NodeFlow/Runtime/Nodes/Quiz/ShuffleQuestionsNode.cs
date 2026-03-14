using System;
using System.Collections.Generic;
using UnityEngine;
using QuizSystem;

namespace NodeSystem.Nodes.Quiz
{
    /// <summary>
    /// Shuffles the QuizManager's question order so each play-through can differ.
    /// Call after Start Quiz (or ensure QuizManager has questions). Runs ShuffleQuestionsNow() on the manager.
    /// </summary>
    [Serializable]
    public class ShuffleQuestionsNode : NodeData
    {
        [SerializeField]
        public string quizManagerPath = "QuizManager";

        public override string Name => "Shuffle Questions";
        public override Color Color => new Color(0.25f, 0.75f, 0.5f);
        public override string Category => "Quiz";
        public override string Description => "Randomizes the order of the questions configured in the QuizManager.";

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
            // Use shared ref from StartQuizNode first, then fallback to path
            QuizManager manager = QuizState.Instance.quizManagerRef;
            if (manager == null && !string.IsNullOrEmpty(quizManagerPath))
            {
                var managerObj = GameObject.Find(quizManagerPath);
                if (managerObj != null)
                    manager = managerObj.GetComponent<QuizManager>();
            }
            if (manager == null)
            {
                Debug.LogWarning($"[ShuffleQuestionsNode] QuizManager not found. Set it on StartQuizNode or provide a path.");
                Complete();
                return;
            }

            manager.ShuffleQuestionsNow();
            Complete();
        }
    }
}
