using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Loop types
    /// </summary>
    public enum LoopType
    {
        Count,      // Repeat N times
        Condition,  // Repeat until condition is false
        Infinite    // Repeat forever (until stopped)
    }

    /// <summary>
    /// Repeats connected nodes multiple times
    /// </summary>
    [Serializable]
    public class LoopNode : NodeData
    {
        [SerializeField]
        public LoopType loopType = LoopType.Count;
        
        [SerializeField]
        public int loopCount = 3;
        
        [SerializeField]
        public string conditionVariable = "";
        
        [SerializeField]
        public bool conditionValue = true;

        [NonSerialized]
        private int _currentIteration = 0;

        public override string Name => "Loop";
        public override Color Color => new Color(0.6f, 0.3f, 0.8f); // Purple
        public override string Category => "Flow";

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
                new PortData("loop", "Loop Body", PortDirection.Output),
                new PortData("done", "Done", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            _currentIteration = 0;

            switch (loopType)
            {
                case LoopType.Count:
                    Runner?.StartCoroutine(LoopByCount());
                    break;
                case LoopType.Condition:
                    Runner?.StartCoroutine(LoopByCondition());
                    break;
                case LoopType.Infinite:
                    Runner?.StartCoroutine(LoopInfinite());
                    break;
            }
        }

        private IEnumerator LoopByCount()
        {
            for (int i = 0; i < loopCount; i++)
            {
                _currentIteration = i + 1;
                Debug.Log($"[LoopNode] Iteration {_currentIteration}/{loopCount}");

                // Execute loop body
                var nextNodes = Runner.Graph.GetConnectedNodes(Guid, "loop");
                if (nextNodes.Count > 0)
                {
                    yield return Runner.StartCoroutine(ExecuteAndWait(nextNodes[0]));
                }
                else
                {
                    yield return new WaitForSeconds(0.1f); // Small delay if no body
                }
            }

            Debug.Log("[LoopNode] Loop complete");
            Complete();
        }

        private IEnumerator LoopByCondition()
        {
            while (true)
            {
                // Check condition
                if (Runner == null || Runner.Graph == null) break;

                var variable = Runner.Graph.GetVariable(conditionVariable);
                if (variable != null)
                {
                    bool currentValue = variable.GetBoolValue();
                    if (currentValue != conditionValue)
                    {
                        break; // Condition no longer met
                    }
                }
                else
                {
                    Debug.LogWarning($"[LoopNode] Condition variable '{conditionVariable}' not found");
                    break;
                }

                _currentIteration++;
                Debug.Log($"[LoopNode] Iteration {_currentIteration} (condition: {conditionVariable} = {conditionValue})");

                // Execute loop body
                var nextNodes = Runner.Graph.GetConnectedNodes(Guid, "loop");
                if (nextNodes.Count > 0)
                {
                    yield return Runner.StartCoroutine(ExecuteAndWait(nextNodes[0]));
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            Debug.Log("[LoopNode] Loop complete (condition false)");
            Complete();
        }

        private IEnumerator LoopInfinite()
        {
            while (true)
            {
                _currentIteration++;
                Debug.Log($"[LoopNode] Infinite loop iteration {_currentIteration}");

                // Execute loop body
                var nextNodes = Runner.Graph.GetConnectedNodes(Guid, "loop");
                if (nextNodes.Count > 0)
                {
                    yield return Runner.StartCoroutine(ExecuteAndWait(nextNodes[0]));
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        private IEnumerator ExecuteAndWait(NodeData node)
        {
            bool completed = false;
            node.OnComplete = (n) => completed = true;
            node.Runner = Runner;
            node.Execute();

            while (!completed && Runner.IsRunning)
            {
                yield return null;
            }
        }

        public override void Reset()
        {
            base.Reset();
            _currentIteration = 0;
        }
    }
}

