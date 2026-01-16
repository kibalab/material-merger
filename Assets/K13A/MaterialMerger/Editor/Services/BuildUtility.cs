#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    /// <summary>
    /// Pure utility functions for build operations.
    /// These functions have no side effects and are easily testable.
    /// </summary>
    public static class BuildUtility
    {
        #region Property Analysis

        /// <summary>
        /// Check if a BakeMode requires a target texture property
        /// </summary>
        public static bool RequiresTargetTexture(BakeMode mode)
        {
            return mode == BakeMode.BakeColorToTexture ||
                   mode == BakeMode.BakeScalarToGrayscale ||
                   mode == BakeMode.MultiplyColorWithTexture;
        }

        /// <summary>
        /// Check if a row represents an unresolved difference
        /// </summary>
        public static bool IsUnresolvedDifference(Row row)
        {
            if (row == null) return false;
            if (row.type == ShaderUtil.ShaderPropertyType.TexEnv) return false;
            
            return (row.type == ShaderUtil.ShaderPropertyType.Color ||
                    row.type == ShaderUtil.ShaderPropertyType.Float ||
                    row.type == ShaderUtil.ShaderPropertyType.Range ||
                    row.type == ShaderUtil.ShaderPropertyType.Vector) &&
                   row.distinctCount > 1 && !row.doAction;
        }

        /// <summary>
        /// Check if a group has unresolved differences
        /// </summary>
        public static bool HasUnresolvedDifferences(GroupScan group)
        {
            if (group?.rows == null) return false;
            return group.rows.Any(IsUnresolvedDifference);
        }

        /// <summary>
        /// Get all active texture properties for atlasing
        /// </summary>
        public static List<string> GetActiveTextureProperties(GroupScan group)
        {
            if (group?.rows == null) return new List<string>();
            
            return group.rows
                .Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.doAction)
                .Select(r => r.name)
                .ToList();
        }

        /// <summary>
        /// Get rows that need baking (not textures, with bake action)
        /// </summary>
        public static List<Row> GetBakeRows(GroupScan group)
        {
            if (group?.rows == null) return new List<Row>();
            
            return group.rows
                .Where(r => r.doAction &&
                           r.type != ShaderUtil.ShaderPropertyType.TexEnv &&
                           RequiresTargetTexture(r.bakeMode))
                .ToList();
        }

        /// <summary>
        /// Get rows that need reset to shader default
        /// </summary>
        public static List<Row> GetResetRows(GroupScan group)
        {
            if (group?.rows == null) return new List<Row>();
            
            return group.rows
                .Where(r => r.doAction &&
                           r.type != ShaderUtil.ShaderPropertyType.TexEnv &&
                           r.bakeMode == BakeMode.ResetToDefault)
                .ToList();
        }

        /// <summary>
        /// Get all atlas properties (textures + bake targets)
        /// </summary>
        public static HashSet<string> GetAllAtlasProperties(GroupScan group)
        {
            var result = new HashSet<string>(GetActiveTextureProperties(group), StringComparer.Ordinal);
            
            foreach (var row in GetBakeRows(group))
            {
                if (!string.IsNullOrEmpty(row.targetTexProp))
                    result.Add(row.targetTexProp);
            }
            
            return result;
        }

        #endregion

        #region Grid Calculations

        /// <summary>
        /// Calculate optimal grid dimensions for a given material count
        /// </summary>
        /// <param name="materialCount">Number of materials</param>
        /// <param name="maxGrid">Maximum grid dimension</param>
        /// <returns>Tuple of (columns, rows)</returns>
        public static (int cols, int rows) CalculateOptimalGrid(int materialCount, int maxGrid)
        {
            if (materialCount <= 0) return (1, 1);
            
            int cols = Mathf.CeilToInt(Mathf.Sqrt(materialCount));
            int rows = Mathf.CeilToInt(materialCount / (float)cols);
            
            // Limit to max grid
            if (cols > maxGrid)
            {
                cols = maxGrid;
                rows = Mathf.CeilToInt(materialCount / (float)maxGrid);
            }
            
            return (cols, rows);
        }

        /// <summary>
        /// Calculate actual atlas size based on grid dimensions
        /// </summary>
        public static int CalculateAtlasSize(int gridCols, int gridRows, int cellSize, int maxSize)
        {
            int neededSize = Mathf.Max(gridCols, gridRows) * cellSize;
            return Mathf.Clamp(neededSize, cellSize, maxSize);
        }

        /// <summary>
        /// Calculate tile position in grid
        /// </summary>
        public static (int x, int y) CalculateTilePosition(int tileIndex, int gridCols, int cellSize, int padding)
        {
            int gx = tileIndex % gridCols;
            int gy = tileIndex / gridCols;
            return (gx * cellSize + padding, gy * cellSize + padding);
        }

        /// <summary>
        /// Calculate UV transform for a tile
        /// </summary>
        public static (Vector2 scale, Vector2 offset) CalculateUvTransform(
            int tileIndex, int gridCols, int cellSize, int contentSize, int padding, int atlasSize)
        {
            int gx = tileIndex % gridCols;
            int gy = tileIndex / gridCols;
            
            float tileScale = contentSize / (float)atlasSize;
            float ox = (gx * cellSize + padding) / (float)atlasSize;
            float oy = (gy * cellSize + padding) / (float)atlasSize;
            
            return (new Vector2(tileScale, tileScale), new Vector2(ox, oy));
        }

        #endregion

        #region Property Detection

        /// <summary>
        /// Check if a property name indicates a normal map
        /// </summary>
        public static bool IsNormalMapProperty(string propName)
        {
            if (string.IsNullOrEmpty(propName)) return false;
            
            return propName.IndexOf("Bump", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   propName.IndexOf("Normal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Check if a property name indicates linear (non-sRGB) data
        /// </summary>
        public static bool IsLinearProperty(string propName)
        {
            if (string.IsNullOrEmpty(propName)) return false;
            
            string p = propName.ToLowerInvariant();
            return p.Contains("mask") ||
                   p.Contains("metal") ||
                   p.Contains("rough") ||
                   p.Contains("smooth") ||
                   p.Contains("occlusion") ||
                   p.Contains("ao") ||
                   p.Contains("spec") ||
                   p.Contains("depth");
        }

        /// <summary>
        /// Get ST (scale/tiling) vector from material
        /// </summary>
        public static Vector4 GetScaleTiling(Material material, string propName)
        {
            if (!material) return new Vector4(1, 1, 0, 0);
            
            string stName = propName + "_ST";
            if (!material.HasProperty(stName)) return new Vector4(1, 1, 0, 0);
            
            return material.GetVector(stName);
        }

        #endregion

        #region String Operations

        /// <summary>
        /// Generate a safe folder name for a group
        /// </summary>
        public static string GetGroupFolderName(GroupScan group, Func<string, string> sanitize)
        {
            if (group == null) return "Unknown";
            
            string shaderName = group.key.shader 
                ? group.key.shader.name 
                : (string.IsNullOrEmpty(group.shaderName) ? "NullShader" : group.shaderName);
            
            string baseName = sanitize(shaderName);
            string keyPart = $"RQ{group.key.renderQueue}_KW{group.key.keywordsHash}_T{group.key.transparencyKey}";
            string combined = $"{baseName}_{keyPart}";
            combined = sanitize(combined);
            
            return string.IsNullOrEmpty(combined) ? "Plan" : combined;
        }

        /// <summary>
        /// Get output material base name for a group
        /// </summary>
        public static string GetOutputMaterialName(GroupScan group)
        {
            if (!string.IsNullOrWhiteSpace(group?.outputMaterialName)) 
                return group.outputMaterialName;
            
            string shaderName = group?.key.shader 
                ? group.key.shader.name 
                : group?.shaderName;
            
            if (!string.IsNullOrWhiteSpace(shaderName)) 
                return shaderName;
            
            return "Merged";
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate build settings
        /// </summary>
        public static (bool isValid, string errorMessage) ValidateBuildSettings(BuildSettings settings)
        {
            if (settings == null)
                return (false, "BuildSettings is null");
            
            if (settings.CloneRootOnApply && !settings.Root)
                return (false, "Root is required when clone is enabled");
            
            if (settings.AtlasSize < 64)
                return (false, "Atlas size too small");
            
            if (settings.Grid < 1)
                return (false, "Grid must be at least 1");
            
            if (settings.ContentSize <= 0)
                return (false, "Padding too large for cell size");
            
            if (string.IsNullOrEmpty(settings.OutputFolder))
                return (false, "Output folder not specified");
            
            return (true, null);
        }

        /// <summary>
        /// Check if a group should be processed
        /// </summary>
        public static bool ShouldProcessGroup(GroupScan group, DiffPolicy policy)
        {
            if (group == null || !group.enabled) return false;
            
            if (policy == DiffPolicy.StopIfUnresolved && HasUnresolvedDifferences(group))
                return false;
            
            return true;
        }

        #endregion

        #region Submesh Merging

        /// <summary>
        /// Try to build a submesh merge map for materials that share the same merged material
        /// </summary>
        public static bool TryBuildSubmeshMergeMap(
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
            
            // Build effective materials array
            var effectiveMaterials = new Material[subMeshCount];
            var fallback = materials[materials.Length - 1];
            for (int s = 0; s < subMeshCount; s++)
                effectiveMaterials[s] = s < materials.Length ? materials[s] : fallback;
            
            // Build merge map
            var keyToIndex = new Dictionary<(int, MeshTopology), int>();
            var uniqueMaterials = new List<Material>();
            var map = new int[subMeshCount];
            
            for (int s = 0; s < subMeshCount; s++)
            {
                var mat = effectiveMaterials[s];
                var topology = mesh.GetTopology(s);
                
                if (!mat || (mergeCandidates != null && !mergeCandidates.Contains(mat)))
                {
                    map[s] = uniqueMaterials.Count;
                    uniqueMaterials.Add(mat);
                    continue;
                }
                
                var key = (mat.GetInstanceID(), topology);
                if (!keyToIndex.TryGetValue(key, out var index))
                {
                    index = uniqueMaterials.Count;
                    keyToIndex[key] = index;
                    uniqueMaterials.Add(mat);
                }
                
                map[s] = index;
            }
            
            // Check if any merging occurred
            if (uniqueMaterials.Count >= subMeshCount) return false;
            
            mergedMaterials = uniqueMaterials.ToArray();
            submeshMergeMap = map;
            return true;
        }

        #endregion
    }
}
#endif
