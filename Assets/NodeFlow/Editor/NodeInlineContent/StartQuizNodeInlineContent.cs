#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes.Quiz;
using QuizSystem;

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

            // QuizManager - drag & drop reference with path fallback
            // Try to resolve current reference from path if not set
            if (node.quizManagerObject == null && !string.IsNullOrEmpty(node.quizManagerPath))
            {
                var found = GameObject.Find(node.quizManagerPath);
                if (found != null && found.GetComponent<QuizManager>() != null)
                    node.quizManagerObject = found;
            }

            CreateObjectField<GameObject>("", node.quizManagerObject, go =>
            {
                node.quizManagerObject = go;
                // Sync the fallback path from the assigned object
                if (go != null)
                    node.quizManagerPath = go.name;
                else
                    node.quizManagerPath = "";
            });
        }
    }
}
#endif
