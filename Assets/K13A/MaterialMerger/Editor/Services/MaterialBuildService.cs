#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    public class MaterialBuildService : IMaterialBuildService
    {
        public IAtlasGenerator AtlasGenerator { get; set; }
        public ITextureProcessor TextureProcessor { get; set; }
        public IMeshRemapper MeshRemapper { get; set; }
        public IMaterialScanService ScanService { get; set; }

        public void BuildAndApplyWithConfirm(
            dynamic owner,
            GameObject root,
            List<GroupScan> scans,
            DiffPolicy diffPolicy,
            string outputFolder)
        {
            if (scans == null || scans.Count == 0)
            {
                EditorUtility.DisplayDialog("멀티 아틀라스", "스캔 결과가 없습니다.", "OK");
                return;
            }

            var list = new List<ConfirmWindow.GroupInfo>();

            foreach (var g in scans)
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
                gi.skipReason = willRun ? "" : "미해결 diff가 있는데 정책이 '미해결이면중단'이라 이 Material Plan은 스킵됩니다.";
                gi.atlasProps = atlasProps;
                gi.generatedProps = generatedProps;

                list.Add(gi);
            }

            if (list.Count == 0)
            {
                EditorUtility.DisplayDialog("멀티 아틀라스", "활성화된 Material Plan이 없습니다.", "OK");
                return;
            }

            ConfirmWindow.Open(owner, list);
        }

        public void BuildAndApply(
            GameObject root,
            List<GroupScan> scans,
            DiffPolicy diffPolicy,
            bool cloneRootOnApply,
            bool deactivateOriginalRoot,
            string outputFolder,
            int atlasSize,
            int grid,
            int paddingPx,
            bool groupByKeywords,
            bool groupByRenderQueue,
            bool splitOpaqueTransparent)
        {
            if (TextureProcessor == null || AtlasGenerator == null || MeshRemapper == null || ScanService == null)
            {
                EditorUtility.DisplayDialog("멀티 아틀라스", "서비스가 초기화되지 않았습니다.", "OK");
                return;
            }

            if (cloneRootOnApply && !root)
            {
                EditorUtility.DisplayDialog("멀티 아틀라스", "루트가 필요합니다(적용 시 루트 복제 옵션).", "OK");
                return;
            }

            Directory.CreateDirectory(outputFolder);

            var log = ScriptableObject.CreateInstance<KibaMultiAtlasMergerLog>();
            string logPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(outputFolder, $"MultiAtlasLog_{DateTime.Now:yyyyMMdd_HHmmss}.asset").Replace("\\", "/"));
            AssetDatabase.CreateAsset(log, logPath);
            log.createdAssetPaths.Add(logPath);

            GameObject applyRootObj = root;
            if (cloneRootOnApply && root)
            {
                applyRootObj = CloneRootForApply(root, deactivateOriginalRoot);
                if (!applyRootObj) return;
            }

            log.sourceRootGlobalId = root ? GlobalObjectId.GetGlobalObjectIdSlow(root).ToString() : "";
            log.appliedRootGlobalId = applyRootObj ? GlobalObjectId.GetGlobalObjectIdSlow(applyRootObj).ToString() : "";

            var applyScans = ScanService.ScanGameObject(
                applyRootObj,
                groupByKeywords,
                groupByRenderQueue,
                splitOpaqueTransparent,
                grid
            );
            CopySettings(scans, applyScans);

            int cell = atlasSize / grid;
            int content = cell - paddingPx * 2;
            if (content <= 0)
            {
                content = cell;
                paddingPx = 0;
            }

            Undo.IncrementCurrentGroup();
            int ug = Undo.GetCurrentGroup();

            foreach (var g in applyScans)
            {
                if (!g.enabled) continue;

                bool hasUnresolved = g.rows.Any(r =>
                    (r.type == ShaderUtil.ShaderPropertyType.Color ||
                     r.type == ShaderUtil.ShaderPropertyType.Float ||
                     r.type == ShaderUtil.ShaderPropertyType.Range ||
                     r.type == ShaderUtil.ShaderPropertyType.Vector) &&
                    r.distinctCount > 1 && !r.doAction);

                if (hasUnresolved && diffPolicy == DiffPolicy.미해결이면중단)
                    continue;

                BuildGroup(g, log, cell, content, atlasSize, grid, paddingPx, outputFolder);
            }

            EditorUtility.SetDirty(log);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Undo.CollapseUndoOperations(ug);

            EditorUtility.DisplayDialog("멀티 아틀라스", $"완료\n로그: {logPath}", "OK");
        }

        public GameObject CloneRootForApply(GameObject src, bool deactivateOriginal)
        {
            var parent = src.transform.parent;
            var clone = UnityEngine.Object.Instantiate(src, parent);
            clone.name = src.name + "_AtlasMerged";
            clone.transform.localPosition = src.transform.localPosition;
            clone.transform.localRotation = src.transform.localRotation;
            clone.transform.localScale = src.transform.localScale;
            Undo.RegisterCreatedObjectUndo(clone, "루트 복제");

            if (deactivateOriginal)
            {
                Undo.RecordObject(src, "원본 루트 비활성화");
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
            string outputFolder)
        {
            string shaderFolder = AtlasGenerator.SanitizeFileName(g.key.shader ? g.key.shader.name : "NullShader");
            string groupFolder = Path.Combine(outputFolder, g.tag, shaderFolder).Replace("\\", "/");
            Directory.CreateDirectory(groupFolder);

            var mats = g.mats.ToList();
            int tilesPerPage = g.tilesPerPage;

            var texMeta = g.rows.Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv).ToDictionary(r => r.name, r => r, StringComparer.Ordinal);

            var enabledTexProps = g.rows.Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.doAction).Select(r => r.name).ToList();

            var bakeRows = g.rows.Where(r =>
                r.doAction &&
                r.type != ShaderUtil.ShaderPropertyType.TexEnv &&
                (r.bakeMode == BakeMode.색상굽기_텍스처타일 || r.bakeMode == BakeMode.스칼라굽기_그레이타일 || r.bakeMode == BakeMode.색상곱_텍스처타일)).ToList();

            var resetRows = g.rows.Where(r =>
                r.doAction &&
                r.type != ShaderUtil.ShaderPropertyType.TexEnv &&
                r.bakeMode == BakeMode.리셋_쉐이더기본값).ToList();

            var allAtlasProps = new HashSet<string>(enabledTexProps, StringComparer.Ordinal);
            foreach (var r in bakeRows)
            {
                if (string.IsNullOrEmpty(r.targetTexProp)) continue;
                allAtlasProps.Add(r.targetTexProp);
            }

            for (int page = 0; page < g.pageCount; page++)
            {
                int start = page * tilesPerPage;
                int end = Mathf.Min(mats.Count, start + tilesPerPage);
                var pageItems = mats.GetRange(start, end - start);

                string pageFolder = Path.Combine(groupFolder, $"Page_{page:00}").Replace("\\", "/");
                Directory.CreateDirectory(pageFolder);

                var atlasByProp = new Dictionary<string, Texture2D>(StringComparer.Ordinal);

                foreach (var prop in allAtlasProps)
                {
                    bool normalLike = texMeta.TryGetValue(prop, out var meta) ? meta.isNormalLike : IsNormalLikeProperty(prop);
                    bool sRGB = texMeta.TryGetValue(prop, out meta) ? meta.isSRGB : !IsLinearProperty(prop);
                    if (normalLike) sRGB = false;
                    atlasByProp[prop] = AtlasGenerator.CreateAtlas(atlasSize, sRGB);
                }

                for (int i = 0; i < pageItems.Count; i++)
                {
                    int gx = i % grid;
                    int gy = i / grid;

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

                foreach (var kv in atlasByProp)
                {
                    kv.Value.Apply(true, false);
                    string p = AtlasGenerator.SaveAtlasPNG(kv.Value, pageFolder, $"{AtlasGenerator.SanitizeFileName(kv.Key)}.png");
                    log.createdAssetPaths.Add(p);

                    bool normalLike = texMeta.TryGetValue(kv.Key, out var meta) ? meta.isNormalLike : IsNormalLikeProperty(kv.Key);
                    bool sRGB = texMeta.TryGetValue(kv.Key, out meta) ? meta.isSRGB : !IsLinearProperty(kv.Key);
                    AtlasGenerator.ConfigureImporter(p, atlasSize, sRGB, normalLike);
                }

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

                string matPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(pageFolder, "Merged.mat").Replace("\\", "/"));
                AssetDatabase.CreateAsset(merged, matPath);
                log.createdAssetPaths.Add(matPath);

                ApplyToRenderers(g, pageItems, merged, log, cell, content, atlasSize, grid, paddingPx, outputFolder);
            }
        }

        private void ApplyToRenderers(
            GroupScan g,
            List<MatInfo> pageItems,
            Material mergedMat,
            KibaMultiAtlasMergerLog log,
            int cell,
            int content,
            int atlasSizePx,
            int grid,
            int paddingPx,
            string outputFolder)
        {
            float tileScale = content / (float)atlasSizePx;

            var matToIndex = new Dictionary<Material, int>();
            for (int i = 0; i < pageItems.Count; i++) matToIndex[pageItems[i].mat] = i;

            var meshCache = new Dictionary<(Mesh, int), Mesh>();

            foreach (var mi in pageItems)
            {
                foreach (var r in mi.users)
                {
                    if (!r) continue;

                    var shared = r.sharedMaterials;
                    if (shared == null || shared.Length == 0) continue;

                    int hitCount = 0;
                    int tileIndex = -1;

                    for (int s = 0; s < shared.Length; s++)
                    {
                        if (shared[s] && matToIndex.TryGetValue(shared[s], out var idx))
                        {
                            hitCount++;
                            if (tileIndex < 0) tileIndex = idx;
                            else if (tileIndex != idx)
                            {
                                tileIndex = -2;
                                break;
                            }
                        }
                    }

                    if (tileIndex < 0) continue;
                    if (tileIndex == -2) continue;
                    if (hitCount > 1) continue;

                    var beforeMats = shared.ToArray();
                    bool replaced = false;

                    Undo.RecordObject(r, "머지 머티리얼 적용");
                    for (int s = 0; s < shared.Length; s++)
                    {
                        if (shared[s] == mi.mat)
                        {
                            shared[s] = mergedMat;
                            replaced = true;
                        }
                    }

                    if (!replaced) continue;
                    r.sharedMaterials = shared;

                    int gx = tileIndex % grid;
                    int gy = tileIndex / grid;
                    float ox = (gx * cell + paddingPx) / (float)atlasSizePx;
                    float oy = (gy * cell + paddingPx) / (float)atlasSizePx;

                    var scale = new Vector2(tileScale, tileScale);
                    var offset = new Vector2(ox, oy);

                    Mesh beforeMesh = null;
                    Mesh afterMesh = null;

                    if (r is SkinnedMeshRenderer smr)
                    {
                        beforeMesh = smr.sharedMesh;
                        if (beforeMesh)
                        {
                            Undo.RecordObject(smr, "메쉬 UV 리맵");
                            afterMesh = MeshRemapper.GetOrCreateRemappedMesh(beforeMesh, tileIndex, scale, offset, meshCache, outputFolder);
                            smr.sharedMesh = afterMesh;
                        }
                    }
                    else
                    {
                        var mf = r.GetComponent<MeshFilter>();
                        if (mf && mf.sharedMesh)
                        {
                            beforeMesh = mf.sharedMesh;
                            Undo.RecordObject(mf, "메쉬 UV 리맵");
                            afterMesh = MeshRemapper.GetOrCreateRemappedMesh(beforeMesh, tileIndex, scale, offset, meshCache, outputFolder);
                            mf.sharedMesh = afterMesh;
                        }
                    }

                    var entry = new KibaMultiAtlasMergerLog.Entry();
                    entry.rendererGlobalId = GlobalObjectId.GetGlobalObjectIdSlow(r).ToString();
                    entry.beforeMaterials = beforeMats;
                    entry.afterMaterials = r.sharedMaterials.ToArray();
                    entry.beforeMesh = beforeMesh;
                    entry.afterMesh = afterMesh;
                    log.entries.Add(entry);
                }
            }
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
