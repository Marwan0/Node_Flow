# Node System Overview

High-level
- Visual node graph system authored in a custom GraphView editor.
- Graph data lives in NodeGraph ScriptableObject assets serialized to a single JSON string.
- Runtime executes via NodeGraphRunner, starting at StartNode and following connections.

Core data model
- NodeGraph holds nodes, connections, variables, and editor groups.
  - JSON serialization: _jsonData, loaded by EnsureLoaded().
  - Runtime caches: _runtimeNodes, _runtimeConnections, _runtimeVariables.
  - Lookup indices for O(1) queries: node, connection, variable, event.
  - Separate stores for UnityEvents and asset references (not JSON-serializable).
- NodeData is the base class for all nodes.
  - Defines ports via GetInputPorts / GetOutputPorts.
  - Executes via Execute -> OnExecute -> Complete.
  - Runtime state: NodeState (Idle, Running, Completed, Failed).

Runtime execution (NodeGraphRunner)
- Run(): validates graph, resets nodes, sets Runner on each node, starts at StartNode.
- ExecuteNode(): handles pause/breakpoints and calls ExecuteNodeInternal().
- OnNodeComplete(): chooses output port based on node type/state, then executes connected nodes.
- Parallel branches are supported; runner tracks active node GUIDs to avoid premature stop.
- Signals: SendSignalNode broadcasts by id to ReceiveSignalNode(s).
- Debug: pause/resume/step, breakpoints, static runtime events for editor visuals.

Editor architecture
- NodeGraphEditorWindow hosts NodeGraphView and inspector panels.
- NodeGraphView is the GraphView implementation:
  - loads graphs, creates NodeView, creates edges, handles add/remove, copy/paste, undo.
  - persists group layout and membership.
  - listens to runtime events to highlight nodes and edges.
- NodeView renders title, ports, inline content, and runtime states.
- NodeSearchWindow finds all NodeData subclasses and groups them by Category.

Variables
- GraphVariable stores typed values as strings, with typed getters/setters.
- [GraphVariable] attribute marks fields for variable selection in editor UI.

Special cases
- RandomBranchNode picks a single outgoing node based on weights.
- WaitForAllNode waits for all upstream branches to complete before continuing.
- SubGraphNode temporarily switches the runner to a sub-graph, then restores.

Key files
- Assets/NodeFlow/Runtime/Core/NodeGraph.cs
- Assets/NodeFlow/Runtime/Core/NodeData.cs
- Assets/NodeFlow/Runtime/Core/ConnectionData.cs
- Assets/NodeFlow/Runtime/Core/NodeGraphRunner.cs
- Assets/NodeFlow/Editor/NodeGraphEditorWindow.cs
- Assets/NodeFlow/Editor/NodeGraphView.cs
- Assets/NodeFlow/Editor/NodeView.cs
- Assets/NodeFlow/Editor/NodeSearchWindow.cs
