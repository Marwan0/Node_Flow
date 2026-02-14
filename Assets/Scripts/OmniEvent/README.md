# OmniEvent Inspector - User Guide

## Overview

The OmniEvent Inspector is an enhanced editor for configuring OmniEvent fields in Unity. It provides:

- **Static/Dynamic Arguments**: Configure fixed values or live references to method parameters
- **Enhanced Type Support**: Better UI for Lists, Arrays, Enums, and complex types
- **Visual Feedback**: See argument types at a glance with icons and labels
- **Event Reordering**: Drag to reorder event listeners (via Unity's built-in reorderable list)
- **Advanced Inspector Window**: Full-featured window for detailed event configuration

## Quick Start

### 1. Using OmniEvent in Inspector

```csharp
using UnityEngine;
using OmniEvent;

public class MyScript : MonoBehaviour
{
    public OmniEvent<int> onValueChanged = new OmniEvent<int>();
    public OmniEvent<Vector3, Color> onTransformEvent = new OmniEvent<Vector3, Color>();
    public OmniEvent<List<string>> onItemsUpdated = new OmniEvent<List<string>>();
}
```

### 2. Opening the OmniEvent Inspector

There are three ways to open the OmniEvent Inspector:

#### Method 1: Via Toolbar Button
- Select a GameObject with OmniEvent fields
- Click the "OmniEvent Tools" toolbar at the bottom of the inspector
- Click "Open OmniEvent Inspector"

#### Method 2: Via Property Button
- Look for the ⚙ button next to any OmniEvent field in the inspector
- Click it to open the advanced inspector for that specific event

#### Method 3: Via Menu
- Go to `Window > OmniEvent Inspector`

## Features

### Static vs Dynamic Arguments

OmniEvent supports two modes for passing arguments:

#### Static Arguments (■)
Static arguments use fixed values that are set in the inspector:

```
■ int: 42
■ string: "Hello World"
■ Vector3: (1, 2, 3)
```

**Use when:** You want to pass the same value every time the event fires.

#### Dynamic Arguments (●)
Dynamic arguments use references to live objects/properties:

```
● int: [EnemyController.currentHealth]
● string: [GameManager.playerName]
● Vector3: [transform.position]
```

**Use when:** You want to pass the current value from an object or component at runtime.

### Enhanced Type Support

The OmniEvent Inspector provides enhanced UI for:

| Type | Enhancement |
|------|-------------|
| `Enum` | Dropdown menu with all enum values |
| `Vector2/3/4` | Expanded X, Y, Z, W fields |
| `Color` | Color picker with RGBA fields |
| `Quaternion` | Euler angle editor |
| `LayerMask` | Layer mask editor |
| `List<T>` | Expandable list with size control |
| `T[]` | Array editor with index indicators |

### Event Reordering

The UnityEvent system (which OmniEvent wraps) includes a reorderable list. To reorder event listeners:

1. Open the OmniEvent Inspector window
2. Click on a listener event call
3. Use the ↑↓ buttons to reorder

**Note:** Reordering affects the order in which listeners are invoked when the event fires.

### Visual Indicators

The inspector uses visual cues to help you understand your event configuration:

- **● Blue dot**: Dynamic argument (using a reference)
- **■ Gray square**: Static argument (using a fixed value)
- **Type icons**: Each argument type shows an appropriate icon
- **Argument info**: Shows all argument types below the event field

## Inspector Window

The OmniEvent Inspector Window provides advanced features:

### Toolbar
- **Advanced Toggle**: Show/hide advanced options
- **Refresh**: Reload event data from the property

### Event List
Shows all configured event listeners with:
- Target object reference
- Method name
- Arguments with types
- Reordering buttons (↑↓)
- Remove button (✕)

### Advanced Options
When enabled, shows:
- Static value editors for each argument
- Persistent call count information
- Additional metadata

## Examples

### Example 1: Static Integer Event

```csharp
public OmniEvent<int> onScoreChanged = new OmniEvent<int>();

public void AddScore(int points)
{
    score += points;
    onScoreChanged.Invoke(score);
}

public void ShowScore(int score)
{
    scoreText.text = $"Score: {score}";
}
```

**Inspector Setup:**
1. Add listener to `onScoreChanged`
2. Select `ShowScore` method
3. Set int parameter to `0` (static - will be replaced by the actual value when invoked)
4. The event will pass the actual score value at runtime

### Example 2: Dynamic Transform Event

```csharp
public OmniEvent<Vector3> onTeleport = new OmniEvent<Vector3>();

public void TeleportTo(Vector3 position)
{
    transform.position = position;
}
```

**Inspector Setup:**
1. Add listener to `onTeleport`
2. Select `TeleportTo` method
3. For the Vector3 parameter, click the object selector circle
4. Choose `TeleportPoint > transform > position` (dynamic reference)
5. The event will use the current position of TeleportPoint at runtime

### Example 3: Multi-Parameter Event with Complex Types

```csharp
public OmniEvent<Color, List<string>, float> onEffectTriggered = 
    new OmniEvent<Color, List<string>, float>();

public void PlayEffect(Color color, List<string> tags, float duration)
{
    particleSystem.startColor = color;
    effectTags = new List<string>(tags);
    effectDuration = duration;
    particleSystem.Play();
}
```

**Inspector Setup:**
1. Add listener to `onEffectTriggered`
2. Select `PlayEffect` method
3. Configure parameters:
   - `Color`: Use color picker (static) or reference to Renderer.material.color (dynamic)
   - `List<string>`: Set list size and add string items
   - `float`: Set duration value
4. Each parameter can be static or dynamic independently

## Best Practices

### When to Use Static Arguments
- Configuration values (constants, thresholds, multipliers)
- Default values that rarely change
- Test values for prototyping

### When to Use Dynamic Arguments
- Values that change at runtime
- References to other components
- Data from game state or managers

### Performance Considerations
- Static arguments are slightly faster (no reference resolution)
- Dynamic arguments add minimal overhead for reference lookups
- Both approaches are optimized for Unity's event system

## Tips & Tricks

### 1. Debug Argument Types
Enable "Advanced" mode to see the full type names and structure of your arguments.

### 2. Reuse Event Configurations
Copy-paste listener configurations between OmniEvent fields with matching signatures.

### 3. Preview Argument Values
When using dynamic arguments, you can see the current values in Play mode by watching the referenced objects.

### 4. Use the Inspector Button
The ⚙ button next to each OmniEvent field provides quick access to the advanced inspector.

### 5. Batch Configure
Use the OmniEvent Inspector window to configure multiple events at once for a component.

## Troubleshooting

### Issue: Arguments Not Passing Correctly
**Solution**: 
- Check if the argument types match between the event and the method
- Verify that dynamic references point to valid objects
- Ensure the method signature matches the event's generic parameters

### Issue: Inspector Shows "Unable to load OmniEvent"
**Solution**:
- Make sure you're using OmniEvent<T> or one of its variants
- Check that the field is public or has [SerializeField]
- Verify the OmniEvent namespace is properly imported

### Issue: Dynamic Arguments Show Wrong Values
**Solution**:
- Ensure the referenced object still exists
- Check that the property path is correct (e.g., `transform.position` not `transform`)
- Try refreshing the inspector window

## API Reference

### Core Classes

#### `OmniEvent`
Base event class with no parameters.

#### `OmniEvent<T>`
Event with one parameter.

#### `OmniEvent<T1, T2>`
Event with two parameters.

#### `OmniEvent<T1, T2, T3>`
Event with three parameters.

#### `OmniEvent<T1, T2, T3, T4>`
Event with four parameters.

### Editor Classes

#### `OmniEventPropertyDrawer`
Property drawer that renders OmniEvent fields with enhanced UI.

#### `OmniEventInspectorWindow`
Advanced inspector window for detailed event configuration.

#### `OmniEventInspectorHelper`
Utility class for type detection and formatting.

#### `OmniEventInspectorButton`
Adds quick-access buttons to OmniEvent fields.

## Compatibility

- **Unity Version**: 2019.4 LTS or higher
- **Render Pipelines**: Built-in, URP, HDRP
- **UI Toolkit**: Supported (for future UI Toolkit-based inspector)

## Future Enhancements

Planned features for future versions:
- [ ] Conditional event execution (only fire under certain conditions)
- [ ] Event profiling and performance metrics
- [ ] Event visualization graphs
- [ ] Batch event configuration
- [ ] Event templates and presets
- [ ] Integration with Unity's new Input System

## License

This OmniEvent Inspector is part of your Quiz Master project.
