#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
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
        }

        public void BuildAndApplyWithConfirm(
            dynamic owner,
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

                bool willRun = !(hasUnresolved && diffPolicy == DiffPolicy.미해결이면중단);

                var atlasProps = g.rows
                    .Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.doAction)
                    .Select(r => r.name)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                bool NeedsTarget(Row r) =>
                    r.bakeMode == BakeMode.색상굽기_텍스처타일 ||
                    r.bakeMode == BakeMode.스칼라굽기_그레이타일 ||
                    r.bakeMode == BakeMode.색상곱_텍스처타일;

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

            ConfirmWindow.Open(owner, list, LocalizationService);
        }

        public void BuildAndApply(
            GameObject root,
            List<GroupScan> scans,
            DiffPolicy diffPolicy,
            Material sampleMaterial,
            bool cloneRootOnApply,
            bool deactivateOriginalRoot,
            bool keepPrefabOnClone,
            string outputFolder,
            int atlasSize,
            int grid,
            int paddingPx,
            bool groupByKeywords,
            bool groupByRenderQueue,
            bool splitOpaqueTransparent)
        {
            LoggingService?.Info("═══════════════════════════════════════", null, true);
            LoggingService?.Info("    Build & Apply Started", $"Atlas size: {atlasSize}, Grid: {grid}x{grid}", true);
            LoggingService?.Info("═══════════════════════════════════════", null, true);

            if (TextureProcessor == null || AtlasGenerator == null || MeshRemapper == null || ScanService == null)
            {
                LoggingService?.Error("Service initialization error", "Required services not initialized", true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogServiceNotInitialized), "OK");
                return;
            }

            if (cloneRootOnApply && !root)
            {
                LoggingService?.Error("Root required", "Clone root on apply is enabled but root is missing", true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogRootRequired), "OK");
                return;
            }

            LoggingService?.Info("Creating output folder", outputFolder);
            Directory.CreateDirectory(outputFolder);

            var log = ScriptableObject.CreateInstance<KibaMultiAtlasMergerLog>();
            string logPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(outputFolder, $"MultiAtlasLog_{DateTime.Now:yyyyMMdd_HHmmss}.asset").Replace("\\", "/"));
            AssetDatabase.CreateAsset(log, logPath);
            log.createdAssetPaths.Add(logPath);

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

            int cell = atlasSize / grid;
            int content = cell - paddingPx * 2;
            if (content <= 0)
            {
                content = cell;
                paddingPx = 0;
            }

            Undo.IncrementCurrentGroup();
            int ug = Undo.GetCurrentGroup();

            int processedCount = 0;
            int skippedCount = 0;

            foreach (var g in mergedApplyScans)
            {
                if (!g.enabled)
                {
                    skippedCount++;
                    continue;
                }

                bool hasUnresolved = g.rows.Any(r =>
                    (r.type == ShaderUtil.ShaderPropertyType.Color ||
                     r.type == ShaderUtil.ShaderPropertyType.Float ||
                     r.type == ShaderUtil.ShaderPropertyType.Range ||
                     r.type == ShaderUtil.ShaderPropertyType.Vector) &&
                    r.distinctCount > 1 && !r.doAction);

                if (hasUnresolved && diffPolicy == DiffPolicy.미해결이면중단)
                {
                    LoggingService?.Warning("Group skipped", $"Unresolved diff: {g.shaderName} [{g.tag}]");
                    skippedCount++;
                    continue;
                }

                LoggingService?.Info("Building group", $"{g.shaderName} [{g.tag}] - {g.mats.Count} materials");
                BuildGroup(g, log, cell, content, atlasSize, grid, paddingPx, outputFolder, diffPolicy, sampleMaterial);
                LoggingService?.Success("Group build complete", $"{g.shaderName} [{g.tag}]");
                processedCount++;
            }

            LoggingService?.Info("Saving assets");
            EditorUtility.SetDirty(log);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Undo.CollapseUndoOperations(ug);

            LoggingService?.Info("═══════════════════════════════════════", null, true);
            LoggingService?.Success("    Build & Apply Complete", $"Processed: {processedCount}, Skipped: {skippedCount}", true);
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

        private void BuildGroup(
            GroupScan g,
            KibaMultiAtlasMergerLog log,
            int cell,
            int content,
            int atlasSize,
            int grid,
            int paddingPx,
            string outputFolder,
            DiffPolicy diffPolicy,
            Material sampleMaterial)
        {
            LoggingService?.Info($"Preparing group build: {g.shaderName}", $"Materials: {g.mats.Count}, Pages: {g.pageCount}");

            // Blit 머티리얼 초기화 (텍스처 샘플링에 필요)
            string blitShaderPath = Path.Combine(outputFolder, "Hidden_KibaAtlasBlit.shader").Replace("\\", "/");
            LoggingService?.Info("Checking blit shader", blitShaderPath);
            TextureProcessor.EnsureBlitMaterial(blitShaderPath);

            string planFolder = GetPlanFolderName(g);
            string groupFolder = Path.Combine(outputFolder, planFolder).Replace("\\", "/");
            Directory.CreateDirectory(groupFolder);
            LoggingService?.Info($"Created output folder: {planFolder}");

            var mats = g.mats.ToList();
            int tilesPerPage = g.tilesPerPage;

            var texMeta = g.rows.Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv).ToDictionary(r => r.name, r => r, StringComparer.Ordinal);

            var enabledTexProps = g.rows.Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.doAction).Select(r => r.name).ToList();
            LoggingService?.Info($"Active texture properties: {enabledTexProps.Count}", string.Join(", ", enabledTexProps));

            var bakeRows = g.rows.Where(r =>
                r.doAction &&
                r.type != ShaderUtil.ShaderPropertyType.TexEnv &&
                (r.bakeMode == BakeMode.색상굽기_텍스처타일 || r.bakeMode == BakeMode.스칼라굽기_그레이타일 || r.bakeMode == BakeMode.색상곱_텍스처타일)).ToList();
            LoggingService?.Info($"Bake properties: {bakeRows.Count}");

            var resetRows = g.rows.Where(r =>
                r.doAction &&
                r.type != ShaderUtil.ShaderPropertyType.TexEnv &&
                r.bakeMode == BakeMode.리셋_쉐이더기본값).ToList();
            LoggingService?.Info($"Reset properties: {resetRows.Count}");

            var allAtlasProps = new HashSet<string>(enabledTexProps, StringComparer.Ordinal);
            foreach (var r in bakeRows)
            {
                if (string.IsNullOrEmpty(r.targetTexProp)) continue;
                allAtlasProps.Add(r.targetTexProp);
            }
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

                // 페이지당 실제 머티리얼 수에 맞춰 동적 그리드 계산
                int actualMatCount = pageItems.Count;

                // 최적의 그리드 배치 계산 (정사각형에 가깝게)
                int actualGridCols = Mathf.CeilToInt(Mathf.Sqrt(actualMatCount));
                int actualGridRows = Mathf.CeilToInt(actualMatCount / (float)actualGridCols);

                // 원래 설정된 그리드 크기를 초과하지 않도록 제한
                if (actualGridCols > grid)
                {
                    actualGridCols = grid;
                    actualGridRows = Mathf.CeilToInt(actualMatCount / (float)grid);
                }

                // 동적 아틀라스 크기 계산
                // 각 타일은 정사각형(cell x cell)이므로, 아틀라스도 정사각형 기준으로 계산
                // 필요한 최대 차원을 기준으로 정사각형 아틀라스 생성
                int actualAtlasSize = Mathf.Max(actualGridCols, actualGridRows) * cell;

                // 최대 크기 제한 (사용자 설정 초과 방지)
                actualAtlasSize = Mathf.Min(actualAtlasSize, atlasSize);

                // 최소 크기 보장 (너무 작으면 품질 저하)
                actualAtlasSize = Mathf.Max(actualAtlasSize, cell);

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
                    bool normalLike = texMeta.TryGetValue(prop, out var meta) ? meta.isNormalLike : IsNormalLikeProperty(prop);
                    bool sRGB = texMeta.TryGetValue(prop, out meta) ? meta.isSRGB : !IsLinearProperty(prop);
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
                            d.bakeMode == BakeMode.색상굽기_텍스처타일 &&
                            d.type == ShaderUtil.ShaderPropertyType.Color &&
                            d.targetTexProp == prop).ToList();

                        var solidScalarRules = bakeRows.Where(d =>
                            d.bakeMode == BakeMode.스칼라굽기_그레이타일 &&
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
                        Vector4 st = GetST(mat, prop);

                        bool normalLike = texMeta.TryGetValue(prop, out var meta) ? meta.isNormalLike : IsNormalLikeProperty(prop);
                        bool sRGB = texMeta.TryGetValue(prop, out meta) ? meta.isSRGB : !IsLinearProperty(prop);
                        if (normalLike) sRGB = false;

                        var pixels = TextureProcessor.SampleWithScaleTiling(src, st, content, content, sRGB, normalLike);

                        foreach (var mulRule in bakeRows.Where(d =>
                                     d.bakeMode == BakeMode.색상곱_텍스처타일 &&
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

                    bool normalLike = texMeta.TryGetValue(kv.Key, out var meta) ? meta.isNormalLike : IsNormalLikeProperty(kv.Key);
                    bool sRGB = texMeta.TryGetValue(kv.Key, out meta) ? meta.isSRGB : !IsLinearProperty(kv.Key);
                    AtlasGenerator.ConfigureImporter(p, atlasSize, sRGB, normalLike);
                }

                LoggingService?.Info($"  Creating merged material");
                var template = pageItems[0].mat;
                var merged = new Material(template);

                foreach (var prop in allAtlasProps)
                {
                    var atlasPath = Path.Combine(pageFolder, $"{AtlasGenerator.SanitizeFileName(prop)}.png").Replace("\\", "/");
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
                    if (tex && merged.HasProperty(prop)) merged.SetTexture(prop, tex);

                    string stName = prop + "_ST";
                    if (merged.HasProperty(stName)) merged.SetVector(stName, new Vector4(1, 1, 0, 0));
                }

                var defMat = TextureProcessor.GetDefaultMaterial(g.key.shader);

                foreach (var r in resetRows)
                {
                    if (!merged || !merged.HasProperty(r.name) || !defMat) continue;

                    if (r.type == ShaderUtil.ShaderPropertyType.Color)
                        merged.SetColor(r.name, defMat.GetColor(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Vector)
                        merged.SetVector(r.name, defMat.GetVector(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Float || r.type == ShaderUtil.ShaderPropertyType.Range)
                        merged.SetFloat(r.name, defMat.GetFloat(r.name));
                }

                foreach (var r in g.rows)
                {
                    if (!r.doAction) continue;
                    if (!r.resetSourceAfterBake) continue;
                    if (!merged || !merged.HasProperty(r.name) || !defMat) continue;

                    bool bakedOrMul =
                        r.bakeMode == BakeMode.색상굽기_텍스처타일 ||
                        r.bakeMode == BakeMode.스칼라굽기_그레이타일 ||
                        r.bakeMode == BakeMode.색상곱_텍스처타일;

                    if (!bakedOrMul) continue;

                    if (r.type == ShaderUtil.ShaderPropertyType.Color)
                        merged.SetColor(r.name, defMat.GetColor(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Vector)
                        merged.SetVector(r.name, defMat.GetVector(r.name));
                    else if (r.type == ShaderUtil.ShaderPropertyType.Float || r.type == ShaderUtil.ShaderPropertyType.Range)
                        merged.SetFloat(r.name, defMat.GetFloat(r.name));
                }

                if (diffPolicy == DiffPolicy.샘플머테리얼기준으로진행 && sampleMaterial)
                    ApplySampleMaterialOverrides(g, merged, sampleMaterial);

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
                    mergedMaterial = merged
                });
                
                LoggingService?.Success($"Page {page + 1}/{g.pageCount} complete");
            }

            if (matToPage.Count == 0 || pageInfos.Count == 0)
            {
                LoggingService?.Warning("No material/page info to apply");
                return;
            }

            LoggingService?.Info("Applying materials to renderers");
            ApplyToRenderers(g, matToPage, pageInfos, log, cell, content, paddingPx, outputFolder);
            LoggingService?.Success($"Group build complete: {g.shaderName}");
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
                if (TryBuildSubmeshMergeMap(beforeMesh, shared, mergeCandidates, out var mergedMaterials, out var mergeMap))
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

        private bool TryBuildSubmeshMergeMap(
            Mesh mesh,
            Material[] materials,
            HashSet<Material> mergeCandidates,
            out Material[] mergedMaterials,
            out int[] submeshMergeMap)
        {
            mergedMaterials = null;
            submeshMergeMap = null;
            if (!mesh || materials == null || materials.Length == 0) return false;

            int subMeshCount = mesh.subMeshCount;
            if (subMeshCount <= 1) return false;

            var effectiveMaterials = new Material[subMeshCount];
            var fallback = materials[materials.Length - 1];
            for (int s = 0; s < subMeshCount; s++)
                effectiveMaterials[s] = s < materials.Length ? materials[s] : fallback;

            var keyToIndex = new Dictionary<(int, MeshTopology), int>();
            var uniqueMaterials = new List<Material>();
            var map = new int[subMeshCount];

            for (int s = 0; s < subMeshCount; s++)
            {
                var mat = effectiveMaterials[s];
                var topology = mesh.GetTopology(s);

                if (!mat)
                {
                    map[s] = uniqueMaterials.Count;
                    uniqueMaterials.Add(mat);
                    continue;
                }

                if (mergeCandidates != null && !mergeCandidates.Contains(mat))
                {
                    map[s] = uniqueMaterials.Count;
                    uniqueMaterials.Add(mat);
                    continue;
                }

                var key = (mat.GetInstanceID(), topology);
                if (!keyToIndex.TryGetValue(key, out var index))
                {
                    index = uniqueMaterials.Count;
                    uniqueMaterials.Add(mat);
                    keyToIndex[key] = index;
                }
                map[s] = index;
            }

            if (uniqueMaterials.Count >= subMeshCount) return false;

            mergedMaterials = uniqueMaterials.ToArray();
            submeshMergeMap = map;
            return true;
        }

        private Vector4 GetST(Material mat, string prop)
        {
            if (!mat) return new Vector4(1, 1, 0, 0);
            string stName = prop + "_ST";
            if (!mat.HasProperty(stName)) return new Vector4(1, 1, 0, 0);
            return mat.GetVector(stName);
        }

        private bool IsNormalLikeProperty(string prop)
        {
            return prop.IndexOf("Bump", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   prop.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private bool IsLinearProperty(string prop)
        {
            var lower = prop.ToLowerInvariant();
            return lower.Contains("mask") ||
                   lower.Contains("normal") ||
                   lower.Contains("bump") ||
                   lower.Contains("height") ||
                   lower.Contains("displace") ||
                   lower.Contains("metallic") ||
                   lower.Contains("smoothness") ||
                   lower.Contains("rough") ||
                   lower.Contains("occlusion") ||
                   lower.Contains("ao");
        }
    }
}
#endif
