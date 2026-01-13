# Guide: Creating Custom Nodes

This guide will walk you through creating your own custom nodes for the Node Graph System.

## Table of Contents
1. [Basic Node Structure](#basic-node-structure)
2. [Step-by-Step Guide](#step-by-step-guide)
3. [Node Types & Examples](#node-types--examples)
4. [Ports & Connections](#ports--connections)
5. [Custom Inline Content (UI)](#custom-inline-content-ui)
6. [Registration](#registration)
7. [Best Practices](#best-practices)

---

## Basic Node Structure

Every custom node must:
- Inherit from `NodeData`
- Be marked with `[Serializable]`
- Override required properties: `Name`, `Color`, `Category`
- Override port methods: `GetInputPorts()`, `GetOutputPorts()`
- Override execution method: `OnExecute()`
- Call `Complete()` when finished

---

## Step-by-Step Guide

### Step 1: Create the Node Class

Create a new C# file in `Assets/Scripts/NodeSystem/Nodes/`:

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    [Serializable]
    public class MyCustomNode : NodeData
    {
        // Your node properties here
        [SerializeField]
        public string myProperty = "Default Value";

        // Required: Node display name
        public override string Name => "My Custom Node";

        // Required: Node color (RGB 0-1)
        public override Color Color => new Color(0.5f, 0.7f, 0.9f); // Light blue

        // Required: Node category (for search menu)
        public override string Category => "Custom";

        // Required: Define input ports
        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input)
            };
        }

        // Required: Define output ports
        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "Next", PortDirection.Output)
            };
        }

        // Required: Execution logic
        protected override void OnExecute()
        {
            // Your logic here
            Debug.Log($"[MyCustomNode] Executing with property: {myProperty}");
            
            // Always call Complete() when done!
            Complete();
        }
    }
}
```

### Step 2: Define Your Properties

Add serialized fields for any data your node needs:

```csharp
[SerializeField]
public string textValue = "";

[SerializeField]
public int numberValue = 0;

[SerializeField]
public bool toggleValue = false;

[SerializeField]
public GameObject targetObject; // For object references
```

### Step 3: Implement Execution Logic

The `OnExecute()` method is called when the node runs:

```csharp
protected override void OnExecute()
{
    // Synchronous execution (completes immediately)
    DoSomething();
    Complete();
}
```

For async operations (delays, coroutines), use the Runner:

```csharp
protected override void OnExecute()
{
    // Async execution (completes later)
    Runner?.StartCoroutine(DoSomethingAsync());
}

private IEnumerator DoSomethingAsync()
{
    yield return new WaitForSeconds(2f);
    Complete();
}
```

### Step 4: (Optional) Create Custom Inline Content

If you want custom UI inside the node, create an inline content class:

```csharp
#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class MyCustomNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as MyCustomNode;
            if (node == null) return;

            // Use helper methods to create UI fields
            CreateTextField(node.textValue, v => node.textValue = v, "Enter text...");
            CreateIntField("Number", node.numberValue, v => node.numberValue = v);
            CreateToggle("Enable", node.toggleValue, v => node.toggleValue = v);
            CreateObjectField<GameObject>("Target", node.targetObject, go => node.targetObject = go);
        }
    }
}
#endif
```

### Step 5: Register Custom Inline Content (if created)

In `Assets/Scripts/NodeSystem/Editor/NodeInlineContent/NodeInlineContentFactory.cs`, add:

```csharp
// In the static constructor
Register<Nodes.MyCustomNode, MyCustomNodeInlineContent>();
```

**That's it!** Your node will automatically appear in the node creation menu.

---

## Node Types & Examples

### Example 1: Simple Synchronous Node

```csharp
[Serializable]
public class SimpleNode : NodeData
{
    [SerializeField]
    public string message = "Hello";

    public override string Name => "Simple";
    public override Color Color => new Color(0.8f, 0.8f, 0.8f);
    public override string Category => "Custom";

    public override List<PortData> GetInputPorts()
    {
        return new List<PortData>
        {
            new PortData("input", "Execute", PortDirection.Input)
        };
    }

    public override List<PortData> GetOutputPorts()
    {
        return new List<PortData>
        {
            new PortData("output", "Next", PortDirection.Output)
        };
    }

    protected override void OnExecute()
    {
        Debug.Log($"[SimpleNode] {message}");
        Complete(); // Complete immediately
    }
}
```

### Example 2: Async Node (Coroutine)

```csharp
[Serializable]
public class AsyncNode : NodeData
{
    [SerializeField]
    public float waitTime = 2f;

    public override string Name => "Async Wait";
    public override Color Color => new Color(0.6f, 0.8f, 0.6f);
    public override string Category => "Custom";

    public override List<PortData> GetInputPorts()
    {
        return new List<PortData>
        {
            new PortData("input", "Execute", PortDirection.Input)
        };
    }

    public override List<PortData> GetOutputPorts()
    {
        return new List<PortData>
        {
            new PortData("output", "Done", PortDirection.Output)
        };
    }

    protected override void OnExecute()
    {
        // Start coroutine - Complete() will be called later
        Runner?.StartCoroutine(WaitCoroutine());
    }

    private IEnumerator WaitCoroutine()
    {
        yield return new WaitForSeconds(waitTime);
        Debug.Log("[AsyncNode] Wait complete!");
        Complete(); // Complete after delay
    }
}
```

### Example 3: Node with Multiple Outputs (Branching)

```csharp
[Serializable]
public class BranchNode : NodeData
{
    [SerializeField]
    public bool condition = true;

    public override string Name => "Branch";
    public override Color Color => new Color(0.9f, 0.7f, 0.5f);
    public override string Category => "Flow";

    public override List<PortData> GetInputPorts()
    {
        return new List<PortData>
        {
            new PortData("input", "Execute", PortDirection.Input)
        };
    }

    public override List<PortData> GetOutputPorts()
    {
        return new List<PortData>
        {
            new PortData("true", "True", PortDirection.Output),
            new PortData("false", "False", PortDirection.Output)
        };
    }

    protected override void OnExecute()
    {
        // Set state to indicate which branch to take
        // The runner will use the appropriate output port based on state
        State = condition ? NodeState.Completed : NodeState.Failed;
        Complete();
    }
}
```

**Note:** For branching nodes, you need special handling in `NodeGraphRunner.OnNodeComplete()`:

```csharp
// In NodeGraphRunner.cs, OnNodeComplete() method
if (completedNode is Nodes.BranchNode)
{
    outputPort = completedNode.State == NodeState.Completed ? "true" : "false";
}
```

### Example 4: Node with Visual Feedback

```csharp
protected override void OnExecute()
{
    // Fire visual event for editor feedback
    NodeGraphRunner.BroadcastNodeStarted(Runner, this);
    
    // Do your work
    DoWork();
    
    // Fire completion event
    NodeGraphRunner.BroadcastNodeCompleted(Runner, this);
    
    Complete();
}
```

---

## Ports & Connections

### Port IDs

- **Input ports**: Usually "input" or "execute"
- **Output ports**: Usually "output", "next", "done", "complete"
- **Branching**: Use descriptive names like "true", "false", "success", "error"

### Port Capacity

```csharp
// Single connection (default)
new PortData("output", "Next", PortDirection.Output, PortCapacity.Single)

// Multiple connections allowed
new PortData("output", "Broadcast", PortDirection.Output, PortCapacity.Multi)
```

### Multiple Inputs

```csharp
public override List<PortData> GetInputPorts()
{
    return new List<PortData>
    {
        new PortData("input1", "Input A", PortDirection.Input),
        new PortData("input2", "Input B", PortDirection.Input)
    };
}
```

### Dynamic Ports (Advanced)

For nodes like SequenceNode that add ports dynamically:

```csharp
[SerializeField]
private List<string> _dynamicPorts = new List<string>();

public override List<PortData> GetOutputPorts()
{
    var ports = new List<PortData>();
    
    // Add dynamic ports
    foreach (var portId in _dynamicPorts)
    {
        ports.Add(new PortData(portId, $"Step {portId}", PortDirection.Output));
    }
    
    // Add static ports
    ports.Add(new PortData("done", "Done", PortDirection.Output));
    
    return ports;
}
```

---

## Custom Inline Content (UI)

### Available Helper Methods

```csharp
// Text field
CreateTextField(value, onChanged, placeholder);

// Number fields
CreateIntField("Label", value, onChanged);
CreateFloatField("Label", value, onChanged);

// Boolean
CreateToggle("Label", value, onChanged);

// Enum dropdown
CreateEnumField("Label", enumValue, onChanged);

// Object reference
CreateObjectField<GameObject>("Label", objectValue, onChanged);

// Slider
CreateSlider("Label", value, min, max, onChanged);
```

### Adaptive UI Example

```csharp
public override void Draw()
{
    var node = Node as MyNode;
    if (node == null) return;

    // Show different fields based on type
    CreateEnumField("Type", node.myType, v => 
    {
        node.myType = v;
        MarkDirty();
        RequestRefresh(); // Refresh UI when type changes
    });

    // Conditional fields
    if (node.myType == MyType.OptionA)
    {
        CreateTextField(node.textValue, v => node.textValue = v);
    }
    else if (node.myType == MyType.OptionB)
    {
        CreateIntField("Count", node.countValue, v => node.countValue = v);
    }
}
```

### Full Example: Custom Inline Content

```csharp
#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class MyCustomNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as MyCustomNode;
            if (node == null) return;

            // Text input
            CreateTextField(node.message, v => 
            {
                node.message = v;
                MarkDirty(); // Mark for save
            }, "Enter message...");

            // Number with label
            CreateIntField("Count", node.count, v => 
            {
                node.count = Mathf.Max(0, v); // Clamp value
                MarkDirty();
            });

            // Toggle
            CreateToggle("Enabled", node.enabled, v => 
            {
                node.enabled = v;
                MarkDirty();
            });

            // Enum dropdown
            CreateEnumField("Mode", node.mode, (MyMode v) => 
            {
                node.mode = v;
                MarkDirty();
                RequestRefresh(); // Refresh if UI needs to change
            });

            // GameObject picker
            CreateObjectField<GameObject>("Target", node.target, go => 
            {
                node.target = go;
                MarkDirty();
            });

            // Float with slider
            CreateSlider("Speed", node.speed, 0f, 10f, v => 
            {
                node.speed = v;
                MarkDirty();
            });
        }
    }
}
#endif
```

---

## Registration

### Register Custom Inline Content

In `NodeInlineContentFactory.cs`, add to the static constructor:

```csharp
// Register your custom inline content
Register<Nodes.MyCustomNode, MyCustomNodeInlineContent>();
```

### Exclude from Inline Content

If your node has no editable properties (like StartNode):

```csharp
// In NodeInlineContentFactory.cs
_excludedTypes.Add(typeof(Nodes.MySimpleFlowNode));
```

---

## Best Practices

### 1. Always Call Complete()

```csharp
protected override void OnExecute()
{
    try
    {
        DoWork();
        Complete(); // ✅ Always call this!
    }
    catch (Exception e)
    {
        Debug.LogError($"[MyNode] Error: {e.Message}");
        Complete(); // ✅ Even on error, call Complete()
    }
}
```

### 2. Use Visual Events for Editor Feedback

```csharp
protected override void OnExecute()
{
    // Fire start event (node turns orange)
    NodeGraphRunner.BroadcastNodeStarted(Runner, this);
    
    DoWork();
    
    // Fire complete event (node turns green)
    NodeGraphRunner.BroadcastNodeCompleted(Runner, this);
    
    Complete();
}
```

### 3. Handle Null Runner

```csharp
protected override void OnExecute()
{
    if (Runner == null)
    {
        Debug.LogError("[MyNode] No runner assigned!");
        Complete();
        return;
    }
    
    Runner.StartCoroutine(MyCoroutine());
}
```

### 4. Use MarkDirty() in Inline Content

```csharp
CreateTextField(node.value, v => 
{
    node.value = v;
    MarkDirty(); // ✅ Mark for save
});
```

### 5. Use RequestRefresh() for Adaptive UI

```csharp
CreateEnumField("Type", node.type, v => 
{
    node.type = v;
    MarkDirty();
    RequestRefresh(); // ✅ Refresh UI when type changes
});
```

### 6. Validate Inputs

```csharp
CreateIntField("Count", node.count, v => 
{
    node.count = Mathf.Max(0, v); // ✅ Clamp to valid range
    MarkDirty();
});
```

### 7. Use Descriptive Port Names

```csharp
// ✅ Good
new PortData("output", "On Success", PortDirection.Output)
new PortData("error", "On Error", PortDirection.Output)

// ❌ Bad
new PortData("out1", "Out", PortDirection.Output)
```

### 8. Store Runtime-Only Data as NonSerialized

```csharp
[NonSerialized]
private bool _isRunning; // ✅ Won't be saved

[SerializeField]
private float duration; // ✅ Will be saved
```

---

## Common Patterns

### Pattern 1: Wait for Condition

```csharp
protected override void OnExecute()
{
    Runner?.StartCoroutine(WaitForCondition());
}

private IEnumerator WaitForCondition()
{
    while (!SomeCondition())
    {
        yield return null;
    }
    Complete();
}
```

### Pattern 2: Execute Multiple Times

```csharp
protected override void OnExecute()
{
    Runner?.StartCoroutine(ExecuteLoop());
}

private IEnumerator ExecuteLoop()
{
    for (int i = 0; i < loopCount; i++)
    {
        DoWork();
        yield return new WaitForSeconds(interval);
    }
    Complete();
}
```

### Pattern 3: Get Connected Nodes

```csharp
protected override void OnExecute()
{
    // Get nodes connected to a specific output port
    var connectedNodes = Runner.Graph.GetConnectedNodes(Guid, "output");
    
    foreach (var node in connectedNodes)
    {
        // Do something with connected nodes
    }
    
    Complete();
}
```

---

## Testing Your Node

1. **Create the node class** in `Assets/Scripts/NodeSystem/Nodes/`
2. **Create inline content** (if needed) in `Assets/Scripts/NodeSystem/Editor/NodeInlineContent/`
3. **Register inline content** in `NodeInlineContentFactory.cs`
4. **Open the Node Graph Editor**
5. **Right-click** in the graph view
6. **Search for your node** by name or category
7. **Test execution** in play mode

---

## Troubleshooting

### Node doesn't appear in menu
- Check namespace: Should be `NodeSystem.Nodes`
- Check inheritance: Must inherit from `NodeData`
- Check serialization: Must have `[Serializable]` attribute

### Node doesn't execute
- Check if `Complete()` is called
- Check if `Runner` is assigned
- Check console for errors

### Properties not saving
- Ensure fields are `[SerializeField]`
- Call `MarkDirty()` in inline content callbacks
- Check if graph is being saved

### Visual state not updating
- Fire `BroadcastNodeStarted()` and `BroadcastNodeCompleted()` events
- Use `schedule.Execute()` for UI updates if needed

---

## Quick Reference

### Required Overrides
- `Name` → Display name
- `Color` → Node color (RGB 0-1)
- `Category` → Menu category
- `GetInputPorts()` → Input port definitions
- `GetOutputPorts()` → Output port definitions
- `OnExecute()` → Execution logic

### Important Methods
- `Complete()` → Mark node as complete
- `Runner` → Access to coroutines and graph
- `State` → Set node state (for branching)
- `MarkDirty()` → Mark data for save (in inline content)
- `RequestRefresh()` → Refresh UI (in inline content)

### Important Events
- `NodeGraphRunner.BroadcastNodeStarted()` → Fire start event
- `NodeGraphRunner.BroadcastNodeCompleted()` → Fire complete event

---

## Example: Complete Custom Node

Here's a complete example combining everything:

**MyCustomNode.cs:**
```csharp
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NodeSystem.Nodes
{
    [Serializable]
    public class MyCustomNode : NodeData
    {
        [SerializeField]
        public string message = "Hello World";
        
        [SerializeField]
        public float delay = 1f;
        
        [SerializeField]
        public bool showDebug = true;

        public override string Name => "My Custom Node";
        public override Color Color => new Color(0.5f, 0.7f, 0.9f);
        public override string Category => "Custom";

        public override List<PortData> GetInputPorts()
        {
            return new List<PortData>
            {
                new PortData("input", "Execute", PortDirection.Input)
            };
        }

        public override List<PortData> GetOutputPorts()
        {
            return new List<PortData>
            {
                new PortData("output", "Next", PortDirection.Output)
            };
        }

        protected override void OnExecute()
        {
            if (Runner == null)
            {
                Debug.LogError("[MyCustomNode] No runner!");
                Complete();
                return;
            }

            // Fire visual events
            NodeGraphRunner.BroadcastNodeStarted(Runner, this);
            
            // Start async operation
            Runner.StartCoroutine(ExecuteAsync());
        }

        private IEnumerator ExecuteAsync()
        {
            if (showDebug)
            {
                Debug.Log($"[MyCustomNode] {message}");
            }

            yield return new WaitForSeconds(delay);

            // Fire completion event
            NodeGraphRunner.BroadcastNodeCompleted(Runner, this);
            
            Complete();
        }
    }
}
```

**MyCustomNodeInlineContent.cs:**
```csharp
#if UNITY_EDITOR
using UnityEngine;
using NodeSystem.Nodes;

namespace NodeSystem.Editor
{
    public class MyCustomNodeInlineContent : NodeInlineContentBase
    {
        public override void Draw()
        {
            var node = Node as MyCustomNode;
            if (node == null) return;

            CreateTextField(node.message, v => 
            {
                node.message = v;
                MarkDirty();
            }, "Enter message...");

            CreateFloatField("Delay", node.delay, v => 
            {
                node.delay = Mathf.Max(0, v);
                MarkDirty();
            });

            CreateToggle("Show Debug", node.showDebug, v => 
            {
                node.showDebug = v;
                MarkDirty();
            });
        }
    }
}
#endif
```

**Register in NodeInlineContentFactory.cs:**
```csharp
Register<Nodes.MyCustomNode, MyCustomNodeInlineContent>();
```

---

That's everything you need to create custom nodes! Happy coding! 🚀

