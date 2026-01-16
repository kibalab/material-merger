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
        /// Build assets for a single group
        /// </summary>
        /// <param name="group">Group to build</param>
        /// <param name="log">Log to record created assets</param>
        /// <param name="settings">Build settings</param>
        /// <param name="outputFolder">Output folder path</param>
        /// <returns>Build data for the group, or null if failed</returns>
        GroupBuildData BuildGroupAssets(
            GroupScan group,
            KibaMultiAtlasMergerLog log,
            BuildSettings settings,
            string outputFolder);
    }
}
#endif
