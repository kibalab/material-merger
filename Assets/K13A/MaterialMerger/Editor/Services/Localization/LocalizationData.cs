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

                // Global Settings
                [L10nKey.MaterialGroupingRules] = "머테리얼 분리 규칙",
                [L10nKey.GroupByKeywords] = "키워드로 분리",
                [L10nKey.GroupByRenderQueue] = "RenderQueue로 분리",
                [L10nKey.SplitOpaqueTransparent] = "불투명/투명 분리",

                [L10nKey.ApplicationMethod] = "적용 방식",
                [L10nKey.CloneRootOnApply] = "적용 시 루트 복제",
                [L10nKey.DeactivateOriginalRoot] = "원본 루트 비활성화",

                [L10nKey.Atlas] = "아틀라스",
                [L10nKey.AtlasSize] = "크기",
                [L10nKey.Grid] = "그리드",
                [L10nKey.Padding] = "패딩(px)",

                [L10nKey.Policy] = "정책",
                [L10nKey.UnresolvedDiffPolicy] = "미해결 diff 처리",

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
                [L10nKey.CollapseAll] = "전체 접기",
                [L10nKey.EnableAll] = "전체 활성",
                [L10nKey.DisableAll] = "전체 비활성",
                [L10nKey.NoScanMessage] = "루트를 지정한 뒤 스캔을 실행하세요.",

                // Group Panel
                [L10nKey.Material] = "머티리얼",
                [L10nKey.Page] = "페이지",
                [L10nKey.Skip] = "MultiMat {0}",
                [L10nKey.OutputMaterialName] = "Output Name",
                [L10nKey.SingleMaterial] = "단일(병합불필요)",
                [L10nKey.RelevantOnly] = "관련만",
                [L10nKey.TexturesOnly] = "텍스처만",
                [L10nKey.ScalarsOnly] = "스칼라만",
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

                // Global Settings
                [L10nKey.MaterialGroupingRules] = "Material Grouping Rules",
                [L10nKey.GroupByKeywords] = "Group by Keywords",
                [L10nKey.GroupByRenderQueue] = "Group by RenderQueue",
                [L10nKey.SplitOpaqueTransparent] = "Split Opaque/Transparent",

                [L10nKey.ApplicationMethod] = "Application Method",
                [L10nKey.CloneRootOnApply] = "Clone Root on Apply",
                [L10nKey.DeactivateOriginalRoot] = "Deactivate Original Root",

                [L10nKey.Atlas] = "Atlas",
                [L10nKey.AtlasSize] = "Size",
                [L10nKey.Grid] = "Grid",
                [L10nKey.Padding] = "Padding(px)",

                [L10nKey.Policy] = "Policy",
                [L10nKey.UnresolvedDiffPolicy] = "Unresolved Diff Policy",

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
                [L10nKey.CollapseAll] = "Collapse All",
                [L10nKey.EnableAll] = "Enable All",
                [L10nKey.DisableAll] = "Disable All",
                [L10nKey.NoScanMessage] = "Please specify a root and run scan.",

                // Group Panel
                [L10nKey.Material] = "Material",
                [L10nKey.Page] = "Page",
                [L10nKey.Skip] = "MultiMat {0}",
                [L10nKey.OutputMaterialName] = "Output Name",
                [L10nKey.SingleMaterial] = "Single(No merge needed)",
                [L10nKey.RelevantOnly] = "Relevant",
                [L10nKey.TexturesOnly] = "Textures",
                [L10nKey.ScalarsOnly] = "Scalars",
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

                // Global Settings
                [L10nKey.MaterialGroupingRules] = "マテリアルグループ化ルール",
                [L10nKey.GroupByKeywords] = "キーワードでグループ化",
                [L10nKey.GroupByRenderQueue] = "RenderQueueでグループ化",
                [L10nKey.SplitOpaqueTransparent] = "不透明/透明を分離",

                [L10nKey.ApplicationMethod] = "適用方法",
                [L10nKey.CloneRootOnApply] = "適用時にルートを複製",
                [L10nKey.DeactivateOriginalRoot] = "元のルートを無効化",

                [L10nKey.Atlas] = "アトラス",
                [L10nKey.AtlasSize] = "サイズ",
                [L10nKey.Grid] = "グリッド",
                [L10nKey.Padding] = "パディング(px)",

                [L10nKey.Policy] = "ポリシー",
                [L10nKey.UnresolvedDiffPolicy] = "未解決diff処理",

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
                [L10nKey.CollapseAll] = "すべて折りたたむ",
                [L10nKey.EnableAll] = "すべて有効",
                [L10nKey.DisableAll] = "すべて無効",
                [L10nKey.NoScanMessage] = "ルートを指定してスキャンを実行してください。",

                // Group Panel
                [L10nKey.Material] = "マテリアル",
                [L10nKey.Page] = "ページ",
                [L10nKey.Skip] = "MultiMat {0}",
                [L10nKey.OutputMaterialName] = "Output Name",
                [L10nKey.SingleMaterial] = "単一(マージ不要)",
                [L10nKey.RelevantOnly] = "関連のみ",
                [L10nKey.TexturesOnly] = "テクスチャのみ",
                [L10nKey.ScalarsOnly] = "スカラーのみ",
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
