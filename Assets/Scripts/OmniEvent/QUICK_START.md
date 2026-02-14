# OmniEvent Inspector - Quick Start Guide

## What I've Created

I've built an enhanced inspector system for your OmniEvent with the following features:

### ✅ Created Files

1. **OmniEventInspectorHelper.cs** - Utility functions for type detection and formatting
2. **OmniEventPropertyDrawer.cs** - Main property drawer with enhanced UI
3. **OmniEventInspectorWindow.cs** - Advanced inspector window for detailed configuration
4. **OmniEventInspectorButton.cs** - Quick-access buttons (optional, currently disabled to avoid conflicts)
5. **README.md** - Complete user guide and documentation
6. **OmniEventInspectorDemo.cs** - Demo script showcasing all features

### ✅ Modified Files

1. **OmniEventDrawer.cs** - Renamed to `OmniEventDrawerInternal` (used internally)

## Key Features

### 1. Static/Dynamic Arguments
- **■ Gray square**: Static value (fixed number, string, color, etc.)
- **● Blue dot**: Dynamic reference (live object/property)

### 2. Enhanced Type Support
Better UI for:
- `Enum` - Dropdown menus
- `Vector2/3/4` - Expanded X, Y, Z, W fields
- `Color` - Color picker
- `List<T>` - Expandable list editor
- `T[]` - Array editor
- And more!

### 3. Visual Feedback
- Type icons for each argument
- Argument type information displayed below events
- Clear distinction between static and dynamic arguments

### 4. Advanced Inspector Window
Access via `Window > OmniEvent Inspector` menu
- View all event listeners
- Reorder events with ↑↓ buttons
- Advanced options for detailed configuration
- Static value editors

## How to Use

### Step 1: Try the Demo

1. Create a GameObject in your scene
2. Add the `OmniEventInspectorDemo` component
3. You'll see various OmniEvent fields configured for testing

### Step 2: Configure an Event in Inspector

1. In the Inspector, find any OmniEvent field (e.g., "On Score Changed")
2. Click the `+` button to add a listener
3. Drag the same GameObject into the object field
4. Select a method from the dropdown (e.g., "HandleScore")
5. You'll see parameter fields where you can:
   - Set a static value (e.g., type "100")
   - Or click the object selector to use a dynamic reference

### Step 3: Test in Play Mode

1. Enter Play mode
2. Check one of the test checkboxes (e.g., "Trigger Score")
3. Watch the Console for output

### Step 4: Open Advanced Inspector

1. Go to `Window > OmniEvent Inspector`
2. The window will show detailed information about configured events
3. You can reorder events with ↑↓ buttons
4. Enable "Advanced" toggle for more options

## Inspector Features Explained

### Static vs Dynamic Arguments

**Static Argument Example:**
```
On Score Changed Event
├─ HandleScore(GameObject)
│   └─ ■ int: 100  [Fixed value - always 100]
```

**Dynamic Argument Example:**
```
On Position Changed Event
├─ MoveToPosition(GameObject)
│   └─ ● Vector3: [Player.transform.position]  [Live reference - follows player]
```

### Type Enhancements

**Enum Support:**
- Dropdown menu shows all enum values
- Easy to switch between states

**List Support:**
- Set list size
- Add/remove elements
- Each element has its own type-specific editor

**Complex Types:**
- Vector3: X, Y, Z fields
- Color: Color picker with RGBA
- Quaternion: Euler angle editor

## Tips

1. **Quick Access**: Use the ⚙ button next to OmniEvent fields (if you enable it in OmniEventInspectorButton.cs)

2. **Visual Feedback**: Look for the argument type info below each OmniEvent field:
   ```
   Arguments: ■ int, ● Vector3, ■ Color
   ```

3. **Reordering**: Use the Advanced Inspector Window to reorder event listeners. This affects the order they're invoked.

4. **Dynamic References**: When using dynamic arguments, make sure the referenced object exists in the scene.

5. **Debugging**: Enable "Advanced" mode in the inspector window to see full type information.

## Optional: Enable Quick-Access Buttons

To add a ⚙ button next to each OmniEvent field:

1. Open `OmniEventInspectorButton.cs`
2. Uncomment this line:
   ```csharp
   [CustomPropertyDrawer(typeof(OmniEventBase), isForChildClasses: true)]
   ```
3. Uncomment this line too:
   ```csharp
   [CanEditMultipleObjects]
   [CustomEditor(typeof(MonoBehaviour), true)]
   ```

This will add:
- ⚙ button next to each OmniEvent field
- "OmniEvent Tools" toolbar at the bottom of the inspector for components with OmniEvent fields

## Next Steps

1. ✅ Test the demo script in your scene
2. ✅ Configure some events with both static and dynamic arguments
3. ✅ Try the advanced inspector window
4. ✅ Add OmniEvent to your own components
5. ✅ Refer to `README.md` for complete documentation

## What's Supported

| Feature | Status |
|---------|--------|
| 0-4 parameters | ✅ Full support |
| Static arguments | ✅ Full support |
| Dynamic arguments | ✅ Full support |
| Enums | ✅ Enhanced UI |
| Lists | ✅ Enhanced UI |
| Arrays | ✅ Enhanced UI |
| Vector types | ✅ Enhanced UI |
| Color | ✅ Enhanced UI |
| Quaternion | ✅ Enhanced UI |
| LayerMask | ✅ Enhanced UI |
| Event reordering | ✅ Via advanced inspector |
| Visual type feedback | ✅ |

## Future Enhancements (Not Yet Implemented)

If you want to add more features, these are good candidates:

- [ ] Conditional event execution (only fire when conditions are met)
- [ ] Event listener reordering via drag-and-drop in main inspector
- [ ] Event templates and presets
- [ ] Integration with Unity's new Input System
- [ ] Event profiling and performance metrics

## Troubleshooting

**Inspector shows "Unable to load OmniEvent"**
- Make sure you're using `OmniEvent<T>` or one of its variants
- Check that the field is `public` or has `[SerializeField]`

**Arguments not passing correctly**
- Verify argument types match between event and method
- Check that dynamic references point to valid objects

**Missing argument type info**
- This is normal for `OmniEvent` with no parameters
- Only parameterized events show argument info

## Need Help?

Refer to the complete documentation in `README.md` for detailed explanations, examples, and troubleshooting.
