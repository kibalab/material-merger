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
                    var buildData = BuildGroupAssets(g, log, settings, settings.OutputFolder);

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
            AssetDatabase.Refresh();

            return result;
        }

        public GroupBuildData BuildGroupAssets(
            GroupScan g,
            KibaMultiAtlasMergerLog log,
            BuildSettings settings,
            string outputFolder)
        {
            LoggingService?.Info($"Preparing group build: {g.shaderName}", $"Materials: {g.mats.Count}, Pages: {g.pageCount}");

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
            var mats = g.mats.ToList();
            int tilesPerPage = g.tilesPerPage;

            // Get texture metadata for color space detection
            var texMeta = g.rows
                .Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv)
                .ToDictionary(r => r.name, r => r, StringComparer.Ordinal);

            var enabledTexProps = BuildUtility.GetActiveTextureProperties(g);
            var bakeRows = BuildUtility.GetBakeRows(g);
            var allAtlasProps = BuildUtility.GetAllAtlasProperties(g);

            LoggingService?.Info($"Properties: {enabledTexProps.Count} textures, {bakeRows.Count} bake, {allAtlasProps.Count} total");

            // Build each page
            for (int page = 0; page < g.pageCount; page++)
            {
                LoggingService?.Info($"Processing page {page + 1}/{g.pageCount}");

                int start = page * tilesPerPage;
                int end = Mathf.Min(mats.Count, start + tilesPerPage);
                var pageItems = mats.GetRange(start, end - start);

                string pageFolder = g.pageCount > 1
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
                SaveAtlasTextures(atlasByProp, texMeta, pageFolder, atlasSize, log);

                // Create merged material
                string matPath = CreateMergedMaterial(g, pageItems[0].mat, page, pageFolder, log);

                result.pageInfos.Add(new PageBuildInfo
                {
                    atlasSize = actualAtlasSize,
                    gridCols = actualGridCols,
                    mergedMaterial = null,
                    materialPath = matPath
                });

                LoggingService?.Success($"Page {page + 1}/{g.pageCount} complete");
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
            KibaMultiAtlasMergerLog log)
        {
            foreach (var kv in atlasByProp)
            {
                kv.Value.Apply(true, false);
                string path = AtlasGenerator.SaveAtlasPNG(kv.Value, pageFolder, 
                    $"{AtlasGenerator.SanitizeFileName(kv.Key)}.png");
                log.createdAssetPaths.Add(path);

                bool normalLike = texMeta.TryGetValue(kv.Key, out var meta) 
                    ? meta.isNormalLike 
                    : BuildUtility.IsNormalMapProperty(kv.Key);
                bool sRGB = texMeta.TryGetValue(kv.Key, out meta) 
                    ? meta.isSRGB 
                    : !BuildUtility.IsLinearProperty(kv.Key);

                AtlasGenerator.ConfigureImporter(path, maxSize, sRGB, normalLike);
            }
        }

        private string CreateMergedMaterial(
            GroupScan g, 
            Material template, 
            int page, 
            string pageFolder,
            KibaMultiAtlasMergerLog log)
        {
            var merged = new Material(template);
            string baseName = BuildUtility.GetOutputMaterialName(g);
            baseName = AtlasGenerator.SanitizeFileName(baseName);
            if (string.IsNullOrEmpty(baseName)) baseName = "Merged";

            string matFile = g.pageCount > 1 ? $"{baseName}_P{page:00}.mat" : $"{baseName}.mat";
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
