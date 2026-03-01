#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services
{
    public interface IProfileService
    {
        /// <summary>
        /// GameObject에서 MaterialMergeProfile 컴포넌트 가져오기/생성
        /// </summary>
        MaterialMergeProfile EnsureProfile(GameObject target, bool createIfMissing);

        /// <summary>
        /// 프로필에서 스캔 결과 로드
        /// </summary>
        List<GroupScan> LoadScansFromProfile(
            MaterialMergeProfile profile,
            GameObject root,
            int grid,
            IMaterialScanService scanService
        );

        /// <summary>
        /// 프로필의 저장된 설정을 스캔 결과에 적용
        /// </summary>
        void ApplyProfileToScans(
            MaterialMergeProfile profile,
            List<GroupScan> scans
        );

        /// <summary>
        /// 스캔 결과를 프로필에 저장
        /// </summary>
        void SaveScansToProfile(
            MaterialMergeProfile profile,
            List<GroupScan> scans
        );

        /// <summary>
        /// 셰이더에서 프로퍼티 정보 가져오기
        /// </summary>
        bool TryGetShaderPropertyInfo(
            Shader shader,
            string propertyName,
            out ShaderUtil.ShaderPropertyType type,
            out int index
        );

        /// <summary>
        /// 루트 GameObject에서 GroupData에 매칭되는 셰이더 찾기
        /// </summary>
        Shader TryResolveShaderFromRoot(
            GameObject root,
            MaterialMergeProfile.GroupData groupData,
            IMaterialScanService scanService,
            bool groupByKeywords,
            bool groupByRenderQueue,
            bool splitOpaqueTransparent
        );
    }
}
#endif
