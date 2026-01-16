#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Logging;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace K13A.MaterialMerger.Editor.Services
{
    /// <summary>
    /// Applies built atlas assets to scene renderers.
    /// Single Responsibility: Scene modification only.
    /// </summary>
    public class SceneApplier : ISceneApplier
    {
        public IAtlasGenerator AtlasGenerator { get; set; }
        public ITextureProcessor TextureProcessor { get; set; }
        public IMeshRemapper MeshRemapper { get; set; }
        public IMaterialScanService ScanService { get; set; }
        public ILoggingService LoggingService { get; set; }

        public SceneApplyResult ApplyToScene(
            BuildSettings settings,
            List<GroupScan> scans,
            AssetBuildResult buildResult)
        {
            var result = new SceneApplyResult();

            if (buildResult == null || !buildResult.success || buildResult.groupBuildData == null)
            {
                result.success = false;
                result.errorMessage = "Invalid build result";
                return result;
            }

            LoggingService?.Info("═══ Phase 2: Applying to Scene ═══");

            // Clone root if needed
            GameObject applyRootObj = settings.Root;
            if (settings.CloneRootOnApply && settings.Root)
            {
                LoggingService?.Info("Cloning root", $"Original: {settings.Root.name}, KeepPrefab: {settings.KeepPrefabOnClone}");
                applyRootObj = CloneRootForApply(settings.Root, settings.DeactivateOriginalRoot, settings.KeepPrefabOnClone);
                if (!applyRootObj)
                {
                    result.success = false;
                    result.errorMessage = "Root clone failed";
                    LoggingService?.Error("Root clone failed");
                    return result;
                }

                LoggingService?.Success("Root cloned", $"Clone: {applyRootObj.name}");
            }

            result.appliedRoot = applyRootObj;

            // Update log with root references
            if (buildResult.log != null)
            {
                buildResult.log.sourceRootGlobalId = settings.Root ? GlobalObjectId.GetGlobalObjectIdSlow(settings.Root).ToString() : "";
                buildResult.log.appliedRootGlobalId = applyRootObj ? GlobalObjectId.GetGlobalObjectIdSlow(applyRootObj).ToString() : "";
            }

            // Rescan apply target
            LoggingService?.Info("Rescanning apply target");
            var applyScans = ScanService.ScanGameObject(
                applyRootObj,
                settings.GroupByKeywords,
                settings.GroupByRenderQueue,
                settings.SplitOpaqueTransparent,
                settings.Grid
            );
            CopySettings(scans, applyScans);
            var mergedApplyScans = GroupMergeUtility.BuildMergedScans(applyScans, ScanService);
            LoggingService?.Info("Rescan complete", $"{mergedApplyScans.Count} groups");

            Undo.IncrementCurrentGroup();
            int ug = Undo.GetCurrentGroup();

            // Apply to renderers using the build data
            LoggingService?.Info("Applying materials to renderers");
            int appliedCount = 0;

            foreach (var buildData in buildResult.groupBuildData)
            {
                // Find matching group in applyScans
                var matchingGroup = mergedApplyScans.FirstOrDefault(g =>
                    g.key.Equals(buildData.groupKey) && g.enabled);

                if (matchingGroup != null)
                {
                    ApplyGroupToRenderers(
                        matchingGroup,
                        buildData,
                        buildResult.log,
                        settings.CellSize,
                        settings.ContentSize,
                        settings.PaddingPx,
                        settings.OutputFolder,
                        settings.DiffPolicy,
                        settings.SampleMaterial);
                    appliedCount++;
                }
            }

            Undo.CollapseUndoOperations(ug);

            if (buildResult.log != null)
            {
                EditorUtility.SetDirty(buildResult.log);
            }

            AssetDatabase.SaveAssets();

            result.renderersProcessed = appliedCount;
            result.success = true;

            LoggingService?.Success("Scene apply complete", $"Processed: {appliedCount} groups");

            return result;
        }

        public GameObject CloneRootForApply(GameObject src, bool deactivateOriginal, bool keepPrefab)
        {
            var parent = src.transform.parent;
            GameObject clone;

            // If keep prefab option is enabled and this is a prefab instance
            if (keepPrefab && PrefabUtility.IsPartOfPrefabInstance(src))
            {
                LoggingService?.Info("Cloning as prefab instance");

                try
                {
                    // Check if it's a prefab root
                    var prefabInstanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(src);

                    if (prefabInstanceRoot == src)
                    {
                        // src is the prefab root
                        var prefabAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(src);

                        if (!string.IsNullOrEmpty(prefabAssetPath))
                        {
                            var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
                            if (prefabAsset != null)
                            {
                                var instantiated = PrefabUtility.InstantiatePrefab(prefabAsset, parent);
                                clone = instantiated as GameObject;

                                if (clone != null)
                                {
                                    LoggingService?.Success("Prefab instance created successfully");
                                }
                                else
                                {
                                    LoggingService?.Warning("Prefab instantiation returned non-GameObject, falling back");
                                    clone = Object.Instantiate(src, parent);
                                }
                            }
                            else
                            {
                                LoggingService?.Warning("Failed to load prefab asset, falling back to regular clone");
                                clone = Object.Instantiate(src, parent);
                            }
                        }
                        else
                        {
                            LoggingService?.Warning("Failed to get prefab asset path, falling back to regular clone");
                            clone = Object.Instantiate(src, parent);
                        }
                    }
                    else
                    {
                        // src is a child of the prefab - use regular clone
                        LoggingService?.Warning("Source is not a prefab root, falling back to regular clone");
                        clone = Object.Instantiate(src, parent);
                    }
                }
                catch (Exception ex)
                {
                    LoggingService?.Error("Exception during prefab clone", ex.Message);
                    clone = Object.Instantiate(src, parent);
                }
            }
            else
            {
                LoggingService?.Info("Cloning as regular instance (unpacked)");
                clone = Object.Instantiate(src, parent);
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

                Undo.RecordObject(r, "Apply merged material");
                r.sharedMaterials = shared;

                if (smr)
                {
                    Undo.RecordObject(smr, "Remap mesh UV");
                    afterMesh = MeshRemapper.GetOrCreateRemappedMesh(beforeMesh, transforms, meshCache, outputFolder, submeshMergeMap);
                    smr.sharedMesh = afterMesh;
                }
                else if (mf)
                {
                    Undo.RecordObject(mf, "Remap mesh UV");
                    afterMesh = MeshRemapper.GetOrCreateRemappedMesh(beforeMesh, transforms, meshCache, outputFolder, submeshMergeMap);
                    mf.sharedMesh = afterMesh;
                }

                if (log != null)
                {
                    var entry = new KibaMultiAtlasMergerLog.Entry();
                    entry.rendererGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(r).ToString();
                    entry.beforeMaterials = beforeMats;
                    entry.afterMaterials = r.sharedMaterials.ToArray();
                    entry.beforeMesh = beforeMesh;
                    entry.afterMesh = afterMesh;
                    log.entries.Add(entry);
                }

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
    }
}
#endif