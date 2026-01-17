using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeSystem
{
    /// <summary>
    /// Execution state for nodes
    /// </summary>
    public enum NodeState
    {
        Idle,
        Running,
        Completed,
        Failed
    }

    /// <summary>
    /// Port direction
    /// </summary>
    public enum PortDirection
    {
        Input,
        Output
    }

    /// <summary>
    /// Port capacity (single or multiple connections)
    /// </summary>
    public enum PortCapacity
    {
        Single,
        Multi
    }

    /// <summary>
    /// Defines a port on a node
    /// </summary>
    [Serializable]
    public class PortData
    {
        public string id;
        public string name;
        public PortDirection direction;
        public PortCapacity capacity;

        public PortData(string id, string name, PortDirection direction, PortCapacity capacity = PortCapacity.Single)
        {
            this.id = id;
            this.name = name;
            this.direction = direction;
            
            // Smart default: Output ports allow multiple connections (one-to-many)
            // Input ports are single by default (many-to-one)
            if (capacity == PortCapacity.Single && direction == PortDirection.Output)
            {
                this.capacity = PortCapacity.Multi; // Output ports default to Multi
            }
            else
            {
                this.capacity = capacity; // Use specified capacity or Single for inputs
            }
        }
    }

    /// <summary>
    /// Base class for all node data.
    /// This is a pure C# class (not MonoBehaviour/ScriptableObject) that holds node configuration.
    /// Must be [Serializable] for Unity's serialization.
    /// </summary>
    [Serializable]
    public abstract class NodeData
    {
        /// <summary>Unique identifier</summary>
        [SerializeField]
        private string _guid;
        public string Guid 
        { 
            get => _guid; 
            set => _guid = value; 
        }

        /// <summary>Position in editor</summary>
        [SerializeField]
        private Vector2 _position;
        public Vector2 Position 
        { 
            get => _position; 
            set => _position = value; 
        }

        /// <summary>Display name in editor</summary>
        public abstract string Name { get; }

        /// <summary>Node color in editor</summary>
        public virtual Color Color => new Color(0.3f, 0.3f, 0.3f);

        /// <summary>Category in search menu</summary>
        public virtual string Category => "General";

        /// <summary>Runtime state (not serialized)</summary>
        [NonSerialized]
        public NodeState State = NodeState.Idle;

        /// <summary>Runtime executor reference (not serialized)</summary>
        [NonSerialized]
        public NodeGraphRunner Runner;

        /// <summary>Completion callback (not serialized)</summary>
        [NonSerialized]
        public Action<NodeData> OnComplete;

        /// <summary>Breakpoint flag (serialized for editor)</summary>
        [SerializeField]
        public bool hasBreakpoint = false;

        /// <summary>Custom label shown next to node name (optional)</summary>
        [SerializeField]
        public string displayLabel = "";

        protected NodeData()
        {
            _guid = System.Guid.NewGuid().ToString();
        }

        /// <summary>Define input ports</summary>
        public abstract List<PortData> GetInputPorts();

        /// <summary>Define output ports</summary>
        public abstract List<PortData> GetOutputPorts();

        /// <summary>
        /// Get the capacity of an output port (Single or Multi)
        /// </summary>
        public PortCapacity GetOutputPortCapacity(string portId)
        {
            var ports = GetOutputPorts();
            var port = ports.FirstOrDefault(p => p.id == portId);
            return port?.capacity ?? PortCapacity.Single;
        }

        /// <summary>Execute the node (called by runner)</summary>
        public void Execute()
        {
            // Allow execution even if already running (for parallel execution scenarios)
            // The node's OnExecute() should handle re-entrancy if needed
            if (State == NodeState.Running)
            {
                Debug.LogWarning($"[NodeData] Node {Name} is already running, skipping execution. This might indicate a parallel execution issue.");
                return;
            }
            
            State = NodeState.Running;
            OnExecute();
        }

        /// <summary>Override to implement node logic</summary>
        protected abstract void OnExecute();

        /// <summary>Call when execution completes</summary>
        protected void Complete()
        {
            // Only set to Completed if not already set to Failed (for branching nodes)
            if (State != NodeState.Failed)
            {
                State = NodeState.Completed;
            }
            OnComplete?.Invoke(this);
        }

        /// <summary>Reset to idle state</summary>
        public virtual void Reset()
        {
            State = NodeState.Idle;
        }
    }
}


