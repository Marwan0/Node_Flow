using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    public enum SequenceWrapMode
    {
        Loop,          // Step 1→2→3→1→2→3→...
        LoopWithDone,  // Step 1→2→3→Done→1→2→3→Done→...
        OnceThenDone,  // Step 1→2→3→Done→Done→Done→...
        OnceThenStop   // Step 1→2→3 (then no output, branch ends)
    }

    /// <summary>
    /// Cycles through its output ports one at a time.
    /// First execution fires Step 1, second fires Step 2, and so on.
    /// Behavior after the last step depends on the wrap mode.
    /// </summary>
    [Serializable]
    public class SequenceStepNode : NodeData
    {
        [SerializeField]
        [Range(2, 10)]
        [Tooltip("Number of step output ports.")]
        public int stepCount = 3;

        [SerializeField]
        [Tooltip("What happens after all steps have been executed.")]
        public SequenceWrapMode wrapMode = SequenceWrapMode.Loop;

        [NonSerialized]
        private int _currentIndex;

        /// <summary>
        /// Set during OnExecute so the runner knows which port to follow.
        /// </summary>
        [NonSerialized]
        public string SelectedPortId;

        public override string Name => "Step Sequence";
        public override Color Color => new Color(0.2f, 0.7f, 0.7f); // Teal
        public override string Category => "Flow";
        public override string Description => "A round-robin router. The first time it is triggered, it fires output step_1. The second time, step_2, and so on.";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input, PortCapacity.Multi)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            var ports = new List<PortData>();
            for (int i = 1; i <= stepCount; i++)
            {
                ports.Add(new PortData($"step_{i}", $"Step {i}", PortDirection.Output));
            }

            // Add Done port for modes that use it
            if (wrapMode == SequenceWrapMode.LoopWithDone || wrapMode == SequenceWrapMode.OnceThenDone)
            {
                ports.Add(new PortData("done", "Done", PortDirection.Output));
            }

            return ports;
        }

        protected override void OnExecute()
        {
            switch (wrapMode)
            {
                case SequenceWrapMode.Loop:
                {
                    int step = (_currentIndex % stepCount) + 1;
                    SelectedPortId = $"step_{step}";
                    _currentIndex++;
                    break;
                }

                case SequenceWrapMode.LoopWithDone:
                {
                    // Cycle length = stepCount + 1 (steps + done)
                    int pos = _currentIndex % (stepCount + 1);
                    if (pos < stepCount)
                        SelectedPortId = $"step_{pos + 1}";
                    else
                        SelectedPortId = "done";
                    _currentIndex++;
                    break;
                }

                case SequenceWrapMode.OnceThenDone:
                {
                    if (_currentIndex < stepCount)
                    {
                        SelectedPortId = $"step_{_currentIndex + 1}";
                        _currentIndex++;
                    }
                    else
                    {
                        SelectedPortId = "done";
                    }
                    break;
                }

                case SequenceWrapMode.OnceThenStop:
                {
                    if (_currentIndex < stepCount)
                    {
                        SelectedPortId = $"step_{_currentIndex + 1}";
                        _currentIndex++;
                    }
                    else
                    {
                        // No output — branch ends
                        SelectedPortId = null;
                    }
                    break;
                }
            }

            Debug.Log($"[SequenceStepNode] Execution {_currentIndex} → port '{SelectedPortId ?? "(none)"}'");
            Complete();
        }

        public override void Reset()
        {
            base.Reset();
            // Do NOT reset _currentIndex here — the runner calls Reset() before
            // every re-execution, which would always send us back to Step 1.
            SelectedPortId = null;
        }

        /// <summary>
        /// Full reset including the step counter. Called when the graph restarts.
        /// </summary>
        public void ResetStepCounter()
        {
            _currentIndex = 0;
        }
    }
}
