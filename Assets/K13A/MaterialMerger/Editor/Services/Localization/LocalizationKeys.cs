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
        public const string ScanTooltip = "top.scan_tooltip";
        public const string BuildAndApplyTooltip = "top.build_and_apply_tooltip";
        public const string RootTooltip = "top.root_tooltip";
        public const string LastScanTooltip = "top.last_scan_tooltip";
        public const string OutputFolderTooltip = "top.output_folder_tooltip";
        public const string LanguageTooltip = "top.language_tooltip";

        // Global Settings
        public const string MaterialGroupingRules = "global.material_grouping_rules";
        public const string GroupByKeywords = "global.group_by_keywords";
        public const string GroupByRenderQueue = "global.group_by_renderqueue";
        public const string SplitOpaqueTransparent = "global.split_opaque_transparent";
        public const string GroupByKeywordsTooltip = "global.group_by_keywords_tooltip";
        public const string GroupByRenderQueueTooltip = "global.group_by_renderqueue_tooltip";
        public const string SplitOpaqueTransparentTooltip = "global.split_opaque_transparent_tooltip";
        public const string GroupingShader = "global.grouping_shader";
        public const string GroupingSummary = "global.grouping_summary";

        public const string ApplicationMethod = "global.application_method";
        public const string CloneRootOnApply = "global.clone_root_on_apply";
        public const string DeactivateOriginalRoot = "global.deactivate_original_root";
        public const string CloneRootOnApplyTooltip = "global.clone_root_on_apply_tooltip";
        public const string DeactivateOriginalRootTooltip = "global.deactivate_original_root_tooltip";
        public const string ApplySummaryCloneDeactivate = "global.apply_summary_clone_deactivate";
        public const string ApplySummaryCloneKeep = "global.apply_summary_clone_keep";
        public const string ApplySummaryDirect = "global.apply_summary_direct";

        public const string Atlas = "global.atlas";
        public const string AtlasSize = "global.atlas_size";
        public const string Grid = "global.grid";
        public const string Padding = "global.padding";
        public const string AtlasSizeTooltip = "global.atlas_size_tooltip";
        public const string GridTooltip = "global.grid_tooltip";
        public const string PaddingTooltip = "global.padding_tooltip";
        public const string AtlasSummary = "global.atlas_summary";

        public const string Policy = "global.policy";
        public const string UnresolvedDiffPolicy = "global.unresolved_diff_policy";
        public const string UnresolvedDiffPolicyTooltip = "global.unresolved_diff_policy_tooltip";
        public const string SampleMaterial = "global.sample_material";
        public const string SampleMaterialTooltip = "global.sample_material_tooltip";
        public const string PolicySummaryStop = "global.policy_summary_stop";
        public const string PolicySummaryProceed = "global.policy_summary_proceed";
        public const string PolicySummarySample = "global.policy_summary_sample";
        public const string PolicySummarySampleMissing = "global.policy_summary_sample_missing";

        // Statistics
        public const string Statistics = "stats.statistics";
        public const string TotalMaterials = "stats.total_materials";
        public const string UniqueMaterials = "stats.unique_materials";
        public const string MergableGroups = "stats.mergable_groups";
        public const string BeforeMerge = "stats.before_merge";
        public const string AfterMerge = "stats.after_merge";
        public const string TotalRenderers = "stats.total_renderers";

        // Group List
        public const string PlanList = "grouplist.plan_list";
        public const string ExpandAll = "grouplist.expand_all";
        public const string CollapseAll = "grouplist.collapse_all";
        public const string EnableAll = "grouplist.enable_all";
        public const string DisableAll = "grouplist.disable_all";
        public const string ExpandAllTooltip = "grouplist.expand_all_tooltip";
        public const string CollapseAllTooltip = "grouplist.collapse_all_tooltip";
        public const string EnableAllTooltip = "grouplist.enable_all_tooltip";
        public const string DisableAllTooltip = "grouplist.disable_all_tooltip";
        public const string MergeSelected = "grouplist.merge_selected";
        public const string MergeSelectedTooltip = "grouplist.merge_selected_tooltip";
        public const string ClearMerge = "grouplist.clear_merge";
        public const string ClearMergeTooltip = "grouplist.clear_merge_tooltip";

        public const string NoScanMessage = "grouplist.no_scan_message";

        // Group Panel
        public const string Material = "group.material";
        public const string Page = "group.page";
        public const string Skip = "group.skip";
        public const string SingleMaterial = "group.single_material";
        public const string RelevantOnly = "group.relevant_only";
        public const string TexturesOnly = "group.textures_only";
        public const string ScalarsOnly = "group.scalars_only";
        public const string EnableAllTextureAtlas = "group.enable_all_texture_atlas";
        public const string DisableAllTextureAtlas = "group.disable_all_texture_atlas";
        public const string OutputMaterialName = "group.output_material_name";
        public const string SingleMaterialTooltip = "group.single_material_tooltip";
        public const string PageTooltip = "group.page_tooltip";
        public const string MultiMatTooltip = "group.multimat_tooltip";
        public const string FilterRelevantTooltip = "group.filter_relevant_tooltip";
        public const string FilterTexturesTooltip = "group.filter_textures_tooltip";
        public const string FilterScalarsTooltip = "group.filter_scalars_tooltip";
        public const string OutputMaterialNameTooltip = "group.output_material_name_tooltip";
        public const string PlanMaterials = "group.plan_materials";
        public const string PlanMaterialsTooltip = "group.plan_materials_tooltip";
        public const string PlanMaterialsTitle = "group.plan_materials_title";
        public const string PlanMaterialsEmpty = "group.plan_materials_empty";
        public const string PlanDragHandleTooltip = "group.plan_drag_handle_tooltip";
        public const string MergeSelect = "group.merge_select";
        public const string MergeSelectTooltip = "group.merge_select_tooltip";
        public const string MergedTag = "group.merged_tag";
        public const string MergedTagTooltip = "group.merged_tag_tooltip";

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
