# Node System File Map

Core runtime
- Assets/Scripts/NodeSystem/Core/NodeGraph.cs
  - Graph serialization, loading, indices, variables, asset references.
- Assets/Scripts/NodeSystem/Core/NodeData.cs
  - Base node contract and execution lifecycle.
- Assets/Scripts/NodeSystem/Core/ConnectionData.cs
  - Connection model (node GUID + port IDs).
- Assets/Scripts/NodeSystem/Core/NodeGraphRunner.cs
  - Runtime execution, routing, debug controls, signals.

Nodes (examples)
- Assets/Scripts/NodeSystem/Nodes/StartNode.cs
- Assets/Scripts/NodeSystem/Nodes/ConditionalNode.cs
- Assets/Scripts/NodeSystem/Nodes/RandomBranchNode.cs
- Assets/Scripts/NodeSystem/Nodes/WaitForAllNode.cs
- Assets/Scripts/NodeSystem/Nodes/SubGraphNode.cs
- Assets/Scripts/NodeSystem/Nodes/SendSignalNode.cs
- Assets/Scripts/NodeSystem/Nodes/ReceiveSignalNode.cs

Editor
- Assets/Scripts/NodeSystem/Editor/NodeGraphEditorWindow.cs
  - Editor window, toolbar, runtime status.
- Assets/Scripts/NodeSystem/Editor/NodeGraphView.cs
  - GraphView implementation, add/remove, groups, edges.
- Assets/Scripts/NodeSystem/Editor/NodeView.cs
  - Node UI, ports, inline content, runtime visuals.
- Assets/Scripts/NodeSystem/Editor/NodeSearchWindow.cs
  - Node search and creation.

Inline content and inspectors
- Assets/Scripts/NodeSystem/Editor/NodeInlineContent/NodeInlineContentFactory.cs
- Assets/Scripts/NodeSystem/Editor/NodeInspector/NodeInspectorFactory.cs

Existing docs
- PROJECT_OVERVIEW.md
- GUIDE_CREATING_CUSTOM_NODES.md
