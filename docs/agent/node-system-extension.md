# Node System Extension Guide (Runtime + Editor)

Goal
- Add new node types and integrate them into the editor and runtime.

1. Create a new node
- File: Assets/NodeFlow/Runtime/Nodes/YourNode.cs
- Inherit NodeData, mark [Serializable].
- Override Name, Color, Category.
- Define ports in GetInputPorts / GetOutputPorts.
- Implement OnExecute and call Complete() when done.

2. Branching or special routing
- If the node changes the next port based on state, add handling in:
  - Assets/NodeFlow/Runtime/Core/NodeGraphRunner.cs (OnNodeComplete).
- Example patterns:
  - Conditional: output port depends on State Completed vs Failed.
  - RandomBranch: uses SelectedNodeGuid to execute only one node.
  - WaitForAll: use event-driven completion tracking.

3. Inline editor UI
- Optional: create inline content for custom fields.
- File: Assets/NodeFlow/Editor/NodeInlineContent/YourNodeInlineContent.cs
- Register in NodeInlineContentFactory static constructor.
- Use MarkDirty() when data changes, RequestRefresh() when layout changes.

4. Inspector UI
- Optional: add a custom inspector panel.
- File: Assets/NodeFlow/Editor/NodeInspector/YourNodeInspector.cs
- Register in NodeInspectorFactory.

5. Assets and UnityEvents
- If a node stores UnityEngine.Object references, consider:
  - NodeGraph.SetNodeAssetReference and RestoreAssetReferences.
  - Asset paths for editor-time rebind.
- UnityEvent nodes use NodeGraph and NodeGraphRunner event storage by node GUID.

6. Ports and capacity
- Default behavior: output ports are multi-capacity, inputs are single.
- Use PortCapacity.Multi explicitly for fan-in or fan-out.

7. Debugging support
- Use Runner.Pause/Resume/Step for debug flow.
- Breakpoints: NodeView supports a per-node hasBreakpoint toggle.

Common pitfalls
- Forgetting Complete() will stall execution.
- Not registering inline content means default FullNodeInlineContent is used.
- Adding branching nodes without updating OnNodeComplete leads to wrong routing.
- Forgetting to MarkDirty() means edits will not persist.
