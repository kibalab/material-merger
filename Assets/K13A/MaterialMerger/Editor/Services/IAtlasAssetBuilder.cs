#if UNITY_EDITOR
using System.Collections.Generic;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    /// <summary>
    /// Responsible for building atlas textures and merged materials (Phase 1).
    /// Single Responsibility: Asset creation only, no scene modification.
    /// </summary>
    public interface IAtlasAssetBuilder
    {
        /// <summary>
        /// Build atlas assets for all enabled groups
        /// </summary>
        /// <param name="settings">Build settings</param>
        /// <param name="scans">Groups to build</param>
        /// <returns>Build result containing created assets</returns>
        AssetBuildResult BuildAssets(BuildSettings settings, List<GroupScan> scans);

        /// <summary>
        /// Begin async asset building
        /// </summary>
        /// <param name="settings">Build settings</param>
        /// <param name="scans">Groups to build</param>
        /// <returns>Async build context</returns>
        AssetBuildContext BeginBuildAsync(BuildSettings settings, List<GroupScan> scans);

        /// <summary>
        /// Step async build process (time-sliced)
        /// </summary>
        /// <param name="context">Async build context</param>
        /// <param name="timeBudgetSeconds">Time budget for this step</param>
        /// <returns>True if still running</returns>
        bool StepBuildAsync(AssetBuildContext context, double timeBudgetSeconds);

        /// <summary>
        /// Cancel async build process
        /// </summary>
        /// <param name="context">Async build context</param>
        void CancelBuildAsync(AssetBuildContext context);

        /// <summary>
        /// Build assets for a single group
        /// </summary>
        /// <param name="group">Group to build</param>
        /// <param name="log">Log to record created assets</param>
        /// <param name="settings">Build settings</param>
        /// <param name="outputFolder">Output folder path</param>
        /// <param name="textureImports">Texture import requests to enqueue</param>
        /// <returns>Build data for the group, or null if failed</returns>
        GroupBuildData BuildGroupAssets(
            GroupScan group,
            KibaMultiAtlasMergerLog log,
            BuildSettings settings,
            string outputFolder,
            List<TextureImportRequest> textureImports);
    }
}
#endif
