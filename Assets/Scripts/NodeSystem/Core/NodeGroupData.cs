using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem
{
    /// <summary>
    /// Data for a node group (visual grouping of nodes)
    /// </summary>
    [Serializable]
    public class NodeGroupData
    {
        [SerializeField]
        private string _guid;
        public string Guid
        {
            get => _guid;
            set => _guid = value;
        }

        [SerializeField]
        private string _title = "Group";
        public string Title
        {
            get => _title;
            set => _title = value;
        }

        [SerializeField]
        private Rect _position;
        public Rect Position
        {
            get => _position;
            set => _position = value;
        }

        [SerializeField]
        private Color _color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
        public Color Color
        {
            get => _color;
            set => _color = value;
        }

        [SerializeField]
        private List<string> _containedNodeGuids = new List<string>();
        public List<string> ContainedNodeGuids => _containedNodeGuids;

        public NodeGroupData()
        {
            _guid = System.Guid.NewGuid().ToString();
        }

        public void AddNode(string nodeGuid)
        {
            if (!_containedNodeGuids.Contains(nodeGuid))
            {
                _containedNodeGuids.Add(nodeGuid);
            }
        }

        public void RemoveNode(string nodeGuid)
        {
            _containedNodeGuids.Remove(nodeGuid);
        }

        public bool ContainsNode(string nodeGuid)
        {
            return _containedNodeGuids.Contains(nodeGuid);
        }
    }
}

