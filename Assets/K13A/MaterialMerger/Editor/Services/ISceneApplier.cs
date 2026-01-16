#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    /// <summary>
    /// Responsible for applying built assets to the scene (Phase 2).
    /// Single Responsibility: Scene modification only, no asset creation.
    /// </summary>
    public interface ISceneApplier
    {
        /// <summary>
        /// Apply built assets to scene renderers
        /// </summary>
        /// <param name="settings">Build settings</param>
        /// <param name="scans">Original scans with settings</param>
        /// <param name="buildResult">Result from asset building phase</param>
        /// <returns>Result of scene application</returns>
        SceneApplyResult ApplyToScene(
            BuildSettings settings,
            List<GroupScan> scans,
            AssetBuildResult buildResult);

        /// <summary>
        /// Clone the root GameObject for applying materials
        /// </summary>
        /// <param name="src">Source root object</param>
        /// <param name="deactivateOriginal">Whether to deactivate the original</param>
        /// <param name="keepPrefab">Whether to keep prefab links</param>
        /// <returns>Cloned GameObject</returns>
        GameObject CloneRootForApply(GameObject src, bool deactivateOriginal, bool keepPrefab);

        /// <summary>
        /// Copy settings from source scans to target scans
        /// </summary>
        /// <param name="from">Source scans with settings</param>
        /// <param name="to">Target scans to copy settings to</param>
        void CopySettings(List<GroupScan> from, List<GroupScan> to);
    }
}
#endif
