using System;
using UnityEngine;
using UnityEngine.Events;

namespace OmniEvent
{
    /// <summary>
    /// Base class for all OmniEvent types. Provides common functionality for serialization and editor support.
    /// </summary>
    [Serializable]
    public abstract class OmniEventBase
    {
        /// <summary>
        /// Gets the number of listeners currently registered to this event.
        /// </summary>
        public abstract int GetPersistentEventCount();

        /// <summary>
        /// Removes all listeners from this event.
        /// </summary>
        public abstract void RemoveAllListeners();
    }
}
