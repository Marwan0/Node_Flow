#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes.Quiz;

namespace NodeSystem.Editor
{
    public class QuizTimerNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as QuizTimerNode;
            if (node == null) return;

            // Action dropdown
            CreateEnumField("Action", node.action, v => 
            {
                node.action = v;
                RequestRefresh();
            });

            // Duration (only for Start)
            if (node.action == TimerAction.Start)
            {
                CreateFloatField("Duration (s)", node.duration, v => node.duration = Mathf.Max(1f, v));
            }

            // Branch on expiry
            if (node.action != TimerAction.WaitForExpiry)
            {
                CreateToggle("Branch on Expiry", node.branchOnExpiry, v => 
                {
                    node.branchOnExpiry = v;
                    RequestRefresh();
                });
            }
        }
    }
}
#endif
