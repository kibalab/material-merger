#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Logging;

namespace K13A.MaterialMerger.Editor.Services
{
    public interface IMaterialScanService
    {
        ILoggingService LoggingService { get; set; }
        /// <summary>
        /// GameObject 계층에서 모든 Renderer 수집
        /// </summary>
        List<Renderer> CollectRenderers(GameObject root);

        /// <summary>
        /// 머티리얼이 투명인지 판단
        /// </summary>
        bool IsTransparent(Material material);

        /// <summary>
        /// 머티리얼에서 GroupKey 생성
        /// </summary>
        GroupKey CreateGroupKey(Material material, bool groupByKeywords, bool groupByRenderQueue, bool splitOpaqueTransparent);

        /// <summary>
        /// GameObject를 스캔하여 GroupScan 목록 생성
        /// </summary>
        List<GroupScan> ScanGameObject(GameObject root, bool groupByKeywords, bool groupByRenderQueue, bool splitOpaqueTransparent, int grid);

        /// <summary>
        /// 셰이더에서 특정 타입의 프로퍼티 이름 목록 가져오기
        /// </summary>
        List<string> GetShaderPropertiesByType(Shader shader, ShaderUtil.ShaderPropertyType type);

        /// <summary>
        /// 그룹의 multi-material 스킵 개수 추정
        /// </summary>
        int EstimateMultiMaterialSkips(GroupScan group);

        /// <summary>
        /// 그룹의 셰이더 프로퍼티별 Row 생성
        /// </summary>
        List<Row> BuildPropertyRows(GroupScan group);
    }
}
#endif
