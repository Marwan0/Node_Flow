# Node System File Map

Core runtime
- Assets/NodeFlow/Runtime/Core/NodeGraph.cs
  - Graph serialization, loading, indices, variables, asset references.
- Assets/NodeFlow/Runtime/Core/NodeData.cs
  - Base node contract and execution lifecycle.
- Assets/NodeFlow/Runtime/Core/ConnectionData.cs
  - Connection model (node GUID + port IDs).
- Assets/NodeFlow/Runtime/Core/NodeGraphRunner.cs
  - Runtime execution, routing, debug controls, signals.

Nodes (examples)
- Assets/NodeFlow/Runtime/Nodes/StartNode.cs
- Assets/NodeFlow/Runtime/Nodes/ConditionalNode.cs
- Assets/NodeFlow/Runtime/Nodes/RandomBranchNode.cs
- Assets/NodeFlow/Runtime/Nodes/WaitForAllNode.cs
- Assets/NodeFlow/Runtime/Nodes/SubGraphNode.cs
- Assets/NodeFlow/Runtime/Nodes/SendSignalNode.cs
- Assets/NodeFlow/Runtime/Nodes/ReceiveSignalNode.cs

Editor
- Assets/NodeFlow/Editor/NodeGraphEditorWindow.cs
  - Editor window, toolbar, runtime status.
- Assets/NodeFlow/Editor/NodeGraphView.cs
  - GraphView implementation, add/remove, groups, edges.
- Assets/NodeFlow/Editor/NodeView.cs
  - Node UI, ports, inline content, runtime visuals.
- Assets/NodeFlow/Editor/NodeSearchWindow.cs
  - Node search and creation.

Inline content and inspectors
- Assets/NodeFlow/Editor/NodeInlineContent/NodeInlineContentFactory.cs
- Assets/NodeFlow/Editor/NodeInspector/NodeInspectorFactory.cs

Existing docs
- PROJECT_OVERVIEW.md
- GUIDE_CREATING_CUSTOM_NODES.md
