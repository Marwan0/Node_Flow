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
            var managerObj = GameObject.Find(quizManagerPath);
            if (managerObj == null)
            {
                Debug.LogWarning($"[ShuffleQuestionsNode] QuizManager not found at path: {quizManagerPath}");
                Complete();
                return;
            }

            var manager = managerObj.GetComponent<QuizManager>();
            if (manager == null)
            {
                Debug.LogWarning($"[ShuffleQuestionsNode] No QuizManager on: {quizManagerPath}");
                Complete();
                return;
            }

            manager.ShuffleQuestionsNow();
            Complete();
        }
    }
}
