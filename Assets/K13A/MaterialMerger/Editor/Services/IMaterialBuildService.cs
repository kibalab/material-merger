#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor.Services
{
    public interface IMaterialBuildService
    {
        ILocalizationService LocalizationService { get; set; }

        /// <summary>
        /// Show build confirmation window
        /// </summary>
        /// <param name="executor">Build executor callback</param>
        /// <param name="root">Root GameObject</param>
        /// <param name="scans">Scanned groups</param>
        /// <param name="diffPolicy">Difference handling policy</param>
        /// <param name="sampleMaterial">Sample material for diff resolution</param>
        /// <param name="outputFolder">Output folder path</param>
        void BuildAndApplyWithConfirm(
            IBuildExecutor executor,
            GameObject root,
            List<GroupScan> scans,
            DiffPolicy diffPolicy,
            Material sampleMaterial,
            string outputFolder
        );

        /// <summary>
        /// Build and apply atlas with settings object
        /// </summary>
        /// <param name="settings">Build settings</param>
        /// <param name="scans">Scanned groups</param>
        void BuildAndApply(BuildSettings settings, List<GroupScan> scans);

        /// <summary>
        /// 적용을 위한 루트 복제
        /// </summary>
        GameObject CloneRootForApply(GameObject src, bool deactivateOriginal, bool keepPrefab);

        /// <summary>
        /// 설정 복사 (from -> to)
        /// </summary>
        void CopySettings(List<GroupScan> from, List<GroupScan> to);
    }
}
#endif
