#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    /// <summary>
    /// Helper for merging plan groups.
    /// </summary>
    public static class GroupMergeUtility
    {
        public static List<GroupScan> BuildMergedScans(List<GroupScan> scans, IMaterialScanService scanService)
        {
            var result = new List<GroupScan>();
            if (scans == null || scans.Count == 0) return result;

            var grouped = new Dictionary<string, List<GroupScan>>(StringComparer.Ordinal);
            foreach (var g in scans)
            {
                if (g == null || string.IsNullOrEmpty(g.mergeKey)) continue;
                if (!grouped.TryGetValue(g.mergeKey, out var list))
                {
                    list = new List<GroupScan>();
                    grouped[g.mergeKey] = list;
                }
                list.Add(g);
            }

            var processed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var g in scans)
            {
                if (g == null) continue;
                if (string.IsNullOrEmpty(g.mergeKey))
                {
                    g.mergeChildren = null;
                    result.Add(g);
                    continue;
                }

                if (processed.Contains(g.mergeKey)) continue;
                processed.Add(g.mergeKey);

                if (!grouped.TryGetValue(g.mergeKey, out var list) || list.Count <= 1)
                {
                    g.mergeChildren = null;
                    result.Add(g);
                    continue;
                }

                result.Add(CreateMergedGroup(list, scanService));
            }

            return result;
        }

        public static void ApplyMergedSettingsToChildren(GroupScan merged)
        {
            if (merged == null || merged.mergeChildren == null || merged.mergeChildren.Count <= 1) return;

            foreach (var child in merged.mergeChildren)
            {
                if (child == null) continue;
                SyncGroupSettings(merged, child);
                CopyRowSettings(merged, child);
            }
        }

        private static GroupScan CreateMergedGroup(List<GroupScan> groups, IMaterialScanService scanService)
        {
            var primary = groups[0];
            var merged = CloneShell(primary);

            merged.mergeChildren = groups;
            merged.mergeKey = primary.mergeKey ?? "";

            var matMap = new Dictionary<Material, MatInfo>();
            foreach (var group in groups)
            {
                if (group == null || group.mats == null) continue;

                foreach (var mi in group.mats)
                {
                    if (mi == null || !mi.mat) continue;
                    if (!matMap.TryGetValue(mi.mat, out var dst))
                    {
                        dst = new MatInfo { mat = mi.mat };
                        matMap[mi.mat] = dst;
                    }

                    foreach (var user in mi.users)
                    {
                        if (!user) continue;
                        if (!dst.users.Contains(user))
                            dst.users.Add(user);
                    }
                }
            }

            merged.mats = matMap.Values.ToList();
            merged.tilesPerPage = Mathf.Max(1, primary.tilesPerPage);
            merged.pageCount = Mathf.CeilToInt(merged.mats.Count / (float)merged.tilesPerPage);

            if (scanService != null)
            {
                merged.shaderTexProps = scanService.GetShaderPropertiesByType(merged.key.shader, ShaderUtil.ShaderPropertyType.TexEnv);
                merged.shaderScalarProps = scanService.GetShaderPropertiesByType(merged.key.shader, ShaderUtil.ShaderPropertyType.Float)
                    .Concat(scanService.GetShaderPropertiesByType(merged.key.shader, ShaderUtil.ShaderPropertyType.Range))
                    .Distinct()
                    .ToList();
                merged.rows = scanService.BuildPropertyRows(merged);
                merged.skippedMultiMat = scanService.EstimateMultiMaterialSkips(merged);
                CopyRowSettings(primary, merged);
            }
            else
            {
                merged.shaderTexProps = primary.shaderTexProps;
                merged.shaderScalarProps = primary.shaderScalarProps;
                merged.rows = primary.rows;
                merged.skippedMultiMat = primary.skippedMultiMat;
            }

            return merged;
        }

        private static GroupScan CloneShell(GroupScan src)
        {
            return new GroupScan
            {
                key = src.key,
                shaderName = src.shaderName,
                tag = src.tag,
                tilesPerPage = src.tilesPerPage,
                pageCount = src.pageCount,
                skippedMultiMat = src.skippedMultiMat,
                outputMaterialName = src.outputMaterialName,
                enabled = src.enabled,
                foldout = src.foldout,
                search = src.search ?? "",
                onlyRelevant = src.onlyRelevant,
                showTexturesOnly = src.showTexturesOnly,
                showScalarsOnly = src.showScalarsOnly,
                mergeKey = src.mergeKey ?? "",
                mergeSelected = src.mergeSelected
            };
        }

        private static void SyncGroupSettings(GroupScan source, GroupScan target)
        {
            target.enabled = source.enabled;
            target.foldout = source.foldout;
            target.search = source.search ?? "";
            target.onlyRelevant = source.onlyRelevant;
            target.showTexturesOnly = source.showTexturesOnly;
            target.showScalarsOnly = source.showScalarsOnly;
            target.outputMaterialName = source.outputMaterialName;
            target.mergeKey = source.mergeKey ?? "";
            target.mergeSelected = source.mergeSelected;
        }

        private static void CopyRowSettings(GroupScan source, GroupScan target)
        {
            if (source.rows == null || target.rows == null) return;

            var map = source.rows
                .Where(r => r != null && !string.IsNullOrEmpty(r.name) && r.name != "_DummyProperty")
                .GroupBy(r => r.name, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            foreach (var tr in target.rows)
            {
                if (tr == null || string.IsNullOrEmpty(tr.name) || tr.name == "_DummyProperty") continue;
                if (!map.TryGetValue(tr.name, out var sr)) continue;

                tr.doAction = sr.doAction;
                tr.bakeMode = sr.bakeMode;
                tr.includeAlpha = sr.includeAlpha;
                tr.resetSourceAfterBake = sr.resetSourceAfterBake;

                tr.targetTexIndex = sr.targetTexIndex;
                tr.targetTexProp = sr.targetTexProp;

                if (target.shaderTexProps != null && target.shaderTexProps.Count > 0)
                {
                    var maxTex = Mathf.Max(0, target.shaderTexProps.Count - 1);
                    tr.targetTexIndex = Mathf.Clamp((int)sr.targetTexIndex, 0, maxTex);
                    tr.targetTexProp = target.shaderTexProps[tr.targetTexIndex];
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
                if (target.shaderScalarProps != null && target.shaderScalarProps.Count > 0)
                {
                    var maxSca = Mathf.Max(0, target.shaderScalarProps.Count - 1);
                    tr.modPropIndex = Mathf.Clamp((int)sr.modPropIndex, 0, maxSca);
                    tr.modProp = target.shaderScalarProps[tr.modPropIndex];
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
}
#endif
