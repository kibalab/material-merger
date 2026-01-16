#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Localization;
using K13A.MaterialMerger.Editor.Services.Logging;

namespace K13A.MaterialMerger.Editor.Services
{
    public class MaterialBuildService : IMaterialBuildService
    {
        public IAtlasGenerator AtlasGenerator { get; set; }
        public ITextureProcessor TextureProcessor { get; set; }
        public IMeshRemapper MeshRemapper { get; set; }
        public IMaterialScanService ScanService { get; set; }
        public ILocalizationService LocalizationService { get; set; }
        public ILoggingService LoggingService { get; set; }

        private struct PageTileInfo
        {
            public int pageIndex;
            public int tileIndex;
        }

        private class PageBuildInfo
        {
            public int atlasSize;
            public int gridCols;
            public Material mergedMaterial;
            public string materialPath;
        }

        private class GroupBuildData
        {
            public GroupKey groupKey;
            public List<PageBuildInfo> pageInfos;
            public Dictionary<Material, PageTileInfo> matToPage;
        }

        public void BuildAndApplyWithConfirm(
            IBuildExecutor executor,
            GameObject root,
            List<GroupScan> scans,
            DiffPolicy diffPolicy,
            Material sampleMaterial,
            string outputFolder)
        {
            if (scans == null || scans.Count == 0)
            {
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogNoScan), "OK");
                return;
            }

            var mergedScans = GroupMergeUtility.BuildMergedScans(scans, ScanService);
            if (mergedScans.Count == 0)
            {
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogNoPlan), "OK");
                return;
            }

            var list = new List<ConfirmWindow.GroupInfo>();

            foreach (var g in mergedScans)
            {
                if (!g.enabled) continue;

                var shaderName = g.key.shader ? g.key.shader.name : (!string.IsNullOrEmpty(g.shaderName) ? g.shaderName : "NULL_SHADER");
                var title = $"{shaderName} [{g.tag}]  (mat:{g.mats.Count}, pages:{g.pageCount})";

                bool hasUnresolved = g.rows.Any(r =>
                    (r.type == ShaderUtil.ShaderPropertyType.Color ||
                     r.type == ShaderUtil.ShaderPropertyType.Float ||
                     r.type == ShaderUtil.ShaderPropertyType.Range ||
                     r.type == ShaderUtil.ShaderPropertyType.Vector) &&
                    r.distinctCount > 1 && !r.doAction);

                bool willRun = !(hasUnresolved && diffPolicy == DiffPolicy.StopIfUnresolved);

                var atlasProps = g.rows
                    .Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.doAction)
                    .Select(r => r.name)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                bool NeedsTarget(Row r) =>
                    r.bakeMode == BakeMode.BakeColorToTexture ||
                    r.bakeMode == BakeMode.BakeScalarToGrayscale ||
                    r.bakeMode == BakeMode.MultiplyColorWithTexture;

                var generatedProps = g.rows
                    .Where(r => r.doAction && r.type != ShaderUtil.ShaderPropertyType.TexEnv && NeedsTarget(r))
                    .Select(r => r.targetTexProp)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                var gi = new ConfirmWindow.GroupInfo();
                gi.title = title;
                gi.willRun = willRun;
                gi.skipReason = willRun ? "" : LocalizationService.Get(L10nKey.UnresolvedDiffReason);
                gi.atlasProps = atlasProps;
                gi.generatedProps = generatedProps;

                list.Add(gi);
            }

            if (list.Count == 0)
            {
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogNoPlan), "OK");
                return;
            }

            ConfirmWindow.Open(executor, list, LocalizationService);
        }

        public void BuildAndApply(BuildSettings settings, List<GroupScan> scans)
        {
            // Validate settings
            var (isValid, errorMessage) = BuildUtility.ValidateBuildSettings(settings);
            if (!isValid)
            {
                LoggingService?.Error("Invalid build settings", errorMessage, true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), errorMessage, "OK");
                return;
            }

            LoggingService?.Info("═══════════════════════════════════════", null, true);
            LoggingService?.Info("    Build & Apply Started", $"Atlas size: {settings.AtlasSize}, Grid: {settings.Grid}x{settings.Grid}", true);
            LoggingService?.Info("═══════════════════════════════════════", null, true);

            if (TextureProcessor == null || AtlasGenerator == null || MeshRemapper == null || ScanService == null)
            {
                LoggingService?.Error("Service initialization error", "Required services not initialized", true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogServiceNotInitialized), "OK");
                return;
            }

            LoggingService?.Info("Creating output folder", settings.OutputFolder);
            Directory.CreateDirectory(settings.OutputFolder);

            string blitShaderPath = Path.Combine(settings.OutputFolder, Constants.BlitShaderFileName).Replace("\\", "/");
            LoggingService?.Info("Preparing blit material", blitShaderPath);
            TextureProcessor.EnsureBlitMaterial(blitShaderPath);
            if (!TextureProcessor.BlitMaterial)
            {
                LoggingService?.Error("Blit material initialization failed", blitShaderPath, true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogBlitFailed), "OK");
                return;
            }

            // Use settings values
            var root = settings.Root;
            var outputFolder = settings.OutputFolder;
            var atlasSize = settings.AtlasSize;
            var grid = settings.Grid;
            var paddingPx = settings.PaddingPx;
            var diffPolicy = settings.DiffPolicy;
            var sampleMaterial = settings.SampleMaterial;
            var cloneRootOnApply = settings.CloneRootOnApply;
            var deactivateOriginalRoot = settings.DeactivateOriginalRoot;
            var keepPrefabOnClone = settings.KeepPrefabOnClone;
            var groupByKeywords = settings.GroupByKeywords;
            var groupByRenderQueue = settings.GroupByRenderQueue;
            var splitOpaqueTransparent = settings.SplitOpaqueTransparent;

            int cell = settings.CellSize;
            int content = settings.ContentSize;

            var log = ScriptableObject.CreateInstance<KibaMultiAtlasMergerLog>();
            string logPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(outputFolder, $"{Constants.LogFilePrefix}{DateTime.Now:yyyyMMdd_HHmmss}.asset").Replace("\\", "/"));
            AssetDatabase.CreateAsset(log, logPath);
            log.createdAssetPaths.Add(logPath);

            // ===== PHASE 1: Build all assets (textures and materials) in batch mode =====
            LoggingService?.Info("═══ Phase 1: Building Assets ═══");
            LoggingService?.Info("Starting batch asset creation (editor will remain responsive)");

            var groupBuildData = new List<GroupBuildData>();

            AssetDatabase.StartAssetEditing();

            try
            {
                var mergedScans = GroupMergeUtility.BuildMergedScans(scans, ScanService);

                int processedCount = 0;
                int skippedCount = 0;

                foreach (var g in mergedScans)
                {
                    if (!g.enabled)
                    {
                        skippedCount++;
                        continue;
                    }

                    if (!BuildUtility.ShouldProcessGroup(g, diffPolicy))
                    {
                        LoggingService?.Warning("Group skipped", $"Unresolved diff: {g.shaderName} [{g.tag}]");
                        skippedCount++;
                        continue;
                    }

                    LoggingService?.Info("Building assets for group", $"{g.shaderName} [{g.tag}]");
                    GroupBuildData buildData = BuildGroupAssets(g, log, cell, content, atlasSize, grid, paddingPx, outputFolder);

                    if (buildData != null)
                    {
                        groupBuildData.Add(buildData);
                        LoggingService?.Success("Group assets complete", $"{g.shaderName} [{g.tag}]");
                        processedCount++;
                    }
                }

                LoggingService?.Info($"Asset creation complete: {processedCount} groups, {skippedCount} skipped");
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            // Save and import all assets at once
            LoggingService?.Info("Saving and importing all assets (this may take a moment)...");
            EditorUtility.SetDirty(log);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LoggingService?.Success("Asset import complete");

            // ===== PHASE 2: Clone root and apply materials to renderers =====
            LoggingService?.Info("═══ Phase 2: Applying to Scene ═══");

            GameObject applyRootObj = root;
            if (cloneRootOnApply && root)
            {
                LoggingService?.Info("Cloning root", $"Original: {root.name}, KeepPrefab: {keepPrefabOnClone}");
                applyRootObj = CloneRootForApply(root, deactivateOriginalRoot, keepPrefabOnClone);
                if (!applyRootObj)
                {
                    LoggingService?.Error("Root clone failed");
                    return;
                }

                LoggingService?.Success("Root cloned", $"Clone: {applyRootObj.name}");
            }

            log.sourceRootGlobalId = root ? GlobalObjectId.GetGlobalObjectIdSlow(root).ToString() : "";
            log.appliedRootGlobalId = applyRootObj ? GlobalObjectId.GetGlobalObjectIdSlow(applyRootObj).ToString() : "";

            LoggingService?.Info("Rescanning apply target");
            var applyScans = ScanService.ScanGameObject(
                applyRootObj,
                groupByKeywords,
                groupByRenderQueue,
                splitOpaqueTransparent,
                grid
            );
            CopySettings(scans, applyScans);
            var mergedApplyScans = GroupMergeUtility.BuildMergedScans(applyScans, ScanService);
            LoggingService?.Info("Rescan complete", $"{mergedApplyScans.Count} groups");

            Undo.IncrementCurrentGroup();
            int ug = Undo.GetCurrentGroup();

            // Apply to renderers using the build data
            LoggingService?.Info("Applying materials to renderers");
            int appliedCount = 0;

            foreach (var buildData in groupBuildData)
            {
                // Find matching group in applyScans
                var matchingGroup = mergedApplyScans.FirstOrDefault(g =>
                    g.key.Equals(buildData.groupKey) && g.enabled);

                if (matchingGroup != null)
                {
                    ApplyGroupToRenderers(matchingGroup, buildData, log, cell, content, paddingPx, outputFolder, diffPolicy, sampleMaterial);
                    appliedCount++;
                }
            }

            Undo.CollapseUndoOperations(ug);
            EditorUtility.SetDirty(log);
            AssetDatabase.SaveAssets();

            LoggingService?.Info("═══════════════════════════════════════", null, true);
            LoggingService?.Success("    Build & Apply Complete", $"Processed: {appliedCount} groups", true);
            LoggingService?.Info("═══════════════════════════════════════", null, true);

            var message = $"{LocalizationService.Get(L10nKey.DialogComplete)}\n{LocalizationService.Get(L10nKey.DialogLog, logPath)}";
            EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), message, "OK");
        }

        public GameObject CloneRootForApply(GameObject src, bool deactivateOriginal, bool keepPrefab)
        {
            var parent = src.transform.parent;
            GameObject clone;

            // 프리팹 유지 옵션이 켜져있고 프리팹 인스턴스인 경우
            if (keepPrefab && PrefabUtility.IsPartOfPrefabInstance(src))
            {
                LoggingService?.Info("Cloning as prefab instance");

                try
                {
                    // 프리팹 루트인지 확인
                    var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(src);

                    if (prefabInstanceRoot == src)
                    {
                        // src가 프리팹 루트인 경우
                        // 원본 프리팹 에셋 가져오기
                        var prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(src);

                        if (!string.IsNullOrEmpty(prefabAssetPath))
                        {
                            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
                            if (prefabAsset != null)
                            {
                                // 프리팹 에셋으로부터 새 인스턴스 생성
                                var instantiated = PrefabUtility.InstantiatePrefab(prefabAsset, parent);
                                clone = instantiated as GameObject;

                                if (clone != null)
                                {
                                    LoggingService?.Success("Prefab instance created successfully");
                                }
                                else
                                {
                                    LoggingService?.Warning("Prefab instantiation returned non-GameObject, falling back");
                                    clone = UnityEngine.Object.Instantiate(src, parent);
                                }
                            }
                            else
                            {
                                LoggingService?.Warning("Failed to load prefab asset, falling back to regular clone");
                                clone = UnityEngine.Object.Instantiate(src, parent);
                            }
                        }
                        else
                        {
                            LoggingService?.Warning("Failed to get prefab asset path, falling back to regular clone");
                            clone = UnityEngine.Object.Instantiate(src, parent);
                        }
                    }
                    else
                    {
                        // src가 프리팹의 자식인 경우 - 일반 복제 사용
                        LoggingService?.Warning("Source is not a prefab root, falling back to regular clone");
                        clone = UnityEngine.Object.Instantiate(src, parent);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService?.Error("Exception during prefab clone", ex.Message);
                    clone = UnityEngine.Object.Instantiate(src, parent);
                }
            }
            else
            {
                LoggingService?.Info("Cloning as regular instance (unpacked)");
                // 일반 복제 (프리팹 언팩)
                clone = UnityEngine.Object.Instantiate(src, parent);
            }

            clone.name = src.name + "_AtlasMerged";
            clone.transform.localPosition = src.transform.localPosition;
            clone.transform.localRotation = src.transform.localRotation;
            clone.transform.localScale = src.transform.localScale;
            Undo.RegisterCreatedObjectUndo(clone, "Clone root");

            if (deactivateOriginal)
            {
                Undo.RecordObject(src, "Deactivate original root");
                src.SetActive(false);
            }

            Selection.activeGameObject = clone;
            return clone;
        }

        public void CopySettings(List<GroupScan> from, List<GroupScan> to)
        {
            var map = new Dictionary<GroupKey, GroupScan>();
            foreach (var g in from) map[g.key] = g;

            foreach (var tg in to)
            {
                if (!map.TryGetValue(tg.key, out var sg)) continue;

                tg.enabled = sg.enabled;
                tg.foldout = sg.foldout;
                tg.search = sg.search;
                tg.onlyRelevant = sg.onlyRelevant;
                tg.showTexturesOnly = sg.showTexturesOnly;
                tg.showScalarsOnly = sg.showScalarsOnly;
                tg.outputMaterialName = string.IsNullOrEmpty(sg.outputMaterialName) ? "Merged" : sg.outputMaterialName;
                tg.mergeKey = sg.mergeKey;

                var srcRow = sg.rows
                    .Where(r => r != null && !string.IsNullOrEmpty(r.name) && r.name != "_DummyProperty")
                    .GroupBy(r => r.name, StringComparer.Ordinal)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

                foreach (var tr in tg.rows)
                {
                    if (tr == null) continue;
                    if (string.IsNullOrEmpty(tr.name)) continue;
                    if (tr.name == "_DummyProperty") continue;

                    if (!srcRow.TryGetValue(tr.name, out var sr)) continue;

                    tr.doAction = sr.doAction;

                    tr.bakeMode = sr.bakeMode;
                    tr.includeAlpha = sr.includeAlpha;
                    tr.resetSourceAfterBake = sr.resetSourceAfterBake;

                    tr.targetTexProp = sr.targetTexProp;

                    if (tg.shaderTexProps != null && tg.shaderTexProps.Count > 0)
                    {
                        var maxTex = Mathf.Max(0, tg.shaderTexProps.Count - 1);
                        tr.targetTexIndex = Mathf.Clamp((int)sr.targetTexIndex, 0, maxTex);
                        tr.targetTexProp = tg.shaderTexProps[tr.targetTexIndex];
                    }
                    else
                    {
                        tr.targetTexIndex = 0;
                        tr.targetTexProp = null;
                    }

                    tr.modOp = sr.modOp;
                    tr.modClamp01 = sr.modClamp01;
                    tr.modScale = sr.modScale;
                    tr.modBias = sr.modBias;
                    tr.modAffectsAlpha = sr.modAffectsAlpha;

                    tr.modProp = sr.modProp;

                    if (tg.shaderScalarProps != null && tg.shaderScalarProps.Count > 0)
                    {
                        var maxSca = Mathf.Max(0, tg.shaderScalarProps.Count - 1);
                        tr.modPropIndex = Mathf.Clamp((int)sr.modPropIndex, 0, maxSca);
                        tr.modProp = tg.shaderScalarProps[tr.modPropIndex];
                    }
                    else
                    {
                        tr.modPropIndex = 0;
                        tr.modProp = null;
                    }

                    tr.expanded = sr.expanded;
                }
            }
        }

        private GroupBuildData BuildGroupAssets(
            GroupScan g,
            KibaMultiAtlasMergerLog log,
            int cell,
            int content,
            int atlasSize,
            int grid,
            int paddingPx,
            string outputFolder)
        {
            LoggingService?.Info($"Preparing group build: {g.shaderName}", $"Materials: {g.mats.Count}, Pages: {g.pageCount}");

            if (!TextureProcessor.BlitMaterial)
            {
                LoggingService?.Error("Blit material is missing", $"{g.shaderName} [{g.tag}]");
                return null;
            }

            string planFolder = GetPlanFolderName(g);
            string groupFolder = Path.Combine(outputFolder, planFolder).Replace("\\", "/");
            Directory.CreateDirectory(groupFolder);
            LoggingService?.Info($"Created output folder: {planFolder}");

            var mats = g.mats.ToList();
            int tilesPerPage = g.tilesPerPage;

            var texMeta = g.rows.Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv).ToDictionary(r => r.name, r => r, StringComparer.Ordinal);

            var enabledTexProps = BuildUtility.GetActiveTextureProperties(g);
            LoggingService?.Info($"Active texture properties: {enabledTexProps.Count}", string.Join(", ", enabledTexProps));

            var bakeRows = BuildUtility.GetBakeRows(g);
            LoggingService?.Info($"Bake properties: {bakeRows.Count}");

            var resetRows = BuildUtility.GetResetRows(g);
            LoggingService?.Info($"Reset properties: {resetRows.Count}");

            var allAtlasProps = BuildUtility.GetAllAtlasProperties(g);
            LoggingService?.Info($"Total atlas properties: {allAtlasProps.Count}", string.Join(", ", allAtlasProps));

            var pageInfos = new List<PageBuildInfo>();
            var matToPage = new Dictionary<Material, PageTileInfo>();

            LoggingService?.Info($"Creating pages: {g.pageCount}");
            for (int page = 0; page < g.pageCount; page++)
            {
                LoggingService?.Info($"Processing page {page + 1}/{g.pageCount}");

                int start = page * tilesPerPage;
                int end = Mathf.Min(mats.Count, start + tilesPerPage);
                var pageItems = mats.GetRange(start, end - start);
                LoggingService?.Info($"  Page materials: {pageItems.Count}");

                string pageFolder = g.pageCount > 1
                    ? Path.Combine(groupFolder, $"Page_{page:00}").Replace("\\", "/")
                    : groupFolder;
                Directory.CreateDirectory(pageFolder);

                // Calculate optimal grid for this page using utility
                int actualMatCount = pageItems.Count;
                var (actualGridCols, actualGridRows) = BuildUtility.CalculateOptimalGrid(actualMatCount, grid);

                // Calculate atlas size using utility
                int actualAtlasSize = BuildUtility.CalculateAtlasSize(actualGridCols, actualGridRows, cell, atlasSize);

                for (int i = 0; i < pageItems.Count; i++)
                {
                    var mat = pageItems[i].mat;
                    if (mat)
                        matToPage[mat] = new PageTileInfo { pageIndex = page, tileIndex = i };
                }

                var atlasByProp = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

                LoggingService?.Info($"  Creating atlas textures: {allAtlasProps.Count}",
                    $"Size: {actualAtlasSize}x{actualAtlasSize}, Grid: {actualGridCols}x{actualGridRows}");

                foreach (var prop in allAtlasProps)
                {
                    bool normalLike = texMeta.TryGetValue(prop, out var meta) ? meta.isNormalLike : BuildUtility.IsNormalMapProperty(prop);
                    bool sRGB = texMeta.TryGetValue(prop, out meta) ? meta.isSRGB : !BuildUtility.IsLinearProperty(prop);
                    if (normalLike) sRGB = false;

                    // 정사각형 아틀라스 생성 (UV 찌그러짐 방지)
                    atlasByProp[prop] = AtlasGenerator.CreateAtlas(actualAtlasSize, sRGB);
                    LoggingService?.Info($"    Created atlas: {prop} ({(sRGB ? "sRGB" : "Linear")}{(normalLike ? ", Normal" : "")})");
                }

                LoggingService?.Info($"  Baking tiles: {pageItems.Count}");
                for (int i = 0; i < pageItems.Count; i++)
                {
                    int gx = i % actualGridCols;
                    int gy = i / actualGridCols;

                    int px = gx * cell + paddingPx;
                    int py = gy * cell + paddingPx;

                    var mi = pageItems[i];
                    var mat = mi.mat;

                    foreach (var prop in allAtlasProps)
                    {
                        var atlas = atlasByProp[prop];

                        var solidColorRules = bakeRows.Where(d =>
                            d.bakeMode == BakeMode.BakeColorToTexture &&
                            d.type == ShaderUtil.ShaderPropertyType.Color &&
                            d.targetTexProp == prop).ToList();

                        var solidScalarRules = bakeRows.Where(d =>
                            d.bakeMode == BakeMode.BakeScalarToGrayscale &&
                            (d.type == ShaderUtil.ShaderPropertyType.Float || d.type == ShaderUtil.ShaderPropertyType.Range) &&
                            d.targetTexProp == prop).ToList();

                        if (solidColorRules.Count > 0)
                        {
                            var rule = solidColorRules[0];
                            Color c = Color.white;
                            if (mat && mat.HasProperty(rule.name)) c = mat.GetColor(rule.name);
                            if (!rule.includeAlpha) c.a = 1f;
                            float m = TextureProcessor.EvaluateModifier(mat, rule);
                            c = TextureProcessor.ApplyModifierToColor(c, m, rule);
                            var solid = TextureProcessor.CreateSolidPixels(content, content, c);
                            AtlasGenerator.PutTileWithPadding(atlas, px, py, content, content, solid, paddingPx);
                            continue;
                        }

                        if (solidScalarRules.Count > 0)
                        {
                            var rule = solidScalarRules[0];
                            float v = 1f;
                            if (mat && mat.HasProperty(rule.name)) v = mat.GetFloat(rule.name);
                            float m = TextureProcessor.EvaluateModifier(mat, rule);
                            v = TextureProcessor.ApplyModifierToScalar(v, m, rule);
                            v = Mathf.Clamp01(v);
                            var solid = TextureProcessor.CreateSolidPixels(content, content, new Color(v, v, v, 1f));
                            AtlasGenerator.PutTileWithPadding(atlas, px, py, content, content, solid, paddingPx);
                            continue;
                        }

                        Texture2D src = (mat && mat.HasProperty(prop)) ? (mat.GetTexture(prop) as Texture2D) : null;
                        Vector4 st = BuildUtility.GetScaleTiling(mat, prop);

                        bool normalLike = texMeta.TryGetValue(prop, out var meta) ? meta.isNormalLike : BuildUtility.IsNormalMapProperty(prop);
                        bool sRGB = texMeta.TryGetValue(prop, out meta) ? meta.isSRGB : !BuildUtility.IsLinearProperty(prop);
                        if (normalLike) sRGB = false;

                        var pixels = TextureProcessor.SampleWithScaleTiling(src, st, content, content, sRGB, normalLike);

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

                        AtlasGenerator.PutTileWithPadding(atlas, px, py, content, content, pixels, paddingPx);
                    }
                }

                LoggingService?.Info($"  Saving atlas textures: {atlasByProp.Count}");
                foreach (var kv in atlasByProp)
                {
                    kv.Value.Apply(true, false);
                    string p = AtlasGenerator.SaveAtlasPNG(kv.Value, pageFolder, $"{AtlasGenerator.SanitizeFileName(kv.Key)}.png");
                    log.createdAssetPaths.Add(p);
                    LoggingService?.Info($"    Saved: {kv.Key} → {System.IO.Path.GetFileName(p)}");

                    bool normalLike = texMeta.TryGetValue(kv.Key, out var meta) ? meta.isNormalLike : BuildUtility.IsNormalMapProperty(kv.Key);
                    bool sRGB = texMeta.TryGetValue(kv.Key, out meta) ? meta.isSRGB : !BuildUtility.IsLinearProperty(kv.Key);
                    AtlasGenerator.ConfigureImporter(p, atlasSize, sRGB, normalLike);
                }

                LoggingService?.Info($"  Creating merged material");
                var template = pageItems[0].mat;
                var merged = new Material(template);

                string baseName = GetOutputMaterialBaseName(g);
                baseName = AtlasGenerator.SanitizeFileName(baseName);
                if (string.IsNullOrEmpty(baseName)) baseName = "Merged";
                string matFile = g.pageCount > 1 ? $"{baseName}_P{page:00}.mat" : $"{baseName}.mat";
                string matPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(pageFolder, matFile).Replace("\\", "/"));
                AssetDatabase.CreateAsset(merged, matPath);
                log.createdAssetPaths.Add(matPath);
                LoggingService?.Info($"  Saved material: {System.IO.Path.GetFileName(matPath)}");

                pageInfos.Add(new PageBuildInfo
                {
                    atlasSize = actualAtlasSize,
                    gridCols = actualGridCols,
                    mergedMaterial = null,
                    materialPath = matPath
                });

                LoggingService?.Success($"Page {page + 1}/{g.pageCount} complete");
            }

            if (matToPage.Count == 0 || pageInfos.Count == 0)
            {
                LoggingService?.Warning("No material/page info to apply");
                return null;
            }

            LoggingService?.Success($"Group assets built: {g.shaderName}");

            return new GroupBuildData
            {
                groupKey = g.key,
                pageInfos = pageInfos,
                matToPage = matToPage
            };
        }

        private void ApplyGroupToRenderers(
            GroupScan g,
            GroupBuildData buildData,
            KibaMultiAtlasMergerLog log,
            int cell,
            int content,
            int paddingPx,
            string outputFolder,
            DiffPolicy diffPolicy,
            Material sampleMaterial)
        {
            if (g == null || buildData == null)
            {
                LoggingService?.Warning("Apply skipped: invalid group or build data");
                return;
            }

            if (buildData.matToPage == null || buildData.matToPage.Count == 0 ||
                buildData.pageInfos == null || buildData.pageInfos.Count == 0)
            {
                LoggingService?.Warning("Apply skipped: missing page/material data", $"{g.shaderName} [{g.tag}]");
                return;
            }

            List<string> enabledTexProps = g.rows
                .Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.doAction)
                .Select(r => r.name)
                .ToList();

            List<Row> bakeRows = g.rows
                .Where(r =>
                    r.doAction &&
                    r.type != ShaderUtil.ShaderPropertyType.TexEnv &&
                    (r.bakeMode == BakeMode.BakeColorToTexture ||
                     r.bakeMode == BakeMode.BakeScalarToGrayscale ||
                     r.bakeMode == BakeMode.MultiplyColorWithTexture))
                .ToList();

            List<Row> resetRows = g.rows
                .Where(r =>
                    r.doAction &&
                    r.type != ShaderUtil.ShaderPropertyType.TexEnv &&
                    r.bakeMode == BakeMode.ResetToDefault)
                .ToList();

            HashSet<string> allAtlasProps = new HashSet<string>(enabledTexProps, StringComparer.Ordinal);
            foreach (Row r in bakeRows)
            {
                if (string.IsNullOrEmpty(r.targetTexProp)) continue;
                allAtlasProps.Add(r.targetTexProp);
            }

            string planFolder = GetPlanFolderName(g);
            string groupFolder = Path.Combine(outputFolder, planFolder).Replace("\\", "/");

            LoggingService?.Info($"Loading merged materials: {g.shaderName} [{g.tag}]");
            bool hasMissingMaterial = false;

            for (int page = 0; page < buildData.pageInfos.Count; page++)
            {
                PageBuildInfo pageInfo = buildData.pageInfos[page];
                if (string.IsNullOrEmpty(pageInfo.materialPath))
                {
                    LoggingService?.Warning("Missing material path", $"{g.shaderName} [{g.tag}] page {page + 1}");
                    hasMissingMaterial = true;
                    continue;
                }

                Material merged = AssetDatabase.LoadAssetAtPath<Material>(pageInfo.materialPath);
                if (!merged)
                {
                    LoggingService?.Warning("Failed to load merged material", pageInfo.materialPath);
                    hasMissingMaterial = true;
                    continue;
                }

                string pageFolder = g.pageCount > 1
                    ? Path.Combine(groupFolder, $"Page_{page:00}").Replace("\\", "/")
                    : groupFolder;

                foreach (string prop in allAtlasProps)
                {
                    string atlasPath = Path.Combine(pageFolder, $"{AtlasGenerator.SanitizeFileName(prop)}.png").Replace("\\", "/");
                    Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                    if (tex && merged.HasProperty(prop)) merged.SetTexture(prop, tex);

                    string stName = prop + "_ST";
                    if (merged.HasProperty(stName)) merged.SetVector(stName, new Vector4(1, 1, 0, 0));
                }

                Material defMat = TextureProcessor.GetDefaultMaterial(g.key.shader);

                foreach (Row r in resetRows)
                {
                    if (!merged || !merged.HasProperty(r.name) || !defMat) continue;

                    if (r.type == ShaderUtil.ShaderPropertyType.Color)
                        merged.SetColor(r.name, defMat.GetColor(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Vector)
                        merged.SetVector(r.name, defMat.GetVector(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Float || r.type == ShaderUtil.ShaderPropertyType.Range)
                        merged.SetFloat(r.name, defMat.GetFloat(r.name));
                }

                foreach (Row r in g.rows)
                {
                    if (!r.doAction) continue;
                    if (!r.resetSourceAfterBake) continue;
                    if (!merged || !merged.HasProperty(r.name) || !defMat) continue;

                    bool bakedOrMul =
                        r.bakeMode == BakeMode.BakeColorToTexture ||
                        r.bakeMode == BakeMode.BakeScalarToGrayscale ||
                        r.bakeMode == BakeMode.MultiplyColorWithTexture;

                    if (!bakedOrMul) continue;

                    if (r.type == ShaderUtil.ShaderPropertyType.Color)
                        merged.SetColor(r.name, defMat.GetColor(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Vector)
                        merged.SetVector(r.name, defMat.GetVector(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Float || r.type == ShaderUtil.ShaderPropertyType.Range)
                        merged.SetFloat(r.name, defMat.GetFloat(r.name));
                }

                if (diffPolicy == DiffPolicy.UseSampleMaterial && sampleMaterial)
                    ApplySampleMaterialOverrides(g, merged, sampleMaterial);

                EditorUtility.SetDirty(merged);
                pageInfo.mergedMaterial = merged;
            }

            if (hasMissingMaterial || buildData.pageInfos.Any(p => p == null || !p.mergedMaterial))
            {
                LoggingService?.Error("Apply skipped: merged materials missing", $"{g.shaderName} [{g.tag}]");
                return;
            }

            ApplyToRenderers(g, buildData.matToPage, buildData.pageInfos, log, cell, content, paddingPx, outputFolder);
        }

        private void ApplySampleMaterialOverrides(GroupScan g, Material merged, Material sampleMaterial)
        {
            if (g == null || !merged || !sampleMaterial) return;

            foreach (var r in g.rows)
            {
                if (r == null || r.doAction || r.distinctCount <= 1) continue;
                if (!merged.HasProperty(r.name) || !sampleMaterial.HasProperty(r.name)) continue;

                if (r.type == ShaderUtil.ShaderPropertyType.Color)
                    merged.SetColor(r.name, sampleMaterial.GetColor(r.name));
                else if (r.type == ShaderUtil.ShaderPropertyType.Vector)
                    merged.SetVector(r.name, sampleMaterial.GetVector(r.name));
                else if (r.type == ShaderUtil.ShaderPropertyType.Float || r.type == ShaderUtil.ShaderPropertyType.Range)
                    merged.SetFloat(r.name, sampleMaterial.GetFloat(r.name));
            }
        }

        private void ApplyToRenderers(
            GroupScan g,
            Dictionary<Material, PageTileInfo> matToPage,
            List<PageBuildInfo> pageInfos,
            KibaMultiAtlasMergerLog log,
            int cell,
            int content,
            int paddingPx,
            string outputFolder)
        {
            var renderers = new HashSet<Renderer>();
            foreach (var mi in g.mats)
            foreach (var u in mi.users)
                if (u)
                    renderers.Add(u);

            LoggingService?.Info($"  Renderers to process: {renderers.Count}");

            var meshCache = new Dictionary<(Mesh, string), Mesh>();
            int processedRenderers = 0;

            foreach (var r in renderers)
            {
                if (!r) continue;

                var shared = r.sharedMaterials;
                if (shared == null || shared.Length == 0) continue;

                Mesh beforeMesh = null;
                Mesh afterMesh = null;
                MeshFilter mf = null;
                SkinnedMeshRenderer smr = null;

                if (r is SkinnedMeshRenderer smrRenderer)
                {
                    smr = smrRenderer;
                    beforeMesh = smr.sharedMesh;
                }
                else
                {
                    mf = r.GetComponent<MeshFilter>();
                    if (mf) beforeMesh = mf.sharedMesh;
                }

                if (!beforeMesh) continue;

                int subMeshCount = beforeMesh.subMeshCount;
                int maxIndex = Mathf.Min(shared.Length, subMeshCount);
                var transforms = new List<SubmeshUvTransform>();
                var beforeMats = shared.ToArray();
                bool replaced = false;

                for (int s = 0; s < maxIndex; s++)
                {
                    var mat = beforeMats[s];
                    if (!mat) continue;
                    if (!matToPage.TryGetValue(mat, out var pageTile)) continue;
                    if (pageTile.pageIndex < 0 || pageTile.pageIndex >= pageInfos.Count) continue;

                    var page = pageInfos[pageTile.pageIndex];
                    if (page.atlasSize <= 0 || page.gridCols <= 0) continue;

                    int gx = pageTile.tileIndex % page.gridCols;
                    int gy = pageTile.tileIndex / page.gridCols;
                    float tileScale = content / (float)page.atlasSize;
                    float ox = (gx * cell + paddingPx) / (float)page.atlasSize;
                    float oy = (gy * cell + paddingPx) / (float)page.atlasSize;

                    transforms.Add(new SubmeshUvTransform
                    {
                        subMeshIndex = s,
                        scale = new Vector2(tileScale, tileScale),
                        offset = new Vector2(ox, oy)
                    });

                    shared[s] = page.mergedMaterial;
                    replaced = true;
                }

                if (beforeMats.Length > 0 && beforeMats.Length < subMeshCount)
                {
                    var fallbackMat = beforeMats[beforeMats.Length - 1];
                    if (fallbackMat && matToPage.TryGetValue(fallbackMat, out var fallbackPage))
                    {
                        if (fallbackPage.pageIndex >= 0 && fallbackPage.pageIndex < pageInfos.Count)
                        {
                            var page = pageInfos[fallbackPage.pageIndex];
                            if (page.atlasSize > 0 && page.gridCols > 0)
                            {
                                int gx = fallbackPage.tileIndex % page.gridCols;
                                int gy = fallbackPage.tileIndex / page.gridCols;
                                float tileScale = content / (float)page.atlasSize;
                                float ox = (gx * cell + paddingPx) / (float)page.atlasSize;
                                float oy = (gy * cell + paddingPx) / (float)page.atlasSize;

                                if (shared.Length > 0)
                                    shared[shared.Length - 1] = page.mergedMaterial;
                                replaced = true;

                                for (int s = shared.Length; s < subMeshCount; s++)
                                {
                                    transforms.Add(new SubmeshUvTransform
                                    {
                                        subMeshIndex = s,
                                        scale = new Vector2(tileScale, tileScale),
                                        offset = new Vector2(ox, oy)
                                    });
                                }
                            }
                        }
                    }
                }

                if (!replaced || transforms.Count == 0) continue;

                transforms.Sort((a, b) => a.subMeshIndex.CompareTo(b.subMeshIndex));

                var mergeCandidates = new HashSet<Material>();
                foreach (var page in pageInfos)
                    if (page != null && page.mergedMaterial)
                        mergeCandidates.Add(page.mergedMaterial);

                int[] submeshMergeMap = null;
                if (BuildUtility.TryBuildSubmeshMergeMap(beforeMesh, shared, mergeCandidates, out var mergedMaterials, out var mergeMap))
                {
                    shared = mergedMaterials;
                    submeshMergeMap = mergeMap;
                }

                Undo.RecordObject(r, "머지 머티리얼 적용");
                r.sharedMaterials = shared;

                if (smr)
                {
                    Undo.RecordObject(smr, "메쉬 UV 리맵");
                    afterMesh = MeshRemapper.GetOrCreateRemappedMesh(beforeMesh, transforms, meshCache, outputFolder, submeshMergeMap);
                    smr.sharedMesh = afterMesh;
                }
                else if (mf)
                {
                    Undo.RecordObject(mf, "메쉬 UV 리맵");
                    afterMesh = MeshRemapper.GetOrCreateRemappedMesh(beforeMesh, transforms, meshCache, outputFolder, submeshMergeMap);
                    mf.sharedMesh = afterMesh;
                }

                var entry = new KibaMultiAtlasMergerLog.Entry();
                entry.rendererGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(r).ToString();
                entry.beforeMaterials = beforeMats;
                entry.afterMaterials = r.sharedMaterials.ToArray();
                entry.beforeMesh = beforeMesh;
                entry.afterMesh = afterMesh;
                log.entries.Add(entry);

                processedRenderers++;
                if (processedRenderers % 10 == 0 || processedRenderers == renderers.Count)
                {
                    LoggingService?.Info($"    Renderer progress: {processedRenderers}/{renderers.Count}");
                }
            }

            LoggingService?.Success($"  Renderers processed: {processedRenderers}");
        }

        private string GetPlanFolderName(GroupScan g)
        {
            string shaderName = g.key.shader ? g.key.shader.name : (string.IsNullOrEmpty(g.shaderName) ? "NullShader" : g.shaderName);
            string baseName = AtlasGenerator.SanitizeFileName(shaderName);
            string keyPart = $"RQ{g.key.renderQueue}_KW{g.key.keywordsHash}_T{g.key.transparencyKey}";
            string combined = $"{baseName}_{keyPart}";
            combined = AtlasGenerator.SanitizeFileName(combined);
            return string.IsNullOrEmpty(combined) ? "Plan" : combined;
        }

        private string GetOutputMaterialBaseName(GroupScan g)
        {
            if (!string.IsNullOrWhiteSpace(g.outputMaterialName)) return g.outputMaterialName;
            string shaderName = g.key.shader ? g.key.shader.name : g.shaderName;
            if (!string.IsNullOrWhiteSpace(shaderName)) return shaderName;
            return "Merged";
        }
    }
}
#endif