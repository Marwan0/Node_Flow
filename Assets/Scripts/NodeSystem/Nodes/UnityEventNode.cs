using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NodeSystem.Nodes
{
    /// <summary>
    /// Runtime data holder for UnityEvent invocation
    /// This node requires a companion MonoBehaviour to hold the actual UnityEvent
    /// </summary>
    [Serializable]
    public class UnityEventNode : NodeData
    {
        // No serialized fields needed - event is stored in graph asset by GUID

        public override string Name => "Unity Event";
        public override Color Color => new Color(0.6f, 0.3f, 0.7f); // Purple
        public override string Category => "Events";

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
                new PortData("output", "Next", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (Runner != null)
            {
                Runner.InvokeUnityEvent(Guid);
                Debug.Log($"[UnityEventNode] Invoked event for node {Guid}");
            }
            
            Complete();
        }
    }

    // NodeEventHolder has been moved to its own file in Assets/Scripts/NodeSystem/Components/NodeEventHolder.cs
}

