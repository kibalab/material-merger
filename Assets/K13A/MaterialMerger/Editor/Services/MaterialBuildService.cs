#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Localization;
using K13A.MaterialMerger.Editor.Services.Logging;

namespace K13A.MaterialMerger.Editor.Services
{
    /// <summary>
    /// Orchestrates the material merge build process.
    /// Delegates asset creation to IAtlasAssetBuilder and scene modification to ISceneApplier.
    /// </summary>
    public class MaterialBuildService : IMaterialBuildService
    {
        // Sub-services for delegation
        public IAtlasAssetBuilder AssetBuilder { get; set; }
        public ISceneApplier SceneApplier { get; set; }

        // Legacy dependencies (kept for backward compatibility with confirm dialog)
        public IAtlasGenerator AtlasGenerator { get; set; }
        public ITextureProcessor TextureProcessor { get; set; }
        public IMeshRemapper MeshRemapper { get; set; }
        public IMaterialScanService ScanService { get; set; }
        public ILocalizationService LocalizationService { get; set; }
        public ILoggingService LoggingService { get; set; }

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

                bool hasUnresolved = BuildUtility.HasUnresolvedDifferences(g);
                bool willRun = !(hasUnresolved && diffPolicy == DiffPolicy.StopIfUnresolved);

                var atlasProps = g.rows
                    .Where(r => r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.doAction)
                    .Select(r => r.name)
                    .Where(x => !string.IsNullOrEmpty(x))
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToList();

                var generatedProps = g.rows
                    .Where(r => r.doAction && r.type != ShaderUtil.ShaderPropertyType.TexEnv && BuildUtility.RequiresTargetTexture(r.bakeMode))
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

            // Validate services
            if (!ValidateServices())
            {
                return;
            }

            // Ensure output folder exists
            LoggingService?.Info("Creating output folder", settings.OutputFolder);
            Directory.CreateDirectory(settings.OutputFolder);

            // Prepare blit material
            string blitShaderPath = Path.Combine(settings.OutputFolder, Constants.BlitShaderFileName).Replace("\\", "/");
            LoggingService?.Info("Preparing blit material", blitShaderPath);
            TextureProcessor.EnsureBlitMaterial(blitShaderPath);
            if (!TextureProcessor.BlitMaterial)
            {
                LoggingService?.Error("Blit material initialization failed", blitShaderPath, true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogBlitFailed), "OK");
                return;
            }

            // ===== PHASE 1: Build Assets =====
            var buildResult = AssetBuilder.BuildAssets(settings, scans);

            if (!buildResult.success)
            {
                LoggingService?.Error("Asset build failed", buildResult.errorMessage, true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), buildResult.errorMessage, "OK");
                return;
            }

            LoggingService?.Success("Asset import complete");

            // ===== PHASE 2: Apply to Scene =====
            var applyResult = SceneApplier.ApplyToScene(settings, scans, buildResult);

            if (!applyResult.success)
            {
                LoggingService?.Error("Scene apply failed", applyResult.errorMessage, true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), applyResult.errorMessage, "OK");
                return;
            }

            LoggingService?.Info("═══════════════════════════════════════", null, true);
            LoggingService?.Success("    Build & Apply Complete", $"Processed: {applyResult.renderersProcessed} groups", true);
            LoggingService?.Info("═══════════════════════════════════════", null, true);

            var message = $"{LocalizationService.Get(L10nKey.DialogComplete)}\n{LocalizationService.Get(L10nKey.DialogLog, buildResult.logPath)}";
            EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), message, "OK");
        }

        private bool ValidateServices()
        {
            if (AssetBuilder == null)
            {
                LoggingService?.Error("Service initialization error", "AssetBuilder not initialized", true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogServiceNotInitialized), "OK");
                return false;
            }

            if (SceneApplier == null)
            {
                LoggingService?.Error("Service initialization error", "SceneApplier not initialized", true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogServiceNotInitialized), "OK");
                return false;
            }

            if (TextureProcessor == null)
            {
                LoggingService?.Error("Service initialization error", "TextureProcessor not initialized", true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogServiceNotInitialized), "OK");
                return false;
            }

            return true;
        }

        #region Legacy Methods (for backward compatibility)

        /// <summary>
        /// Clone the root GameObject for applying materials.
        /// Delegates to SceneApplier.
        /// </summary>
        public GameObject CloneRootForApply(GameObject src, bool deactivateOriginal, bool keepPrefab)
        {
            return SceneApplier?.CloneRootForApply(src, deactivateOriginal, keepPrefab);
        }

        /// <summary>
        /// Copy settings from source scans to target scans.
        /// Delegates to SceneApplier.
        /// </summary>
        public void CopySettings(List<GroupScan> from, List<GroupScan> to)
        {
            SceneApplier?.CopySettings(from, to);
        }

        #endregion
    }
}
#endif
