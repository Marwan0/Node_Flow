using System;
using UnityEngine;
using UnityEngine.Events;

namespace OmniEvent
{
    // ==================== OmniEvent (No Parameters) ====================
    
    /// <summary>
    /// OmniEvent with no parameters. Replacement for UnityEvent.
    /// </summary>
    [Serializable]
    public class OmniEvent : OmniEventBase
    {
        [SerializeField]
        public UnityEvent m_Event = new UnityEvent();

        /// <summary>
        /// Invoke the event.
        /// </summary>
        public void Invoke()
        {
            m_Event?.Invoke();
        }

        /// <summary>
        /// Add a listener to this event.
        /// </summary>
        public void AddListener(UnityAction call)
        {
            m_Event.AddListener(call);
        }

        /// <summary>
        /// Remove a listener from this event.
        /// </summary>
        public void RemoveListener(UnityAction call)
        {
            m_Event.RemoveListener(call);
        }

        public override void RemoveAllListeners()
        {
            m_Event.RemoveAllListeners();
        }

        public override int GetPersistentEventCount()
        {
            return m_Event.GetPersistentEventCount();
        }
    }

    // ==================== OmniEvent<T> (Single Parameter) ====================
    
    /// <summary>
    /// OmniEvent with one parameter. Supports complex types like Vector3, Color, Lists, etc.
    /// </summary>
    [Serializable]
    public class OmniEvent<T> : OmniEventBase
    {
        [SerializeField]
        public UnityEvent<T> m_Event = new UnityEvent<T>();

        /// <summary>
        /// Invoke the event with one parameter.
        /// </summary>
        public void Invoke(T arg0)
        {
            m_Event?.Invoke(arg0);
        }

        /// <summary>
        /// Add a listener to this event.
        /// </summary>
        public void AddListener(UnityAction<T> call)
        {
            m_Event.AddListener(call);
        }

        /// <summary>
        /// Remove a listener from this event.
        /// </summary>
        public void RemoveListener(UnityAction<T> call)
        {
            m_Event.RemoveListener(call);
        }

        public override void RemoveAllListeners()
        {
            m_Event.RemoveAllListeners();
        }

        public override int GetPersistentEventCount()
        {
            return m_Event.GetPersistentEventCount();
        }
    }

    // ==================== OmniEvent<T1, T2> (Two Parameters) ====================
    
    /// <summary>
    /// OmniEvent with two parameters. Supports complex types like Vector3, Color, Lists, etc.
    /// </summary>
    [Serializable]
    public class OmniEvent<T1, T2> : OmniEventBase
    {
        [SerializeField]
        public UnityEvent<T1, T2> m_Event = new UnityEvent<T1, T2>();

        /// <summary>
        /// Invoke the event with two parameters.
        /// </summary>
        public void Invoke(T1 arg0, T2 arg1)
        {
            m_Event?.Invoke(arg0, arg1);
        }

        /// <summary>
        /// Add a listener to this event.
        /// </summary>
        public void AddListener(UnityAction<T1, T2> call)
        {
            m_Event.AddListener(call);
        }

        /// <summary>
        /// Remove a listener from this event.
        /// </summary>
        public void RemoveListener(UnityAction<T1, T2> call)
        {
            m_Event.RemoveListener(call);
        }

        public override void RemoveAllListeners()
        {
            m_Event.RemoveAllListeners();
        }

        public override int GetPersistentEventCount()
        {
            return m_Event.GetPersistentEventCount();
        }
    }

    // ==================== OmniEvent<T1, T2, T3> (Three Parameters) ====================
    
    /// <summary>
    /// OmniEvent with three parameters. Supports complex types like Vector3, Color, Lists, etc.
    /// </summary>
    [Serializable]
    public class OmniEvent<T1, T2, T3> : OmniEventBase
    {
        [SerializeField]
        public UnityEvent<T1, T2, T3> m_Event = new UnityEvent<T1, T2, T3>();

        /// <summary>
        /// Invoke the event with three parameters.
        /// </summary>
        public void Invoke(T1 arg0, T2 arg1, T3 arg2)
        {
            m_Event?.Invoke(arg0, arg1, arg2);
        }

        /// <summary>
        /// Add a listener to this event.
        /// </summary>
        public void AddListener(UnityAction<T1, T2, T3> call)
        {
            m_Event.AddListener(call);
        }

        /// <summary>
        /// Remove a listener from this event.
        /// </summary>
        public void RemoveListener(UnityAction<T1, T2, T3> call)
        {
            m_Event.RemoveListener(call);
        }

        public override void RemoveAllListeners()
        {
            m_Event.RemoveAllListeners();
        }

        public override int GetPersistentEventCount()
        {
            return m_Event.GetPersistentEventCount();
        }
    }

    // ==================== OmniEvent<T1, T2, T3, T4> (Four Parameters) ====================
    
    /// <summary>
    /// OmniEvent with four parameters. Supports complex types like Vector3, Color, Lists, etc.
    /// </summary>
    [Serializable]
    public class OmniEvent<T1, T2, T3, T4> : OmniEventBase
    {
        [SerializeField]
        public UnityEvent<T1, T2, T3, T4> m_Event = new UnityEvent<T1, T2, T3, T4>();

        /// <summary>
        /// Invoke the event with four parameters.
        /// </summary>
        public void Invoke(T1 arg0, T2 arg1, T3 arg2, T4 arg3)
        {
            m_Event?.Invoke(arg0, arg1, arg2, arg3);
        }

        /// <summary>
        /// Add a listener to this event.
        /// </summary>
        public void AddListener(UnityAction<T1, T2, T3, T4> call)
        {
            m_Event.AddListener(call);
        }

        /// <summary>
        /// Remove a listener from this event.
        /// </summary>
        public void RemoveListener(UnityAction<T1, T2, T3, T4> call)
        {
            m_Event.RemoveListener(call);
        }

        public override void RemoveAllListeners()
        {
            m_Event.RemoveAllListeners();
        }

        public override int GetPersistentEventCount()
        {
            return m_Event.GetPersistentEventCount();
        }
    }
}
