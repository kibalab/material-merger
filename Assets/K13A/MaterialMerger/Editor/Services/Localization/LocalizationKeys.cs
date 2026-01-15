#if UNITY_EDITOR
namespace K13A.MaterialMerger.Editor.Services.Localization
{
    /// <summary>
    /// 현지화 키 상수 정의
    /// </summary>
    public static class L10nKey
    {
        // Window Title
        public const string WindowTitle = "window.title";

        // Top Panel
        public const string Scan = "top.scan";
        public const string BuildAndApply = "top.build_and_apply";
        public const string Root = "top.root";
        public const string LastScan = "top.last_scan";
        public const string OutputFolder = "top.output_folder";
        public const string NoScan = "top.no_scan";

        // Global Settings
        public const string MaterialGroupingRules = "global.material_grouping_rules";
        public const string GroupByKeywords = "global.group_by_keywords";
        public const string GroupByRenderQueue = "global.group_by_renderqueue";
        public const string SplitOpaqueTransparent = "global.split_opaque_transparent";

        public const string ApplicationMethod = "global.application_method";
        public const string CloneRootOnApply = "global.clone_root_on_apply";
        public const string DeactivateOriginalRoot = "global.deactivate_original_root";

        public const string Atlas = "global.atlas";
        public const string AtlasSize = "global.atlas_size";
        public const string Grid = "global.grid";
        public const string Padding = "global.padding";

        public const string Policy = "global.policy";
        public const string UnresolvedDiffPolicy = "global.unresolved_diff_policy";

        // Group List
        public const string PlanList = "grouplist.plan_list";
        public const string ExpandAll = "grouplist.expand_all";
        public const string CollapseAll = "grouplist.collapse_all";
        public const string EnableAll = "grouplist.enable_all";
        public const string DisableAll = "grouplist.disable_all";

        public const string NoScanMessage = "grouplist.no_scan_message";

        // Group Panel
        public const string Material = "group.material";
        public const string Page = "group.page";
        public const string Skip = "group.skip";
        public const string RelevantOnly = "group.relevant_only";
        public const string TexturesOnly = "group.textures_only";
        public const string ScalarsOnly = "group.scalars_only";
        public const string EnableAllTextureAtlas = "group.enable_all_texture_atlas";
        public const string DisableAllTextureAtlas = "group.disable_all_texture_atlas";

        // Property Table
        public const string Property = "table.property";
        public const string Type = "table.type";
        public const string Action = "table.action";
        public const string Target = "table.target";
        public const string Info = "table.info";
        public const string NoPropertiesMatch = "table.no_properties_match";

        // Property Row
        public const string Texture = "row.texture";
        public const string Color = "row.color";
        public const string Float = "row.float";
        public const string Range = "row.range";
        public const string Vector = "row.vector";

        public const string TextureAtlas = "row.texture_atlas";
        public const string NormalMap = "row.normal_map";
        public const string SRGB = "row.srgb";
        public const string Linear = "row.linear";
        public const string Empty = "row.empty";
        public const string NotApplied = "row.not_applied";
        public const string Same = "row.same";
        public const string ShowMore = "row.show_more";
        public const string Collapse = "row.collapse";

        public const string NoTexEnv = "row.no_texenv";

        // Property Row Expanded
        public const string TextureAtlasing = "row_exp.texture_atlasing";
        public const string EnableCheckboxToInclude = "row_exp.enable_checkbox_to_include";
        public const string TextureWillBeIncluded = "row_exp.texture_will_be_included";
        public const string ColorSpaceAutoDetected = "row_exp.colorspace_auto_detected";

        public const string EnableCheckboxToApply = "row_exp.enable_checkbox_to_apply";
        public const string ResetAfterBake = "row_exp.reset_after_bake";
        public const string IncludeAlpha = "row_exp.include_alpha";
        public const string ModifierOptional = "row_exp.modifier_optional";
        public const string Clamp01 = "row_exp.clamp01";
        public const string ApplyToAlpha = "row_exp.apply_to_alpha";
        public const string BakeOptionsWhenSelected = "row_exp.bake_options_when_selected";

        public const string None = "row_exp.none";

        // Bake Modes
        public const string BakeModeReset = "bakemode.reset";
        public const string BakeModeColorBake = "bakemode.color_bake";
        public const string BakeModeScalarBake = "bakemode.scalar_bake";
        public const string BakeModeColorMultiply = "bakemode.color_multiply";
        public const string BakeModeKeep = "bakemode.keep";

        // Diff Policy
        public const string DiffPolicyStopIfUnresolved = "diffpolicy.stop_if_unresolved";
        public const string DiffPolicyProceedWithFirst = "diffpolicy.proceed_with_first";

        // Confirm Window
        public const string ConfirmTitle = "confirm.title";
        public const string ConfirmHeader = "confirm.header";
        public const string ConfirmMessage = "confirm.message";
        public const string Run = "confirm.run";
        public const string Skipped = "confirm.skipped";
        public const string UnresolvedDiffReason = "confirm.unresolved_diff_reason";
        public const string AtlasIncludedTexEnv = "confirm.atlas_included_texenv";
        public const string GeneratedTexEnv = "confirm.generated_texenv";
        public const string Cancel = "confirm.cancel";
        public const string Execute = "confirm.execute";

        // Dialogs
        public const string DialogNoScan = "dialog.no_scan";
        public const string DialogNoPlan = "dialog.no_plan";
        public const string DialogBlitFailed = "dialog.blit_failed";
        public const string DialogRootRequired = "dialog.root_required";
        public const string DialogComplete = "dialog.complete";
        public const string DialogLog = "dialog.log";
        public const string DialogOutputFolderTitle = "dialog.output_folder_title";
        public const string DialogOutputFolderError = "dialog.output_folder_error";
        public const string DialogServiceNotInitialized = "dialog.service_not_initialized";

        // Rollback
        public const string RollbackMenuItem = "rollback.menu_item";
        public const string RollbackTitle = "rollback.title";
        public const string RollbackComplete = "rollback.complete";
        public const string RollbackOwnerClosed = "rollback.owner_closed";
        public const string Close = "rollback.close";

        // Language Settings
        public const string LanguageSettings = "language.settings";
        public const string LanguageKorean = "language.korean";
        public const string LanguageEnglish = "language.english";
        public const string LanguageJapanese = "language.japanese";
    }
}
#endif
