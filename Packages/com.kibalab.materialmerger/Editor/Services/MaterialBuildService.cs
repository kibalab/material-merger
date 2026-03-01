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

        private const double BuildStepBudgetSeconds = 0.01d;
        private const double ApplyStepBudgetSeconds = 0.008d;
        private const int ImportSettingsPerTick = 1;
        private AsyncPipelineContext pipeline;

        private enum PipelinePhase
        {
            Build,
            ImportSettings,
            Apply
        }

        private class AsyncPipelineContext
        {
            public BuildSettings settings;
            public List<GroupScan> scans;
            public AssetBuildContext buildContext;
            public AssetBuildResult buildResult;
            public SceneApplyContext applyContext;
            public Queue<TextureImportRequest> importQueue;
            public int totalImports;
            public int processedImports;
            public PipelinePhase phase;
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

            if (pipeline != null)
            {
                LoggingService?.Warning("Async build already in progress");
                return;
            }
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

            pipeline = new AsyncPipelineContext
            {
                settings = settings,
                scans = scans,
                phase = PipelinePhase.Build
            };

            pipeline.buildContext = AssetBuilder.BeginBuildAsync(settings, scans);
            if (pipeline.buildContext == null)
            {
                pipeline = null;
                LoggingService?.Error("Asset build failed", "Build context not initialized", true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), LocalizationService.Get(L10nKey.DialogServiceNotInitialized), "OK");
                return;
            }

            if (pipeline.buildContext.completed && !pipeline.buildContext.result.success)
            {
                var msg = string.IsNullOrEmpty(pipeline.buildContext.errorMessage)
                    ? "Asset build failed"
                    : pipeline.buildContext.errorMessage;
                pipeline = null;
                LoggingService?.Error("Asset build failed", msg, true);
                EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), msg, "OK");
                return;
            }

            LoggingService?.Info("Async build pipeline started");
            EditorApplication.update += ProcessAsyncPipeline;
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

        private void ProcessAsyncPipeline()
        {
            if (pipeline == null)
            {
                EditorApplication.update -= ProcessAsyncPipeline;
                return;
            }

            if (EditorApplication.isCompiling)
                return;

            switch (pipeline.phase)
            {
                case PipelinePhase.Build:
                {
                    bool running = AssetBuilder.StepBuildAsync(pipeline.buildContext, BuildStepBudgetSeconds);
                    if (!running)
                    {
                        if (!pipeline.buildContext.result.success)
                        {
                            var msg = string.IsNullOrEmpty(pipeline.buildContext.errorMessage)
                                ? "Asset build failed"
                                : pipeline.buildContext.errorMessage;
                            FailPipeline(msg);
                            return;
                        }

                        pipeline.buildResult = pipeline.buildContext.result;
                        var imports = pipeline.buildResult.textureImports ?? new List<TextureImportRequest>();
                        pipeline.importQueue = new Queue<TextureImportRequest>(imports);
                        pipeline.totalImports = pipeline.importQueue.Count;
                        pipeline.processedImports = 0;
                        if (pipeline.totalImports > 0)
                            LoggingService?.Info("Importing textures (async)", $"{pipeline.totalImports} textures");
                        pipeline.phase = PipelinePhase.ImportSettings;
                    }
                    break;
                }
                case PipelinePhase.ImportSettings:
                {
                    if (pipeline.importQueue != null && pipeline.importQueue.Count > 0)
                    {
                        int count = Math.Min(ImportSettingsPerTick, pipeline.importQueue.Count);
                        for (int i = 0; i < count; i++)
                        {
                            var request = pipeline.importQueue.Dequeue();
                            ApplyTextureImportSettings(request);
                            pipeline.processedImports++;
                        }

                        if (pipeline.importQueue.Count == 0 && pipeline.totalImports > 0)
                            LoggingService?.Success("Texture import settings applied", $"{pipeline.processedImports} textures");

                        return;
                    }

                    ReleaseAutoRefreshIfNeeded();

                    pipeline.applyContext = SceneApplier.BeginApplyAsync(pipeline.settings, pipeline.scans, pipeline.buildResult);
                    if (pipeline.applyContext.completed && !string.IsNullOrEmpty(pipeline.applyContext.errorMessage))
                    {
                        FailPipeline(pipeline.applyContext.errorMessage);
                        return;
                    }

                    pipeline.phase = PipelinePhase.Apply;
                    break;
                }
                case PipelinePhase.Apply:
                {
                    bool running = SceneApplier.StepApplyAsync(pipeline.applyContext, ApplyStepBudgetSeconds);
                    if (!running)
                    {
                        if (!string.IsNullOrEmpty(pipeline.applyContext.errorMessage))
                        {
                            FailPipeline(pipeline.applyContext.errorMessage);
                            return;
                        }

                        FinishPipeline();
                    }
                    break;
                }
            }
        }

        private void ApplyTextureImportSettings(TextureImportRequest request)
        {
            TextureImportUtility.ApplySettings(request);
        }

        private void ReleaseAutoRefreshIfNeeded()
        {
            if (pipeline == null || pipeline.buildContext == null) return;
            if (!pipeline.buildContext.autoRefreshDisabled) return;

            AssetDatabase.AllowAutoRefresh();
            pipeline.buildContext.autoRefreshDisabled = false;
        }

        private void FinishPipeline()
        {
            ReleaseAutoRefreshIfNeeded();
            var completed = pipeline;
            pipeline = null;
            EditorApplication.update -= ProcessAsyncPipeline;

            LoggingService?.Info("------------------------------------------------", null, true);
            LoggingService?.Success("    Build & Apply Complete", $"Processed: {completed.applyContext.renderersProcessed} renderers", true);
            LoggingService?.Info("------------------------------------------------", null, true);

            var message = $"{LocalizationService.Get(L10nKey.DialogComplete)}\n{LocalizationService.Get(L10nKey.DialogLog, completed.buildResult.logPath)}";
            EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), message, "OK");
        }

        private void FailPipeline(string errorMessage)
        {
            if (pipeline != null)
            {
                if (pipeline.buildContext != null)
                    AssetBuilder.CancelBuildAsync(pipeline.buildContext);
                if (pipeline.applyContext != null)
                    SceneApplier.CancelApplyAsync(pipeline.applyContext);
            }

            pipeline = null;
            EditorApplication.update -= ProcessAsyncPipeline;

            LoggingService?.Error("Async build failed", errorMessage, true);
            EditorUtility.DisplayDialog(LocalizationService.Get(L10nKey.WindowTitle), errorMessage, "OK");
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

