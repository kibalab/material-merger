#if UNITY_EDITOR
namespace K13A.MaterialMerger.Editor.Core
{
    /// <summary>
    /// Material Merger에서 사용하는 상수 정의
    /// 매직 넘버 제거 및 유지보수성 향상
    /// </summary>
    public static class Constants
    {
        #region Atlas Settings

        /// <summary>기본 아틀라스 크기 (픽셀)</summary>
        public const int DefaultAtlasSize = 8192;

        /// <summary>기본 그리드 크기</summary>
        public const int DefaultGrid = 4;

        /// <summary>기본 패딩 (픽셀)</summary>
        public const int DefaultPaddingPx = 16;

        /// <summary>지원되는 아틀라스 크기 목록</summary>
        public static readonly int[] SupportedAtlasSizes = { 256, 512, 1024, 2048, 4096, 8192 };

        /// <summary>지원되는 그리드 크기 목록</summary>
        public static readonly int[] SupportedGridSizes = { 2, 3, 4, 5, 6, 8 };

        #endregion

        #region Scan Settings

        /// <summary>투명 렌더 큐 임계값</summary>
        public const int TransparentRenderQueueThreshold = 3000;

        /// <summary>고유 값 수집 최대 개수 (메모리 절약)</summary>
        public const int MaxDistinctValuesToCollect = 64;

        /// <summary>멀티 머티리얼 감지 임계값</summary>
        public const int MultiMaterialHitThreshold = 999;

        #endregion

        #region UI Settings

        /// <summary>행 헤더 높이 (픽셀)</summary>
        public const float RowHeaderHeight = 24f;

        /// <summary>머지 인덴트</summary>
        public const float MergeIndent = 16f;

        /// <summary>드래그 조정 최대 반복 횟수 (무한 루프 방지)</summary>
        public const int MaxDragAdjustIterations = 5;

        /// <summary>검색 필드 너비</summary>
        public const float SearchFieldWidth = 260f;

        /// <summary>툴바 버튼 최소 너비</summary>
        public const float ToolbarButtonMinWidth = 90f;

        #endregion

        #region File Paths

        /// <summary>기본 출력 폴더</summary>
        public const string DefaultOutputFolder = "Assets/_Generated/MultiAtlas";

        /// <summary>메시 서브폴더</summary>
        public const string MeshesSubfolder = "_Meshes";

        /// <summary>Blit 셰이더 파일명</summary>
        public const string BlitShaderFileName = "Hidden_KibaAtlasBlit.shader";

        /// <summary>로그 파일 접두사</summary>
        public const string LogFilePrefix = "MultiAtlasLog_";

        /// <summary>파일명 최대 길이</summary>
        public const int MaxFileNameLength = 90;

        #endregion

        #region Serialization

        /// <summary>Float 비교 정밀도</summary>
        public const string FloatPrecisionFormat = "F5";

        /// <summary>상세 Float 비교 정밀도</summary>
        public const string FloatDetailedFormat = "F6";

        #endregion

        #region Mesh

        /// <summary>16비트 인덱스 최대 정점 수</summary>
        public const int MaxVerticesFor16BitIndex = 65535;

        /// <summary>리매핑된 메시 접미사</summary>
        public const string RemappedMeshSuffix = "_AtlasMulti";

        #endregion
    }
}
#endif
