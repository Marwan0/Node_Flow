using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NodeSystem
{
    /// <summary>
    /// Internal class to hold all graph data for JSON serialization
    /// </summary>
    [Serializable]
    internal class GraphData
    {
        public List<NodeEntry> nodes = new List<NodeEntry>();
        public List<ConnectionEntry> connections = new List<ConnectionEntry>();
        public List<GraphVariable> variables = new List<GraphVariable>();
    }

    [Serializable]
    internal class NodeEntry
    {
        public string typeName;
        public string json;
    }

    [Serializable]
    internal class ConnectionEntry
    {
        public string outNode;
        public string outPort;
        public string inNode;
        public string inPort;
    }

    /// <summary>
    /// ScriptableObject that stores a node graph.
    /// Uses a single JSON string for all data - most reliable serialization approach.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNodeGraph", menuName = "Node System/Node Graph")]
    public class NodeGraph : ScriptableObject
    {
        [Header("Graph Info")]
        public string graphName = "New Graph";
        
        [TextArea(2, 3)]
        public string description;

        // Single JSON string stores ALL graph data
        [SerializeField, HideInInspector]
        private string _jsonData = "";

        // Runtime cache
        [NonSerialized] private List<NodeData> _runtimeNodes;
        [NonSerialized] private List<ConnectionData> _runtimeConnections;
        [NonSerialized] private List<GraphVariable> _runtimeVariables;
        [NonSerialized] private bool _loaded = false;

        public IReadOnlyList<NodeData> Nodes
        {
            get { EnsureLoaded(); return _runtimeNodes; }
        }

        public IReadOnlyList<ConnectionData> Connections
        {
            get { EnsureLoaded(); return _runtimeConnections; }
        }

        public int NodeCount
        {
            get { EnsureLoaded(); return _runtimeNodes.Count; }
        }

        public int ConnectionCount
        {
            get { EnsureLoaded(); return _runtimeConnections.Count; }
        }

        public IReadOnlyList<GraphVariable> Variables
        {
            get { EnsureLoaded(); return _runtimeVariables; }
        }

        public int VariableCount
        {
            get { EnsureLoaded(); return _runtimeVariables.Count; }
        }

        private void OnEnable()
        {
            _loaded = false;
            EnsureLoaded();
            Debug.Log($"[NodeGraph] OnEnable: {graphName} - {_runtimeNodes?.Count ?? 0} nodes, {_runtimeConnections?.Count ?? 0} connections");
        }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            
            // Clear existing data
            _runtimeNodes = new List<NodeData>();
            _runtimeConnections = new List<ConnectionData>();
            _runtimeVariables = new List<GraphVariable>();
            _loaded = true;

            if (string.IsNullOrEmpty(_jsonData))
            {
                Debug.LogWarning($"[NodeGraph] {graphName}: _jsonData is empty! Graph will appear empty.");
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<GraphData>(_jsonData);
                if (data == null)
                {
                    Debug.LogError($"[NodeGraph] {graphName}: Failed to deserialize graph data from JSON!");
                    return;
                }
                
                if (data.nodes == null)
                {
                    Debug.LogError($"[NodeGraph] {graphName}: Deserialized data has null nodes array!");
                    return;
                }

                // Load nodes
                var loadedGuids = new HashSet<string>();
                foreach (var entry in data.nodes)
                {
                    if (string.IsNullOrEmpty(entry.typeName)) continue;
                    
                    var type = Type.GetType(entry.typeName);
                    if (type == null)
                    {
                        Debug.LogWarning($"[NodeGraph] Type not found: {entry.typeName}");
                        continue;
                    }

                    var node = (NodeData)JsonUtility.FromJson(entry.json, type);
                    if (node != null)
                    {
                        // Check for duplicate GUIDs (prevent duplicate nodes)
                        if (loadedGuids.Contains(node.Guid))
                        {
                            Debug.LogWarning($"[NodeGraph] Duplicate node GUID detected: {node.Guid} ({node.Name}). Skipping duplicate.");
                            continue;
                        }
                        loadedGuids.Add(node.Guid);
                        _runtimeNodes.Add(node);
                    }
                }

                // Load connections
                foreach (var entry in data.connections)
                {
                    _runtimeConnections.Add(new ConnectionData(
                        entry.outNode, entry.outPort, entry.inNode, entry.inPort
                    ));
                }

                // Load variables
                if (data.variables != null)
                {
                    _runtimeVariables.AddRange(data.variables);
                }

            }
            catch (Exception e)
            {
                Debug.LogError($"[NodeGraph] Failed to load: {e.Message}");
            }
        }

        /// <summary>
        /// Save the current graph state to JSON.
        /// Call this whenever node data changes to persist modifications.
        /// </summary>
        public void SaveToJson()
        {
            var data = new GraphData();

            // Save nodes
            foreach (var node in _runtimeNodes)
            {
                if (node == null) continue;
                data.nodes.Add(new NodeEntry
                {
                    typeName = node.GetType().AssemblyQualifiedName,
                    json = JsonUtility.ToJson(node)
                });
            }

            // Save connections
            foreach (var conn in _runtimeConnections)
            {
                data.connections.Add(new ConnectionEntry
                {
                    outNode = conn.outputNodeGuid,
                    outPort = conn.outputPortId,
                    inNode = conn.inputNodeGuid,
                    inPort = conn.inputPortId
                });
            }

            // Save variables
            data.variables = new List<GraphVariable>(_runtimeVariables);

            _jsonData = JsonUtility.ToJson(data);
            
            Debug.Log($"[NodeGraph] SaveToJson: {_runtimeNodes.Count} nodes, {_runtimeConnections.Count} connections, {_runtimeVariables.Count} variables");
        }

        // === Public API ===

        public NodeData GetNode(string guid)
        {
            EnsureLoaded();
            return _runtimeNodes.FirstOrDefault(n => n.Guid == guid);
        }

        public NodeData GetEntryNode()
        {
            EnsureLoaded();
            return _runtimeNodes.FirstOrDefault(n => n is Nodes.StartNode);
        }

        public List<NodeData> GetConnectedNodes(string nodeGuid, string outputPortId)
        {
            EnsureLoaded();
            var result = new List<NodeData>();

            foreach (var conn in _runtimeConnections)
            {
                if (conn.outputNodeGuid == nodeGuid && conn.outputPortId == outputPortId)
                {
                    var targetNode = GetNode(conn.inputNodeGuid);
                    if (targetNode != null) result.Add(targetNode);
                }
            }

            return result;
        }

        public void AddNode(NodeData node)
        {
            EnsureLoaded();
            if (_runtimeNodes.Any(n => n.Guid == node.Guid)) return;
            
            _runtimeNodes.Add(node);
            SaveAndMarkDirty();
            Debug.Log($"[NodeGraph] Added node: {node.Name}");
        }

        public void RemoveNode(NodeData node)
        {
            EnsureLoaded();
            _runtimeConnections.RemoveAll(c => 
                c.outputNodeGuid == node.Guid || c.inputNodeGuid == node.Guid);
            _runtimeNodes.Remove(node);
            SaveAndMarkDirty();
            Debug.Log($"[NodeGraph] Removed node: {node.Name}");
        }

        public void AddConnection(ConnectionData connection)
        {
            EnsureLoaded();
            
            // Check duplicate
            bool exists = _runtimeConnections.Any(c =>
                c.outputNodeGuid == connection.outputNodeGuid &&
                c.outputPortId == connection.outputPortId &&
                c.inputNodeGuid == connection.inputNodeGuid &&
                c.inputPortId == connection.inputPortId);
                
            if (exists) return;
            
            _runtimeConnections.Add(connection);
            SaveAndMarkDirty();
            Debug.Log($"[NodeGraph] Added connection: {connection.outputPortId} -> {connection.inputPortId}");
        }

        public void RemoveConnection(ConnectionData connection)
        {
            EnsureLoaded();
            _runtimeConnections.RemoveAll(c =>
                c.outputNodeGuid == connection.outputNodeGuid &&
                c.outputPortId == connection.outputPortId &&
                c.inputNodeGuid == connection.inputNodeGuid &&
                c.inputPortId == connection.inputPortId);
            SaveAndMarkDirty();
        }

        private void SaveAndMarkDirty()
        {
            SaveToJson();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            // Don't call AssetDatabase.SaveAssets() here - it interferes with Unity's Undo system
            // Let Unity save assets when appropriate (e.g., on explicit save, scene save, etc.)
#endif
        }

        /// <summary>
        /// Force reload graph data from JSON (useful after undo/redo)
        /// </summary>
        public void ForceReload()
        {
            _loaded = false;
            EnsureLoaded();
        }

        public void Save()
        {
            SaveAndMarkDirty();
#if UNITY_EDITOR
            // Explicit save should write to disk immediately
            UnityEditor.AssetDatabase.SaveAssets();
#endif
            Debug.Log($"[NodeGraph] Saved: {NodeCount} nodes, {ConnectionCount} connections, {VariableCount} variables");
        }

        // === Variable Management ===

        public GraphVariable GetVariable(string name)
        {
            EnsureLoaded();
            return _runtimeVariables.FirstOrDefault(v => v.Name == name);
        }

        public GraphVariable GetOrCreateVariable(string name, VariableType type, string defaultValue = "")
        {
            EnsureLoaded();
            var variable = GetVariable(name);
            if (variable == null)
            {
                switch (type)
                {
                    case VariableType.Bool:
                        variable = GraphVariable.CreateBool(name, defaultValue == "true");
                        break;
                    case VariableType.Int:
                        variable = GraphVariable.CreateInt(name, int.TryParse(defaultValue, out int i) ? i : 0);
                        break;
                    case VariableType.Float:
                        variable = GraphVariable.CreateFloat(name, float.TryParse(defaultValue, out float f) ? f : 0f);
                        break;
                    case VariableType.String:
                        variable = GraphVariable.CreateString(name, defaultValue);
                        break;
                }
                _runtimeVariables.Add(variable);
                SaveAndMarkDirty();
            }
            return variable;
        }

        public void AddVariable(GraphVariable variable)
        {
            EnsureLoaded();
            if (_runtimeVariables.Any(v => v.Name == variable.Name))
            {
                Debug.LogWarning($"[NodeGraph] Variable '{variable.Name}' already exists");
                return;
            }
            _runtimeVariables.Add(variable);
            SaveAndMarkDirty();
            Debug.Log($"[NodeGraph] Added variable: {variable.Name}");
        }

        public void RemoveVariable(string name)
        {
            EnsureLoaded();
            int removed = _runtimeVariables.RemoveAll(v => v.Name == name);
            if (removed > 0)
            {
                SaveAndMarkDirty();
                Debug.Log($"[NodeGraph] Removed variable: {name}");
            }
        }

        public void RemoveVariable(GraphVariable variable)
        {
            EnsureLoaded();
            if (_runtimeVariables.Remove(variable))
            {
                SaveAndMarkDirty();
                Debug.Log($"[NodeGraph] Removed variable: {variable.Name}");
            }
        }


        public List<string> Validate()
        {
            EnsureLoaded();
            var errors = new List<string>();

            if (_runtimeNodes.Count == 0)
            {
                errors.Add("Graph has no nodes");
                return errors;
            }

            if (GetEntryNode() == null)
                errors.Add("Graph has no Start node");

            foreach (var conn in _runtimeConnections)
            {
                if (GetNode(conn.outputNodeGuid) == null)
                    errors.Add($"Connection references missing node: {conn.outputNodeGuid}");
                if (GetNode(conn.inputNodeGuid) == null)
                    errors.Add($"Connection references missing node: {conn.inputNodeGuid}");
            }

            return errors;
        }

        public void ResetAllNodes()
        {
            EnsureLoaded();
            foreach (var node in _runtimeNodes) node?.Reset();
        }

        // === Debug ===

        [ContextMenu("Debug: Print Info")]
        private void DebugPrint()
        {
            EnsureLoaded();
            Debug.Log($"=== NodeGraph: {graphName} ===");
            Debug.Log($"JSON Length: {_jsonData?.Length ?? 0}");
            Debug.Log($"Nodes: {_runtimeNodes.Count}");
            foreach (var n in _runtimeNodes)
                Debug.Log($"  - {n.Name} ({n.Guid})");
            Debug.Log($"Connections: {_runtimeConnections.Count}");
            foreach (var c in _runtimeConnections)
                Debug.Log($"  - {c.outputNodeGuid}:{c.outputPortId} -> {c.inputNodeGuid}:{c.inputPortId}");
        }

        [ContextMenu("Debug: Show JSON")]
        private void DebugShowJson()
        {
            Debug.Log($"JSON Data:\n{_jsonData}");
        }

        [ContextMenu("Clear All")]
        private void ClearAll()
        {
            _runtimeNodes = new List<NodeData>();
            _runtimeConnections = new List<ConnectionData>();
            _jsonData = "";
            _loaded = true;
            SaveAndMarkDirty();
            Debug.Log("[NodeGraph] Cleared all data");
        }

        [ContextMenu("Clean Orphaned Connections")]
        private void CleanOrphaned()
        {
            EnsureLoaded();
            int removed = _runtimeConnections.RemoveAll(c =>
                GetNode(c.outputNodeGuid) == null || GetNode(c.inputNodeGuid) == null);
            if (removed > 0)
            {
                SaveAndMarkDirty();
                Debug.Log($"[NodeGraph] Cleaned {removed} orphaned connections");
            }
        }
    }
}
