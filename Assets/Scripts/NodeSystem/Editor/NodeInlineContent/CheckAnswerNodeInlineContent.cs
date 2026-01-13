#if UNITY_EDITOR
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class CheckAnswerNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as CheckAnswerNode;
            if (node == null) return;

            // Quiz manager path
            CreateTextField(node.quizManagerPath, v => node.quizManagerPath = v, "QuizManager path...");
        }
    }
}
#endif

