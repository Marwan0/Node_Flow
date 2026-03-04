#if UNITY_EDITOR
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class AnswerOrderToVariableNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as AnswerOrderToVariableNode;
            if (node == null) return;

            CreateLabel("Output variable name");
            CreateTextField(node.outputVariableName, v => node.outputVariableName = v, "answer_order");

            CreateLabel("Correct token");
            CreateTextField(node.correctToken, v => node.correctToken = v, "R");

            CreateLabel("Wrong token");
            CreateTextField(node.wrongToken, v => node.wrongToken = v, "W");

            CreateLabel("Separator (optional)");
            CreateTextField(node.separator, v => node.separator = v, "Leave empty for compact output");
        }
    }
}
#endif

