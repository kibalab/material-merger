# Material Merger User Guide

## Overview

Material Merger is a Unity Editor tool designed for VRChat avatar optimization through texture atlasing. It merges multiple materials that share the same shader into a single material with an atlas texture, reducing draw calls and improving performance.

## Getting Started

### Opening the Tool

1. In Unity Editor, navigate to: **Menu > Kiba > 렌더링 > 멀티 아틀라스 머저**
2. The Material Merger window will open

### Requirements

- Unity 2022.3.22f1 or compatible version
- VRChat SDK (via VPM resolver)
- lilToon shader 2.3.2 (optional, for lilToon materials)

## Basic Workflow

### Step 1: Select Root Object

1. Drag your avatar root GameObject to the **Root** field at the top of the window
2. The tool will automatically find the MaterialMergeProfile component or create one

### Step 2: Configure Grouping Options

| Option | Description |
|--------|-------------|
| **Keywords** | Group materials by shader keywords (recommended) |
| **Render Queue** | Group materials by render queue value |
| **Opaque/Transparent Split** | Separate opaque and transparent materials |

### Step 3: Scan Materials

1. Click the **Scan** button
2. The tool will analyze all renderers and materials in the hierarchy
3. Groups will appear based on shader type and transparency

### Step 4: Configure Groups

Each group shows:
- **Checkbox**: Enable/disable the group for merging
- **Output Name**: Name for the merged material
- **[Opaque/Transparent]**: Transparency tag
- **Page Count**: Number of atlas pages needed

#### Property Configuration

Expand a group to see shader properties:

| Property Type | Actions |
|---------------|---------|
| **Texture** | Enable atlas inclusion (checkbox) |
| **Color** | Reset / Bake to texture / Multiply with texture |
| **Float/Range** | Reset / Bake to grayscale texture |
| **Vector** | Reset to shader default |

### Step 5: Configure Output Settings

| Setting | Description | Default |
|---------|-------------|---------|
| **Atlas Size** | Maximum atlas texture size | 8192 |
| **Grid** | Number of tiles per row/column | 4 |
| **Padding** | Pixels between tiles (prevents bleeding) | 16 |
| **Output Folder** | Where generated assets are saved | Assets/_Generated/MultiAtlas |

### Step 6: Build and Apply

1. Click **Build** button
2. Review the confirmation dialog showing what will be processed
3. Click **Proceed** to start the merge process
4. The tool will:
   - Generate atlas textures
   - Create merged materials
   - Remap mesh UVs
   - Apply to renderers

## Advanced Features

### Clone Options

| Option | Description |
|--------|-------------|
| **Clone Root** | Create a copy of the root object with merged materials |
| **Deactivate Original** | Disable the original root after cloning |
| **Keep Prefab Link** | Maintain prefab connection on cloned object |

### Diff Policy

When materials have different property values:

| Policy | Behavior |
|--------|----------|
| **Stop if Unresolved** | Halt build if conflicts exist |
| **Use First Material** | Use values from the first material in group |
| **Use Sample Material** | Use values from a specified sample material |

### Group Merging

Drag groups to combine them:
1. Drag a group header onto another group
2. They will share the same merge key
3. All materials will be combined into one atlas

### Property Modifiers

For baked properties, you can apply modifiers:
- **Multiply**: Multiply by another scalar property
- **Add**: Add another scalar value
- **Subtract**: Subtract another scalar value

With options:
- **Clamp01**: Clamp result to 0-1 range
- **Scale/Bias**: Apply linear transformation
- **Affect Alpha**: Include alpha channel in modification

## Rollback

If you need to undo changes:
1. Navigate to: **Menu > Kiba > 렌더링 > 멀티 아틀라스 롤백...**
2. Select the log file from the merge operation
3. Click Rollback to restore original state

## Troubleshooting

### Common Issues

**"No scan results"**
- Ensure the root object has Renderer components with materials
- Check that materials have valid shaders

**"Blit material failed"**
- The tool couldn't create the internal blit shader
- Ensure the output folder is writable

**"Unresolved differences"**
- Materials have different property values
- Either enable "doAction" for those properties or change diff policy

**UV seams visible**
- Increase padding value
- Ensure source textures have proper wrapping

### Performance Tips

1. Use smaller atlas sizes if VRAM is a concern
2. Only enable texture atlasing for properties that differ
3. Combine groups with similar shaders using drag-merge
4. Use "Reset to Default" for properties that don't need preservation

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| **ESC** | Close log console |

## Language Support

The tool supports:
- Korean (한국어)
- English
- Japanese (日本語)

Language is auto-detected on first launch and can be changed via the dropdown.
