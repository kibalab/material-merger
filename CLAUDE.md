# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity Editor tool called "멀티 아틀라스 머저" (Multi-Atlas Merger) for VRChat avatar optimization. It scans GameObject hierarchies, groups materials by shader/keywords/render queue, and generates texture atlases to reduce material/draw calls for performance optimization.

**Unity Version:** 2022.3.22f1

**Key Dependencies:**
- VRChat SDK (via VPM resolver)
- lilToon shader (2.3.2) - A feature-rich toon shader commonly used in VRChat avatars

## Architecture

### Core Components

The tool is implemented as a `partial class` split across multiple files in `Assets/K13A/MaterialMerger/Editor/`:

1. **MaterialMerger.cs** - Main EditorWindow class with core data structures:
   - `GroupKey`: Defines how materials are grouped (shader, keywords, render queue, transparency)
   - `GroupScan`: Represents a group of materials that share the same GroupKey
   - `Row`: Represents a single shader property with diff detection and baking configuration
   - `MatInfo`: Tracks which Renderers use which Materials

2. **MaterialMerger.UI.cs** - All GUI rendering logic:
   - Custom GUIStyles for consistent UI appearance
   - Table-based property editor with filtering/search
   - Foldout groups for material groups
   - Per-property action configuration (texture atlasing, color/scalar baking)

3. **MaterialMerger.Scan.cs** - Material scanning and analysis:
   - `Scan()`: Main entry point - collects Renderers, groups Materials by GroupKey
   - `BuildRowsInShaderOrder()`: Analyzes each shader property across all materials in a group
   - Diff detection: Identifies properties with different values across materials
   - Texture/ST analysis: Detects texture and UV tiling differences

4. **MaterialMerger.Build.cs** - Atlas generation and application:
   - `BuildAndApply()`: Main build pipeline
   - Creates texture atlases by packing materials into grid tiles
   - Bakes color/scalar properties into textures when configured
   - Generates new merged materials and updates Renderers
   - Creates rollback logs (`KibaMultiAtlasMergerLog`)

5. **MaterialMerger.Settings.cs** - Profile persistence:
   - `MaterialMergeProfile`: ScriptableObject attached to root GameObject
   - Saves scan results, user configurations, and per-property actions
   - Enables non-destructive workflow with settings persistence

6. **MaterialMergeProfile.cs** (Runtime) - Data container for profile serialization
7. **MaterialMergerLog.cs** - Rollback log for undoing atlas applications
8. **ConfirmWindow.cs** - Pre-build confirmation dialog

### Key Workflows

**Scanning:**
1. User selects a root GameObject (typically avatar root)
2. Tool collects all Renderers recursively
3. Materials are grouped by GroupKey (shader + keywords + render queue + transparency)
4. Each group analyzes all shader properties and detects differences (diffs)
5. Results stored in `MaterialMergeProfile` component on root GameObject

**Diff Resolution:**
- **Unresolved diffs**: Properties with different values that have no action configured
- Two policies: "미해결이면중단" (halt if unresolved) or "첫번째기준으로진행" (use first material's value)
- User must either enable action for diff properties or accept first-material-wins policy

**Building:**
1. For each enabled group:
   - Creates atlas texture(s) sized based on material count and grid setting
   - Packs material textures into atlas tiles with configurable padding
   - Applies color/scalar baking to textures when configured (e.g., bake _Color into _MainTex)
   - Supports modifier operations (multiply/add/subtract from other properties)
2. Generates merged materials with atlas textures and adjusted UV scales/offsets
3. Creates rollback log before modifying scene
4. Updates Renderer materials and optionally clones root GameObject

### Texture Atlasing Details

- **Grid modes**: 2×2 (4 materials/page) or 4×4 (16 materials/page)
- **Atlas sizes**: 4096×4096 or 8192×8192
- **Padding**: Configurable pixel padding between tiles (0-64px) to prevent bleeding
- **Color space**: Auto-detects normal maps and mask textures for correct Linear/sRGB handling
- **ST (Scale/Tiling)**: Preserves original UV tiling by baking into individual tiles

### Property Baking Modes

For non-texture properties with diffs:

- **리셋_쉐이더기본값** (Reset): Set all materials to shader default value
- **색상굽기_텍스처타일** (Bake Color → Texture): Multiply color into each material's atlas tile
- **스칼라굽기_그레이타일** (Bake Scalar → Gray): Bake float value as grayscale into tiles
- **색상곱_텍스처타일** (Color Multiply → Texture): Apply color as multiplicative tint

Supports modifier chains: Apply another float property as multiply/add/subtract before baking.

## Development Commands

### Opening the Tool
- Menu: `Kiba/렌더링/멀티 아틀라스 머저`
- Rollback: `Kiba/렌더링/멀티 아틀라스 롤백...`

### Testing
Unity Editor play mode or direct EditorWindow manipulation. No automated test framework configured.

### Build Output
- Generated assets: `Assets/_Generated/MultiAtlas/` (configurable)
- Creates: Textures (atlas pages), Materials (merged), Shader (blit shader), Log (rollback)

## Important Constraints

1. **Multi-material Renderers**: Materials used by multiple Renderers in same slot are skipped to avoid mesh duplication complexity
2. **Profile Persistence**: Settings auto-save to `MaterialMergeProfile` component on root GameObject
3. **Korean UI**: All UI strings and enums are in Korean (matches VRChat avatar creator demographic)
4. **Undo Support**: Uses Unity's Undo system; rollback menu provides asset-based restoration
5. **VRChat Context**: Designed for avatar optimization (material slot reduction for performance)

## Code Style Notes

- Uses C# partial classes for code organization by responsibility
- Korean variable names in enums match UI labels
- Heavy use of EditorGUI immediate-mode UI with custom styling
- Caches temporary materials with `HideFlags.HideAndDontSave`
- Shader property access via `ShaderUtil` reflection API
- Uses `GlobalObjectId` for serialization-safe Renderer references in logs

## Known Patterns

**GroupKey equality**: Materials with identical shader, keywords, render queue, and transparency state can be merged
**Row-based property model**: Each shader property becomes a "Row" with unified diff detection and action configuration
**Page/tile system**: Atlas generation uses page-based layout where each page is a grid of material tiles
**Profile-driven workflow**: All scan results and user choices persist in scene to enable iterative refinement

## File Structure

```
Assets/K13A/MaterialMerger/
├── Editor/
│   ├── MaterialMerger.cs          # Main EditorWindow + data structures
│   ├── MaterialMerger.UI.cs       # GUI rendering logic
│   ├── MaterialMerger.Scan.cs     # Material scanning and analysis
│   ├── MaterialMerger.Build.cs    # Atlas generation and application
│   ├── MaterialMerger.Settings.cs # Profile persistence
│   ├── MaterialMergerLog.cs       # Rollback log
│   └── ConfirmWindow.cs           # Pre-build confirmation dialog
├── MaterialMergeProfile.cs        # Runtime profile data container
```

## Critical Implementation Details

### Material Grouping Logic (MaterialMerger.Scan.cs)

The `MakeKey()` method determines which materials can be merged together:
- Same shader reference
- Same shader keywords (when `groupByKeywords` enabled)
- Same render queue (when `groupByRenderQueue` enabled)
- Same transparency state (when `splitOpaqueTransparent` enabled)

Materials with different GroupKeys cannot be merged as they render differently.

### Diff Detection

For each shader property in a group, the tool analyzes all materials:
- **Textures**: Counts distinct texture references and ST (scale/tiling) values
- **Colors/Floats/Vectors**: Counts distinct values via hash comparison
- A property with `distinctCount > 1` has differences that need resolution

### UV Coordinate Handling

Critical for texture atlasing:
1. Each material's texture is placed in a grid tile (e.g., material 0 at [0,0], material 1 at [0.5,0], etc.)
2. Original UV scale/offset (_ST) is baked INTO the atlas tile using the blit shader
3. Merged material gets new _ST that maps entire mesh UVs to its assigned tile
4. This preserves per-material tiling without mesh modification

### Blit Shader Purpose

`Hidden/KibaAtlasBlit` shader in `MaterialMerger.Build.cs`:
- Used during atlas generation to copy source textures into atlas tiles
- Applies UV scale/offset transformation to preserve original tiling
- Handles color/scalar baking when configured

### Profile Auto-Save

The `suppressAutosaveOnce` flag prevents save loops:
- When loading profile data into UI, set this flag
- OnGUI change detection triggers `RequestSave()`
- Debounced save happens in `EditorApplication.update` callback
- Profile component is marked dirty for scene save

### Multi-Material Skip Logic

If a material appears in multiple Renderers at different slot indices, it's marked "skipped":
- Merging would require mesh duplication (different UV regions per instance)
- Displayed in UI as "스킵 N" warning
- User must manually split materials or accept lower optimization

## Target Use Case

VRChat avatars often have 20+ materials from clothing/accessories using the same shader (typically lilToon). This tool:
1. Combines them into 1-4 merged materials (depending on grid settings)
2. Reduces material slots from 20+ to <10 for performance
3. Maintains visual appearance by preserving colors, textures, and shader properties
4. Common workflow: Scan → Configure diff resolution → Build → Upload avatar with better performance rating
