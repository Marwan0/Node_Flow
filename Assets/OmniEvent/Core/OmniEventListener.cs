using UnityEngine;
using UnityEngine.Events;

namespace OmniEvent
{
    /// <summary>
    /// Generic component that listens to OmniEvents and triggers responses.
    /// Can be used to bridge between different event systems or add conditional logic.
    /// </summary>
    public class OmniEventListener : MonoBehaviour
    {
        [Header("Event Response")]
        [Tooltip("Event triggered when this listener is activated")]
        public OmniEvent onEventTriggered = new OmniEvent();

        /// <summary>
        /// Manually trigger this listener's response.
        /// </summary>
        public void TriggerResponse()
        {
            onEventTriggered?.Invoke();
        }
    }

    /// <summary>
    /// OmniEventListener with one parameter forwarding.
    /// </summary>
    public class OmniEventListener<T> : MonoBehaviour
    {
        [Header("Event Response")]
        [Tooltip("Event triggered when this listener is activated")]
        public OmniEvent<T> onEventTriggered = new OmniEvent<T>();

        /// <summary>
        /// Trigger this listener's response with one parameter.
        /// </summary>
        public void TriggerResponse(T arg0)
        {
            onEventTriggered?.Invoke(arg0);
        }
    }

    /// <summary>
    /// OmniEventListener with two parameter forwarding.
    /// </summary>
    public class OmniEventListener<T1, T2> : MonoBehaviour
    {
        [Header("Event Response")]
        [Tooltip("Event triggered when this listener is activated")]
        public OmniEvent<T1, T2> onEventTriggered = new OmniEvent<T1, T2>();

        /// <summary>
        /// Trigger this listener's response with two parameters.
        /// </summary>
        public void TriggerResponse(T1 arg0, T2 arg1)
        {
            onEventTriggered?.Invoke(arg0, arg1);
        }
    }

    /// <summary>
    /// OmniEventListener with three parameter forwarding.
    /// </summary>
    public class OmniEventListener<T1, T2, T3> : MonoBehaviour
    {
        [Header("Event Response")]
        [Tooltip("Event triggered when this listener is activated")]
        public OmniEvent<T1, T2, T3> onEventTriggered = new OmniEvent<T1, T2, T3>();

        /// <summary>
        /// Trigger this listener's response with three parameters.
        /// </summary>
        public void TriggerResponse(T1 arg0, T2 arg1, T3 arg2)
        {
            onEventTriggered?.Invoke(arg0, arg1, arg2);
        }
    }

    /// <summary>
    /// OmniEventListener with four parameter forwarding.
    /// </summary>
    public class OmniEventListener<T1, T2, T3, T4> : MonoBehaviour
    {
        [Header("Event Response")]
        [Tooltip("Event triggered when this listener is activated")]
        public OmniEvent<T1, T2, T3, T4> onEventTriggered = new OmniEvent<T1, T2, T3, T4>();

        /// <summary>
        /// Trigger this listener's response with four parameters.
        /// </summary>
        public void TriggerResponse(T1 arg0, T2 arg1, T3 arg2, T4 arg3)
        {
            onEventTriggered?.Invoke(arg0, arg1, arg2, arg3);
        }
    }
}
