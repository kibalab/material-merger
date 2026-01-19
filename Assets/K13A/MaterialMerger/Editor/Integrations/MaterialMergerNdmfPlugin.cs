#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using nadena.dev.ndmf;
using K13A.MaterialMerger;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services;
using K13A.MaterialMerger.Editor.Services.Logging;

[assembly: ExportsPlugin(typeof(K13A.MaterialMerger.Editor.Integrations.MaterialMergerNdmfPlugin))]

namespace K13A.MaterialMerger.Editor.Integrations
{
    /// <summary>
    /// NDMF 빌드 파이프라인에 머티리얼 머지 단계를 추가하는 플러그인
    /// </summary>
    public class MaterialMergerNdmfPlugin : Plugin<MaterialMergerNdmfPlugin>
    {
        public override string QualifiedName => "k13a.material-merger.ndmf";
        public override string DisplayName => "Material Merger";

        protected override void Configure()
        {
            InPhase(BuildPhase.Optimizing).Run("Merge materials", ctx =>
            {
                MaterialMergerNdmfRunner.Run(ctx);
            });
        }
    }

    internal static class MaterialMergerNdmfRunner
    {
        public static void Run(BuildContext ctx)
        {
            if (ctx == null || !ctx.AvatarRootObject) return;

            var root = ctx.AvatarRootObject;
            var profile = root.GetComponent<MaterialMergeProfile>();
            if (!profile)
            {
                Debug.LogWarning("[MaterialMerger] NDMF integration skipped: MaterialMergeProfile not found.");
                return;
            }
            if (!profile.ndmfEnabled) return;

            ITextureProcessor textureProcessor = null;

            try
            {
                var outputFolder = ResolveOutputFolder(ctx, profile);

                var loggingService = new LoggingService();
                var atlasGenerator = new AtlasGenerator();
                textureProcessor = new TextureProcessor();
                var meshRemapper = new MeshRemapper();
                var scanService = new MaterialScanService { LoggingService = loggingService };
                var profileService = new ProfileService();

                var settings = new BuildSettings(
                    root,
                    outputFolder,
                    false,
                    false,
                    false,
                    profile.atlasSize,
                    profile.grid,
                    profile.paddingPx,
                    profile.groupByKeywords,
                    profile.groupByRenderQueue,
                    profile.splitOpaqueTransparent,
                    (DiffPolicy)profile.diffPolicy,
                    profile.diffSampleMaterial
                );

                var (isValid, errorMessage) = BuildUtility.ValidateBuildSettings(settings);
                if (!isValid)
                {
                    ReportError(errorMessage);
                    return;
                }

                Directory.CreateDirectory(outputFolder);

                var blitShaderPath = Path.Combine(outputFolder, Constants.BlitShaderFileName).Replace("\\", "/");
                textureProcessor.EnsureBlitMaterial(blitShaderPath);
                if (!textureProcessor.BlitMaterial)
                {
                    ReportError("Blit material initialization failed");
                    return;
                }

                var scans = scanService.ScanGameObject(
                    root,
                    settings.GroupByKeywords,
                    settings.GroupByRenderQueue,
                    settings.SplitOpaqueTransparent,
                    settings.Grid);

                profileService.ApplyProfileToScans(profile, scans);

                var assetBuilder = new AtlasAssetBuilder
                {
                    AtlasGenerator = atlasGenerator,
                    TextureProcessor = textureProcessor,
                    ScanService = scanService,
                    LoggingService = loggingService
                };

                var sceneApplier = new SceneApplier
                {
                    AtlasGenerator = atlasGenerator,
                    TextureProcessor = textureProcessor,
                    MeshRemapper = meshRemapper,
                    ScanService = scanService,
                    LoggingService = loggingService
                };

                var buildResult = assetBuilder.BuildAssets(settings, scans);
                if (!buildResult.success)
                {
                    ReportError(buildResult.errorMessage ?? "Asset build failed");
                    return;
                }

                var applyResult = sceneApplier.ApplyToScene(settings, scans, buildResult);
                if (!applyResult.success)
                {
                    ReportError(applyResult.errorMessage ?? "Scene apply failed");
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MaterialMerger] NDMF integration failed: {ex.Message}");
                ErrorReport.ReportException(ex);
            }
            finally
            {
                textureProcessor?.Cleanup();
            }
        }

        private static string ResolveOutputFolder(
            BuildContext ctx,
            MaterialMergeProfile profile)
        {
            if (profile != null && profile.ndmfUseTemporaryOutputFolder)
            {
                var ndmfRoot = TryGetTemporaryRoot(ctx);
                if (!string.IsNullOrEmpty(ndmfRoot))
                    return Path.Combine(ndmfRoot, "MaterialMerger").Replace("\\", "/");
            }

            if (!string.IsNullOrEmpty(profile.outputFolder))
                return profile.outputFolder.Replace("\\", "/");

            return Constants.DefaultOutputFolder;
        }

        private static string TryGetTemporaryRoot(BuildContext ctx)
        {
            if (ctx == null || ctx.AssetSaver == null) return null;

            var container = ctx.AssetSaver.CurrentContainer;
            if (!container) return null;

            var containerPath = AssetDatabase.GetAssetPath(container);
            if (string.IsNullOrEmpty(containerPath)) return null;

            var dir = Path.GetDirectoryName(containerPath);
            if (string.IsNullOrEmpty(dir)) return null;

            dir = dir.Replace("\\", "/");
            if (dir.EndsWith("/_assets", StringComparison.OrdinalIgnoreCase))
            {
                dir = Path.GetDirectoryName(dir);
                if (string.IsNullOrEmpty(dir)) return null;
                dir = dir.Replace("\\", "/");
            }

            return dir;
        }

        private static void ReportError(string message)
        {
            var errorMessage = string.IsNullOrEmpty(message) ? "Unknown error" : message;
            Debug.LogError($"[MaterialMerger] NDMF integration failed: {errorMessage}");
            ErrorReport.ReportException(new Exception(errorMessage));
        }
    }
}
#endif
