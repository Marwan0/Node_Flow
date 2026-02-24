using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace NodeSystem
{
    /// <summary>
    /// Companion MonoBehaviour to hold UnityEvents for the node system.
    /// Add this to GameObjects that need to trigger Unity Events from nodes.
    /// </summary>
    [AddComponentMenu("Node System/Node Event Holder")]
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
