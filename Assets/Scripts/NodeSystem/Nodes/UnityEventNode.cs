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
        [SerializeField]
        public string eventHolderPath = "";
        
        [SerializeField]
        public string eventName = "OnNodeTriggered";

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
            if (string.IsNullOrEmpty(eventHolderPath))
            {
                Debug.LogWarning("[UnityEventNode] No event holder path specified");
                Complete();
                return;
            }

            var holder = GameObject.Find(eventHolderPath);
            if (holder == null)
            {
                Debug.LogWarning($"[UnityEventNode] Event holder not found: {eventHolderPath}");
                Complete();
                return;
            }

            // Try to find NodeEventHolder component
            var eventHolder = holder.GetComponent<NodeEventHolder>();
            if (eventHolder != null)
            {
                eventHolder.InvokeEvent(eventName);
                Debug.Log($"[UnityEventNode] Invoked event: {eventName}");
            }
            else
            {
                Debug.LogWarning($"[UnityEventNode] No NodeEventHolder on: {eventHolderPath}");
            }

            Complete();
        }
    }

    /// <summary>
    /// Companion MonoBehaviour to hold UnityEvents for the node system
    /// Add this to GameObjects that need to trigger Unity Events from nodes
    /// </summary>
    public class NodeEventHolder : MonoBehaviour
    {
        [Serializable]
        public class NamedEvent
        {
            public string eventName = "OnNodeTriggered";
            public UnityEvent onEvent = new UnityEvent();
        }

        [SerializeField]
        private List<NamedEvent> events = new List<NamedEvent>();

        /// <summary>
        /// Default event (for simple use cases)
        /// </summary>
        public UnityEvent OnNodeTriggered = new UnityEvent();

        /// <summary>
        /// Invoke an event by name
        /// </summary>
        public void InvokeEvent(string eventName)
        {
            // Check default event first
            if (eventName == "OnNodeTriggered")
            {
                OnNodeTriggered?.Invoke();
                return;
            }

            // Check named events
            foreach (var namedEvent in events)
            {
                if (namedEvent.eventName == eventName)
                {
                    namedEvent.onEvent?.Invoke();
                    return;
                }
            }

            Debug.LogWarning($"[NodeEventHolder] Event not found: {eventName}");
        }

        /// <summary>
        /// Add an event listener at runtime
        /// </summary>
        public void AddListener(string eventName, UnityAction action)
        {
            if (eventName == "OnNodeTriggered")
            {
                OnNodeTriggered.AddListener(action);
                return;
            }

            foreach (var namedEvent in events)
            {
                if (namedEvent.eventName == eventName)
                {
                    namedEvent.onEvent.AddListener(action);
                    return;
                }
            }

            // Create new event if not found
            var newEvent = new NamedEvent { eventName = eventName };
            newEvent.onEvent.AddListener(action);
            events.Add(newEvent);
        }
    }
}

