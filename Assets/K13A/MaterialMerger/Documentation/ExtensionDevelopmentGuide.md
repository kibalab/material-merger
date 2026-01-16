# Material Merger Extension Development Guide

## Architecture Overview

Material Merger follows SOLID principles with a service-oriented architecture:

```
MaterialMergerWindow (Orchestrator)
    ├── Services (Business Logic)
    │   ├── IMaterialScanService     → Scanning materials
    │   ├── IMaterialBuildService    → Building atlases
    │   ├── IProfileService          → Profile persistence
    │   ├── ITextureProcessor        → Texture operations
    │   ├── IAtlasGenerator          → Atlas creation
    │   ├── IMeshRemapper            → UV remapping
    │   ├── ILocalizationService     → Translations
    │   └── ILoggingService          → Logging
    │
    ├── Models (Data)
    │   ├── GroupKey                 → Material grouping identity
    │   ├── GroupScan                → Scanned group data
    │   ├── Row                      → Property configuration
    │   ├── MatInfo                  → Material with users
    │   └── BuildSettings            → Build configuration
    │
    └── UI (Presentation)
        ├── TopPanelRenderer
        ├── GlobalSettingsPanelRenderer
        ├── GroupListRenderer
        ├── GroupPanelRenderer
        ├── PropertyTableRenderer
        └── PropertyRowRenderer
```

## Creating a Custom Service

### Step 1: Define the Interface

```csharp
#if UNITY_EDITOR
namespace K13A.MaterialMerger.Editor.Services
{
    public interface IMyCustomService
    {
        /// <summary>
        /// Your method description
        /// </summary>
        void DoSomething(GroupScan group);
    }
}
#endif
```

### Step 2: Implement the Service

```csharp
#if UNITY_EDITOR
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Logging;

namespace K13A.MaterialMerger.Editor.Services
{
    public class MyCustomService : IMyCustomService
    {
        // Dependency injection via property
        public ILoggingService LoggingService { get; set; }
        
        public void DoSomething(GroupScan group)
        {
            LoggingService?.Info("Processing group", group.shaderName);
            // Your implementation
        }
    }
}
#endif
```

### Step 3: Register in MaterialMergerWindow

```csharp
private void InitializeServices()
{
    // ... existing services ...
    
    myCustomService = new MyCustomService
    {
        LoggingService = loggingService
    };
}
```

## Creating a Custom UI Component

### Step 1: Create the Renderer

```csharp
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    public class MyCustomRenderer
    {
        // Dependencies
        public MaterialMergerStyles Styles { get; set; }
        public ILocalizationService Localization { get; set; }
        
        public void Draw(/* your parameters */)
        {
            using (new EditorGUILayout.VerticalScope(Styles.stBox))
            {
                GUILayout.Label("My Custom Section", Styles.stSubTitle);
                // Your UI code
            }
        }
    }
}
#endif
```

### Step 2: Inject Dependencies

```csharp
private void InitializeUI()
{
    // ... existing renderers ...
    
    myCustomRenderer = new MyCustomRenderer
    {
        Styles = styles,
        Localization = localizationService
    };
}
```

## Adding Localization Keys

### Step 1: Add Key Constant

Edit `Services/Localization/LocalizationKeys.cs`:

```csharp
public static class L10nKey
{
    // ... existing keys ...
    public const string MyCustomKey = "MyCustomKey";
    public const string MyCustomTooltip = "MyCustomTooltip";
}
```

### Step 2: Add Translations

Edit `Services/Localization/LocalizationData.cs`:

```csharp
private static Dictionary<string, string> GetKoreanTranslations()
{
    return new Dictionary<string, string>
    {
        // ... existing translations ...
        { L10nKey.MyCustomKey, "내 커스텀 기능" },
        { L10nKey.MyCustomTooltip, "커스텀 기능 설명" },
    };
}

private static Dictionary<string, string> GetEnglishTranslations()
{
    return new Dictionary<string, string>
    {
        // ... existing translations ...
        { L10nKey.MyCustomKey, "My Custom Feature" },
        { L10nKey.MyCustomTooltip, "Custom feature description" },
    };
}

private static Dictionary<string, string> GetJapaneseTranslations()
{
    return new Dictionary<string, string>
    {
        // ... existing translations ...
        { L10nKey.MyCustomKey, "カスタム機能" },
        { L10nKey.MyCustomTooltip, "カスタム機能の説明" },
    };
}
```

## Adding a New Bake Mode

### Step 1: Add Enum Value

Edit `Models/Enums.cs`:

```csharp
public enum BakeMode
{
    유지,
    리셋_쉐이더기본값,
    색상굽기_텍스처타일,
    스칼라굽기_그레이타일,
    색상곱_텍스처타일,
    MyNewBakeMode  // Your new mode
}
```

### Step 2: Handle in MaterialBuildService

Edit `BuildGroupAssets` method:

```csharp
if (myNewBakeRules.Count > 0)
{
    var rule = myNewBakeRules[0];
    // Your baking logic
    var pixels = ProcessMyNewMode(rule, mat, content);
    AtlasGenerator.PutTileWithPadding(atlas, px, py, content, content, pixels, paddingPx);
    continue;
}
```

### Step 3: Update UI

Edit `PropertyRowRenderer.AllowedModesUI`:

```csharp
private BakeMode[] AllowedModesUI(ShaderUtil.ShaderPropertyType t)
{
    if (t == ShaderUtil.ShaderPropertyType.Color)
        return new[] { 
            BakeMode.리셋_쉐이더기본값, 
            BakeMode.색상굽기_텍스처타일, 
            BakeMode.색상곱_텍스처타일,
            BakeMode.MyNewBakeMode  // Add here
        };
    // ...
}
```

### Step 4: Add Localization

```csharp
{ L10nKey.BakeModeMyNew, "My New Mode" },
```

## Extending GroupKey for Custom Grouping

To add custom grouping criteria:

### Step 1: Modify GroupKey

Edit `Models/GroupKey.cs`:

```csharp
public struct GroupKey : IEquatable<GroupKey>
{
    public Shader shader;
    public int keywordsHash;
    public int renderQueue;
    public int transparencyKey;
    public int myCustomKey;  // Your new grouping key

    public bool Equals(GroupKey other)
    {
        return shader == other.shader
               && keywordsHash == other.keywordsHash
               && renderQueue == other.renderQueue
               && transparencyKey == other.transparencyKey
               && myCustomKey == other.myCustomKey;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int h = 17;
            h = h * 31 + (shader ? shader.GetInstanceID() : 0);
            h = h * 31 + keywordsHash;
            h = h * 31 + renderQueue;
            h = h * 31 + transparencyKey;
            h = h * 31 + myCustomKey;
            return h;
        }
    }
}
```

### Step 2: Update MaterialScanService

Edit `CreateGroupKey` method:

```csharp
public GroupKey CreateGroupKey(Material material, bool groupByKeywords, 
    bool groupByRenderQueue, bool splitOpaqueTransparent)
{
    return new GroupKey
    {
        shader = material.shader,
        keywordsHash = CalculateKeywordsHash(material, groupByKeywords),
        renderQueue = groupByRenderQueue ? material.renderQueue : 0,
        transparencyKey = splitOpaqueTransparent ? (IsTransparent(material) ? 1 : 0) : 0,
        myCustomKey = CalculateMyCustomKey(material)
    };
}
```

## Best Practices

### 1. Always Use Interfaces

```csharp
// Good
private IMaterialScanService scanService;

// Avoid
private MaterialScanService scanService;
```

### 2. Use Property Injection

```csharp
// Good
public ILoggingService LoggingService { get; set; }

// Avoid
public MyService(ILoggingService loggingService)
```

### 3. Check for Null Dependencies

```csharp
LoggingService?.Info("Message");  // Safe
```

### 4. Use Constants

```csharp
// Good
if (material.renderQueue >= Constants.TransparentRenderQueueThreshold)

// Avoid
if (material.renderQueue >= 3000)
```

### 5. Wrap Editor Code

```csharp
#if UNITY_EDITOR
// Your editor code
#endif
```

### 6. Use Korean for UI Enum Values

```csharp
public enum BakeMode
{
    유지,           // Keep
    리셋_쉐이더기본값  // Reset to shader default
}
```

## Testing Your Extension

Since there's no automated test framework, manual testing is required:

1. Open Unity Editor with your changes
2. Open the Material Merger window
3. Load a test avatar with various materials
4. Scan and verify your changes appear correctly
5. Build and verify the output is correct
6. Test rollback functionality

## Common Extension Points

| Extension | Location | Purpose |
|-----------|----------|---------|
| New property type handling | `MaterialScanService.BuildPropertyRows` | Detect and configure new property types |
| New bake mode | `MaterialBuildService.BuildGroupAssets` | Add new texture generation logic |
| Custom grouping | `MaterialScanService.CreateGroupKey` | Group materials by custom criteria |
| UI customization | `UI/Components/*Renderer.cs` | Add new UI elements |
| Profile persistence | `ProfileService` | Save/load custom settings |

## API Reference

### IMaterialScanService

| Method | Description |
|--------|-------------|
| `CollectRenderers(root)` | Get all renderers in hierarchy |
| `ScanGameObject(root, ...)` | Create GroupScan list |
| `CreateGroupKey(material, ...)` | Generate grouping key |
| `BuildPropertyRows(group)` | Create property configuration |

### IMaterialBuildService

| Method | Description |
|--------|-------------|
| `BuildAndApplyWithConfirm(...)` | Show confirmation and build |
| `BuildAndApply(...)` | Execute build process |
| `CloneRootForApply(...)` | Clone root object |
| `CopySettings(from, to)` | Transfer settings between scans |

### IAtlasGenerator

| Method | Description |
|--------|-------------|
| `CreateAtlas(size, sRGB)` | Create new atlas texture |
| `PutTileWithPadding(...)` | Place tile in atlas |
| `SaveAtlasPNG(atlas, folder, name)` | Save to disk |
| `ConfigureImporter(path, ...)` | Set import settings |

### ITextureProcessor

| Method | Description |
|--------|-------------|
| `SampleWithScaleTiling(...)` | Sample texture with ST |
| `CreateSolidPixels(...)` | Generate solid color |
| `MultiplyPixels(pixels, color)` | Multiply pixel array |
| `EvaluateModifier(mat, row)` | Calculate modifier value |
