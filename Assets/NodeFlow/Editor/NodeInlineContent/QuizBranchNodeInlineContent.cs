#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class QuizBranchNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as QuizBranchNode;
            if (node == null) return;

            // Condition dropdown
            CreateEnumField("Condition", node.condition, v => 
            {
                node.condition = v;
                RequestRefresh();
            });

            // Threshold value (only for applicable conditions)
            bool needsThreshold = node.condition == BranchCondition.ScoreAbove ||
                                  node.condition == BranchCondition.ScoreBelow ||
                                  node.condition == BranchCondition.CorrectPercentageAbove ||
                                  node.condition == BranchCondition.CorrectPercentageBelow ||
                                  node.condition == BranchCondition.ConsecutiveCorrectAbove ||
                                  node.condition == BranchCondition.ConsecutiveWrongAbove ||
                                  node.condition == BranchCondition.AttemptCountAbove;

            if (needsThreshold)
            {
                string label = node.condition.ToString().Contains("Percentage") ? "Percentage"
                             : node.condition == BranchCondition.AttemptCountAbove ? "Min Attempts"
                             : "Value";
                CreateIntField(label, node.thresholdValue, v => node.thresholdValue = v);
            }
        }
    }
}
#endif
