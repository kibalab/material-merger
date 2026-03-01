#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Logging;

namespace K13A.MaterialMerger.Editor.Services
{
    /// <summary>
    /// Builds atlas textures and merged materials.
    /// Single Responsibility: Asset creation only.
    /// </summary>
    public class AtlasAssetBuilder : IAtlasAssetBuilder
    {
        public IAtlasGenerator AtlasGenerator { get; set; }
        public ITextureProcessor TextureProcessor { get; set; }
        public IMaterialScanService ScanService { get; set; }
        public ILoggingService LoggingService { get; set; }

        public AssetBuildResult BuildAssets(BuildSettings settings, List<GroupScan> scans)
        {
            var result = new AssetBuildResult();

            // Validate
            var (isValid, errorMessage) = BuildUtility.ValidateBuildSettings(settings);
            if (!isValid)
            {
                result.success = false;
                result.errorMessage = errorMessage;
                return result;
            }

            if (!TextureProcessor.BlitMaterial)
            {
                result.success = false;
                result.errorMessage = "Blit material not initialized";
                return result;
            }

            // Create log
            var log = ScriptableObject.CreateInstance<KibaMultiAtlasMergerLog>();
            string logPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(settings.OutputFolder, $"{Constants.LogFilePrefix}{DateTime.Now:yyyyMMdd_HHmmss}.asset")
                    .Replace("\\", "/"));
            AssetDatabase.CreateAsset(log, logPath);
            log.createdAssetPaths.Add(logPath);

            result.log = log;
            result.logPath = logPath;

            // Build merged scans
            var mergedScans = GroupMergeUtility.BuildMergedScans(scans, ScanService);

            LoggingService?.Info("═══ Phase 1: Building Assets ═══");
            LoggingService?.Info("Starting batch asset creation");

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var g in mergedScans)
                {
                    if (!BuildUtility.ShouldProcessGroup(g, settings.DiffPolicy))
                    {
                        LoggingService?.Warning("Group skipped", $"Disabled or unresolved: {g.shaderName} [{g.tag}]");
                        result.skippedCount++;
                        continue;
                    }

                    LoggingService?.Info("Building assets for group", $"{g.shaderName} [{g.tag}]");
                    var buildData = BuildGroupAssets(g, log, settings, settings.OutputFolder, result.textureImports);

                    if (buildData != null && buildData.IsValid)
                    {
                        result.groupBuildData.Add(buildData);
                        LoggingService?.Success("Group assets complete", $"{g.shaderName} [{g.tag}]");
                        result.processedCount++;
                    }
                    else
                    {
                        result.skippedCount++;
                    }
                }

                result.success = true;
                LoggingService?.Info($"Asset creation complete: {result.processedCount} groups, {result.skippedCount} skipped");
            }
            catch (Exception ex)
            {
                result.success = false;
                result.errorMessage = ex.Message;
                LoggingService?.Error("Asset build failed", ex.Message, true);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Save assets
            EditorUtility.SetDirty(log);
            AssetDatabase.SaveAssets();

            return result;
        }

        public AssetBuildContext BeginBuildAsync(BuildSettings settings, List<GroupScan> scans)
        {
            var context = new AssetBuildContext
            {
                settings = settings,
                result = new AssetBuildResult()
            };

            var (isValid, errorMessage) = BuildUtility.ValidateBuildSettings(settings);
            if (!isValid)
            {
                context.errorMessage = errorMessage;
                context.result.success = false;
                context.completed = true;
                return context;
            }

            if (!TextureProcessor.BlitMaterial)
            {
                context.errorMessage = "Blit material not initialized";
                context.result.success = false;
                context.completed = true;
                return context;
            }

            var log = ScriptableObject.CreateInstance<KibaMultiAtlasMergerLog>();
            string logPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(settings.OutputFolder, $"{Constants.LogFilePrefix}{DateTime.Now:yyyyMMdd_HHmmss}.asset")
                    .Replace("\\", "/"));
            AssetDatabase.CreateAsset(log, logPath);
            log.createdAssetPaths.Add(logPath);

            context.result.log = log;
            context.result.logPath = logPath;
            context.log = log;

            context.mergedScans = GroupMergeUtility.BuildMergedScans(scans, ScanService);

            LoggingService?.Info("??? Phase 1: Building Assets (Async) ???");
            LoggingService?.Info("Starting async asset creation");

            AssetDatabase.DisallowAutoRefresh();
            context.autoRefreshDisabled = true;
            context.groupIndex = 0;

            return context;
        }

        public bool StepBuildAsync(AssetBuildContext context, double timeBudgetSeconds)
        {
            if (context == null || context.completed) return false;

            double startTime = EditorApplication.timeSinceStartup;
            double budget = Mathf.Max(0.001f, (float)timeBudgetSeconds);

            try
            {
                while (EditorApplication.timeSinceStartup - startTime < budget)
                {
                    if (context.mergedScans == null || context.groupIndex >= context.mergedScans.Count)
                    {
                        FinishBuildAsync(context);
                        return false;
                    }

                    if (context.groupContext == null)
                    {
                        if (!BeginNextGroup(context))
                            continue;
                    }

                    if (!StepGroupBuild(context))
                        continue;
                }
            }
            catch (Exception ex)
            {
                context.errorMessage = ex.Message;
                context.result.success = false;
                context.completed = true;
                LoggingService?.Error("Async asset build failed", ex.Message, true);
                CancelBuildAsync(context);
                return false;
            }

            return true;
        }

        public void CancelBuildAsync(AssetBuildContext context)
        {
            if (context == null) return;

            if (context.autoRefreshDisabled)
                AssetDatabase.AllowAutoRefresh();
            context.autoRefreshDisabled = false;
        }

        public GroupBuildData BuildGroupAssets(
            GroupScan g,
            KibaMultiAtlasMergerLog log,
            BuildSettings settings,
            string outputFolder,
            List<TextureImportRequest> textureImports)
        {
            var mats = g.mats.ToList();
            int tilesPerPage = settings.TilesPerPage;
            int pageCount = Mathf.CeilToInt(mats.Count / (float)tilesPerPage);
            pageCount = Mathf.Max(0, pageCount);

            LoggingService?.Info($"Preparing group build: {g.shaderName}", $"Materials: {mats.Count}, Pages: {pageCount}");

            if (!TextureProcessor.BlitMaterial)
            {
                LoggingService?.Error("Blit material is missing", $"{g.shaderName} [{g.tag}]");
                return null;
            }

            int cell = settings.CellSize;
            int content = settings.ContentSize;
            int atlasSize = settings.AtlasSize;
            int grid = settings.Grid;
            int paddingPx = settings.PaddingPx;

            string planFolder = BuildUtility.GetGroupFolderName(g, AtlasGenerator.SanitizeFileName);
            string groupFolder = Path.Combine(outputFolder, planFolder).Replace("\\", "/");
            Directory.CreateDirectory(groupFolder);
            LoggingService?.Info($"Created output folder: {planFolder}");

            var result = new GroupBuildData { groupKey = g.key };
            if (mats.Count == 0 || tilesPerPage <= 0 || pageCount == 0)
                return null;

            // Get texture metadata for color space detection
            var texMeta = g.rows
                .Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv)
                .ToDictionary(r => r.name, r => r, StringComparer.Ordinal);

            var enabledTexProps = BuildUtility.GetActiveTextureProperties(g);
            var bakeRows = BuildUtility.GetBakeRows(g);
            var allAtlasProps = BuildUtility.GetAllAtlasProperties(g);

            LoggingService?.Info($"Properties: {enabledTexProps.Count} textures, {bakeRows.Count} bake, {allAtlasProps.Count} total");

            // Build each page
            for (int page = 0; page < pageCount; page++)
            {
                LoggingService?.Info($"Processing page {page + 1}/{pageCount}");

                int start = page * tilesPerPage;
                int end = Mathf.Min(mats.Count, start + tilesPerPage);
                var pageItems = mats.GetRange(start, end - start);

                string pageFolder = pageCount > 1
                    ? Path.Combine(groupFolder, $"Page_{page:00}").Replace("\\", "/")
                    : groupFolder;
                Directory.CreateDirectory(pageFolder);

                // Calculate grid
                var (actualGridCols, _) = BuildUtility.CalculateOptimalGrid(pageItems.Count, grid);
                int actualAtlasSize = BuildUtility.CalculateAtlasSize(actualGridCols, actualGridCols, cell, atlasSize);

                // Record material positions
                for (int i = 0; i < pageItems.Count; i++)
                {
                    var mat = pageItems[i].mat;
                    if (mat)
                        result.matToPage[mat] = new PageTileInfo { pageIndex = page, tileIndex = i };
                }

                // Create atlas textures
                var atlasByProp = CreateAtlasTextures(allAtlasProps, texMeta, actualAtlasSize);

                // Bake tiles
                BakeTiles(pageItems, allAtlasProps, atlasByProp, texMeta, bakeRows, 
                    actualGridCols, cell, content, paddingPx);

                // Save textures
                SaveAtlasTextures(atlasByProp, texMeta, pageFolder, atlasSize, log, textureImports);

                // Create merged material
                string matPath = CreateMergedMaterial(g, pageItems[0].mat, page, pageFolder, log, pageCount > 1);

                result.pageInfos.Add(new PageBuildInfo
                {
                    atlasSize = actualAtlasSize,
                    gridCols = actualGridCols,
                    mergedMaterial = null,
                    materialPath = matPath
                });

                LoggingService?.Success($"Page {page + 1}/{pageCount} complete");
            }

            if (!result.IsValid)
            {
                LoggingService?.Warning("No material/page info generated");
                return null;
            }

            LoggingService?.Success($"Group assets built: {g.shaderName}");
            return result;
        }

        #region Private Helper Methods

        private void FinishBuildAsync(AssetBuildContext context)
        {
            if (context == null) return;

            if (context.log != null)
                EditorUtility.SetDirty(context.log);
            AssetDatabase.SaveAssets();

            context.result.success = true;
            context.completed = true;
            LoggingService?.Info($"Asset creation complete: {context.result.processedCount} groups, {context.result.skippedCount} skipped");
        }

        private bool BeginNextGroup(AssetBuildContext context)
        {
            if (context == null || context.mergedScans == null) return false;
            if (context.groupIndex >= context.mergedScans.Count) return false;

            var g = context.mergedScans[context.groupIndex];
            if (!BuildUtility.ShouldProcessGroup(g, context.settings.DiffPolicy))
            {
                LoggingService?.Warning("Group skipped", $"Disabled or unresolved: {g.shaderName} [{g.tag}]");
                context.result.skippedCount++;
                context.groupIndex++;
                return false;
            }

            var mats = g.mats?.ToList() ?? new List<MatInfo>();
            int tilesPerPage = context.settings.TilesPerPage;
            int pageCount = Mathf.CeilToInt(mats.Count / (float)tilesPerPage);
            pageCount = Mathf.Max(0, pageCount);

            LoggingService?.Info($"Preparing group build: {g.shaderName}", $"Materials: {mats.Count}, Pages: {pageCount}");

            if (mats.Count == 0 || tilesPerPage <= 0 || pageCount == 0)
            {
                context.result.skippedCount++;
                context.groupIndex++;
                return false;
            }

            string planFolder = BuildUtility.GetGroupFolderName(g, AtlasGenerator.SanitizeFileName);
            string groupFolder = Path.Combine(context.settings.OutputFolder, planFolder).Replace("\\", "/");
            Directory.CreateDirectory(groupFolder);
            LoggingService?.Info($"Created output folder: {planFolder}");

            var texMeta = g.rows
                .Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv)
                .ToDictionary(r => r.name, r => r, StringComparer.Ordinal);

            var bakeRows = BuildUtility.GetBakeRows(g);
            var allAtlasProps = BuildUtility.GetAllAtlasProperties(g);
            var atlasPropList = allAtlasProps.OrderBy(x => x, StringComparer.Ordinal).ToList();

            LoggingService?.Info($"Properties: {allAtlasProps.Count} total");

            context.groupContext = new GroupBuildContext
            {
                group = g,
                buildData = new GroupBuildData { groupKey = g.key },
                mats = mats,
                tilesPerPage = tilesPerPage,
                pageCount = pageCount,
                pageIndex = 0,
                texMeta = texMeta,
                bakeRows = bakeRows,
                allAtlasProps = allAtlasProps,
                atlasPropList = atlasPropList,
                cell = context.settings.CellSize,
                content = context.settings.ContentSize,
                atlasSize = context.settings.AtlasSize,
                grid = context.settings.Grid,
                paddingPx = context.settings.PaddingPx,
                groupFolder = groupFolder,
                planFolder = planFolder
            };

            LoggingService?.Info("Building assets for group", $"{g.shaderName} [{g.tag}]");
            return true;
        }

        private bool StepGroupBuild(AssetBuildContext context)
        {
            if (context == null || context.groupContext == null) return false;

            var groupContext = context.groupContext;

            if (groupContext.pageIndex >= groupContext.pageCount)
            {
                if (groupContext.buildData != null && groupContext.buildData.IsValid)
                {
                    context.result.groupBuildData.Add(groupContext.buildData);
                    context.result.processedCount++;
                    LoggingService?.Success("Group assets complete", $"{groupContext.group.shaderName} [{groupContext.group.tag}]");
                }
                else
                {
                    context.result.skippedCount++;
                }

                context.groupContext = null;
                context.groupIndex++;
                return false;
            }

            if (groupContext.pageContext == null)
            {
                return BeginNextPage(groupContext);
            }

            return StepPageBuild(context, groupContext);
        }

        private bool BeginNextPage(GroupBuildContext groupContext)
        {
            if (groupContext == null) return false;

            int start = groupContext.pageIndex * groupContext.tilesPerPage;
            int end = Mathf.Min(groupContext.mats.Count, start + groupContext.tilesPerPage);
            var pageItems = groupContext.mats.GetRange(start, end - start);
            if (pageItems.Count == 0)
            {
                groupContext.pageIndex++;
                return false;
            }

            string pageFolder = groupContext.pageCount > 1
                ? Path.Combine(groupContext.groupFolder, $"Page_{groupContext.pageIndex:00}").Replace("\\", "/")
                : groupContext.groupFolder;
            Directory.CreateDirectory(pageFolder);

            var (actualGridCols, _) = BuildUtility.CalculateOptimalGrid(pageItems.Count, groupContext.grid);
            int actualAtlasSize = BuildUtility.CalculateAtlasSize(actualGridCols, actualGridCols, groupContext.cell, groupContext.atlasSize);

            for (int i = 0; i < pageItems.Count; i++)
            {
                var mat = pageItems[i].mat;
                if (mat)
                    groupContext.buildData.matToPage[mat] = new PageTileInfo { pageIndex = groupContext.pageIndex, tileIndex = i };
            }

            var atlasByProp = CreateAtlasTextures(groupContext.allAtlasProps, groupContext.texMeta, actualAtlasSize);

            groupContext.pageContext = new PageBuildContext
            {
                pageIndex = groupContext.pageIndex,
                pageItems = pageItems,
                actualGridCols = actualGridCols,
                actualAtlasSize = actualAtlasSize,
                atlasByProp = atlasByProp,
                bakeWorkIndex = 0,
                bakeWorkTotal = pageItems.Count * groupContext.atlasPropList.Count,
                saveIndex = 0,
                materialCreated = false,
                pageFolder = pageFolder
            };

            LoggingService?.Info($"Processing page {groupContext.pageIndex + 1}/{groupContext.pageCount}");
            return true;
        }

        private bool StepPageBuild(AssetBuildContext context, GroupBuildContext groupContext)
        {
            var pageContext = groupContext.pageContext;
            if (pageContext == null) return false;

            int propCount = groupContext.atlasPropList.Count;

            if (pageContext.bakeWorkIndex < pageContext.bakeWorkTotal)
            {
                int tileIndex = pageContext.bakeWorkIndex / propCount;
                int propIndex = pageContext.bakeWorkIndex % propCount;

                var mat = pageContext.pageItems[tileIndex].mat;
                string prop = groupContext.atlasPropList[propIndex];
                var atlas = pageContext.atlasByProp[prop];

                int gx = tileIndex % pageContext.actualGridCols;
                int gy = tileIndex / pageContext.actualGridCols;
                int px = gx * groupContext.cell + groupContext.paddingPx;
                int py = gy * groupContext.cell + groupContext.paddingPx;

                var pixels = GetTilePixels(mat, prop, groupContext.texMeta, groupContext.bakeRows, groupContext.content);
                AtlasGenerator.PutTileWithPadding(atlas, px, py, groupContext.content, groupContext.content, pixels, groupContext.paddingPx);

                pageContext.bakeWorkIndex++;
                return true;
            }

            if (pageContext.saveIndex < groupContext.atlasPropList.Count)
            {
                string prop = groupContext.atlasPropList[pageContext.saveIndex];
                var atlas = pageContext.atlasByProp[prop];
                SaveAtlasTexture(
                    prop,
                    atlas,
                    groupContext.texMeta,
                    pageContext.pageFolder,
                    groupContext.atlasSize,
                    context.log,
                    context.result.textureImports);

                pageContext.saveIndex++;
                return true;
            }

            if (!pageContext.materialCreated)
            {
                string matPath = CreateMergedMaterial(
                    groupContext.group,
                    pageContext.pageItems[0].mat,
                    groupContext.pageIndex,
                    pageContext.pageFolder,
                    context.log,
                    groupContext.pageCount > 1);

                groupContext.buildData.pageInfos.Add(new PageBuildInfo
                {
                    atlasSize = pageContext.actualAtlasSize,
                    gridCols = pageContext.actualGridCols,
                    mergedMaterial = null,
                    materialPath = matPath
                });

                pageContext.materialCreated = true;
                LoggingService?.Success($"Page {groupContext.pageIndex + 1}/{groupContext.pageCount} complete");

                groupContext.pageIndex++;
                groupContext.pageContext = null;
                return true;
            }

            return false;
        }

        private Dictionary<string, Texture2D> CreateAtlasTextures(
            HashSet<string> props, 
            Dictionary<string, Row> texMeta, 
            int size)
        {
            var result = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

            foreach (var prop in props)
            {
                bool normalLike = texMeta.TryGetValue(prop, out var meta) 
                    ? meta.isNormalLike 
                    : BuildUtility.IsNormalMapProperty(prop);
                bool sRGB = texMeta.TryGetValue(prop, out meta) 
                    ? meta.isSRGB 
                    : !BuildUtility.IsLinearProperty(prop);
                if (normalLike) sRGB = false;

                result[prop] = AtlasGenerator.CreateAtlas(size, sRGB);
            }

            return result;
        }

        private void BakeTiles(
            List<MatInfo> pageItems,
            HashSet<string> allAtlasProps,
            Dictionary<string, Texture2D> atlasByProp,
            Dictionary<string, Row> texMeta,
            List<Row> bakeRows,
            int gridCols, int cell, int content, int paddingPx)
        {
            for (int i = 0; i < pageItems.Count; i++)
            {
                int gx = i % gridCols;
                int gy = i / gridCols;
                int px = gx * cell + paddingPx;
                int py = gy * cell + paddingPx;

                var mat = pageItems[i].mat;

                foreach (var prop in allAtlasProps)
                {
                    var atlas = atlasByProp[prop];
                    var pixels = GetTilePixels(mat, prop, texMeta, bakeRows, content);
                    AtlasGenerator.PutTileWithPadding(atlas, px, py, content, content, pixels, paddingPx);
                }
            }
        }

        private Color32[] GetTilePixels(
            Material mat, 
            string prop, 
            Dictionary<string, Row> texMeta,
            List<Row> bakeRows,
            int content)
        {
            // Check for solid color bake
            var colorRule = bakeRows.FirstOrDefault(d =>
                d.bakeMode == BakeMode.BakeColorToTexture &&
                d.type == ShaderUtil.ShaderPropertyType.Color &&
                d.targetTexProp == prop);

            if (colorRule != null)
            {
                Color c = Color.white;
                if (mat && mat.HasProperty(colorRule.name)) 
                    c = mat.GetColor(colorRule.name);
                if (!colorRule.includeAlpha) c.a = 1f;
                float m = TextureProcessor.EvaluateModifier(mat, colorRule);
                c = TextureProcessor.ApplyModifierToColor(c, m, colorRule);
                return TextureProcessor.CreateSolidPixels(content, content, c);
            }

            // Check for scalar bake
            var scalarRule = bakeRows.FirstOrDefault(d =>
                d.bakeMode == BakeMode.BakeScalarToGrayscale &&
                (d.type == ShaderUtil.ShaderPropertyType.Float || d.type == ShaderUtil.ShaderPropertyType.Range) &&
                d.targetTexProp == prop);

            if (scalarRule != null)
            {
                float v = 1f;
                if (mat && mat.HasProperty(scalarRule.name)) 
                    v = mat.GetFloat(scalarRule.name);
                float m = TextureProcessor.EvaluateModifier(mat, scalarRule);
                v = TextureProcessor.ApplyModifierToScalar(v, m, scalarRule);
                v = Mathf.Clamp01(v);
                return TextureProcessor.CreateSolidPixels(content, content, new Color(v, v, v, 1f));
            }

            // Sample texture
            Texture2D src = (mat && mat.HasProperty(prop)) ? (mat.GetTexture(prop) as Texture2D) : null;
            Vector4 st = BuildUtility.GetScaleTiling(mat, prop);

            bool normalLike = texMeta.TryGetValue(prop, out var meta) 
                ? meta.isNormalLike 
                : BuildUtility.IsNormalMapProperty(prop);
            bool sRGB = texMeta.TryGetValue(prop, out meta) 
                ? meta.isSRGB 
                : !BuildUtility.IsLinearProperty(prop);
            if (normalLike) sRGB = false;

            var pixels = TextureProcessor.SampleWithScaleTiling(src, st, content, content, sRGB, normalLike);

            // Apply color multiply rules
            foreach (var mulRule in bakeRows.Where(d =>
                d.bakeMode == BakeMode.MultiplyColorWithTexture &&
                d.type == ShaderUtil.ShaderPropertyType.Color &&
                d.targetTexProp == prop))
            {
                if (mat && mat.HasProperty(mulRule.name))
                {
                    Color mul = mat.GetColor(mulRule.name);
                    if (!mulRule.includeAlpha) mul.a = 1f;
                    float mm = TextureProcessor.EvaluateModifier(mat, mulRule);
                    mul = TextureProcessor.ApplyModifierToColor(mul, mm, mulRule);
                    TextureProcessor.MultiplyPixels(pixels, mul);
                }
            }

            return pixels;
        }

        private void SaveAtlasTextures(
            Dictionary<string, Texture2D> atlasByProp,
            Dictionary<string, Row> texMeta,
            string pageFolder,
            int maxSize,
            KibaMultiAtlasMergerLog log,
            List<TextureImportRequest> textureImports)
        {
            foreach (var kv in atlasByProp)
            {
                SaveAtlasTexture(
                    kv.Key,
                    kv.Value,
                    texMeta,
                    pageFolder,
                    maxSize,
                    log,
                    textureImports);
            }
        }

        private void SaveAtlasTexture(
            string prop,
            Texture2D atlas,
            Dictionary<string, Row> texMeta,
            string pageFolder,
            int maxSize,
            KibaMultiAtlasMergerLog log,
            List<TextureImportRequest> textureImports)
        {
            if (!atlas || string.IsNullOrEmpty(prop)) return;

            atlas.Apply(true, false);
            string path = AtlasGenerator.SaveAtlasPNG(atlas, pageFolder, 
                $"{AtlasGenerator.SanitizeFileName(prop)}.png");
            if (log != null)
                log.createdAssetPaths.Add(path);

            bool normalLike = texMeta.TryGetValue(prop, out var meta) 
                ? meta.isNormalLike 
                : BuildUtility.IsNormalMapProperty(prop);
            bool sRGB = texMeta.TryGetValue(prop, out meta) 
                ? meta.isSRGB 
                : !BuildUtility.IsLinearProperty(prop);
            if (normalLike) sRGB = false;

            textureImports?.Add(new TextureImportRequest
            {
                assetPath = path,
                maxSize = maxSize,
                sRGB = sRGB,
                isNormalMap = normalLike
            });
        }

        private string CreateMergedMaterial(
            GroupScan g, 
            Material template, 
            int page, 
            string pageFolder,
            KibaMultiAtlasMergerLog log,
            bool usePageSuffix)
        {
            var merged = new Material(template);
            string baseName = BuildUtility.GetOutputMaterialName(g);
            baseName = AtlasGenerator.SanitizeFileName(baseName);
            if (string.IsNullOrEmpty(baseName)) baseName = "Merged";

            string matFile = usePageSuffix ? $"{baseName}_P{page:00}.mat" : $"{baseName}.mat";
            string matPath = AssetDatabase.GenerateUniqueAssetPath(
                Path.Combine(pageFolder, matFile).Replace("\\", "/"));

            AssetDatabase.CreateAsset(merged, matPath);
            log.createdAssetPaths.Add(matPath);

            LoggingService?.Info($"  Saved material: {Path.GetFileName(matPath)}");
            return matPath;
        }

        #endregion
    }
}
#endif
