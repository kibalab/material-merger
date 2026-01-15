#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 전역 설정 패널을 렌더링하는 컴포넌트
    /// </summary>
    public class GlobalSettingsPanelRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public ILocalizationService Localization { get; set; }

        /// <summary>
        /// 전역 설정 패널 렌더링
        /// </summary>
        public void DrawGlobalSettings(MaterialMergerState state)
        {
            using (new EditorGUILayout.VerticalScope(Styles.stBox))
            {
                DrawGroupingRules(state);
                Utilities.GUIUtility.DrawSeparator();

                DrawApplicationSettings(state);
                Utilities.GUIUtility.DrawSeparator();

                DrawAtlasSettings(state);
                Utilities.GUIUtility.DrawSeparator();

                DrawPolicySettings(state);
            }
        }

        /// <summary>
        /// 머티리얼 분리 규칙
        /// </summary>
        private void DrawGroupingRules(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection(Localization.Get(L10nKey.MaterialGroupingRules), Styles.stSection);
            using (new EditorGUILayout.HorizontalScope())
            {
                var keywordsContent = new GUIContent(Localization.Get(L10nKey.GroupByKeywords),
                    "Split plans by shader keywords (different keyword sets are separated).");
                state.groupByKeywords = EditorGUILayout.ToggleLeft(keywordsContent,
                    state.groupByKeywords, GUILayout.Width(130));
                var queueContent = new GUIContent(Localization.Get(L10nKey.GroupByRenderQueue),
                    "Split plans by RenderQueue value.");
                state.groupByRenderQueue = EditorGUILayout.ToggleLeft(queueContent,
                    state.groupByRenderQueue, GUILayout.Width(150));
                var opaqueContent = new GUIContent(Localization.Get(L10nKey.SplitOpaqueTransparent),
                    "Separate opaque and transparent materials into different plans.");
                state.splitOpaqueTransparent = EditorGUILayout.ToggleLeft(opaqueContent,
                    state.splitOpaqueTransparent, GUILayout.Width(150));
            }

            var groupingParts = new List<string> { "Shader" };
            if (state.groupByKeywords) groupingParts.Add("Keywords");
            if (state.groupByRenderQueue) groupingParts.Add("RenderQueue");
            if (state.splitOpaqueTransparent) groupingParts.Add("Opaque/Transparent");
            var groupingSummary = "Current grouping: " + string.Join(" + ", groupingParts) + ".";
            EditorGUILayout.HelpBox(groupingSummary, MessageType.None);
        }

        /// <summary>
        /// 적용 방식
        /// </summary>
        private void DrawApplicationSettings(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection(Localization.Get(L10nKey.ApplicationMethod), Styles.stSection);
            using (new EditorGUILayout.HorizontalScope())
            {
                var cloneContent = new GUIContent(Localization.Get(L10nKey.CloneRootOnApply),
                    "Apply changes to a cloned root so the original is preserved.");
                state.cloneRootOnApply = EditorGUILayout.ToggleLeft(cloneContent,
                    state.cloneRootOnApply, GUILayout.Width(150));
                using (new EditorGUI.DisabledScope(!state.cloneRootOnApply))
                {
                    var deactivateContent = new GUIContent(Localization.Get(L10nKey.DeactivateOriginalRoot),
                        "Deactivate the original root after cloning.");
                    state.deactivateOriginalRoot = EditorGUILayout.ToggleLeft(deactivateContent,
                        state.deactivateOriginalRoot, GUILayout.Width(160));
                }
            }

            string applySummary;
            if (state.cloneRootOnApply)
            {
                applySummary = state.deactivateOriginalRoot
                    ? "Apply will create a cloned root and deactivate the original."
                    : "Apply will create a cloned root and keep the original active.";
            }
            else
            {
                applySummary = "Apply writes directly to the selected root (no clone).";
            }
            EditorGUILayout.HelpBox(applySummary, MessageType.None);
        }

        /// <summary>
        /// 아틀라스 설정
        /// </summary>
        private void DrawAtlasSettings(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection(Localization.Get(L10nKey.Atlas), Styles.stSection);
            using (new EditorGUILayout.HorizontalScope())
            {
                var atlasContent = new GUIContent(Localization.Get(L10nKey.AtlasSize),
                    "Final atlas texture size per page.");
                state.atlasSize = EditorGUILayout.IntPopup(atlasContent, state.atlasSize,
                    new[] { new GUIContent("4096"), new GUIContent("8192") }, new[] { 4096, 8192 }, GUILayout.Width(220));
                var gridContent = new GUIContent(Localization.Get(L10nKey.Grid),
                    "Tiles per row/column (grid x grid = tiles per page).");
                state.grid = EditorGUILayout.IntPopup(gridContent, state.grid,
                    new[] { new GUIContent("2"), new GUIContent("4") }, new[] { 2, 4 }, GUILayout.Width(200));
                GUILayout.FlexibleSpace();
            }

            var paddingContent = new GUIContent(Localization.Get(L10nKey.Padding),
                "Padding around each tile to reduce texture bleeding.");
            state.paddingPx = EditorGUILayout.IntSlider(paddingContent, state.paddingPx, 0, 64);

            int tilesPerPage = Mathf.Max(1, state.grid * state.grid);
            int cell = Mathf.Max(1, state.atlasSize / Mathf.Max(1, state.grid));
            int content = cell - state.paddingPx * 2;
            if (content <= 0) content = cell;
            var atlasSummary = $"Tiles per page: {tilesPerPage}. Tile size: {cell}px, content: {content}px.";
            EditorGUILayout.HelpBox(atlasSummary, MessageType.None);
        }

        /// <summary>
        /// 정책 설정
        /// </summary>
        private void DrawPolicySettings(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection(Localization.Get(L10nKey.Policy), Styles.stSection);
            var policyContent = new GUIContent(Localization.Get(L10nKey.UnresolvedDiffPolicy),
                "How to handle unresolved scalar/color differences.");
            state.diffPolicy = (DiffPolicy)EditorGUILayout.EnumPopup(policyContent, state.diffPolicy);

            var policySummary = state.diffPolicy == DiffPolicy.미해결이면중단
                ? "Plans with unresolved scalar/color differences are skipped."
                : "Plans with unresolved scalar/color differences use the first material's values.";
            EditorGUILayout.HelpBox(policySummary, MessageType.None);
        }
    }
}
#endif
