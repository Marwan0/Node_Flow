## Quiz_Master / Node Flow – Project Overview

This document summarizes how the **node‑based flow system** in this project works, so it can be shared across chats or with other developers without needing to re‑explain everything.

---

## 1. High‑level concept

The project uses a **visual node graph system** (“Node Flow”) to drive quiz sequences and other logic.

- **Authoring** happens in a custom editor window based on Unity’s **GraphView / UI Toolkit**.
- **Graphs** are stored as `NodeGraph` assets (`ScriptableObject`).
- At runtime, a **NodeGraphRunner** component executes a `NodeGraph`, walking through nodes and following connections.
- The editor provides **Doozy‑style curved connections**, **traveling dots**, and **highlighted nodes/edges** to visualize execution in real time.

You can think of each `NodeGraph` as a **visual script** describing how a quiz (or other flow) progresses.

---

## 2. Core data structures

### 2.1 `NodeGraph` (asset)

File: `Assets/Scripts/NodeSystem/Core/NodeGraph.cs`

- `ScriptableObject` that stores:
  - **Nodes**: list of `NodeData` instances (serialized as JSON).
  - **Connections**: list of `ConnectionData` (by node GUID + port IDs).
  - **Variables**: list of `GraphVariable` for shared state.
- Uses an internal `_jsonData` string to serialize all node/connection/variable data via `JsonUtility`.
- Keeps runtime copies in:
  - `_runtimeNodes : List<NodeData>`
  - `_runtimeConnections : List<ConnectionData>`
  - `_runtimeVariables : List<GraphVariable>`
- `EnsureLoaded()` lazily deserializes `_jsonData` into `_runtime*` lists.
- `SaveToJson()` re‑serializes the runtime lists back into `_jsonData` (used whenever editor data changes).

**Important:** `OnEnable()` only reloads from JSON when `_runtimeNodes` is `null`. This avoids recreating nodes and losing in‑memory edits made by the editor UI.

### 2.2 `NodeData` (base class)

File: `Assets/Scripts/NodeSystem/Core/NodeData.cs`

- Abstract base class for all node types.
- Key fields:
  - `Guid` (string): unique ID for connections and lookup.
  - `Position` (`Vector2`): editor position.
  - `Name` / `Color` / `Category`: metadata for the editor.
  - `hasBreakpoint`, `displayLabel`: editor flags.
  - `State : NodeState` (`Idle`, `Running`, `Completed`, `Failed`) – **runtime only**.
  - `Runner : NodeGraphRunner` – runtime owner.
  - `OnComplete : Action<NodeData>` – callback when node finishes.
- Each node defines:
  - `GetInputPorts()` and `GetOutputPorts()` returning `List<PortData>` (IDs, display names, direction, capacity).
- Execution:
  - `Execute()` sets `State = Running` then calls `OnExecute()` (implemented by concrete node).
  - `Complete()` sets `State = Completed` (unless already `Failed`) and invokes `OnComplete`.
  - `Reset()` sets `State = Idle`.

### 2.3 Ports and connections

- `PortData` describes one logical port on a `NodeData`:
  - `id` (e.g. `"input"`, `"output"`, `"loop"`, `"done"`),
  - `name` (UI label),
  - `direction` (`Input` / `Output`),
  - `capacity` (`Single` / `Multi`).
- `ConnectionData` (in `NodeGraph`) links two ports by GUID + port IDs.

At edit time, Unity’s GraphView `Port` objects mirror these `PortData` definitions; at runtime, the `NodeGraphRunner` uses `ConnectionData` + port IDs to find next nodes.

---

## 3. Editor architecture

### 3.1 `NodeGraphEditorWindow`

File: `Assets/Scripts/NodeSystem/Editor/NodeGraphEditorWindow.cs`

- Main editor window for editing graphs.
- Responsibilities:
  - Hosts a `NodeGraphView` instance.
  - Lets you select a `NodeGraph` asset to edit.
  - Provides toolbar buttons (New, Save) and breadcrumb navigation.
  - Persists state across play mode (current graph path, view position, zoom) via `SessionState`.
  - Subscribes to runtime events (`NodeGraphRunner.OnNodeStarted`, etc.) to reflect execution in the editor.
  - On play mode change, refreshes inline content for all `NodeView`s.

### 3.2 `NodeGraphView`

File: `Assets/Scripts/NodeSystem/Editor/NodeGraphView.cs`

- Core GraphView implementation.
- Key responsibilities:
  - Loading/unloading a `NodeGraph` into editor elements (`LoadGraph`).
  - Creating/removing `NodeView` elements for each `NodeData`.
  - Creating custom **Doozy‑style edges** for connections.
  - Handling selection, deletion, and undo/redo.
  - Managing zoom and panning.
  - Integrating the search window (`NodeSearchWindow`) for node creation.
  - Handling “drag from port to empty space → show node menu” and auto‑connecting the new node.
  - Reacting to **play mode changes**:
    - Clearing runtime highlights on exiting play mode.
    - Safety clean‑up when returning to edit mode.
  - Reacting to **runtime events**:
    - `OnRuntimeNodeStarted` / `OnRuntimeNodeCompleted`
      - Updates node visual state (running / completed / failed).
      - Highlights outgoing edges (`Active` then `Executed`).
      - Controls traveling dots on edges to show “what will execute next”.
  - Keeps parallel execution visuals correct (multiple outgoing edges can be active at once).

### 3.3 `NodeView` and `NodeViewOdin`

File: `Assets/Scripts/NodeSystem/Editor/NodeView.cs`  
File: `Assets/Scripts/NodeSystem/Editor/NodeViewOdin.cs`

- Visual representation of a single `NodeData`.
- Responsible for:
  - Showing the node title, color, and optional `displayLabel`.
  - Creating ports using `Port.Create<DoozyStyleEdge>()`.
  - Hosting **inline content** (custom UI per node type) via the `NodeInlineContent` system.
  - Managing runtime visual state:
    - A glow element (`#node-glow`) that pulses when running.
    - Color/indicator changes for `Completed` and `Failed` (all unified to an electric blue glow, different accent colors for failed if needed).
  - Cleaning up glow/highlights when play mode stops (no highlights in edit mode).
- `NodeView.OnDataChanged` delegates back to `NodeGraphView`, which calls `Graph.SaveToJson()` and marks the graph dirty so edits persist.

### 3.4 Node inline content

Folder: `Assets/Scripts/NodeSystem/Editor/NodeInlineContent/`

- `NodeInlineContentBase` is a helper for building small UI blocks embedded inside nodes (sliders, text fields, toggles, etc.).
- Each node type can have a custom inline content class (e.g. `RandomBranchNodeInlineContent`, `LoopNodeInlineContent`).
- `NodeInlineContentFactory` maps `NodeData` types to these content providers.
- Inline content:
  - Reads/writes properties directly on the `NodeData` instance.
  - Calls `MarkDirty()` to trigger `OnDataChanged` → `SaveToJson()` when values change.
  - Can request a redraw when structure changes (e.g. after adding/removing connections).

### 3.5 Styles and UX (`NodeGraphStyles.uss`)

File: `Assets/Scripts/NodeSystem/Editor/NodeGraphStyles.uss`

- Defines the overall look & feel:
  - Node background colors, header styles.
  - Glow element `#node-glow` (size, border, animation look).
  - Blue unified glow for running/completed/failed nodes.
  - Custom hover/selection border using `#selection-border` with adjustable thickness.
- Provides class selectors like `.node-running`, `.node-completed`, `.node-failed`, `.edge-active`, `.edge-executed` that are driven by `NodeGraphView` at runtime.

### 3.6 Custom edges: `DoozyStyleEdge` / `DoozyStyleEdgeControl`

File: `Assets/Scripts/NodeSystem/Editor/DoozyStyleEdge.cs`

- Subclasses GraphView `Edge` and `EdgeControl` to emulate Doozy Nody’s style:
  - Smooth bezier curves with adjusted tangents.
  - Layered strokes (outline + main color).
  - Animated **traveling dot** that moves along the curve.
  - Different colors for normal, active, and executed runtime states.
- Handles hit‑testing manually so edges are easy to select and delete even with custom drawing.
- Runtime state is held in an enum (`Normal`, `Active`, `Executed`) on the edge control and updated directly by `NodeGraphView`.

---

## 4. Runtime execution (`NodeGraphRunner`)

File: `Assets/Scripts/NodeSystem/Core/NodeGraphRunner.cs`

`NodeGraphRunner` is a MonoBehaviour that executes a `NodeGraph` at runtime:

- Key responsibilities:
  - Loads a `NodeGraph` asset and keeps a reference.
  - Manages **active nodes** (`_activeNodeGuids`) and **execution path** for debugging / WaitForAll.
  - Starts execution from a configured entry node (commonly a `StartNode`).
  - For each node:
    - Creates a runtime `NodeData` instance (from the graph’s `_runtimeNodes`).
    - Sets `Runner`, `OnComplete`, and calls `Execute()`.
  - On node completion:
    - Updates active node tracking.
    - For most nodes, finds the next connected nodes and executes them.
    - Special‑cases synchronization/branching nodes (e.g., `RandomBranchNode`, `WaitForAllNode`) so they can control which next node(s) actually run.
  - Emits static events:
    - `OnNodeStarted(NodeGraphRunner, NodeData)`
    - `OnNodeCompleted(NodeGraphRunner, NodeData)`
    - `OnGraphStarted(NodeGraphRunner)`
    - `OnGraphEnded(NodeGraphRunner)`
    - These are consumed by editor code to animate the graph view at runtime.

Important details:

- **Parallel execution:** if a node has multiple outgoing connections, the runner can start all of them in parallel (except where special nodes decide otherwise).
- **RandomBranchNode integration:** when a `RandomBranchNode` completes, the runner checks `randomBranch.SelectedNodeGuid` and **only executes that chosen node**, not all outgoing connections.
- **WaitForAllNode integration:** runner treats `WaitForAllNode` as a synchronization point, avoiding re‑executing it if it’s already running or completed.

---

## 5. Built‑in nodes and behaviors

Below are the most important custom node types. (There are additional utility nodes like `StartNode`, `DelayNode`, etc., not all listed in detail.)

### 5.1 `StartNode`

- Entry point for a graph. The runner typically starts from this node.
- Output connects to the first action in the flow (e.g., Load Question).

### 5.2 `DebugLogNode` (renamed “Log Message”)

File: `Assets/Scripts/NodeSystem/Nodes/DebugLogNode.cs`

- Logs a configured message to the Unity console when executed.
- Category: `Utility`, name shown as “Log Message”.
- Simple tool for debugging execution paths and branches.

### 5.3 `DelayNode`

- Waits for a specified duration before calling `Complete()`.
- Implemented as a coroutine on the runner.

### 5.4 `WaitForAllNode` (synchronization)

File: `Assets/Scripts/NodeSystem/Nodes/WaitForAllNode.cs`

Concept: **“Wait until all upstream branches have completed, then continue.”**

- Ports:
  - Input: **one multi‑capacity input port** (`Inputs`) – unlimited upstream connections.
  - Output: standard single output for the continuation.
- Behavior:
  - On `Execute()`, it:
    - Scans the graph to find **all nodes connected** to its input port.
    - Subscribes to `NodeGraphRunner.OnNodeCompleted`.
    - Tracks which upstream nodes have completed (considering some may have already finished when the WaitForAll executes).
  - Once **all upstream nodes are complete**, it:
    - Unsubscribes from events.
    - Calls `Complete()` so the downstream chain can continue.
- Acts as a **barrier** when multiple branches converge.

### 5.5 `RandomBranchNode` (weighted random)

File: `Assets/Scripts/NodeSystem/Nodes/RandomBranchNode.cs`  
Inline UI: `RandomBranchNodeInlineContent.cs`

Concept: **“Pick exactly one of the connected branches, using weights.”**

- Ports:
  - Input: `Execute` (single).
  - Output: `Random Out` (multi‑capacity).
- Data:
  - `List<BranchWeight> weights` – each entry is `{ nodeGuid, weight }`.
  - `SelectedNodeGuid` (non‑serialized runtime field) – the chosen next node.
- Execution:
  1. Gets all nodes connected to `Random Out`.
  2. For each, retrieves its weight via `GetWeight(guid)` (defaults to 1 if missing).
  3. Sums all weights.
  4. If sum > 0:
     - Performs a weighted random roll and picks one node.
     - Stores its GUID in `SelectedNodeGuid`.
     - Logs which node was picked and its percentage.
  5. If sum == 0:
     - Falls back to uniform random across all connected nodes.
  6. Calls `Complete()`.
  7. The `NodeGraphRunner.OnNodeComplete` handler detects a `RandomBranchNode` and only executes the node whose GUID matches `SelectedNodeGuid`.

#### Inline UI for RandomBranchNode

File: `RandomBranchNodeInlineContent.cs`

- Dynamically inspects the graph to find all nodes connected to `Random Out`.
- For each branch:
  - Shows the target node’s name.
  - Shows a slider (0–10) for the **raw weight**.
  - Shows a label with the **percentage** of the total weight.
- When any slider changes:
  - Calls `SetWeight(guid, value)`.
  - Marks the graph dirty for saving.
  - Recomputes **all percentages** so they always sum to 100% (or 0 if all weights are zero).
- Graph edits (adding/removing connections) trigger refreshes of this inline UI so branch lists stay in sync.
- Weights are persisted in the graph asset and now survive entering/exiting play mode.

### 5.6 `LoopNode`

File: `Assets/Scripts/NodeSystem/Nodes/LoopNode.cs`  
Inline UI: `LoopNodeInlineContent.cs`

Concept: **“Run the loop body multiple times.”**

- Ports:
  - Input: `Execute` (single).
  - Outputs:
    - `Loop Body` (`loop`): the body to repeat (currently uses only the **first** connected node).
    - `Done` (`done`): intended as “after loop completes” (not wired yet in current implementation).
- Loop modes (enum `LoopType`):
  - `Count`: run the body `loopCount` times.
  - `Condition`: while a graph variable equals `conditionValue`, run body again.
  - `Infinite`: run body forever until the runner stops the graph.
- Implementation:
  - Uses runner coroutines (`LoopByCount`, `LoopByCondition`, `LoopInfinite`).
  - In each iteration, sets `_currentIteration`, logs debug info, executes first `Loop Body` node, and waits until it completes.
  - Calls `Complete()` when the loop condition ends (except infinite).

### 5.7 Math utility nodes

These nodes live in `Assets/Scripts/NodeSystem/Nodes/` and provide reusable math and randomization building blocks. All of them:

- Have a single `Execute` input and a single `Next` output.
- Read/write **graph variables** via `NodeGraph.GetVariable` / `GetOrCreateVariable`.
- Can be combined with other nodes (e.g. `RandomBranchNode`, `WaitForAllNode`) to build more advanced logic.

#### 5.7.1 `MathOperationNode`

File: `Assets/Scripts/NodeSystem/Nodes/MathOperationNode.cs`

- Concept: **“Compute A (op) B and store it in a variable.”**
- Configuration:
  - `variableA`: name of the input variable \(A\) (float or int stored as float).
  - `operation`: `Add`, `Subtract`, `Multiply`, `Divide`, `Modulo`.
  - `operandType`:
    - `Constant`: use `constantValue` as B.
    - `Variable`: read B from `variableB`.
  - `resultVariable`: name of the variable to store the result in.
- Behavior:
  - Reads `variableA` (required) and either `constantValue` or `variableB`.
  - Applies the selected operation, with safety checks for division/modulo by zero.
  - Writes the result into `resultVariable`:
    - If `resultVariable` already exists and is **Int**, writes a rounded int.
    - Otherwise writes a **Float**.
  - Logs the full expression for debugging, then continues to `Next`.

#### 5.7.2 `RandomFloatNode`

File: `Assets/Scripts/NodeSystem/Nodes/RandomFloatNode.cs`

- Concept: **“Generate a random float in a range and store it in a variable.”**
- Configuration:
  - `variableName`: target variable name (required).
  - `minValue`, `maxValue`: inclusive range passed to `Random.Range(min, max)`.
- Behavior:
  - On `Execute`, generates a random float in \[minValue, maxValue\].
  - Stores it as a **Float** in `variableName` (creating the variable if needed).
  - Logs the generated value and range, then continues to `Next`.

#### 5.7.3 `RandomIntNode`

File: `Assets/Scripts/NodeSystem/Nodes/RandomIntNode.cs`

- Concept: **“Generate a random integer in a range and store it in a variable.”**
- Configuration:
  - `variableName`: target variable name (required).
  - `minValue`, `maxValue`: inclusive integer range.
- Behavior:
  - On `Execute`, generates a random int using `Random.Range(minValue, maxValue + 1)` so `maxValue` is **included**.
  - Stores it as an **Int** in `variableName` (creating the variable if needed).
  - Logs the generated value and range, then continues to `Next`.

### 5.8 Quiz‑specific nodes (examples)

There are domain‑specific nodes used by the quiz system, such as:

- `LoadQuestionNode` – loads the next quiz question.
- `ShowQuestionNode` – displays a question to the user.
- Other nodes handle answer checking, scoring, transitions, etc.

These use the same `NodeData` / `NodeGraphRunner` infrastructure; the differences are in their `OnExecute()` logic and which ports they expose.

---

## 6. Visual runtime feedback

The graph editor visualizes runtime execution to make debugging and UX better:

- **Node glow (blue)**
  - Running nodes pulse with a stronger blue glow.
  - Completed / failed nodes also use blue glow (for consistent look), with other UI accents reflecting state.
  - Glows and highlights are cleared when play mode stops (no runtime state in edit mode).

- **Edges**
  - **Active** edges (from currently running nodes) are cyan and show a moving dot traveling from source to target, indicating “this will execute next”.
  - **Executed** edges (already traversed) are typically green (or a distinct color) and do not show traveling dots.
  - Selection and hover styles highlight edges for editing without confusing runtime state.

- **Parallel branches**
  - If a node has multiple outgoing connections, multiple edges may become **Active** at once; each displays its own traveling dot to show all parallel paths.
  - `WaitForAllNode` and `RandomBranchNode` modify this behavior as described above.

---

## 7. Extending the system (how to add a new node)

To add a new node type:

1. **Create a `NodeData` subclass**
   - In `Assets/Scripts/NodeSystem/Nodes/`:
     - Inherit from `NodeData`.
     - Override `Name`, `Color`, `Category`.
     - Implement `GetInputPorts()` / `GetOutputPorts()`.
     - Implement `OnExecute()` and call `Complete()` when done.

2. **Optional: Runtime integration**
   - If your node changes how the runner moves to next nodes (like RandomBranch/WaitForAll), update `NodeGraphRunner.OnNodeComplete` or `ExecuteNodeInternal` logic accordingly.

3. **Optional: Inline editor UI**
   - Add a `YourNodeInlineContent` in `Editor/NodeInlineContent/` inheriting from `NodeInlineContentBase`.
   - Implement `Draw()` using helper methods (`CreateTextField`, `CreateSlider`, etc.).
   - Register it in `NodeInlineContentFactory` so it appears in the node.

4. **Styling**
   - If needed, adjust `NodeGraphStyles.uss` (but most nodes are styled generically).

Once this is done, your node will appear in the node search window and can be used in any `NodeGraph`.

---

## 8. Summary

- **NodeGraph + NodeData** provide the **data model**.
- **NodeGraphRunner** provides the **execution engine**.
- **NodeGraphEditorWindow + NodeGraphView + NodeView** provide the **visual editor** and **runtime visualization**.
- Custom nodes like **RandomBranch**, **WaitForAll**, and **Loop** implement higher‑level flow control on top of this.

This file is intended as a stable, shareable reference so future conversations or contributors can quickly understand how the system fits together without re‑explaining the whole architecture.

