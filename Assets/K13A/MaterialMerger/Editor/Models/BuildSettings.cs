#if UNITY_EDITOR
using UnityEngine;

namespace K13A.MaterialMerger.Editor.Models
{
    /// <summary>
    /// 빌드 설정을 그룹화한 불변 설정 객체
    /// ISP(인터페이스 분리 원칙) 준수를 위해 매개변수 그룹화
    /// </summary>
    public class BuildSettings
    {
        // 루트 및 출력 설정
        public GameObject Root { get; }
        public string OutputFolder { get; }

        // 클론 설정
        public bool CloneRootOnApply { get; }
        public bool DeactivateOriginalRoot { get; }
        public bool KeepPrefabOnClone { get; }

        // 아틀라스 설정
        public int AtlasSize { get; }
        public int Grid { get; }
        public int PaddingPx { get; }

        // 그룹핑 설정
        public bool GroupByKeywords { get; }
        public bool GroupByRenderQueue { get; }
        public bool SplitOpaqueTransparent { get; }

        // 머지 정책
        public DiffPolicy DiffPolicy { get; }
        public Material SampleMaterial { get; }

        // 계산된 값
        public int CellSize => AtlasSize / Grid;
        public int ContentSize => Mathf.Max(1, CellSize - PaddingPx * 2);
        public int TilesPerPage => Grid * Grid;

        public BuildSettings(
            GameObject root,
            string outputFolder,
            bool cloneRootOnApply,
            bool deactivateOriginalRoot,
            bool keepPrefabOnClone,
            int atlasSize,
            int grid,
            int paddingPx,
            bool groupByKeywords,
            bool groupByRenderQueue,
            bool splitOpaqueTransparent,
            DiffPolicy diffPolicy,
            Material sampleMaterial)
        {
            Root = root;
            OutputFolder = outputFolder;
            CloneRootOnApply = cloneRootOnApply;
            DeactivateOriginalRoot = deactivateOriginalRoot;
            KeepPrefabOnClone = keepPrefabOnClone;
            AtlasSize = atlasSize;
            Grid = grid;
            PaddingPx = paddingPx > 0 && (atlasSize / grid - paddingPx * 2) > 0 ? paddingPx : 0;
            GroupByKeywords = groupByKeywords;
            GroupByRenderQueue = groupByRenderQueue;
            SplitOpaqueTransparent = splitOpaqueTransparent;
            DiffPolicy = diffPolicy;
            SampleMaterial = sampleMaterial;
        }

        /// <summary>
        /// MaterialMergerState로부터 BuildSettings 생성
        /// </summary>
        public static BuildSettings FromState(Core.MaterialMergerState state)
        {
            return new BuildSettings(
                state.root,
                state.outputFolder,
                state.cloneRootOnApply,
                state.deactivateOriginalRoot,
                state.keepPrefabOnClone,
                state.atlasSize,
                state.grid,
                state.paddingPx,
                state.groupByKeywords,
                state.groupByRenderQueue,
                state.splitOpaqueTransparent,
                state.diffPolicy,
                state.diffSampleMaterial
            );
        }
    }
}
#endif
