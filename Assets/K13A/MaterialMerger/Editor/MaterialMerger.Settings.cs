#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor
{
    public partial class MaterialMerger
    {
        MaterialMergeProfile EnsureProfile(GameObject target, bool createIfMissing)
        {
            if (!target) return null;
            var p = target.GetComponent<MaterialMergeProfile>();
            if (p || !createIfMissing) return p;

            p = Undo.AddComponent<MaterialMergeProfile>(target);
            return p;
        }

        void SetRoot(GameObject newRoot)
        {
            SaveNow();

            root = newRoot;

            scans.Clear();
            profile = null;

            if (root)
                profile = EnsureProfile(root, false);

            LoadSettingsFromProfile();
            LoadScansFromProfile();
            suppressAutosaveOnce = true;

            Repaint();
        }

        void LoadSettingsFromProfile()
        {
            if (!profile) return;

            groupByKeywords = profile.groupByKeywords;
            groupByRenderQueue = profile.groupByRenderQueue;
            splitOpaqueTransparent = profile.splitOpaqueTransparent;

            cloneRootOnApply = profile.cloneRootOnApply;
            deactivateOriginalRoot = profile.deactivateOriginalRoot;

            atlasSize = profile.atlasSize;
            grid = profile.grid;
            paddingPx = profile.paddingPx;

            diffPolicy = (DiffPolicy)profile.diffPolicy;
            if (!string.IsNullOrEmpty(profile.outputFolder))
                outputFolder = profile.outputFolder;

            globalFoldout = profile.globalFoldout;
        }

        void LoadScansFromProfile()
        {
            scans.Clear();
            if (!profile || profile.groups == null || profile.groups.Count == 0) return;

            var result = new List<GroupScan>();

            foreach (var gs in profile.groups)
            {
                Shader shader = null;
                var shaderName = gs.shaderName ?? "";
                if (!string.IsNullOrEmpty(gs.shaderGuid))
                {
                    var sp = AssetDatabase.GUIDToAssetPath(gs.shaderGuid);
                    if (!string.IsNullOrEmpty(sp))
                        shader = AssetDatabase.LoadAssetAtPath<Shader>(sp);
                }

                if (!shader && !string.IsNullOrEmpty(shaderName))
                    shader = Shader.Find(shaderName);
                if (!shader)
                    shader = TryResolveShaderFromRoot(gs);
                if (shader)
                    shaderName = shader.name;

                var g = new GroupScan();
                g.key = new GroupKey
                {
                    shader = shader,
                    keywordsHash = gs.keywordsHash,
                    renderQueue = gs.renderQueue,
                    transparencyKey = gs.transparencyKey
                };
                g.shaderName = shaderName;
                g.tag = string.IsNullOrEmpty(gs.tag) ? (gs.transparencyKey == 1 ? "투명" : "불투명") : gs.tag;
                g.tilesPerPage = gs.tilesPerPage > 0 ? gs.tilesPerPage : Mathf.Max(1, grid * grid);
                g.pageCount = gs.pageCount;
                g.skippedMultiMat = gs.skippedMultiMat;

                g.enabled = gs.enabled;
                g.foldout = gs.foldout;

                g.search = gs.search ?? "";
                g.onlyRelevant = gs.onlyRelevant;
                g.showTexturesOnly = gs.showTexturesOnly;
                g.showScalarsOnly = gs.showScalarsOnly;

                g.shaderTexProps = GetShaderPropsOfType(shader, ShaderUtil.ShaderPropertyType.TexEnv);
                g.shaderScalarProps = GetShaderPropsOfType(shader, ShaderUtil.ShaderPropertyType.Float)
                    .Concat(GetShaderPropsOfType(shader, ShaderUtil.ShaderPropertyType.Range))
                    .Distinct()
                    .ToList();

                int matCount = gs.materialCount;
                if (matCount <= 0 && gs.pageCount > 0 && g.tilesPerPage > 0)
                    matCount = gs.pageCount * g.tilesPerPage;
                if (matCount > 0)
                {
                    g.mats = new List<MatInfo>(matCount);
                    for (int i = 0; i < matCount; i++) g.mats.Add(new MatInfo());
                }

                g.rows = new List<Row>();
                if (gs.rows != null && gs.rows.Count > 0)
                {
                    foreach (var rs in gs.rows)
                    {
                        if (rs == null || string.IsNullOrEmpty(rs.name)) continue;

                        var r = new Row();
                        r.name = rs.name;
                        r.shaderPropIndex = rs.shaderPropIndex;
                        r.type = (ShaderUtil.ShaderPropertyType)rs.shaderPropType;

                        if (shader && TryGetShaderPropInfo(shader, r.name, out var t, out var idx))
                        {
                            r.type = t;
                            r.shaderPropIndex = idx;
                        }

                        r.doAction = rs.doAction;

                        r.bakeMode = (BakeMode)rs.bakeMode;
                        r.targetTexIndex = rs.targetTexIndex;
                        r.targetTexProp = rs.targetTexProp;

                        r.includeAlpha = rs.includeAlpha;
                        r.resetSourceAfterBake = rs.resetSourceAfterBake;

                        r.modOp = (ModOp)rs.modOp;
                        r.modPropIndex = rs.modPropIndex;
                        r.modProp = rs.modProp;
                        r.modClamp01 = rs.modClamp01;
                        r.modScale = rs.modScale;
                        r.modBias = rs.modBias;
                        r.modAffectsAlpha = rs.modAffectsAlpha;

                        r.expanded = rs.expanded;

                        r.texNonNull = rs.texNonNull;
                        r.texDistinct = rs.texDistinct;
                        r.stDistinct = rs.stDistinct;
                        r.isNormalLike = rs.isNormalLike;
                        r.isSRGB = rs.isSRGB;
                        r.distinctCount = rs.distinctCount;

                        g.rows.Add(r);
                    }
                }

                if (g.shaderTexProps != null && g.shaderTexProps.Count > 0)
                {
                    foreach (var r in g.rows)
                    {
                        if (r.type == ShaderUtil.ShaderPropertyType.TexEnv) continue;

                        if (!string.IsNullOrEmpty(r.targetTexProp))
                        {
                            int idx = g.shaderTexProps.IndexOf(r.targetTexProp);
                            if (idx >= 0) r.targetTexIndex = idx;
                        }

                        r.targetTexIndex = Mathf.Clamp(r.targetTexIndex, 0, g.shaderTexProps.Count - 1);
                        r.targetTexProp = g.shaderTexProps[r.targetTexIndex];
                    }
                }

                if (g.shaderScalarProps != null && g.shaderScalarProps.Count > 0)
                {
                    foreach (var r in g.rows)
                    {
                        if (string.IsNullOrEmpty(r.modProp))
                        {
                            r.modPropIndex = 0;
                            continue;
                        }

                        int idx = g.shaderScalarProps.IndexOf(r.modProp);
                        if (idx >= 0)
                        {
                            r.modPropIndex = idx + 1;
                            r.modProp = g.shaderScalarProps[idx];
                        }
                        else
                        {
                            r.modPropIndex = 0;
                            r.modProp = "";
                        }
                    }
                }

                result.Add(g);
            }

            scans = result;
        }

        bool TryGetShaderPropInfo(Shader shader, string propName, out ShaderUtil.ShaderPropertyType type, out int index)
        {
            type = ShaderUtil.ShaderPropertyType.Float;
            index = -1;
            if (!shader || string.IsNullOrEmpty(propName)) return false;

            int pc = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < pc; i++)
            {
                if (ShaderUtil.GetPropertyName(shader, i) == propName)
                {
                    type = ShaderUtil.GetPropertyType(shader, i);
                    index = i;
                    return true;
                }
            }

            return false;
        }

        Shader TryResolveShaderFromRoot(MaterialMergeProfile.GroupData gs)
        {
            if (!root) return null;
            var renderers = CollectRenderers(root);
            for (int i = 0; i < renderers.Count; i++)
            {
                var r = renderers[i];
                if (!r) continue;
                var mats = r.sharedMaterials;
                if (mats == null) continue;

                for (int m = 0; m < mats.Length; m++)
                {
                    var mat = mats[m];
                    if (!mat || !mat.shader) continue;

                    var key = MakeKey(mat);
                    if (key.keywordsHash == gs.keywordsHash &&
                        key.renderQueue == gs.renderQueue &&
                        key.transparencyKey == gs.transparencyKey)
                        return mat.shader;
                }
            }

            return null;
        }

        void ApplyProfileToScansIfAny()
        {
            if (!profile) return;
            if (scans == null || scans.Count == 0) return;

            var gmap = new Dictionary<(string, int, int, int), MaterialMergeProfile.GroupData>();
            var gmapByName = new Dictionary<(string, int, int, int), MaterialMergeProfile.GroupData>();
            foreach (var gs in profile.groups)
            {
                gmap[(gs.shaderGuid ?? "", gs.keywordsHash, gs.renderQueue, gs.transparencyKey)] = gs;
                if (!string.IsNullOrEmpty(gs.shaderName))
                    gmapByName[(gs.shaderName, gs.keywordsHash, gs.renderQueue, gs.transparencyKey)] = gs;
            }

            foreach (var g in scans)
            {
                var shaderGuid = "";
                if (g.key.shader)
                {
                    var sp = AssetDatabase.GetAssetPath(g.key.shader);
                    if (!string.IsNullOrEmpty(sp))
                        shaderGuid = AssetDatabase.AssetPathToGUID(sp);
                }

                if (!gmap.TryGetValue((shaderGuid, g.key.keywordsHash, g.key.renderQueue, g.key.transparencyKey), out var gs))
                {
                    var nameKey = g.key.shader ? g.key.shader.name : g.shaderName;
                    if (string.IsNullOrEmpty(nameKey) || !gmapByName.TryGetValue((nameKey, g.key.keywordsHash, g.key.renderQueue, g.key.transparencyKey), out gs))
                        continue;
                }

                g.enabled = gs.enabled;
                g.foldout = gs.foldout;

                g.search = gs.search ?? "";
                g.onlyRelevant = gs.onlyRelevant;
                g.showTexturesOnly = gs.showTexturesOnly;
                g.showScalarsOnly = gs.showScalarsOnly;

                var rmap = gs.rows
                    .Where(x => x != null && !string.IsNullOrEmpty(x.name))
                    .GroupBy(x => x.name, StringComparer.Ordinal)
                    .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);

                foreach (var r in g.rows)
                {
                    if (r == null || string.IsNullOrEmpty(r.name)) continue;
                    if (!rmap.TryGetValue(r.name, out var rs)) continue;

                    r.doAction = rs.doAction;

                    r.bakeMode = (BakeMode)rs.bakeMode;
                    r.targetTexIndex = rs.targetTexIndex;
                    r.targetTexProp = rs.targetTexProp;

                    r.includeAlpha = rs.includeAlpha;
                    r.resetSourceAfterBake = rs.resetSourceAfterBake;

                    r.modOp = (ModOp)rs.modOp;
                    r.modPropIndex = rs.modPropIndex;
                    r.modProp = rs.modProp;
                    r.modClamp01 = rs.modClamp01;
                    r.modScale = rs.modScale;
                    r.modBias = rs.modBias;
                    r.modAffectsAlpha = rs.modAffectsAlpha;

                    r.expanded = rs.expanded;
                }
            }
        }

        void RequestSave()
        {
            if (!root || !profile) return;

            if (saveQueued) return;
            saveQueued = true;

            EditorApplication.delayCall += () =>
            {
                if (this == null) return;
                saveQueued = false;
                SaveNow();
            };
        }

        void SaveNow()
        {
            if (!root || !profile) return;

            profile.groupByKeywords = groupByKeywords;
            profile.groupByRenderQueue = groupByRenderQueue;
            profile.splitOpaqueTransparent = splitOpaqueTransparent;

            profile.cloneRootOnApply = cloneRootOnApply;
            profile.deactivateOriginalRoot = deactivateOriginalRoot;

            profile.atlasSize = atlasSize;
            profile.grid = grid;
            profile.paddingPx = paddingPx;

            profile.diffPolicy = (int)diffPolicy;
            profile.outputFolder = outputFolder;

            profile.globalFoldout = globalFoldout;

            profile.groups.Clear();

            if (scans != null && scans.Count > 0)
            {
                foreach (var g in scans)
                {
                    var gs = new MaterialMergeProfile.GroupData();

                    var shaderGuid = "";
                    if (g.key.shader)
                    {
                        var sp = AssetDatabase.GetAssetPath(g.key.shader);
                        if (!string.IsNullOrEmpty(sp))
                            shaderGuid = AssetDatabase.AssetPathToGUID(sp);
                    }

                    gs.shaderGuid = shaderGuid;
                    gs.shaderName = g.key.shader ? g.key.shader.name : (g.shaderName ?? "");
                    gs.keywordsHash = g.key.keywordsHash;
                    gs.renderQueue = g.key.renderQueue;
                    gs.transparencyKey = g.key.transparencyKey;

                    gs.tag = g.tag;
                    gs.materialCount = g.mats != null ? g.mats.Count : 0;
                    gs.tilesPerPage = g.tilesPerPage;
                    gs.pageCount = g.pageCount;
                    gs.skippedMultiMat = g.skippedMultiMat;

                    gs.enabled = g.enabled;
                    gs.foldout = g.foldout;

                    gs.search = g.search ?? "";
                    gs.onlyRelevant = g.onlyRelevant;
                    gs.showTexturesOnly = g.showTexturesOnly;
                    gs.showScalarsOnly = g.showScalarsOnly;

                    gs.rows.Clear();
                    foreach (var r in g.rows)
                    {
                        if (r == null || string.IsNullOrEmpty(r.name)) continue;

                        var row = new MaterialMergeProfile.RowData();
                        row.name = r.name;
                        row.shaderPropIndex = r.shaderPropIndex;
                        row.shaderPropType = (int)r.type;
                        row.doAction = r.doAction;

                        row.bakeMode = (int)r.bakeMode;
                        row.targetTexIndex = r.targetTexIndex;
                        row.targetTexProp = r.targetTexProp;

                        row.includeAlpha = r.includeAlpha;
                        row.resetSourceAfterBake = r.resetSourceAfterBake;

                        row.modOp = (int)r.modOp;
                        row.modPropIndex = r.modPropIndex;
                        row.modProp = r.modProp;
                        row.modClamp01 = r.modClamp01;
                        row.modScale = r.modScale;
                        row.modBias = r.modBias;
                        row.modAffectsAlpha = r.modAffectsAlpha;

                        row.expanded = r.expanded;

                        row.texNonNull = r.texNonNull;
                        row.texDistinct = r.texDistinct;
                        row.stDistinct = r.stDistinct;
                        row.isNormalLike = r.isNormalLike;
                        row.isSRGB = r.isSRGB;
                        row.distinctCount = r.distinctCount;

                        gs.rows.Add(row);
                    }

                    profile.groups.Add(gs);
                }
            }

            EditorUtility.SetDirty(profile);
        }
    }
}
#endif