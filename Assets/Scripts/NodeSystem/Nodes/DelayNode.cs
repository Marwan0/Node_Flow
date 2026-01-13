using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Waits for a specified duration before proceeding
    /// </summary>
    [Serializable]
    public class DelayNode : NodeData
    {
        [SerializeField]
        public float delaySeconds = 1f;

        public override string Name => "Delay";
        public override Color Color => new Color(0.5f, 0.5f, 0.5f); // Gray
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
                new PortData("output", "After Delay", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            Debug.Log($"[DelayNode] Waiting {delaySeconds}s...");
            Runner?.StartCoroutine(DelayCoroutine());
        }

        private IEnumerator DelayCoroutine()
        {
            yield return new WaitForSeconds(delaySeconds);
            Debug.Log("[DelayNode] Complete");
            Complete();
        }
    }
}


