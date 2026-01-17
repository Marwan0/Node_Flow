#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class StartQuizNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as StartQuizNode;
            if (node == null) return;

            // Quiz settings
            CreateIntField("Total Questions", node.totalQuestions, v => node.totalQuestions = Mathf.Max(1, v));
            CreateIntField("Max Score", node.maxScore, v => node.maxScore = Mathf.Max(0, v));

            // Timer
            CreateToggle("Start Timer", node.startTimer, v => 
            {
                node.startTimer = v;
                RequestRefresh();
            });

            if (node.startTimer)
            {
                CreateFloatField("Duration (s)", node.timerDuration, v => node.timerDuration = Mathf.Max(10f, v));
            }

            // QuizManager path
            CreateTextField(node.quizManagerPath, v => node.quizManagerPath = v, "QuizManager path");
        }
    }
}
#endif
