#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    public interface IMaterialBuildService
    {
        /// <summary>
        /// 빌드 확인 창 표시
        /// </summary>
        void BuildAndApplyWithConfirm(
            dynamic owner,
            GameObject root,
            List<GroupScan> scans,
            DiffPolicy diffPolicy,
            string outputFolder
        );

        /// <summary>
        /// 머티리얼 빌드 및 적용
        /// </summary>
        void BuildAndApply(
            GameObject root,
            List<GroupScan> scans,
            DiffPolicy diffPolicy,
            bool cloneRootOnApply,
            bool deactivateOriginalRoot,
            string outputFolder,
            int atlasSize,
            int grid,
            int paddingPx,
            bool groupByKeywords,
            bool groupByRenderQueue,
            bool splitOpaqueTransparent
        );

        /// <summary>
        /// 적용을 위한 루트 복제
        /// </summary>
        GameObject CloneRootForApply(GameObject src, bool deactivateOriginal);

        /// <summary>
        /// 설정 복사 (from -> to)
        /// </summary>
        void CopySettings(List<GroupScan> from, List<GroupScan> to);
    }
}
#endif
