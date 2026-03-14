using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Throttles execution: only forwards to Next if at least cooldownSeconds have passed since last run.
    /// When throttled, completes immediately without forwarding (branch ends here).
    /// Prevents double-fires from buttons or repeated Send Signal.
    /// </summary>
    [Serializable]
    public class CooldownNode : NodeData
    {
        [SerializeField]
        [Tooltip("Minimum seconds between successful forwards. If triggered sooner, completes without forwarding.")]
        public float cooldownSeconds = 1f;

        [NonSerialized]
        private float _lastRunTime = -1f;

        public override string Name => "Cooldown";
        public override Color Color => new Color(0.45f, 0.5f, 0.55f);
        public override string Category => "Flow";
        public override string Description => "Limits how often a path can be executed. If triggered while on cooldown, execution diverts to the throttled port. Otherwise, it resets the timer and proceeds out output.";

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
                new PortData("output", "Next", PortDirection.Output),
                new PortData("throttled", "Throttled", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            float now = Time.time;
            if (_lastRunTime >= 0f && (now - _lastRunTime) < cooldownSeconds)
            {
                // Throttled: complete without forwarding. Runner will see no connection from output.
                // We still must complete so the runner can continue; but we don't want to follow Next.
                // So we complete and the runner looks at our output port - it will execute connected nodes.
                // So we need to NOT trigger Next when throttled. Option: set State = Failed and have runner
                // treat "failed" as "no next" for this node type, or we need a second output "throttled".
                // Plan said: "complete immediately without forwarding". So when throttled we should complete
                // but the runner should not execute Next. Only way: two outputs "next" and "throttled", and
                // when throttled we set State = Failed and runner routes to "throttled" (which can have no
                // connections). So we need runner to support CooldownNode: outputPort = "output" when
                // State Completed, and "throttled" when State Failed (and don't reset to Completed).
                // Then user connects only from "output" (Next). When throttled, we go to "throttled" and
                // if nothing connected, branch ends. Perfect.
                State = NodeState.Failed;
                OnComplete?.Invoke(this);
                return;
            }
            _lastRunTime = now;
            Complete();
        }
    }
}
