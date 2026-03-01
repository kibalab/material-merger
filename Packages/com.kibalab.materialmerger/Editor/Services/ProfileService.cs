#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    public class ProfileService : IProfileService
    {
        public MaterialMergeProfile EnsureProfile(GameObject target, bool createIfMissing)
        {
            if (!target) return null;
            var profile = target.GetComponent<MaterialMergeProfile>();
            if (profile || !createIfMissing) return profile;

            profile = Undo.AddComponent<MaterialMergeProfile>(target);
            return profile;
        }

        public List<GroupScan> LoadScansFromProfile(
            MaterialMergeProfile profile,
            GameObject root,
            int grid,
            IMaterialScanService scanService)
        {
            var result = new List<GroupScan>();
            if (!profile || profile.groups == null || profile.groups.Count == 0) return result;

            Dictionary<GroupKey, GroupScan> scanMap = null;
            if (root && scanService != null)
            {
                var scannedGroups = scanService.ScanGameObject(
                    root,
                    profile.groupByKeywords,
                    profile.groupByRenderQueue,
                    profile.splitOpaqueTransparent,
                    grid);
                scanMap = scannedGroups.ToDictionary(g => g.key, g => g);
            }

            foreach (var gs in profile.groups)
            {
                Shader shader = null;
                var shaderName = gs.shaderName ?? "";

                // GUID로 셰이더 찾기
                if (!string.IsNullOrEmpty(gs.shaderGuid))
                {
                    var shaderPath = AssetDatabase.GUIDToAssetPath(gs.shaderGuid);
                    if (!string.IsNullOrEmpty(shaderPath))
                        shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                }

                // 이름으로 찾기
                if (!shader && !string.IsNullOrEmpty(shaderName))
                    shader = Shader.Find(shaderName);

                // 루트에서 찾기
                if (!shader)
                    shader = TryResolveShaderFromRoot(root, gs, scanService,
                        profile.groupByKeywords, profile.groupByRenderQueue, profile.splitOpaqueTransparent);

                if (shader)
                    shaderName = shader.name;

                var group = CreateGroupScanFromProfileData(gs, shader, shaderName, grid, scanService);

                if (scanMap != null && scanMap.TryGetValue(group.key, out var scannedGroup))
                {
                    group.mats = scannedGroup.mats ?? new List<MatInfo>();
                    group.tilesPerPage = scannedGroup.tilesPerPage;
                    group.pageCount = scannedGroup.pageCount;
                    group.skippedMultiMat = scannedGroup.skippedMultiMat;
                }

                result.Add(group);
            }

            return result;
        }

        public void ApplyProfileToScans(MaterialMergeProfile profile, List<GroupScan> scans)
        {
            if (!profile || scans == null || scans.Count == 0) return;

            // GroupData를 키로 매핑
            var groupMap = new Dictionary<(string, int, int, int), MaterialMergeProfile.GroupData>();
            var groupMapByName = new Dictionary<(string, int, int, int), MaterialMergeProfile.GroupData>();

            foreach (var gs in profile.groups)
            {
                groupMap[(gs.shaderGuid ?? "", gs.keywordsHash, gs.renderQueue, gs.transparencyKey)] = gs;
                if (!string.IsNullOrEmpty(gs.shaderName))
                    groupMapByName[(gs.shaderName, gs.keywordsHash, gs.renderQueue, gs.transparencyKey)] = gs;
            }

            foreach (var group in scans)
            {
                var shaderGuid = "";
                if (group.key.shader)
                {
                    var shaderPath = AssetDatabase.GetAssetPath(group.key.shader);
                    if (!string.IsNullOrEmpty(shaderPath))
                        shaderGuid = AssetDatabase.AssetPathToGUID(shaderPath);
                }

                // GUID로 찾기
                if (!groupMap.TryGetValue((shaderGuid, group.key.keywordsHash, group.key.renderQueue, group.key.transparencyKey), out var gs))
                {
                    // 이름으로 찾기
                    var nameKey = group.key.shader ? group.key.shader.name : group.shaderName;
                    if (string.IsNullOrEmpty(nameKey) || !groupMapByName.TryGetValue((nameKey, group.key.keywordsHash, group.key.renderQueue, group.key.transparencyKey), out gs))
                        continue;
                }

                ApplyGroupDataToGroupScan(gs, group);
            }
        }

        public void SaveScansToProfile(MaterialMergeProfile profile, List<GroupScan> scans)
        {
            if (!profile) return;

            profile.groups.Clear();

            if (scans == null || scans.Count == 0) return;

            foreach (var group in scans)
            {
                var groupData = CreateProfileGroupDataFromScan(group);
                profile.groups.Add(groupData);
            }

            EditorUtility.SetDirty(profile);
        }

        public bool TryGetShaderPropertyInfo(Shader shader, string propertyName, out ShaderUtil.ShaderPropertyType type, out int index)
        {
            type = ShaderUtil.ShaderPropertyType.Float;
            index = -1;
            if (!shader || string.IsNullOrEmpty(propertyName)) return false;

            int propertyCount = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < propertyCount; i++)
            {
                if (ShaderUtil.GetPropertyName(shader, i) == propertyName)
                {
                    type = ShaderUtil.GetPropertyType(shader, i);
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public Shader TryResolveShaderFromRoot(
            GameObject root,
            MaterialMergeProfile.GroupData groupData,
            IMaterialScanService scanService,
            bool groupByKeywords,
            bool groupByRenderQueue,
            bool splitOpaqueTransparent)
        {
            if (!root) return null;

            var renderers = scanService.CollectRenderers(root);
            foreach (var renderer in renderers)
            {
                if (!renderer) continue;
                var materials = renderer.sharedMaterials;
                if (materials == null) continue;

                foreach (var material in materials)
                {
                    if (!material || !material.shader) continue;

                    var key = scanService.CreateGroupKey(material, groupByKeywords, groupByRenderQueue, splitOpaqueTransparent);
                    if (key.keywordsHash == groupData.keywordsHash &&
                        key.renderQueue == groupData.renderQueue &&
                        key.transparencyKey == groupData.transparencyKey)
                        return material.shader;
                }
            }

            return null;
        }

        private GroupScan CreateGroupScanFromProfileData(
            MaterialMergeProfile.GroupData gs,
            Shader shader,
            string shaderName,
            int grid,
            IMaterialScanService scanService)
        {
            var group = new GroupScan();
            group.key = new GroupKey
            {
                shader = shader,
                keywordsHash = gs.keywordsHash,
                renderQueue = gs.renderQueue,
                transparencyKey = gs.transparencyKey
            };
            group.shaderName = shaderName;
            group.tag = string.IsNullOrEmpty(gs.tag) ? (gs.transparencyKey == 1 ? "투명" : "불투명") : gs.tag;
            group.tilesPerPage = gs.tilesPerPage > 0 ? gs.tilesPerPage : Mathf.Max(1, grid * grid);
            group.pageCount = gs.pageCount;
            group.skippedMultiMat = gs.skippedMultiMat;
            var defaultOutputName = string.IsNullOrEmpty(gs.outputMaterialName) ? shaderName : gs.outputMaterialName;
            group.outputMaterialName = string.IsNullOrEmpty(defaultOutputName) ? "Merged" : defaultOutputName;
            group.mergeKey = gs.mergeKey ?? "";

            group.enabled = gs.enabled;
            group.foldout = gs.foldout;

            group.search = gs.search ?? "";
            group.onlyRelevant = gs.onlyRelevant;
            group.showTexturesOnly = gs.showTexturesOnly;
            group.showScalarsOnly = gs.showScalarsOnly;

            group.shaderTexProps = scanService.GetShaderPropertiesByType(shader, ShaderUtil.ShaderPropertyType.TexEnv);
            group.shaderScalarProps = scanService.GetShaderPropertiesByType(shader, ShaderUtil.ShaderPropertyType.Float)
                .Concat(scanService.GetShaderPropertiesByType(shader, ShaderUtil.ShaderPropertyType.Range))
                .Distinct()
                .ToList();

            // MaterialCount 복원
            int matCount = gs.materialCount;
            if (matCount <= 0 && gs.pageCount > 0 && group.tilesPerPage > 0)
                matCount = gs.pageCount * group.tilesPerPage;
            if (matCount > 0)
            {
                group.mats = new List<MatInfo>(matCount);
                for (int i = 0; i < matCount; i++) group.mats.Add(new MatInfo());
            }

            // Rows 복원
            group.rows = LoadRowsFromProfileData(gs, shader, group);

            return group;
        }

        private List<Row> LoadRowsFromProfileData(
            MaterialMergeProfile.GroupData gs,
            Shader shader,
            GroupScan group)
        {
            var rows = new List<Row>();
            if (gs.rows == null || gs.rows.Count == 0) return rows;

            foreach (var rs in gs.rows)
            {
                if (rs == null || string.IsNullOrEmpty(rs.name)) continue;

                var row = new Row();
                row.name = rs.name;
                row.shaderPropIndex = rs.shaderPropIndex;
                row.type = (ShaderUtil.ShaderPropertyType)rs.shaderPropType;

                // 셰이더에서 프로퍼티 정보 업데이트
                if (shader && TryGetShaderPropertyInfo(shader, row.name, out var type, out var index))
                {
                    row.type = type;
                    row.shaderPropIndex = index;
                }

                row.doAction = rs.doAction;
                row.bakeMode = (BakeMode)rs.bakeMode;
                row.targetTexIndex = rs.targetTexIndex;
                row.targetTexProp = rs.targetTexProp;

                row.includeAlpha = rs.includeAlpha;
                row.resetSourceAfterBake = rs.resetSourceAfterBake;

                row.modOp = (ModOp)rs.modOp;
                row.modPropIndex = rs.modPropIndex;
                row.modProp = rs.modProp;
                row.modClamp01 = rs.modClamp01;
                row.modScale = rs.modScale;
                row.modBias = rs.modBias;
                row.modAffectsAlpha = rs.modAffectsAlpha;

                row.expanded = rs.expanded;

                row.texNonNull = rs.texNonNull;
                row.texDistinct = rs.texDistinct;
                row.stDistinct = rs.stDistinct;
                row.isNormalLike = rs.isNormalLike;
                row.isSRGB = rs.isSRGB;
                row.distinctCount = rs.distinctCount;

                rows.Add(row);
            }

            // TargetTexProp 인덱스 재계산
            if (group.shaderTexProps != null && group.shaderTexProps.Count > 0)
            {
                foreach (var row in rows)
                {
                    if (row.type == ShaderUtil.ShaderPropertyType.TexEnv) continue;

                    if (!string.IsNullOrEmpty(row.targetTexProp))
                    {
                        int idx = group.shaderTexProps.IndexOf(row.targetTexProp);
                        if (idx >= 0) row.targetTexIndex = idx;
                    }

                    row.targetTexIndex = Mathf.Clamp(row.targetTexIndex, 0, group.shaderTexProps.Count - 1);
                    row.targetTexProp = group.shaderTexProps[row.targetTexIndex];
                }
            }

            // ModProp 인덱스 재계산
            if (group.shaderScalarProps != null && group.shaderScalarProps.Count > 0)
            {
                foreach (var row in rows)
                {
                    if (string.IsNullOrEmpty(row.modProp))
                    {
                        row.modPropIndex = 0;
                        continue;
                    }

                    int idx = group.shaderScalarProps.IndexOf(row.modProp);
                    if (idx >= 0)
                    {
                        row.modPropIndex = idx + 1;
                        row.modProp = group.shaderScalarProps[idx];
                    }
                    else
                    {
                        row.modPropIndex = 0;
                        row.modProp = "";
                    }
                }
            }

            return rows;
        }

        private void ApplyGroupDataToGroupScan(MaterialMergeProfile.GroupData gs, GroupScan group)
        {
            group.enabled = gs.enabled;
            group.foldout = gs.foldout;

            group.search = gs.search ?? "";
            group.onlyRelevant = gs.onlyRelevant;
            group.showTexturesOnly = gs.showTexturesOnly;
            group.showScalarsOnly = gs.showScalarsOnly;
            var shaderName = group.key.shader ? group.key.shader.name : group.shaderName;
            var defaultOutputName = string.IsNullOrEmpty(gs.outputMaterialName) ? shaderName : gs.outputMaterialName;
            group.outputMaterialName = string.IsNullOrEmpty(defaultOutputName) ? "Merged" : defaultOutputName;
            group.mergeKey = gs.mergeKey ?? "";

            // Row 설정 적용
            var rowMap = gs.rows
                .Where(x => x != null && !string.IsNullOrEmpty(x.name))
                .GroupBy(x => x.name, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);

            foreach (var row in group.rows)
            {
                if (row == null || string.IsNullOrEmpty(row.name)) continue;
                if (!rowMap.TryGetValue(row.name, out var rs)) continue;

                row.doAction = rs.doAction;
                row.bakeMode = (BakeMode)rs.bakeMode;
                row.targetTexIndex = rs.targetTexIndex;
                row.targetTexProp = rs.targetTexProp;

                row.includeAlpha = rs.includeAlpha;
                row.resetSourceAfterBake = rs.resetSourceAfterBake;

                row.modOp = (ModOp)rs.modOp;
                row.modPropIndex = rs.modPropIndex;
                row.modProp = rs.modProp;
                row.modClamp01 = rs.modClamp01;
                row.modScale = rs.modScale;
                row.modBias = rs.modBias;
                row.modAffectsAlpha = rs.modAffectsAlpha;

                row.expanded = rs.expanded;
            }
        }

        private MaterialMergeProfile.GroupData CreateProfileGroupDataFromScan(GroupScan group)
        {
            var gs = new MaterialMergeProfile.GroupData();

            var shaderGuid = "";
            if (group.key.shader)
            {
                var shaderPath = AssetDatabase.GetAssetPath(group.key.shader);
                if (!string.IsNullOrEmpty(shaderPath))
                    shaderGuid = AssetDatabase.AssetPathToGUID(shaderPath);
            }

            gs.shaderGuid = shaderGuid;
            gs.shaderName = group.key.shader ? group.key.shader.name : (group.shaderName ?? "");
            gs.keywordsHash = group.key.keywordsHash;
            gs.renderQueue = group.key.renderQueue;
            gs.transparencyKey = group.key.transparencyKey;

            gs.tag = group.tag;
            gs.materialCount = group.mats != null ? group.mats.Count : 0;
            gs.tilesPerPage = group.tilesPerPage;
            gs.pageCount = group.pageCount;
            gs.skippedMultiMat = group.skippedMultiMat;
            gs.outputMaterialName = group.outputMaterialName;
            gs.mergeKey = group.mergeKey ?? "";

            gs.enabled = group.enabled;
            gs.foldout = group.foldout;

            gs.search = group.search ?? "";
            gs.onlyRelevant = group.onlyRelevant;
            gs.showTexturesOnly = group.showTexturesOnly;
            gs.showScalarsOnly = group.showScalarsOnly;

            gs.rows.Clear();
            foreach (var row in group.rows)
            {
                if (row == null || string.IsNullOrEmpty(row.name)) continue;

                var rowData = new MaterialMergeProfile.RowData();
                rowData.name = row.name;
                rowData.shaderPropIndex = row.shaderPropIndex;
                rowData.shaderPropType = (int)row.type;
                rowData.doAction = row.doAction;

                rowData.bakeMode = (int)row.bakeMode;
                rowData.targetTexIndex = row.targetTexIndex;
                rowData.targetTexProp = row.targetTexProp;

                rowData.includeAlpha = row.includeAlpha;
                rowData.resetSourceAfterBake = row.resetSourceAfterBake;

                rowData.modOp = (int)row.modOp;
                rowData.modPropIndex = row.modPropIndex;
                rowData.modProp = row.modProp;
                rowData.modClamp01 = row.modClamp01;
                rowData.modScale = row.modScale;
                rowData.modBias = row.modBias;
                rowData.modAffectsAlpha = row.modAffectsAlpha;

                rowData.expanded = row.expanded;

                rowData.texNonNull = row.texNonNull;
                rowData.texDistinct = row.texDistinct;
                rowData.stDistinct = row.stDistinct;
                rowData.isNormalLike = row.isNormalLike;
                rowData.isSRGB = row.isSRGB;
                rowData.distinctCount = row.distinctCount;

                gs.rows.Add(rowData);
            }

            return gs;
        }
    }
}
#endif
