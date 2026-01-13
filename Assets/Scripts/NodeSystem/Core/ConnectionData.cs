using System;
using UnityEngine;

namespace NodeSystem
{
    /// <summary>
    /// Represents a connection between two node ports.
    /// Uses GUIDs to reference nodes (not direct references).
    /// </summary>
    [Serializable]
    public class ConnectionData
    {
        [SerializeField]
        public string outputNodeGuid;
        
        [SerializeField]
        public string outputPortId;
        
        [SerializeField]
        public string inputNodeGuid;
        
        [SerializeField]
        public string inputPortId;

        public ConnectionData() { }

        public ConnectionData(string outNode, string outPort, string inNode, string inPort)
        {
            outputNodeGuid = outNode;
            outputPortId = outPort;
            inputNodeGuid = inNode;
            inputPortId = inPort;
        }

        public override bool Equals(object obj)
        {
            if (obj is ConnectionData other)
            {
                return outputNodeGuid == other.outputNodeGuid &&
                       outputPortId == other.outputPortId &&
                       inputNodeGuid == other.inputNodeGuid &&
                       inputPortId == other.inputPortId;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(outputNodeGuid, outputPortId, inputNodeGuid, inputPortId);
        }
    }
}


