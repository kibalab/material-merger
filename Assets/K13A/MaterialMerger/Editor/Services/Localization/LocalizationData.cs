#if UNITY_EDITOR
using System.Collections.Generic;

namespace K13A.MaterialMerger.Editor.Services.Localization
{
    /// <summary>
    /// 모든 언어의 번역 데이터
    /// </summary>
    public static class LocalizationData
    {
        public static Dictionary<Language, Dictionary<string, string>> GetAllTranslations()
        {
            return new Dictionary<Language, Dictionary<string, string>>
            {
                { Language.Korean, GetKoreanTranslations() },
                { Language.English, GetEnglishTranslations() },
                { Language.Japanese, GetJapaneseTranslations() }
            };
        }

        private static Dictionary<string, string> GetKoreanTranslations()
        {
            return new Dictionary<string, string>
            {
                // Window
                [L10nKey.WindowTitle] = "멀티 아틀라스 머저",

                // Top Panel
                [L10nKey.Scan] = "스캔",
                [L10nKey.BuildAndApply] = "빌드 & 적용",
                [L10nKey.Root] = "루트",
                [L10nKey.LastScan] = "마지막 스캔",
                [L10nKey.OutputFolder] = "출력 폴더",
                [L10nKey.NoScan] = "(없음)",
                [L10nKey.ScanTooltip] = "선택한 루트를 스캔해 머티리얼 플랜을 만듭니다.",
                [L10nKey.BuildAndApplyTooltip] = "아틀라스를 생성하고 머지 머티리얼을 메시에 적용합니다.",
                [L10nKey.RootTooltip] = "스캔 및 적용 대상 루트 GameObject.",
                [L10nKey.LastScanTooltip] = "프로필에 저장된 마지막 스캔 시간.",
                [L10nKey.OutputFolderTooltip] = "Assets 아래에 생성 파일을 저장할 폴더를 선택합니다.",
                [L10nKey.LanguageTooltip] = "UI 언어를 변경합니다.",

                // Global Settings
                [L10nKey.MaterialGroupingRules] = "머테리얼 분리 규칙",
                [L10nKey.GroupByKeywords] = "키워드로 분리",
                [L10nKey.GroupByKeywordsTooltip] = "셰이더 키워드가 다르면 플랜을 분리합니다.",
                [L10nKey.GroupByRenderQueue] = "RenderQueue로 분리",
                [L10nKey.GroupByRenderQueueTooltip] = "RenderQueue 값이 다르면 플랜을 분리합니다.",
                [L10nKey.SplitOpaqueTransparent] = "불투명/투명 분리",
                [L10nKey.SplitOpaqueTransparentTooltip] = "불투명/투명 머티리얼을 분리합니다.",
                [L10nKey.GroupingShader] = "셰이더",
                [L10nKey.GroupingSummary] = "현재 그룹 기준: {0}.",

                [L10nKey.ApplicationMethod] = "적용 방식",
                [L10nKey.CloneRootOnApply] = "적용 시 루트 복제",
                [L10nKey.CloneRootOnApplyTooltip] = "적용 시 루트를 복제해 원본을 보존합니다.",
                [L10nKey.DeactivateOriginalRoot] = "원본 루트 비활성화",
                [L10nKey.DeactivateOriginalRootTooltip] = "복제 후 원본 루트를 비활성화합니다.",
                [L10nKey.ApplySummaryCloneDeactivate] = "복제본에 적용하고 원본을 비활성화합니다.",
                [L10nKey.ApplySummaryCloneKeep] = "복제본에 적용하고 원본을 유지합니다.",
                [L10nKey.ApplySummaryDirect] = "선택한 루트에 바로 적용합니다.",

                [L10nKey.Atlas] = "아틀라스",
                [L10nKey.AtlasSize] = "크기",
                [L10nKey.AtlasSizeTooltip] = "페이지별 최종 아틀라스 크기.",
                [L10nKey.Grid] = "그리드",
                [L10nKey.GridTooltip] = "행/열 타일 수 (grid x grid = 페이지당 타일).",
                [L10nKey.Padding] = "패딩(px)",
                [L10nKey.PaddingTooltip] = "타일 간 번짐 방지를 위한 패딩.",
                [L10nKey.AtlasSummary] = "페이지당 타일: {0}. 타일 크기: {1}px, 내용: {2}px.",

                [L10nKey.Policy] = "정책",
                [L10nKey.UnresolvedDiffPolicy] = "미해결 diff 처리",
                [L10nKey.UnresolvedDiffPolicyTooltip] = "해결되지 않은 스칼라/색상 차이 처리 방식.",
                [L10nKey.SampleMaterial] = "샘플 머티리얼",
                [L10nKey.SampleMaterialTooltip] = "미해결 diff 처리에 사용할 샘플 머티리얼.",
                [L10nKey.PolicySummaryStop] = "해결되지 않은 차이가 있으면 해당 플랜을 건너뜁니다.",
                [L10nKey.PolicySummaryProceed] = "해결되지 않은 차이는 첫 번째 머티리얼 값을 사용합니다.",
                [L10nKey.PolicySummarySample] = "미해결 차이는 샘플 머티리얼 값을 사용합니다.",
                [L10nKey.PolicySummarySampleMissing] = "샘플 머티리얼이 없어 첫 번째 머티리얼 값을 사용합니다.",

                // Statistics
                [L10nKey.Statistics] = "통계",
                [L10nKey.TotalMaterials] = "총 머티리얼",
                [L10nKey.UniqueMaterials] = "고유 머티리얼",
                [L10nKey.MergableGroups] = "병합 가능 그룹",
                [L10nKey.BeforeMerge] = "병합 전",
                [L10nKey.AfterMerge] = "병합 후",
                [L10nKey.TotalRenderers] = "총 렌더러",

                // Group List
                [L10nKey.PlanList] = "계획 목록",
                [L10nKey.ExpandAll] = "전체 펼치기",
                [L10nKey.ExpandAllTooltip] = "모든 플랜을 펼칩니다.",
                [L10nKey.CollapseAll] = "전체 접기",
                [L10nKey.CollapseAllTooltip] = "모든 플랜을 접습니다.",
                [L10nKey.EnableAll] = "전체 활성",
                [L10nKey.EnableAllTooltip] = "모든 플랜을 빌드 대상으로 활성화합니다.",
                [L10nKey.DisableAll] = "전체 비활성",
                [L10nKey.DisableAllTooltip] = "모든 플랜을 비활성화합니다.",
                [L10nKey.MergeSelected] = "선택 플랜 병합",
                [L10nKey.MergeSelectedTooltip] = "선택한 플랜들을 하나로 묶어 단일 플랜으로 빌드합니다.",
                [L10nKey.ClearMerge] = "플랜 병합 해제",
                [L10nKey.ClearMergeTooltip] = "병합된 플랜을 다시 각각의 플랜으로 분리합니다.",
                [L10nKey.NoScanMessage] = "루트를 지정한 뒤 스캔을 실행하세요.",

                // Group Panel
                [L10nKey.Material] = "머티리얼",
                [L10nKey.Page] = "페이지",
                [L10nKey.PageTooltip] = "이 플랜에서 생성될 아틀라스 페이지 수.",
                [L10nKey.Skip] = "MultiMat {0}",
                [L10nKey.MultiMatTooltip] = "이 플랜의 머티리얼을 여러 개 사용하는 렌더러 수.",
                [L10nKey.OutputMaterialName] = "Output Name",
                [L10nKey.OutputMaterialNameTooltip] = "이 플랜에서 생성될 머티리얼 이름.",
                [L10nKey.PlanMaterials] = "머티리얼 목록",
                [L10nKey.PlanMaterialsTooltip] = "이 플랜에 포함된 머티리얼을 그리드로 보기",
                [L10nKey.PlanMaterialsTitle] = "머티리얼 목록: {0}",
                [L10nKey.PlanMaterialsEmpty] = "표시할 머티리얼이 없습니다.",
                [L10nKey.PlanDragHandleTooltip] = "드래그하여 순서 변경. 다른 플랜 위에 놓으면 플랜 병합, 그룹 밖에 놓으면 병합 해제됩니다.",
                [L10nKey.MergeSelect] = "플랜 병합",
                [L10nKey.MergeSelectTooltip] = "이 플랜을 플랜 병합 대상으로 선택합니다.",
                [L10nKey.MergedTag] = "플랜 병합 {0}",
                [L10nKey.MergedTagTooltip] = "이 플랜은 {0}개의 플랜을 하나로 묶은 병합 플랜입니다.",
                [L10nKey.SingleMaterial] = "단일(병합불필요)",
                [L10nKey.SingleMaterialTooltip] = "이 플랜은 머티리얼이 1개라 병합이 필요 없습니다.",
                [L10nKey.RelevantOnly] = "관련만",
                [L10nKey.FilterRelevantTooltip] = "차이가 있거나 활성화된 프로퍼티만 표시.",
                [L10nKey.TexturesOnly] = "텍스처만",
                [L10nKey.FilterTexturesTooltip] = "텍스처(TexEnv) 프로퍼티만 표시.",
                [L10nKey.ScalarsOnly] = "스칼라만",
                [L10nKey.FilterScalarsTooltip] = "스칼라/색상/벡터 프로퍼티만 표시.",
                [L10nKey.EnableAllTextureAtlas] = "텍스처 아틀라스 전체 켜기",
                [L10nKey.DisableAllTextureAtlas] = "텍스처 아틀라스 전체 끄기",

                // Property Table
                [L10nKey.Property] = "프로퍼티",
                [L10nKey.Type] = "타입",
                [L10nKey.Action] = "액션",
                [L10nKey.Target] = "대상",
                [L10nKey.Info] = "정보",
                [L10nKey.NoPropertiesMatch] = "필터 조건에 맞는 프로퍼티가 없습니다.",

                // Property Row
                [L10nKey.Texture] = "텍스처",
                [L10nKey.Color] = "색상",
                [L10nKey.Float] = "Float",
                [L10nKey.Range] = "Range",
                [L10nKey.Vector] = "Vector",

                [L10nKey.TextureAtlas] = "텍스처 아틀라스",
                [L10nKey.NormalMap] = "노말맵",
                [L10nKey.SRGB] = "sRGB",
                [L10nKey.Linear] = "Linear",
                [L10nKey.Empty] = "비어있음",
                [L10nKey.NotApplied] = "미적용",
                [L10nKey.Same] = "동일",
                [L10nKey.ShowMore] = "더보기",
                [L10nKey.Collapse] = "접기",
                [L10nKey.NoTexEnv] = "(TexEnv 없음)",

                // Property Row Expanded
                [L10nKey.TextureAtlasing] = "텍스처 아틀라싱",
                [L10nKey.EnableCheckboxToInclude] = "체크박스를 켜면 이 프로퍼티를 아틀라스에 포함합니다.",
                [L10nKey.TextureWillBeIncluded] = "이 텍스처 프로퍼티를 아틀라스에 포함합니다.",
                [L10nKey.ColorSpaceAutoDetected] = "노말/마스크 계열은 색공간이 자동 추정됩니다.",

                [L10nKey.EnableCheckboxToApply] = "체크박스를 켜면 이 프로퍼티에 액션을 적용합니다.",
                [L10nKey.ResetAfterBake] = "굽기/곱 적용 후 원본 프로퍼티 리셋",
                [L10nKey.IncludeAlpha] = "알파 포함(기본 꺼짐)",
                [L10nKey.ModifierOptional] = "모디파이어(옵션): 다른 float 프로퍼티로 곱/가산/감산",
                [L10nKey.Clamp01] = "Clamp01",
                [L10nKey.ApplyToAlpha] = "알파에도 적용",
                [L10nKey.BakeOptionsWhenSelected] = "굽기/곱 액션 선택 시 추가 옵션이 활성화됩니다.",
                [L10nKey.None] = "(없음)",

                // Bake Modes
                [L10nKey.BakeModeReset] = "리셋(쉐이더 기본값)",
                [L10nKey.BakeModeColorBake] = "색상 굽기 → 텍스처",
                [L10nKey.BakeModeScalarBake] = "스칼라 굽기 → 그레이",
                [L10nKey.BakeModeColorMultiply] = "색상 곱 → 텍스처",
                [L10nKey.BakeModeKeep] = "유지",

                // Diff Policy
                [L10nKey.DiffPolicyStopIfUnresolved] = "미해결이면중단",
                [L10nKey.DiffPolicyProceedWithFirst] = "첫번째기준으로진행",

                // Confirm Window
                [L10nKey.ConfirmTitle] = "빌드 확인",
                [L10nKey.ConfirmHeader] = "아틀라스 빌드 & 적용 실행 전 확인",
                [L10nKey.ConfirmMessage] = "실행 대상 Material Plan: {0} / 스킵: {1}\n아래 프로퍼티 목록이 실제로 텍스처 아틀라싱/생성 대상입니다.",
                [L10nKey.Run] = "실행",
                [L10nKey.Skipped] = "스킵",
                [L10nKey.UnresolvedDiffReason] = "미해결 diff가 있는데 정책이 '미해결이면중단'이라 이 Material Plan은 스킵됩니다.",
                [L10nKey.AtlasIncludedTexEnv] = "아틀라싱 포함 TexEnv ({0})",
                [L10nKey.GeneratedTexEnv] = "텍스처 생성/타겟 TexEnv ({0})",
                [L10nKey.Cancel] = "취소",
                [L10nKey.Execute] = "실행",

                // Dialogs
                [L10nKey.DialogNoScan] = "스캔 결과가 없습니다.",
                [L10nKey.DialogNoPlan] = "활성화된 Material Plan이 없습니다.",
                [L10nKey.DialogBlitFailed] = "Blit 머티리얼 생성 실패",
                [L10nKey.DialogRootRequired] = "루트가 필요합니다(적용 시 루트 복제 옵션).",
                [L10nKey.DialogComplete] = "완료",
                [L10nKey.DialogLog] = "로그: {0}",
                [L10nKey.DialogOutputFolderTitle] = "출력 폴더 선택",
                [L10nKey.DialogOutputFolderError] = "Assets 폴더 내부만 가능합니다.",
                [L10nKey.DialogServiceNotInitialized] = "서비스가 초기화되지 않았습니다.",

                // Rollback
                [L10nKey.RollbackMenuItem] = "멀티 아틀라스 롤백...",
                [L10nKey.RollbackTitle] = "롤백",
                [L10nKey.RollbackComplete] = "로그 기반 롤백 완료",
                [L10nKey.RollbackOwnerClosed] = "원본 창이 닫혀서 실행할 수 없습니다.",
                [L10nKey.Close] = "닫기",

                // Language
                [L10nKey.LanguageSettings] = "언어 설정",
                [L10nKey.LanguageKorean] = "한국어",
                [L10nKey.LanguageEnglish] = "English",
                [L10nKey.LanguageJapanese] = "日本語"
            };
        }

        private static Dictionary<string, string> GetEnglishTranslations()
        {
            return new Dictionary<string, string>
            {
                // Window
                [L10nKey.WindowTitle] = "Multi Atlas Merger",

                // Top Panel
                [L10nKey.Scan] = "Scan",
                [L10nKey.BuildAndApply] = "Build & Apply",
                [L10nKey.Root] = "Root",
                [L10nKey.LastScan] = "Last Scan",
                [L10nKey.OutputFolder] = "Output Folder",
                [L10nKey.NoScan] = "(None)",
                [L10nKey.ScanTooltip] = "Scan selected root to create material plans.",
                [L10nKey.BuildAndApplyTooltip] = "Build atlases and apply merged materials to meshes.",
                [L10nKey.RootTooltip] = "Root GameObject for scan and apply.",
                [L10nKey.LastScanTooltip] = "Last scan time saved in profile.",
                [L10nKey.OutputFolderTooltip] = "Select folder under Assets to save generated files.",
                [L10nKey.LanguageTooltip] = "Change UI language.",

                // Global Settings
                [L10nKey.MaterialGroupingRules] = "Material Grouping Rules",
                [L10nKey.GroupByKeywords] = "Group by Keywords",
                [L10nKey.GroupByKeywordsTooltip] = "Split plans when shader keywords differ.",
                [L10nKey.GroupByRenderQueue] = "Group by RenderQueue",
                [L10nKey.GroupByRenderQueueTooltip] = "Split plans when RenderQueue differs.",
                [L10nKey.SplitOpaqueTransparent] = "Split Opaque/Transparent",
                [L10nKey.SplitOpaqueTransparentTooltip] = "Split opaque and transparent materials.",
                [L10nKey.GroupingShader] = "Shader",
                [L10nKey.GroupingSummary] = "Current grouping: {0}.",

                [L10nKey.ApplicationMethod] = "Application Method",
                [L10nKey.CloneRootOnApply] = "Clone Root on Apply",
                [L10nKey.CloneRootOnApplyTooltip] = "Clone the root on apply to keep the original.",
                [L10nKey.DeactivateOriginalRoot] = "Deactivate Original Root",
                [L10nKey.DeactivateOriginalRootTooltip] = "Deactivate original root after cloning.",
                [L10nKey.ApplySummaryCloneDeactivate] = "Apply to clone and deactivate the original.",
                [L10nKey.ApplySummaryCloneKeep] = "Apply to clone and keep the original.",
                [L10nKey.ApplySummaryDirect] = "Apply directly to the selected root.",

                [L10nKey.Atlas] = "Atlas",
                [L10nKey.AtlasSize] = "Size",
                [L10nKey.AtlasSizeTooltip] = "Final atlas size per page.",
                [L10nKey.Grid] = "Grid",
                [L10nKey.GridTooltip] = "Tiles per row/column (grid x grid = tiles per page).",
                [L10nKey.Padding] = "Padding(px)",
                [L10nKey.PaddingTooltip] = "Padding between tiles to prevent bleeding.",
                [L10nKey.AtlasSummary] = "Tiles per page: {0}. Tile size: {1}px, content: {2}px.",

                [L10nKey.Policy] = "Policy",
                [L10nKey.UnresolvedDiffPolicy] = "Unresolved Diff Policy",
                [L10nKey.UnresolvedDiffPolicyTooltip] = "How to handle unresolved scalar/color differences.",
                [L10nKey.SampleMaterial] = "Sample Material",
                [L10nKey.SampleMaterialTooltip] = "Sample material used to resolve unresolved diffs.",
                [L10nKey.PolicySummaryStop] = "Skip this plan if there are unresolved differences.",
                [L10nKey.PolicySummaryProceed] = "Use first material values for unresolved differences.",
                [L10nKey.PolicySummarySample] = "Unresolved differences use sample material values.",
                [L10nKey.PolicySummarySampleMissing] = "No sample material set; using first material values.",

                // Statistics
                [L10nKey.Statistics] = "Statistics",
                [L10nKey.TotalMaterials] = "Total Materials",
                [L10nKey.UniqueMaterials] = "Unique Materials",
                [L10nKey.MergableGroups] = "Mergable Groups",
                [L10nKey.BeforeMerge] = "Before Merge",
                [L10nKey.AfterMerge] = "After Merge",
                [L10nKey.TotalRenderers] = "Total Renderers",

                // Group List
                [L10nKey.PlanList] = "Plan List",
                [L10nKey.ExpandAll] = "Expand All",
                [L10nKey.ExpandAllTooltip] = "Expand all plans.",
                [L10nKey.CollapseAll] = "Collapse All",
                [L10nKey.CollapseAllTooltip] = "Collapse all plans.",
                [L10nKey.EnableAll] = "Enable All",
                [L10nKey.EnableAllTooltip] = "Enable all plans for build.",
                [L10nKey.DisableAll] = "Disable All",
                [L10nKey.DisableAllTooltip] = "Disable all plans.",
                [L10nKey.MergeSelected] = "Merge Plans",
                [L10nKey.MergeSelectedTooltip] = "Combine selected plans into one plan for a single merged output.",
                [L10nKey.ClearMerge] = "Unmerge Plans",
                [L10nKey.ClearMergeTooltip] = "Split merged plans back into separate plans.",
                [L10nKey.NoScanMessage] = "Please specify a root and run scan.",

                // Group Panel
                [L10nKey.Material] = "Material",
                [L10nKey.Page] = "Page",
                [L10nKey.PageTooltip] = "Number of atlas pages generated for this plan.",
                [L10nKey.Skip] = "MultiMat {0}",
                [L10nKey.MultiMatTooltip] = "Renderers using multiple materials in this plan.",
                [L10nKey.OutputMaterialName] = "Output Name",
                [L10nKey.OutputMaterialNameTooltip] = "Name of the material generated for this plan.",
                [L10nKey.PlanMaterials] = "Materials",
                [L10nKey.PlanMaterialsTooltip] = "Show materials in this plan as a grid.",
                [L10nKey.PlanMaterialsTitle] = "Materials: {0}",
                [L10nKey.PlanMaterialsEmpty] = "No materials to display.",
                [L10nKey.PlanDragHandleTooltip] = "Drag to reorder. Drop onto another plan to merge plans. Drop outside the group to unmerge.",
                [L10nKey.MergeSelect] = "Plan Merge",
                [L10nKey.MergeSelectTooltip] = "Include this plan in a plan merge.",
                [L10nKey.MergedTag] = "Merged Plans {0}",
                [L10nKey.MergedTagTooltip] = "This plan is a merge of {0} plans into one.",
                [L10nKey.SingleMaterial] = "Single(No merge needed)",
                [L10nKey.SingleMaterialTooltip] = "This plan has one material and does not need merging.",
                [L10nKey.RelevantOnly] = "Relevant",
                [L10nKey.FilterRelevantTooltip] = "Show only differing or enabled properties.",
                [L10nKey.TexturesOnly] = "Textures",
                [L10nKey.FilterTexturesTooltip] = "Show only texture (TexEnv) properties.",
                [L10nKey.ScalarsOnly] = "Scalars",
                [L10nKey.FilterScalarsTooltip] = "Show only scalar/color/vector properties.",
                [L10nKey.EnableAllTextureAtlas] = "Enable All Texture Atlas",
                [L10nKey.DisableAllTextureAtlas] = "Disable All Texture Atlas",

                // Property Table
                [L10nKey.Property] = "Property",
                [L10nKey.Type] = "Type",
                [L10nKey.Action] = "Action",
                [L10nKey.Target] = "Target",
                [L10nKey.Info] = "Info",
                [L10nKey.NoPropertiesMatch] = "No properties match the filter criteria.",

                // Property Row
                [L10nKey.Texture] = "Texture",
                [L10nKey.Color] = "Color",
                [L10nKey.Float] = "Float",
                [L10nKey.Range] = "Range",
                [L10nKey.Vector] = "Vector",

                [L10nKey.TextureAtlas] = "Texture Atlas",
                [L10nKey.NormalMap] = "Normal Map",
                [L10nKey.SRGB] = "sRGB",
                [L10nKey.Linear] = "Linear",
                [L10nKey.Empty] = "Empty",
                [L10nKey.NotApplied] = "Not Applied",
                [L10nKey.Same] = "Same",
                [L10nKey.ShowMore] = "More",
                [L10nKey.Collapse] = "Less",
                [L10nKey.NoTexEnv] = "(No TexEnv)",

                // Property Row Expanded
                [L10nKey.TextureAtlasing] = "Texture Atlasing",
                [L10nKey.EnableCheckboxToInclude] = "Enable checkbox to include this property in atlas.",
                [L10nKey.TextureWillBeIncluded] = "This texture property will be included in atlas.",
                [L10nKey.ColorSpaceAutoDetected] = "Color space auto-detected for normal/mask textures.",

                [L10nKey.EnableCheckboxToApply] = "Enable checkbox to apply action to this property.",
                [L10nKey.ResetAfterBake] = "Reset original property after bake/multiply",
                [L10nKey.IncludeAlpha] = "Include Alpha (default off)",
                [L10nKey.ModifierOptional] = "Modifier (optional): Multiply/Add/Subtract with another float property",
                [L10nKey.Clamp01] = "Clamp01",
                [L10nKey.ApplyToAlpha] = "Apply to Alpha",
                [L10nKey.BakeOptionsWhenSelected] = "Additional options activated when bake/multiply action is selected.",
                [L10nKey.None] = "(None)",

                // Bake Modes
                [L10nKey.BakeModeReset] = "Reset (Shader Default)",
                [L10nKey.BakeModeColorBake] = "Bake Color → Texture",
                [L10nKey.BakeModeScalarBake] = "Bake Scalar → Gray",
                [L10nKey.BakeModeColorMultiply] = "Multiply Color → Texture",
                [L10nKey.BakeModeKeep] = "Keep",

                // Diff Policy
                [L10nKey.DiffPolicyStopIfUnresolved] = "Stop if Unresolved",
                [L10nKey.DiffPolicyProceedWithFirst] = "Proceed with First",

                // Confirm Window
                [L10nKey.ConfirmTitle] = "Build Confirmation",
                [L10nKey.ConfirmHeader] = "Confirm Before Atlas Build & Apply",
                [L10nKey.ConfirmMessage] = "Material Plans to Run: {0} / Skip: {1}\nThe property lists below are the actual texture atlasing/generation targets.",
                [L10nKey.Run] = "Run",
                [L10nKey.Skipped] = "Skip",
                [L10nKey.UnresolvedDiffReason] = "This Material Plan will be skipped because it has unresolved diffs and policy is 'Stop if Unresolved'.",
                [L10nKey.AtlasIncludedTexEnv] = "Atlas Included TexEnv ({0})",
                [L10nKey.GeneratedTexEnv] = "Generated/Target TexEnv ({0})",
                [L10nKey.Cancel] = "Cancel",
                [L10nKey.Execute] = "Execute",

                // Dialogs
                [L10nKey.DialogNoScan] = "No scan results.",
                [L10nKey.DialogNoPlan] = "No enabled Material Plans.",
                [L10nKey.DialogBlitFailed] = "Failed to create Blit material",
                [L10nKey.DialogRootRequired] = "Root is required (Clone Root on Apply option).",
                [L10nKey.DialogComplete] = "Complete",
                [L10nKey.DialogLog] = "Log: {0}",
                [L10nKey.DialogOutputFolderTitle] = "Select Output Folder",
                [L10nKey.DialogOutputFolderError] = "Only folders inside Assets are allowed.",
                [L10nKey.DialogServiceNotInitialized] = "Services not initialized.",

                // Rollback
                [L10nKey.RollbackMenuItem] = "Multi Atlas Rollback...",
                [L10nKey.RollbackTitle] = "Rollback",
                [L10nKey.RollbackComplete] = "Log-based rollback complete",
                [L10nKey.RollbackOwnerClosed] = "Cannot execute because owner window is closed.",
                [L10nKey.Close] = "Close",

                // Language
                [L10nKey.LanguageSettings] = "Language",
                [L10nKey.LanguageKorean] = "한국어",
                [L10nKey.LanguageEnglish] = "English",
                [L10nKey.LanguageJapanese] = "日本語"
            };
        }

        private static Dictionary<string, string> GetJapaneseTranslations()
        {
            return new Dictionary<string, string>
            {
                // Window
                [L10nKey.WindowTitle] = "マルチアトラスマージャー",

                // Top Panel
                [L10nKey.Scan] = "スキャン",
                [L10nKey.BuildAndApply] = "ビルド＆適用",
                [L10nKey.Root] = "ルート",
                [L10nKey.LastScan] = "最終スキャン",
                [L10nKey.OutputFolder] = "出力フォルダ",
                [L10nKey.NoScan] = "(なし)",
                [L10nKey.ScanTooltip] = "選択したルートをスキャンしてマテリアルプランを作成します。",
                [L10nKey.BuildAndApplyTooltip] = "アトラスを生成し、マージされたマテリアルをメッシュに適用します。",
                [L10nKey.RootTooltip] = "スキャン/適用の対象となるルート GameObject。",
                [L10nKey.LastScanTooltip] = "プロファイルに保存された最後のスキャン時刻。",
                [L10nKey.OutputFolderTooltip] = "Assets 配下の出力先フォルダを選択します。",
                [L10nKey.LanguageTooltip] = "UI 言語を変更します。",

                // Global Settings
                [L10nKey.MaterialGroupingRules] = "マテリアルグループ化ルール",
                [L10nKey.GroupByKeywords] = "キーワードでグループ化",
                [L10nKey.GroupByKeywordsTooltip] = "シェーダーキーワードが異なる場合はプランを分割します。",
                [L10nKey.GroupByRenderQueue] = "RenderQueueでグループ化",
                [L10nKey.GroupByRenderQueueTooltip] = "RenderQueue が異なる場合はプランを分割します。",
                [L10nKey.SplitOpaqueTransparent] = "不透明/透明を分離",
                [L10nKey.SplitOpaqueTransparentTooltip] = "不透明/透明マテリアルを分離します。",
                [L10nKey.GroupingShader] = "シェーダー",
                [L10nKey.GroupingSummary] = "現在のグループ基準: {0}。",

                [L10nKey.ApplicationMethod] = "適用方法",
                [L10nKey.CloneRootOnApply] = "適用時にルートを複製",
                [L10nKey.CloneRootOnApplyTooltip] = "適用時にルートを複製して元を保持します。",
                [L10nKey.DeactivateOriginalRoot] = "元のルートを無効化",
                [L10nKey.DeactivateOriginalRootTooltip] = "複製後に元のルートを無効化します。",
                [L10nKey.ApplySummaryCloneDeactivate] = "複製に適用し、元を無効化します。",
                [L10nKey.ApplySummaryCloneKeep] = "複製に適用し、元を保持します。",
                [L10nKey.ApplySummaryDirect] = "選択したルートに直接適用します。",

                [L10nKey.Atlas] = "アトラス",
                [L10nKey.AtlasSize] = "サイズ",
                [L10nKey.AtlasSizeTooltip] = "ページごとの最終アトラスサイズ。",
                [L10nKey.Grid] = "グリッド",
                [L10nKey.GridTooltip] = "行/列のタイル数（grid x grid = ページあたりのタイル）。",
                [L10nKey.Padding] = "パディング(px)",
                [L10nKey.PaddingTooltip] = "タイル間のにじみ防止パディング。",
                [L10nKey.AtlasSummary] = "ページあたりのタイル: {0}。タイルサイズ: {1}px、内容: {2}px。",

                [L10nKey.Policy] = "ポリシー",
                [L10nKey.UnresolvedDiffPolicy] = "未解決diff処理",
                [L10nKey.UnresolvedDiffPolicyTooltip] = "解決していないスカラー/カラー差分の処理。",
                [L10nKey.SampleMaterial] = "サンプルマテリアル",
                [L10nKey.SampleMaterialTooltip] = "未解決の差分に使用するサンプルマテリアル。",
                [L10nKey.PolicySummaryStop] = "未解決の差分があればそのプランをスキップします。",
                [L10nKey.PolicySummaryProceed] = "未解決の差分は最初のマテリアル値を使用します。",
                [L10nKey.PolicySummarySample] = "未解決の差分はサンプルマテリアルの値を使用します。",
                [L10nKey.PolicySummarySampleMissing] = "サンプルマテリアルがないため最初のマテリアル値を使用します。",

                // Statistics
                [L10nKey.Statistics] = "統計",
                [L10nKey.TotalMaterials] = "総マテリアル",
                [L10nKey.UniqueMaterials] = "固有マテリアル",
                [L10nKey.MergableGroups] = "マージ可能グループ",
                [L10nKey.BeforeMerge] = "マージ前",
                [L10nKey.AfterMerge] = "マージ後",
                [L10nKey.TotalRenderers] = "総レンダラー",

                // Group List
                [L10nKey.PlanList] = "プラン一覧",
                [L10nKey.ExpandAll] = "すべて展開",
                [L10nKey.ExpandAllTooltip] = "すべてのプランを展開します。",
                [L10nKey.CollapseAll] = "すべて折りたたむ",
                [L10nKey.CollapseAllTooltip] = "すべてのプランを折りたたみます。",
                [L10nKey.EnableAll] = "すべて有効",
                [L10nKey.EnableAllTooltip] = "すべてのプランをビルド対象にします。",
                [L10nKey.DisableAll] = "すべて無効",
                [L10nKey.DisableAllTooltip] = "すべてのプランを無効にします。",
                [L10nKey.MergeSelected] = "プラン結合",
                [L10nKey.MergeSelectedTooltip] = "選択したプランを1つにまとめ、単一の出力としてビルドします。",
                [L10nKey.ClearMerge] = "プラン結合解除",
                [L10nKey.ClearMergeTooltip] = "結合したプランを個別のプランに戻します。",
                [L10nKey.NoScanMessage] = "ルートを指定してスキャンを実行してください。",

                // Group Panel
                [L10nKey.Material] = "マテリアル",
                [L10nKey.Page] = "ページ",
                [L10nKey.PageTooltip] = "このプランで生成されるアトラスページ数。",
                [L10nKey.Skip] = "MultiMat {0}",
                [L10nKey.MultiMatTooltip] = "このプランで複数マテリアルを使うレンダラー数。",
                [L10nKey.OutputMaterialName] = "Output Name",
                [L10nKey.OutputMaterialNameTooltip] = "このプランで生成されるマテリアル名。",
                [L10nKey.PlanMaterials] = "マテリアル一覧",
                [L10nKey.PlanMaterialsTooltip] = "このプランのマテリアルをグリッドで表示",
                [L10nKey.PlanMaterialsTitle] = "マテリアル一覧: {0}",
                [L10nKey.PlanMaterialsEmpty] = "表示するマテリアルがありません。",
                [L10nKey.PlanDragHandleTooltip] = "ドラッグで並べ替え。別のプランにドロップするとプラン結合、グループ外で解除します。",
                [L10nKey.MergeSelect] = "プラン結合",
                [L10nKey.MergeSelectTooltip] = "このプランを結合対象として選択します。",
                [L10nKey.MergedTag] = "プラン結合 {0}",
                [L10nKey.MergedTagTooltip] = "このプランは {0} 件のプランを結合したものです。",
                [L10nKey.SingleMaterial] = "単一(マージ不要)",
                [L10nKey.SingleMaterialTooltip] = "このプランはマテリアルが1つのためマージ不要です。",
                [L10nKey.RelevantOnly] = "関連のみ",
                [L10nKey.FilterRelevantTooltip] = "差分または有効なプロパティのみ表示。",
                [L10nKey.TexturesOnly] = "テクスチャのみ",
                [L10nKey.FilterTexturesTooltip] = "テクスチャ（TexEnv）プロパティのみ表示。",
                [L10nKey.ScalarsOnly] = "スカラーのみ",
                [L10nKey.FilterScalarsTooltip] = "スカラー/カラー/ベクターのみ表示。",
                [L10nKey.EnableAllTextureAtlas] = "すべてのテクスチャアトラスを有効化",
                [L10nKey.DisableAllTextureAtlas] = "すべてのテクスチャアトラスを無効化",

                // Property Table
                [L10nKey.Property] = "プロパティ",
                [L10nKey.Type] = "タイプ",
                [L10nKey.Action] = "アクション",
                [L10nKey.Target] = "ターゲット",
                [L10nKey.Info] = "情報",
                [L10nKey.NoPropertiesMatch] = "フィルタ条件に一致するプロパティがありません。",

                // Property Row
                [L10nKey.Texture] = "テクスチャ",
                [L10nKey.Color] = "カラー",
                [L10nKey.Float] = "Float",
                [L10nKey.Range] = "Range",
                [L10nKey.Vector] = "Vector",

                [L10nKey.TextureAtlas] = "テクスチャアトラス",
                [L10nKey.NormalMap] = "ノーマルマップ",
                [L10nKey.SRGB] = "sRGB",
                [L10nKey.Linear] = "Linear",
                [L10nKey.Empty] = "空",
                [L10nKey.NotApplied] = "未適用",
                [L10nKey.Same] = "同一",
                [L10nKey.ShowMore] = "詳細",
                [L10nKey.Collapse] = "折りたたむ",
                [L10nKey.NoTexEnv] = "(TexEnvなし)",

                // Property Row Expanded
                [L10nKey.TextureAtlasing] = "テクスチャアトラス化",
                [L10nKey.EnableCheckboxToInclude] = "チェックボックスを有効にすると、このプロパティがアトラスに含まれます。",
                [L10nKey.TextureWillBeIncluded] = "このテクスチャプロパティはアトラスに含まれます。",
                [L10nKey.ColorSpaceAutoDetected] = "ノーマル/マスク系は色空間が自動検出されます。",

                [L10nKey.EnableCheckboxToApply] = "チェックボックスを有効にすると、このプロパティにアクションが適用されます。",
                [L10nKey.ResetAfterBake] = "ベイク/乗算適用後に元のプロパティをリセット",
                [L10nKey.IncludeAlpha] = "アルファを含める（デフォルトオフ）",
                [L10nKey.ModifierOptional] = "修飾子（オプション）：他のfloatプロパティで乗算/加算/減算",
                [L10nKey.Clamp01] = "Clamp01",
                [L10nKey.ApplyToAlpha] = "アルファにも適用",
                [L10nKey.BakeOptionsWhenSelected] = "ベイク/乗算アクション選択時に追加オプションが有効化されます。",
                [L10nKey.None] = "(なし)",

                // Bake Modes
                [L10nKey.BakeModeReset] = "リセット（シェーダーデフォルト）",
                [L10nKey.BakeModeColorBake] = "カラーをベイク→テクスチャ",
                [L10nKey.BakeModeScalarBake] = "スカラーをベイク→グレー",
                [L10nKey.BakeModeColorMultiply] = "カラーを乗算→テクスチャ",
                [L10nKey.BakeModeKeep] = "維持",

                // Diff Policy
                [L10nKey.DiffPolicyStopIfUnresolved] = "未解決なら中断",
                [L10nKey.DiffPolicyProceedWithFirst] = "最初を基準に進行",

                // Confirm Window
                [L10nKey.ConfirmTitle] = "ビルド確認",
                [L10nKey.ConfirmHeader] = "アトラスビルド＆適用実行前の確認",
                [L10nKey.ConfirmMessage] = "実行対象マテリアルプラン: {0} / スキップ: {1}\n以下のプロパティリストが実際のテクスチャアトラス化/生成対象です。",
                [L10nKey.Run] = "実行",
                [L10nKey.Skipped] = "スキップ",
                [L10nKey.UnresolvedDiffReason] = "未解決diffがあり、ポリシーが「未解決なら中断」なので、このマテリアルプランはスキップされます。",
                [L10nKey.AtlasIncludedTexEnv] = "アトラス含むTexEnv ({0})",
                [L10nKey.GeneratedTexEnv] = "生成/ターゲットTexEnv ({0})",
                [L10nKey.Cancel] = "キャンセル",
                [L10nKey.Execute] = "実行",

                // Dialogs
                [L10nKey.DialogNoScan] = "スキャン結果がありません。",
                [L10nKey.DialogNoPlan] = "有効なマテリアルプランがありません。",
                [L10nKey.DialogBlitFailed] = "Blitマテリアルの作成に失敗しました",
                [L10nKey.DialogRootRequired] = "ルートが必要です（適用時にルートを複製オプション）。",
                [L10nKey.DialogComplete] = "完了",
                [L10nKey.DialogLog] = "ログ: {0}",
                [L10nKey.DialogOutputFolderTitle] = "出力フォルダを選択",
                [L10nKey.DialogOutputFolderError] = "Assetsフォルダ内のみ可能です。",
                [L10nKey.DialogServiceNotInitialized] = "サービスが初期化されていません。",

                // Rollback
                [L10nKey.RollbackMenuItem] = "マルチアトラスロールバック...",
                [L10nKey.RollbackTitle] = "ロールバック",
                [L10nKey.RollbackComplete] = "ログベースのロールバックが完了しました",
                [L10nKey.RollbackOwnerClosed] = "元のウィンドウが閉じているため実行できません。",
                [L10nKey.Close] = "閉じる",

                // Language
                [L10nKey.LanguageSettings] = "言語設定",
                [L10nKey.LanguageKorean] = "한국어",
                [L10nKey.LanguageEnglish] = "English",
                [L10nKey.LanguageJapanese] = "日本語"
            };
        }
    }
}
#endif
