using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using QuizSystem;

namespace NodeSystem
{
    /// <summary>
    /// Internal class to hold all graph data for JSON serialization
    /// </summary>
    /// <summary>
    /// Serialized group (editor-only). Position/size and contained node GUIDs.
    /// </summary>
    [Serializable]
    public class GroupEntry
    {
        public string id = "";
        public string title = "New Group";
        public float x;
        public float y;
        public float width;
        public float height;
        public List<string> nodeGuids = new List<string>();
    }

    [Serializable]
    internal class GraphData
    {
        public List<NodeEntry> nodes = new List<NodeEntry>();
        public List<ConnectionEntry> connections = new List<ConnectionEntry>();
        public List<GraphVariable> variables = new List<GraphVariable>();
        public List<GroupEntry> groups = new List<GroupEntry>();
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

    [Serializable]
    public class NodeUnityEvent
    {
        public string nodeGuid;
        public UnityEngine.Events.UnityEvent onEvent;
    }

    /// <summary>
    /// Stores asset references separately from JSON (for WebGL compatibility)
    /// </summary>
    [Serializable]
    public class NodeAssetReference
    {
        public string nodeGuid;
        public UnityEngine.Object assetReference;
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

        // Separate storage for UnityEvents (cannot be JSON serialized)
        [SerializeField]
        private List<NodeUnityEvent> _nodeEvents = new List<NodeUnityEvent>();

        // Separate storage for asset references (cannot be JSON serialized - for WebGL)
        [SerializeField, HideInInspector]
        private List<NodeAssetReference> _nodeAssetReferences = new List<NodeAssetReference>();

        // Native serialization path (faster, simpler than JSON)
        [SerializeReference]
        private List<NodeData> _serializedNodes = new List<NodeData>();

        // Runtime cache
        [NonSerialized] private List<NodeData> _runtimeNodes;
        [NonSerialized] private List<ConnectionData> _runtimeConnections;
        [NonSerialized] private List<GraphVariable> _runtimeVariables;
        [NonSerialized] private List<GroupEntry> _runtimeGroups;
        [NonSerialized] private List<GroupEntry> _editorGroups;
        [NonSerialized] private bool _loaded = false;
        [NonSerialized] private bool _loadFailed = false;

        // Performance optimization: Dictionary caches for O(1) lookups
        [NonSerialized] private Dictionary<string, NodeData> _nodeLookup;
        [NonSerialized] private Dictionary<string, List<ConnectionData>> _connectionIndex;
        [NonSerialized] private Dictionary<string, GraphVariable> _variableLookup;
        [NonSerialized] private Dictionary<string, NodeUnityEvent> _eventLookup;
        [NonSerialized] private bool _indicesBuilt = false;

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

        /// <summary>
        /// Saved group data (editor-only). Populated when loading from JSON; editor pushes current groups before save.
        /// </summary>
        public IReadOnlyList<GroupEntry> Groups
        {
            get { EnsureLoaded(); return _runtimeGroups ?? new List<GroupEntry>(); }
        }

        /// <summary>
        /// Editor calls this before save to persist current group layout. Only used when building JSON.
        /// </summary>
        public void SetEditorGroups(IReadOnlyList<GroupEntry> groups)
        {
            _editorGroups = groups != null ? new List<GroupEntry>(groups) : new List<GroupEntry>();
        }

        private void OnEnable()
        {
            // Only reload from JSON when _runtimeNodes is null (e.g., after domain reload
            // or first load). If _runtimeNodes already exists, the editor may have modified
            // node data in-memory that hasn't been saved to _jsonData yet — recreating
            // _runtimeNodes would orphan the instances that NodeView.Data points to,
            // causing all in-memory edits (like weight slider changes) to be silently lost.
            if (_runtimeNodes == null)
            {
                _loaded = false;
                EnsureLoaded();
                Debug.Log($"[NodeGraph] OnEnable (reloaded): {graphName} - {_runtimeNodes?.Count ?? 0} nodes, {_runtimeConnections?.Count ?? 0} connections");
            }
            else
            {
                Debug.Log($"[NodeGraph] OnEnable (kept existing): {graphName} - {_runtimeNodes.Count} nodes already in memory");
            }
        }

        private void EnsureLoaded()
        {
            if (_loaded) return;

            // Reset stale failure state before each load attempt.
            // A previous transient load error should not permanently block saving.
            _loadFailed = false;
            
            // Clear existing data
            _runtimeNodes = new List<NodeData>();
            _runtimeConnections = new List<ConnectionData>();
            _runtimeVariables = new List<GraphVariable>();
            _runtimeGroups = new List<GroupEntry>();
            
            // Initialize asset references list if null (don't clear it - preserve existing references)
            if (_nodeAssetReferences == null)
            {
                _nodeAssetReferences = new List<NodeAssetReference>();
            }
            
            InvalidateIndices(); // Clear indices when reloading
            _loaded = true;

            // --- FAIR MIGRATION NATIVE PATH ---
            if (_serializedNodes != null && _serializedNodes.Count > 0)
            {
                Debug.Log($"[NodeGraph] Loading {graphName} via SerializeReference fast path.");
                var loadedGuids = new HashSet<string>();
                foreach (var node in _serializedNodes)
                {
                    if (node != null && !string.IsNullOrEmpty(node.Guid) && !loadedGuids.Contains(node.Guid))
                    {
                        loadedGuids.Add(node.Guid);
                        _runtimeNodes.Add(node);
                    }
                }
                
                LoadMetadataFromJson(_jsonData);
                SyncNodeGroupIdsFromGroups();
                 
#if UNITY_EDITOR
                ValidateAndRestoreReferences();
#endif
                RestoreAssetReferences();
                BuildIndices();
                return;
            }

            // --- FALLBACK PATH: JSON DESERIALIZATION ---
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
                    _loadFailed = true;
                    return;
                }
                
                if (data.nodes == null)
                {
                    Debug.LogError($"[NodeGraph] {graphName}: Deserialized data has null nodes array!");
                    _loadFailed = true;
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

                // Load groups (editor-only layout)
                if (data.groups != null)
                {
                    foreach (var g in data.groups)
                    {
                        _runtimeGroups.Add(new GroupEntry
                        {
                            id = string.IsNullOrEmpty(g.id) ? System.Guid.NewGuid().ToString() : g.id,
                            title = g.title ?? "New Group",
                            x = g.x,
                            y = g.y,
                            width = g.width,
                            height = g.height,
                            nodeGuids = g.nodeGuids != null ? new List<string>(g.nodeGuids) : new List<string>()
                        });
                    }
                }

                // Keep node-level group links in sync with serialized group membership.
                SyncNodeGroupIdsFromGroups();

                // Validate and restore references after loading
#if UNITY_EDITOR
                ValidateAndRestoreReferences();
#endif

                // Restore asset references from separate storage (works in WebGL)
                RestoreAssetReferences();

                // Build performance indices after loading
                BuildIndices();

                // Load completed successfully
                _loadFailed = false;

            }
            catch (Exception e)
            {
                Debug.LogError($"[NodeGraph] Failed to load: {e.Message}");
                _loadFailed = true;
            }
        }

        private void LoadMetadataFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            
            try
            {
                var data = JsonUtility.FromJson<GraphData>(json);
                if (data == null) return;
                
                // Load connections
                if (data.connections != null)
                {
                    foreach (var entry in data.connections)
                    {
                        _runtimeConnections.Add(new ConnectionData(
                            entry.outNode, entry.outPort, entry.inNode, entry.inPort
                        ));
                    }
                }

                // Load variables
                if (data.variables != null)
                {
                    _runtimeVariables.AddRange(data.variables);
                }

                // Load groups (editor-only layout)
                if (data.groups != null)
                {
                    foreach (var g in data.groups)
                    {
                        _runtimeGroups.Add(new GroupEntry
                        {
                            id = string.IsNullOrEmpty(g.id) ? System.Guid.NewGuid().ToString() : g.id,
                            title = g.title ?? "New Group",
                            x = g.x,
                            y = g.y,
                            width = g.width,
                            height = g.height,
                            nodeGuids = g.nodeGuids != null ? new List<string>(g.nodeGuids) : new List<string>()
                        });
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NodeGraph] Failed to load metadata: {e.Message}");
            }
        }

        /// <summary>
        /// Build dictionary indices for O(1) lookups. Called after loading nodes.
        /// </summary>
        private void BuildIndices()
        {
            if (_indicesBuilt) return;

            // Build node lookup dictionary
            _nodeLookup = new Dictionary<string, NodeData>();
            foreach (var node in _runtimeNodes)
            {
                if (node != null && !string.IsNullOrEmpty(node.Guid))
                {
                    _nodeLookup[node.Guid] = node;
                }
            }

            // Build connection index by output node+port
            _connectionIndex = new Dictionary<string, List<ConnectionData>>();
            foreach (var conn in _runtimeConnections)
            {
                string key = $"{conn.outputNodeGuid}:{conn.outputPortId}";
                if (!_connectionIndex.ContainsKey(key))
                {
                    _connectionIndex[key] = new List<ConnectionData>();
                }
                _connectionIndex[key].Add(conn);
            }

            // Build variable lookup dictionary
            _variableLookup = new Dictionary<string, GraphVariable>();
            foreach (var variable in _runtimeVariables)
            {
                if (variable != null && !string.IsNullOrEmpty(variable.Name))
                {
                    _variableLookup[variable.Name] = variable;
                }
            }

            // Build event lookup dictionary
            _eventLookup = new Dictionary<string, NodeUnityEvent>();
            foreach (var evt in _nodeEvents)
            {
                if (evt != null && !string.IsNullOrEmpty(evt.nodeGuid))
                {
                    _eventLookup[evt.nodeGuid] = evt;
                }
            }

            _indicesBuilt = true;
        }

        private void SyncNodeGroupIdsFromGroups()
        {
            if (_runtimeNodes == null || _runtimeGroups == null) return;

            var byGuid = new Dictionary<string, NodeData>();
            foreach (var node in _runtimeNodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Guid)) continue;
                byGuid[node.Guid] = node;
                node.GroupId = ""; // Reset first; groups are source of truth here.
            }

            foreach (var group in _runtimeGroups)
            {
                if (group == null || string.IsNullOrEmpty(group.id) || group.nodeGuids == null) continue;
                foreach (var guid in group.nodeGuids)
                {
                    if (string.IsNullOrEmpty(guid)) continue;
                    if (byGuid.TryGetValue(guid, out var node))
                    {
                        node.GroupId = group.id;
                    }
                }
            }
        }

        /// <summary>
        /// Invalidate indices (call when nodes/connections/variables change)
        /// </summary>
        private void InvalidateIndices()
        {
            _indicesBuilt = false;
            _nodeLookup = null;
            _connectionIndex = null;
            _variableLookup = null;
            _eventLookup = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Validates and restores GameObject/Component references from paths when they're lost.
        /// Called after graph loads to fix references that were lost due to instanceID changes.
        /// </summary>
        private void ValidateAndRestoreReferences()
        {
            TryRestoreSceneReferencesInEditor(saveIfChanged: true);
        }

        /// <summary>
        /// Editor-only: restore scene object references from saved hierarchy paths.
        /// This is needed because JsonUtility stores UnityEngine.Object as session-specific instanceIDs.
        /// </summary>
        public bool TryRestoreSceneReferencesInEditor(bool saveIfChanged = true)
        {
            if (_runtimeNodes == null) return false;

            bool anyRestored = false;
            foreach (var node in _runtimeNodes)
            {
                if (node == null) continue;

                if (node is Nodes.Quiz.LoadQuestionNode loadNode)
                {
                    if (loadNode.questionContainerRef == null && !string.IsNullOrEmpty(loadNode.questionContainerPath))
                    {
                        var restored = RestoreGameObjectFromPath(loadNode.questionContainerPath);
                        if (restored != null)
                        {
                            loadNode.questionContainerRef = restored;
                            anyRestored = true;
                        }
                    }
                }

                if (node is Nodes.Quiz.ScoreProgressBarNode progressNode)
                {
                    if (progressNode.targetRef == null && !string.IsNullOrEmpty(progressNode.targetPath))
                    {
                        var restored = RestoreGameObjectFromPath(progressNode.targetPath);
                        if (restored != null)
                        {
                            progressNode.targetRef = restored;
                            anyRestored = true;
                        }
                    }
                }
            }

            if (anyRestored && saveIfChanged)
            {
                SaveToJson();
                UnityEditor.EditorUtility.SetDirty(this);
            }

            return anyRestored;
        }

        private UnityEngine.Object RestoreGameObjectFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // Try GameObject.Find first (works if object is active)
            var found = GameObject.Find(path);
            if (found != null) return found;

            // Try hierarchical search across all loaded scenes (works even if object is inactive)
            var parts = path.Split('/');
            if (parts.Length > 0)
            {
                string rootName = parts[0];
                for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
                {
                    var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    var rootObjects = scene.GetRootGameObjects();
                    foreach (var rootGo in rootObjects)
                    {
                        if (rootGo.name != rootName) continue;
                        if (parts.Length == 1) return rootGo;

                        var relativePath = string.Join("/", parts, 1, parts.Length - 1);
                        var t = rootGo.transform.Find(relativePath);
                        if (t != null) return t.gameObject;
                    }
                }
            }

            // Fallback: find by leaf name in all loaded scenes.
            string targetName = parts.Length > 0 ? parts[parts.Length - 1] : path;
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var rootGo in scene.GetRootGameObjects())
                {
                    var fallback = FindInHierarchyByName(rootGo.transform, targetName);
                    if (fallback != null) return fallback;
                }
            }

            return null;
        }

        private static GameObject FindInHierarchyByName(Transform parent, string targetName)
        {
            if (parent == null || string.IsNullOrEmpty(targetName)) return null;
            if (parent.name == targetName) return parent.gameObject;

            for (int i = 0; i < parent.childCount; i++)
            {
                var result = FindInHierarchyByName(parent.GetChild(i), targetName);
                if (result != null) return result;
            }
            return null;
        }
#endif

        /// <summary>
        /// Restore asset references from separate storage (called after loading nodes)
        /// This ensures Unity Object references work in WebGL builds
        /// </summary>
        private void RestoreAssetReferences()
        {
            if (_runtimeNodes == null || _nodeAssetReferences == null) return;

            int restoredCount = 0;
            foreach (var node in _runtimeNodes)
            {
                if (node == null) continue;

                // Find asset reference for this node
                var assetRef = _nodeAssetReferences.Find(r => r.nodeGuid == node.Guid);
                if (assetRef == null || assetRef.assetReference == null) continue;

                // Restore LoadQuestionNode question reference
                if (node is Nodes.Quiz.LoadQuestionNode loadNode)
                {
                    if (assetRef.assetReference is QuizSystem.QuestionData questionData)
                    {
                        loadNode.questionRef = questionData;
                        restoredCount++;
                        Debug.Log($"[NodeGraph] Restored QuestionData reference for node {node.Name} ({node.Guid})");
                    }
                    else
                    {
                        Debug.LogWarning($"[NodeGraph] Asset reference for {node.Name} is not QuestionData type: {assetRef.assetReference?.GetType()}");
                    }
                }

                // Restore PlaySoundNode audio clip reference
                if (node is Nodes.PlaySoundNode soundNode)
                {
                    if (assetRef.assetReference is AudioClip audioClip)
                    {
                        soundNode.audioClipRef = audioClip;
                        restoredCount++;
                        Debug.Log($"[NodeGraph] Restored AudioClip reference for node {node.Name} ({node.Guid})");
                    }
                    else
                    {
                        Debug.LogWarning($"[NodeGraph] Asset reference for {node.Name} is not AudioClip type: {assetRef.assetReference?.GetType()}");
                    }
                }
            }

            if (restoredCount > 0)
            {
                Debug.Log($"[NodeGraph] Restored {restoredCount} asset references from separate storage");
            }
            else if (_nodeAssetReferences.Count > 0)
            {
                Debug.LogWarning($"[NodeGraph] Found {_nodeAssetReferences.Count} asset references in storage, but none matched node types or were null");
            }
        }

        /// <summary>
        /// Save asset reference for a node (called when reference is set in editor)
        /// </summary>
        public void SetNodeAssetReference(string nodeGuid, UnityEngine.Object asset)
        {
            if (string.IsNullOrEmpty(nodeGuid))
            {
                Debug.LogWarning("[NodeGraph] SetNodeAssetReference called with empty nodeGuid");
                return;
            }

            // Initialize list if null
            if (_nodeAssetReferences == null)
            {
                _nodeAssetReferences = new List<NodeAssetReference>();
            }

            var existing = _nodeAssetReferences.Find(r => r.nodeGuid == nodeGuid);
            if (existing != null)
            {
                existing.assetReference = asset;
                Debug.Log($"[NodeGraph] Updated asset reference for node {nodeGuid}: {(asset != null ? asset.name : "null")}");
            }
            else
            {
                _nodeAssetReferences.Add(new NodeAssetReference
                {
                    nodeGuid = nodeGuid,
                    assetReference = asset
                });
                Debug.Log($"[NodeGraph] Added asset reference for node {nodeGuid}: {(asset != null ? asset.name : "null")}");
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// Get asset reference for a node
        /// </summary>
        public UnityEngine.Object GetNodeAssetReference(string nodeGuid)
        {
            if (string.IsNullOrEmpty(nodeGuid)) return null;
            var refEntry = _nodeAssetReferences.Find(r => r.nodeGuid == nodeGuid);
            return refEntry?.assetReference;
        }

        /// <summary>
        /// Save the current graph state to JSON.
        /// Call this whenever node data changes to persist modifications.
        /// </summary>
        public void SaveToJson()
        {
            if (_loadFailed)
            {
                Debug.LogError($"[NodeGraph] Cannot save graph '{name}' because loading failed previously. Preventing data overwrite.");
                return;
            }

            // Sync native serialization array
            if (_serializedNodes == null) _serializedNodes = new List<NodeData>();
            _serializedNodes.Clear();
            foreach (var node in _runtimeNodes)
            {
                if (node != null) _serializedNodes.Add(node);
            }

            var data = new GraphData();

            // We no longer serialize nodes to JSON to save performance and memory. 
            // The native [SerializeReference] list now authoritative.
            data.nodes = new List<NodeEntry>();

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

            // Save groups (editor pushes current state). If editor cache is not initialized,
            // preserve already-loaded runtime groups to avoid wiping group data on plain Save().
            var groupsSource = _editorGroups ?? _runtimeGroups ?? new List<GroupEntry>();
            data.groups = new List<GroupEntry>();
            foreach (var g in groupsSource)
            {
                if (g == null) continue;
                data.groups.Add(new GroupEntry
                {
                    id = g.id ?? "",
                    title = g.title ?? "New Group",
                    x = g.x,
                    y = g.y,
                    width = g.width,
                    height = g.height,
                    nodeGuids = g.nodeGuids != null ? new List<string>(g.nodeGuids) : new List<string>()
                });
            }

            // Keep runtime cache aligned with what was just serialized.
            _runtimeGroups = data.groups
                .Select(g => new GroupEntry
                {
                    id = g.id,
                    title = g.title,
                    x = g.x,
                    y = g.y,
                    width = g.width,
                    height = g.height,
                    nodeGuids = g.nodeGuids != null ? new List<string>(g.nodeGuids) : new List<string>()
                })
                .ToList();

            _jsonData = JsonUtility.ToJson(data);
            
            // Cleanup orphaned events and asset references
            CleanupUnityEvents();
            CleanupAssetReferences();
        }

        // === Public API ===

        public NodeData GetNode(string guid)
        {
            EnsureLoaded();
            if (!_indicesBuilt) BuildIndices();
            
            // O(1) lookup instead of O(n)
            if (_nodeLookup != null && _nodeLookup.TryGetValue(guid, out var node))
            {
                return node;
            }
            return null;
        }

        public NodeData GetEntryNode()
        {
            EnsureLoaded();
            // Optimized: iterate once instead of using LINQ
            foreach (var node in _runtimeNodes)
            {
                if (node is Nodes.StartNode)
                {
                    return node;
                }
            }
            return null;
        }

        public List<NodeData> GetConnectedNodes(string nodeGuid, string outputPortId)
        {
            EnsureLoaded();
            if (!_indicesBuilt) BuildIndices();
            
            var result = new List<NodeData>();
            string key = $"{nodeGuid}:{outputPortId}";

            // O(1) lookup instead of O(n) iteration
            if (_connectionIndex != null && _connectionIndex.TryGetValue(key, out var connections))
            {
                foreach (var conn in connections)
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
            if (node == null || string.IsNullOrEmpty(node.Guid)) return;
            
            // O(1) duplicate check instead of O(n)
            if (!_indicesBuilt) BuildIndices();
            if (_nodeLookup != null && _nodeLookup.ContainsKey(node.Guid)) return;
            
            _runtimeNodes.Add(node);
            if (_nodeLookup != null) _nodeLookup[node.Guid] = node;
            SaveAndMarkDirty();
            Debug.Log($"[NodeGraph] Added node: {node.Name}");
        }

        public void RemoveNode(NodeData node)
        {
            EnsureLoaded();
            if (node == null) return;
            
            // Remove connections (need to rebuild index after)
            _runtimeConnections.RemoveAll(c => 
                c.outputNodeGuid == node.Guid || c.inputNodeGuid == node.Guid);
            _runtimeNodes.Remove(node);
            
            // Clean up Unity Event for this node
            _nodeEvents.RemoveAll(e => e.nodeGuid == node.Guid);
            
            // Invalidate indices to force rebuild
            InvalidateIndices();
            
            SaveAndMarkDirty();
            Debug.Log($"[NodeGraph] Removed node: {node.Name}");
        }

        public void AddConnection(ConnectionData connection)
        {
            EnsureLoaded();
            if (connection == null) return;
            
            // O(1) duplicate check using index
            if (!_indicesBuilt) BuildIndices();
            string key = $"{connection.outputNodeGuid}:{connection.outputPortId}";
            
            if (_connectionIndex != null && _connectionIndex.TryGetValue(key, out var existing))
            {
                if (existing.Any(c => c.inputNodeGuid == connection.inputNodeGuid && 
                                     c.inputPortId == connection.inputPortId))
                {
                    return; // Duplicate
                }
            }
            
            _runtimeConnections.Add(connection);
            
            // Update index
            if (_connectionIndex != null)
            {
                if (!_connectionIndex.ContainsKey(key))
                {
                    _connectionIndex[key] = new List<ConnectionData>();
                }
                _connectionIndex[key].Add(connection);
            }
            
            SaveAndMarkDirty();
            Debug.Log($"[NodeGraph] Added connection: {connection.outputPortId} -> {connection.inputPortId}");
        }

        public void RemoveConnection(ConnectionData connection)
        {
            EnsureLoaded();
            if (connection == null) return;
            
            int removed = _runtimeConnections.RemoveAll(c =>
                c.outputNodeGuid == connection.outputNodeGuid &&
                c.outputPortId == connection.outputPortId &&
                c.inputNodeGuid == connection.inputNodeGuid &&
                c.inputPortId == connection.inputPortId);
            
            // Invalidate index if connection was removed
            if (removed > 0)
            {
                InvalidateIndices();
            }
            
            SaveAndMarkDirty();
        }

        /// <summary>
        /// Remove connections that reference ports no longer present on their nodes.
        /// </summary>
        public void CleanupOrphanedConnections()
        {
            EnsureLoaded();
            int removed = _runtimeConnections.RemoveAll(c =>
            {
                var node = GetNode(c.outputNodeGuid);
                if (node == null) return true;
                var ports = node.GetOutputPorts();
                return ports == null || !ports.Exists(p => p.id == c.outputPortId);
            });

            if (removed > 0)
            {
                InvalidateIndices();
                Debug.Log($"[NodeGraph] Cleaned up {removed} orphaned connection(s)");
            }
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
            _loadFailed = false;
            InvalidateIndices();
            EnsureLoaded();
        }

        public void Save()
        {
            // Sync asset references from nodes to separate storage before saving
            SyncAssetReferencesFromNodes();
            
            SaveAndMarkDirty();
#if UNITY_EDITOR
            // Explicit save should write to disk immediately
            UnityEditor.AssetDatabase.SaveAssets();
#endif
            int groupCount = _runtimeGroups?.Count ?? 0;
            int groupedNodeCount = _runtimeGroups?.Sum(g => g?.nodeGuids?.Count ?? 0) ?? 0;
            int groupedNodeLinks = _runtimeNodes?.Count(n => n != null && !string.IsNullOrEmpty(n.GroupId)) ?? 0;
            Debug.Log($"[NodeGraph] Saved: {NodeCount} nodes, {ConnectionCount} connections, {VariableCount} variables, {groupCount} groups, {groupedNodeCount} grouped nodes, {groupedNodeLinks} node-group links, {_nodeAssetReferences?.Count ?? 0} asset references");
        }

        /// <summary>
        /// Sync asset references from nodes to separate storage.
        /// This ensures references are saved even if SetNodeAssetReference wasn't called.
        /// </summary>
        private void SyncAssetReferencesFromNodes()
        {
            EnsureLoaded();
            if (_runtimeNodes == null) return;

            // Initialize list if null
            if (_nodeAssetReferences == null)
            {
                _nodeAssetReferences = new List<NodeAssetReference>();
            }

            int syncedCount = 0;
            int checkedCount = 0;
            
#if UNITY_EDITOR
            foreach (var node in _runtimeNodes)
            {
                if (node == null) continue;

                // Sync LoadQuestionNode question reference
                if (node is Nodes.Quiz.LoadQuestionNode loadNode)
                {
                    checkedCount++;
                    UnityEngine.Object assetToSave = null;
                    
                    // Try direct reference first
                    if (loadNode.questionRef != null)
                    {
                        assetToSave = loadNode.questionRef;
                    }
                    // If null, try loading from path (works in editor)
                    else if (!string.IsNullOrEmpty(loadNode.questionAssetPath))
                    {
                        assetToSave = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestionData>(loadNode.questionAssetPath);
                        if (assetToSave != null)
                        {
                            // Also set the direct reference for next time
                            loadNode.questionRef = assetToSave as QuestionData;
                        }
                    }
                    
                    if (assetToSave != null)
                    {
                        SetNodeAssetReference(node.Guid, assetToSave);
                        syncedCount++;
                        Debug.Log($"[NodeGraph] Synced QuestionData reference for {node.Name}: {assetToSave.name}");
                    }
                    else if (!string.IsNullOrEmpty(loadNode.questionAssetPath))
                    {
                        Debug.LogWarning($"[NodeGraph] LoadQuestionNode {node.Name} has path but asset not found: {loadNode.questionAssetPath}");
                    }
                }

                // Sync PlaySoundNode audio clip reference
                if (node is Nodes.PlaySoundNode soundNode)
                {
                    checkedCount++;
                    UnityEngine.Object assetToSave = null;
                    
                    // Try direct reference first
                    if (soundNode.audioClipRef != null)
                    {
                        assetToSave = soundNode.audioClipRef;
                    }
                    // If null, try loading from path (works in editor)
                    else if (!string.IsNullOrEmpty(soundNode.audioClipPath))
                    {
                        assetToSave = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(soundNode.audioClipPath);
                        if (assetToSave != null)
                        {
                            // Also set the direct reference for next time
                            soundNode.audioClipRef = assetToSave as AudioClip;
                        }
                    }
                    
                    if (assetToSave != null)
                    {
                        SetNodeAssetReference(node.Guid, assetToSave);
                        syncedCount++;
                        Debug.Log($"[NodeGraph] Synced AudioClip reference for {node.Name}: {assetToSave.name}");
                    }
                    else if (!string.IsNullOrEmpty(soundNode.audioClipPath))
                    {
                        Debug.LogWarning($"[NodeGraph] PlaySoundNode {node.Name} has path but asset not found: {soundNode.audioClipPath}");
                    }
                }
            }
#else
            // At runtime, can only sync from direct references (paths won't work)
            foreach (var node in _runtimeNodes)
            {
                if (node == null) continue;

                if (node is Nodes.Quiz.LoadQuestionNode loadNode && loadNode.questionRef != null)
                {
                    checkedCount++;
                    SetNodeAssetReference(node.Guid, loadNode.questionRef);
                    syncedCount++;
                }
                else if (node is Nodes.PlaySoundNode soundNode && soundNode.audioClipRef != null)
                {
                    checkedCount++;
                    SetNodeAssetReference(node.Guid, soundNode.audioClipRef);
                    syncedCount++;
                }
            }
#endif

            if (syncedCount > 0)
            {
                Debug.Log($"[NodeGraph] Synced {syncedCount} asset references from nodes to storage (checked {checkedCount} nodes)");
            }
            else if (checkedCount > 0)
            {
                Debug.LogWarning($"[NodeGraph] Checked {checkedCount} nodes with asset fields, but found 0 references. Make sure you've assigned assets in the editor.");
            }
        }

        // === Variable Management ===

        public GraphVariable GetVariable(string name)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(name)) return null;
            if (!_indicesBuilt) BuildIndices();
            
            // O(1) lookup instead of O(n)
            if (_variableLookup != null && _variableLookup.TryGetValue(name, out var variable))
            {
                return variable;
            }
            return null;
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
            if (variable == null || string.IsNullOrEmpty(variable.Name)) return;
            if (!_indicesBuilt) BuildIndices();
            
            // O(1) duplicate check instead of O(n)
            if (_variableLookup != null && _variableLookup.ContainsKey(variable.Name))
            {
                Debug.LogWarning($"[NodeGraph] Variable '{variable.Name}' already exists");
                return;
            }
            
            _runtimeVariables.Add(variable);
            if (_variableLookup != null) _variableLookup[variable.Name] = variable;
            SaveAndMarkDirty();
            Debug.Log($"[NodeGraph] Added variable: {variable.Name}");
        }

        public void RemoveVariable(string name)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(name)) return;
            
            int removed = _runtimeVariables.RemoveAll(v => v.Name == name);
            if (removed > 0)
            {
                if (_variableLookup != null) _variableLookup.Remove(name);
                SaveAndMarkDirty();
                Debug.Log($"[NodeGraph] Removed variable: {name}");
            }
        }

        public void RemoveVariable(GraphVariable variable)
        {
            EnsureLoaded();
            if (variable == null) return;
            
            if (_runtimeVariables.Remove(variable))
            {
                if (_variableLookup != null && !string.IsNullOrEmpty(variable.Name))
                {
                    _variableLookup.Remove(variable.Name);
                }
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

        // === Unity Event Support ===

        public UnityEngine.Events.UnityEvent GetUnityEvent(string nodeGuid)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(nodeGuid)) return null;
            if (!_indicesBuilt) BuildIndices();
            
            // O(1) lookup instead of O(n)
            if (_eventLookup != null && _eventLookup.TryGetValue(nodeGuid, out var entry))
            {
                return entry.onEvent;
            }
            
            // Create new event
            entry = new NodeUnityEvent { nodeGuid = nodeGuid, onEvent = new UnityEngine.Events.UnityEvent() };
            _nodeEvents.Add(entry);
            if (_eventLookup != null) _eventLookup[nodeGuid] = entry;
            SaveAndMarkDirty();
            return entry.onEvent;
        }

        public void InvokeUnityEvent(string nodeGuid)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(nodeGuid)) return;
            if (!_indicesBuilt) BuildIndices();
            
            // O(1) lookup instead of O(n)
            if (_eventLookup != null && _eventLookup.TryGetValue(nodeGuid, out var entry))
            {
                entry.onEvent?.Invoke();
            }
        }

        /// <summary>
        /// Call this when saving to clean up events for deleted nodes
        /// </summary>
        private void CleanupUnityEvents()
        {
            if (_runtimeNodes == null) return;
            
            var validGuids = new HashSet<string>(_runtimeNodes.Select(n => n.Guid));
            int removed = _nodeEvents.RemoveAll(e => !validGuids.Contains(e.nodeGuid));
            if (removed > 0)
            {
                Debug.Log($"[NodeGraph] Cleaned up {removed} orphaned UnityEvents");
            }
        }

        /// <summary>
        /// Clean up asset references for deleted nodes
        /// </summary>
        private void CleanupAssetReferences()
        {
            if (_runtimeNodes == null) return;
            
            var validGuids = new HashSet<string>(_runtimeNodes.Select(n => n.Guid));
            int removed = _nodeAssetReferences.RemoveAll(r => !validGuids.Contains(r.nodeGuid));
            if (removed > 0)
            {
                Debug.Log($"[NodeGraph] Cleaned up {removed} orphaned asset references");
            }
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

        [ContextMenu("Debug: Check Asset References")]
        private void DebugCheckAssetReferences()
        {
            EnsureLoaded();
            Debug.Log($"=== Asset References Check: {graphName} ===");
            Debug.Log($"Total asset references stored: {_nodeAssetReferences?.Count ?? 0}");
            
            // Check nodes that should have asset references
            int loadQuestionNodes = 0;
            int playSoundNodes = 0;
            foreach (var node in _runtimeNodes)
            {
                if (node is Nodes.Quiz.LoadQuestionNode loadNode)
                {
                    loadQuestionNodes++;
                    bool hasStoredRef = _nodeAssetReferences?.Any(r => r.nodeGuid == node.Guid && r.assetReference != null) ?? false;
                    Debug.Log($"[NodeGraph] LoadQuestionNode: {node.Name} - Path: {loadNode.questionAssetPath}, Has Stored Ref: {hasStoredRef}");
                }
                else if (node is Nodes.PlaySoundNode soundNode)
                {
                    playSoundNodes++;
                    bool hasStoredRef = _nodeAssetReferences?.Any(r => r.nodeGuid == node.Guid && r.assetReference != null) ?? false;
                    Debug.Log($"[NodeGraph] PlaySoundNode: {node.Name} - Path: {soundNode.audioClipPath}, Has Stored Ref: {hasStoredRef}");
                }
            }
            
            Debug.Log($"[NodeGraph] Found {loadQuestionNodes} LoadQuestionNodes and {playSoundNodes} PlaySoundNodes");
            
            if (_nodeAssetReferences == null || _nodeAssetReferences.Count == 0)
            {
                Debug.LogWarning("[NodeGraph] No asset references found in storage! Make sure you've assigned assets to nodes and saved the graph.");
                Debug.LogWarning("[NodeGraph] Try: 1) Drag assets into nodes, 2) Press Save button, 3) Check again");
                return;
            }

            foreach (var refEntry in _nodeAssetReferences)
            {
                var node = GetNode(refEntry.nodeGuid);
                if (node == null)
                {
                    Debug.LogWarning($"[NodeGraph] Asset reference for missing node GUID: {refEntry.nodeGuid}");
                    continue;
                }

                string assetInfo = refEntry.assetReference != null 
                    ? $"{refEntry.assetReference.name} ({refEntry.assetReference.GetType().Name})"
                    : "NULL";

                Debug.Log($"[NodeGraph] Node: {node.Name} ({refEntry.nodeGuid}) -> Asset: {assetInfo}");

                // Check if node has the reference set
                if (node is Nodes.Quiz.LoadQuestionNode loadNode)
                {
                    bool hasRef = loadNode.questionRef != null;
                    Debug.Log($"  LoadQuestionNode.questionRef: {(hasRef ? loadNode.questionRef.name : "NULL")}");
                }
                else if (node is Nodes.PlaySoundNode soundNode)
                {
                    bool hasRef = soundNode.audioClipRef != null;
                    Debug.Log($"  PlaySoundNode.audioClipRef: {(hasRef ? soundNode.audioClipRef.name : "NULL")}");
                }
            }
        }

        [ContextMenu("Debug: Sync Asset References from Paths")]
        private void DebugSyncAssetReferencesFromPaths()
        {
#if UNITY_EDITOR
            EnsureLoaded();
            int synced = 0;
            
            foreach (var node in _runtimeNodes)
            {
                if (node is Nodes.Quiz.LoadQuestionNode loadNode && !string.IsNullOrEmpty(loadNode.questionAssetPath))
                {
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestionData>(loadNode.questionAssetPath);
                    if (asset != null)
                    {
                        SetNodeAssetReference(node.Guid, asset);
                        synced++;
                        Debug.Log($"[NodeGraph] Synced from path: {node.Name} -> {asset.name}");
                    }
                }
                else if (node is Nodes.PlaySoundNode soundNode && !string.IsNullOrEmpty(soundNode.audioClipPath))
                {
                    var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(soundNode.audioClipPath);
                    if (asset != null)
                    {
                        SetNodeAssetReference(node.Guid, asset);
                        synced++;
                        Debug.Log($"[NodeGraph] Synced from path: {node.Name} -> {asset.name}");
                    }
                }
            }
            
            if (synced > 0)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                Debug.Log($"[NodeGraph] Synced {synced} asset references from paths. Now save the graph!");
            }
            else
            {
                Debug.LogWarning("[NodeGraph] No assets found to sync. Make sure nodes have valid asset paths.");
            }
#endif
        }
    }
}
