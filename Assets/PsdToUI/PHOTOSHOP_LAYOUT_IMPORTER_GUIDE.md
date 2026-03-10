# Photoshop Layout Importer - User Guide

## Overview

The **Photoshop Layout Importer** is a Unity editor window that imports Photoshop designs as Unity UI elements. It uses a two-step workflow: first export from Photoshop using a script, then import into Unity.

**Menu Access:** `Window > Photoshop Layout Importer`

## Workflow Summary

1. **Export from Photoshop**: Run the [`export_layers_v3.jsx`](Photoshop/export_layers_v3.jsx) script in Photoshop
2. **Import in Unity**: Use this window to import the generated JSON + PNG files

## Step-by-Step Guide

### Step 1: Export from Photoshop

1. Open your PSD file in Photoshop
2. Go to `File > Scripts > Browse...` and select [`export_layers_v3.jsx`](Photoshop/export_layers_v3.jsx)
3. Select an output folder when prompted
4. Wait for the export to complete (you'll see a progress window)
5. The script will create:
   - Individual PNG files for each layer
   - A `layout.json` file with layer metadata and positioning

**Export Features:**
- Pre-rasterizes all layers (bakes styles, masks, smart objects)
- Preserves Photoshop effects exactly
- Exports layer hierarchy and positioning
- Shows progress with phase indicators

### Step 2: Import in Unity

1. Open the Photoshop Layout Importer window (`Window > Photoshop Layout Importer`)
2. **Select Layout JSON**: Click "Browse" next to "Layout JSON" and select the `layout.json` file from your export folder
3. **Configure Import Settings** (see below)
4. Click "Import Layout"

## Import Settings

### Layout JSON
- **Required**: Path to the `layout.json` file exported from Photoshop
- Click "Browse" to navigate to the file
- The window will validate the file path

### Import Sprites To
- **Required**: Destination folder for imported sprite assets
- Must be under your Unity `Assets` folder
- Default: `Assets/PsdToUI/ImportedLayouts`
- A subfolder will be created automatically based on the layout name
- Click "Browse" to select a folder

### Target Canvas (Optional)
- **Optional**: Existing Canvas RectTransform to use as parent
- If not provided, a new canvas will be created automatically
- The canvas will be configured with appropriate scaling settings
- Useful for importing into existing UI setups

### Create Text Objects
- **Default**: Enabled
- When enabled, creates TextMeshProUGUI components for text layers
- Text layers without associated PNG files will become editable text
- When disabled, text layers are skipped (only image layers imported)

### Respect Visibility
- **Default**: Enabled
- When enabled, layers that were hidden in Photoshop will be inactive in Unity
- When disabled, all layers are imported regardless of Photoshop visibility
- Useful for importing all layers and manually managing visibility later

### Copy Images Into Project
- **Default**: Enabled
- When enabled, copies PNG files from export folder into Unity project
- When disabled, tries to reference PNG files in their original location
- **Recommendation**: Keep enabled for better project organization and portability

### Layer Pivot
- **Default**: Center
- Controls the pivot point for imported UI elements
- Options:
  - **Center**: (0.5, 0.5) - Pivot at center
  - **TopLeft**: (0, 1) - Pivot at top-left corner
  - **TopCenter**: (0.5, 1) - Pivot at top-center
  - **TopRight**: (1, 1) - Pivot at top-right corner
  - **MiddleLeft**: (0, 0.5) - Pivot at middle-left
  - **MiddleRight**: (1, 0.5) - Pivot at middle-right
  - **BottomLeft**: (0, 0) - Pivot at bottom-left corner
  - **BottomCenter**: (0.5, 0) - Pivot at bottom-center
  - **BottomRight**: (1, 0) - Pivot at bottom-right corner
- **Recommendation**: Use Center for most UI elements, TopLeft for positioning from corners

## What Gets Imported

### Canvas
- If no target canvas is specified, a new canvas is created:
  - Name: `PS_Layout_Canvas`
  - Render Mode: Screen Space Overlay
  - Canvas Scaler: Scale With Screen Size
  - Reference Resolution: Matches PSD document dimensions

### Root Object
- A root GameObject is created to hold all imported layers
- Name: Matches the PSD document name
- Anchored to center of canvas
- Size matches PSD document dimensions

### Layers
Each layer from the PSD becomes a Unity GameObject:

#### Image Layers
- GameObject with Image component
- Sprite assigned from imported PNG
- Opacity preserved from Photoshop
- Positioned exactly as in Photoshop
- Raycast Target disabled (can be enabled if needed)

#### Text Layers
- If "Create Text Objects" is enabled and no PNG file exists:
  - GameObject with TextMeshProUGUI component
  - Text content preserved from Photoshop
  - Default black color (can be customized)
  - Positioned as in Photoshop

#### Groups
- Groups become parent GameObjects
- Child layers are nested under group GameObjects
- Maintains layer hierarchy from Photoshop

### Layer Properties Preserved
- **Position**: Exact X, Y coordinates from Photoshop
- **Size**: Width and height from Photoshop
- **Opacity**: Layer opacity (0-1 range)
- **Visibility**: Layer visibility state
- **Hierarchy**: Parent-child relationships
- **Order**: Layer stacking order

## Coordinate System

The importer handles coordinate conversion automatically:
- **Photoshop**: Origin at top-left, Y increases downward
- **Unity UI**: Origin at bottom-left, Y increases upward
- **Result**: Elements appear in the same relative positions

## File Organization

### Export Folder Structure (Photoshop)
```
[Your Export Folder]/
├── layer1.png
├── layer2.png
├── group1_layer1.png
├── group1_layer2.png
└── layout.json
```

### Import Folder Structure (Unity)
```
Assets/PsdToUI/ImportedLayouts/
└── [LayoutName]/
    ├── layer1.png
    ├── layer2.png
    ├── group1_layer1.png
    ├── group1_layer2.png
    ├── layer1.png.meta
    ├── layer2.png.meta
    ├── group1_layer1.png.meta
    └── group1_layer2.png.meta
```

### Hierarchy Structure (Unity)
```
PS_Layout_Canvas (or your target canvas)
└── [LayoutName]
    ├── layer1 (Image)
    ├── layer2 (Image)
    └── group1
        ├── layer1 (Image)
        └── layer2 (Image)
```

## JSON Format Reference

The `layout.json` file contains:

```json
{
  "version": 2,
  "document": {
    "width": 1920,
    "height": 1080,
    "name": "MyDesign"
  },
  "nodes": [
    {
      "id": "0",
      "parentId": "",
      "name": "Background",
      "file": "background.png",
      "x": 0,
      "y": 0,
      "width": 1920,
      "height": 1080,
      "opacity": 1,
      "visible": true,
      "isText": false,
      "isGroup": false,
      "order": 0
    },
    {
      "id": "1",
      "parentId": "0",
      "name": "Button",
      "file": "button.png",
      "x": 100,
      "y": 100,
      "width": 200,
      "height": 50,
      "opacity": 1,
      "visible": true,
      "isText": false,
      "isGroup": false,
      "order": 1
    }
  ]
}
```

## Common Use Cases

### 1. Importing a Complete UI Design
1. Export entire PSD from Photoshop
2. Import with default settings
3. Result: Complete UI hierarchy ready for use

### 2. Importing into Existing Canvas
1. Provide target canvas in settings
2. Import layout
3. Result: Layout added to existing canvas

### 3. Importing Only Visible Layers
1. Enable "Respect Visibility"
2. Hide unwanted layers in Photoshop before export
3. Import with visibility respected
4. Result: Only visible layers imported

### 4. Importing Text as Editable
1. Ensure "Create Text Objects" is enabled
2. Text layers without PNG files become TextMeshPro text
3. Result: Editable text elements in Unity

## Troubleshooting

### Issue: "Layout JSON path is invalid"
**Solution**: Ensure the JSON file exists and the path is correct

### Issue: "Missing source image" warnings
**Solution**: Check that all PNG files from the export are present in the same folder as the JSON

### Issue: Text not appearing
**Solution**: 
- Ensure "Create Text Objects" is enabled
- Check that text layers don't have associated PNG files
- Verify text content in the JSON

### Issue: Layers in wrong order
**Solution**: The importer respects the `order` field in the JSON. Check that the Photoshop script exported layers correctly

### Issue: Images not showing
**Solution**:
- Check that PNG files were imported successfully
- Verify sprite import settings (should be Sprite type, Single mode)
- Check console for import errors

### Issue: Canvas scaling issues
**Solution**:
- Provide a target canvas with your preferred settings
- Or let the importer create a canvas and adjust the CanvasScaler manually

## Best Practices

### Before Export
1. **Organize Layers**: Use groups to organize related elements
2. **Name Layers**: Use descriptive names for easier identification in Unity
3. **Clean Up**: Remove unused layers and effects
4. **Test Visibility**: Ensure layers you want are visible

### During Import
1. **Use Separate Folders**: Import each layout into its own folder
2. **Check Console**: Review any warnings or errors
3. **Verify Hierarchy**: Ensure the structure looks correct
4. **Test Functionality**: Check that buttons and interactive elements work

### After Import
1. **Add Components**: Add necessary components (Button, Toggle, etc.)
2. **Configure Text**: Adjust font, size, and color for TextMeshPro elements
3. **Set Raycast Target**: Enable for interactive elements
4. **Optimize**: Remove unused elements and optimize sprites

## Performance Tips

1. **Large PSDs**: For complex designs, consider splitting into multiple smaller PSDs
2. **Sprite Settings**: Use appropriate compression settings for sprites
3. **Atlas Consideration**: Consider using sprite atlases for many small sprites
4. **Batch Import**: Import multiple layouts in sequence if needed

## Integration with Node Flow

This importer integrates seamlessly with the Node Flow system:
- Import UI designs from Photoshop
- Use imported elements in node-based workflows
- Maintain design-to-implementation pipeline
- Enable rapid prototyping from design files

## Advanced Features

### Custom Canvas Configuration
If you provide a target canvas, the importer will:
- Use your existing canvas
- Configure the CanvasScaler for the layout
- Not create a new canvas

### Layer Filtering
- Use "Respect Visibility" to filter layers
- Hide layers in Photoshop before export
- Or manually disable GameObjects after import

### Text Customization
- TextMeshPro text can be customized after import
- Change font, size, color, alignment
- Add effects and styling

## Limitations

1. **Smart Objects**: Rasterized during export (not editable in Unity)
2. **Layer Effects**: Baked into PNG files (not separate components)
3. **Vector Text**: Rasterized unless imported as TextMeshPro
4. **Animations**: Not imported (can be added manually in Unity)
5. **Layer Styles**: Baked into PNG files

## Future Enhancements

Potential improvements:
- Batch import of multiple layouts
- Custom component mapping
- Animation import support
- Layer style preservation
- Smart object editing
- Prefab generation

## Support

For issues or questions:
1. Check the Unity Console for error messages
2. Verify the JSON file format
3. Ensure all PNG files are present
4. Review the Photoshop script output
5. Test with a simple PSD first

## Quick Reference

| Setting | Purpose | Default |
|---------|---------|---------|
| Layout JSON | Path to exported JSON file | Required |
| Import Sprites To | Destination for sprite assets | `Assets/PsdToUI/ImportedLayouts` |
| Target Canvas | Existing canvas to use | Optional |
| Create Text Objects | Import text as TextMeshPro | Enabled |
| Respect Visibility | Respect Photoshop visibility | Enabled |
| Copy Images Into Project | Copy PNGs to project | Enabled |
| Layer Pivot | Pivot point for UI elements | Center |

---

**Last Updated:** 2026-03-09
**Importer Version:** 3.0
**Unity Version:** Compatible with Unity 2020.3+
