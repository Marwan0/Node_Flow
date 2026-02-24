#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class ShowQuestionNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as ShowQuestionNode;
            if (node == null) return;

            // Quiz manager path
            CreateTextField(node.quizManagerPath, v => node.quizManagerPath = v, "QuizManager path...");

            // Question index
            CreateIntField("Question #", node.questionIndex, v => node.questionIndex = Mathf.Max(0, v));
        }
    }
}
#endif

