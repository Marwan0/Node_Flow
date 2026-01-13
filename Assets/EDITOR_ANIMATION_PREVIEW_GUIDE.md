# Editor-Time Animation Preview Guide

## Overview

The Quiz System now includes a powerful **Animation Preview Window** that allows you to customize and preview animations directly in the Unity Editor without entering play mode - just like Animation Sequencer!

## Accessing the Preview Window

### Method 1: Menu Bar
1. Go to **Tools > Quiz System > Animation Preview**
2. The window will open with preview controls

### Method 2: Inspector Buttons
1. Select a **QuizManager** or **QuestionUI** component in the Inspector
2. In the "Animations" section, click **"Open Animation Preview"** button
3. The window opens and automatically selects the component

## Features

### 1. **Target Selection**
- **QuizManager**: Preview question transition animations (fade/slide/scale)
- **QuestionUI**: Preview feedback animations (correct/wrong/hint)
- Components are auto-detected from the scene if available

### 2. **Preview Controls**

#### For QuizManager:
- **Preview Transition Out** - See how questions fade/slide/scale out
- **Preview Transition In** - See how questions fade/slide/scale in
- **Stop All Previews** - Cancel any running previews

#### For QuestionUI:
- **Preview Correct Answer** - See the scale bounce animation
- **Preview Wrong Answer** - See the shake animation
- **Preview Hint Reveal** - See the slide up + fade animation
- **Preview Button Entrance** (MultipleChoiceUI only) - See staggered button appearances
- **Stop All Previews** - Cancel any running previews

### 3. **Live Animation Settings**
- **Inline Editor**: Full editor view of animation settings
- **Real-time Tweaking**: Adjust values and preview immediately
- **No Play Mode Required**: All previews work in edit mode

## How to Use

### Step 1: Open the Preview Window
```
Tools > Quiz System > Animation Preview
```

### Step 2: Select Your Target
- Drag a **QuizManager** or **QuestionUI** from the scene into the window
- Or use the Inspector button to auto-select

### Step 3: Customize Settings
- Adjust animation durations, styles, and other parameters
- Settings are shown in inline editors for easy tweaking

### Step 4: Preview Animations
- Click any preview button to see the animation
- Adjust settings and preview again to see changes
- Use "Stop All Previews" if needed

### Step 5: Fine-tune
- Keep tweaking values and previewing until it feels right
- All changes are saved to the component automatically

## Example Workflow

### Customizing Question Transitions:

1. **Select QuizManager** in the scene
2. **Open Animation Preview** window (via Inspector button or menu)
3. **Adjust settings**:
   - Change `transitionDuration` to 0.5 seconds
   - Change `transitionStyle` to "Slide"
4. **Click "Preview Transition Out"** - See the slide out animation
5. **Click "Preview Transition In"** - See the slide in animation
6. **Tweak duration** if needed (e.g., 0.4s feels better)
7. **Preview again** to confirm
8. **Done!** Settings are saved automatically

### Customizing Answer Feedback:

1. **Select MultipleChoiceUI** prefab or instance
2. **Open Animation Preview** window
3. **Adjust settings**:
   - Change `feedbackDuration` to 0.6 seconds
   - Change `buttonStaggerDelay` to 0.15 seconds
4. **Click "Preview Correct Answer"** - See the bounce
5. **Click "Preview Button Entrance"** - See staggered buttons
6. **Tweak values** until animations feel perfect
7. **Done!**

## Tips & Tricks

### 1. **Quick Iteration**
- Keep the preview window open while tweaking
- Preview buttons work instantly - no need to close/reopen

### 2. **Multiple Previews**
- You can preview different animations in sequence
- Use "Stop All Previews" to reset if needed

### 3. **Scene View**
- Animations are visible in the Scene view
- Make sure your target is visible in the scene

### 4. **Prefab Mode**
- Works with prefabs in Prefab Mode
- Great for tweaking prefab animations

### 5. **Component Selection**
- Window auto-selects components from scene on open
- You can manually drag/drop different components

## Technical Details

### Editor-Time Animation Execution
- Uses DOTween's editor-time capabilities
- Animations run via `EditorApplication.update`
- No play mode required
- Safe to use - doesn't affect play mode

### Reflection Usage
- Uses reflection to access protected fields
- Allows previewing animations without modifying runtime code
- Safe and non-intrusive

### Performance
- Preview animations are lightweight
- Can be stopped at any time
- No impact on editor performance

## Troubleshooting

### "No target selected"
- Make sure a QuizManager or QuestionUI exists in the scene
- Or manually drag one into the window

### "Animation not visible"
- Check that the target GameObject is visible in Scene view
- Make sure the component has valid references

### "Preview not working"
- Click "Stop All Previews" first
- Make sure DOTween is properly installed
- Check that animation settings are valid (duration > 0)

### "Settings not saving"
- Settings are saved automatically when changed
- If using prefabs, make sure you're in Prefab Mode or have the instance selected

## Advanced Usage

### Custom Animation Timing
1. Open preview window
2. Select component
3. Adjust all timing values
4. Preview each animation type
5. Fine-tune until perfect
6. Settings persist automatically

### Batch Preview
1. Select multiple components
2. Preview animations on each
3. Compare and choose best settings
4. Apply to all similar components

### Animation Consistency
1. Set up one component perfectly
2. Note the settings values
3. Apply same values to other components
4. Use preview to verify consistency

## Integration with Animation Sequencer Pattern

This preview system follows the same philosophy as Animation Sequencer:
- ✅ Editor-time preview
- ✅ No play mode required
- ✅ Real-time tweaking
- ✅ Visual feedback
- ✅ Easy iteration

The difference is we use DOTween directly (which you already have) rather than requiring the Animation Sequencer package.

## Summary

The Animation Preview Window gives you:
- **Fast iteration** - Preview without play mode
- **Easy tweaking** - Adjust and see results immediately
- **Professional workflow** - Similar to Animation Sequencer
- **No dependencies** - Uses existing DOTween setup

Enjoy customizing your quiz animations! 🎨✨

