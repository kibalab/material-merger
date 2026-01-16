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
                    Localization.Get(L10nKey.GroupByKeywordsTooltip));
                state.groupByKeywords = EditorGUILayout.ToggleLeft(keywordsContent,
                    state.groupByKeywords, GUILayout.Width(130));
                var queueContent = new GUIContent(Localization.Get(L10nKey.GroupByRenderQueue),
                    Localization.Get(L10nKey.GroupByRenderQueueTooltip));
                state.groupByRenderQueue = EditorGUILayout.ToggleLeft(queueContent,
                    state.groupByRenderQueue, GUILayout.Width(150));
                var opaqueContent = new GUIContent(Localization.Get(L10nKey.SplitOpaqueTransparent),
                    Localization.Get(L10nKey.SplitOpaqueTransparentTooltip));
                state.splitOpaqueTransparent = EditorGUILayout.ToggleLeft(opaqueContent,
                    state.splitOpaqueTransparent, GUILayout.Width(150));
            }

            var groupingParts = new List<string> { Localization.Get(L10nKey.GroupingShader) };
            if (state.groupByKeywords) groupingParts.Add(Localization.Get(L10nKey.GroupByKeywords));
            if (state.groupByRenderQueue) groupingParts.Add(Localization.Get(L10nKey.GroupByRenderQueue));
            if (state.splitOpaqueTransparent) groupingParts.Add(Localization.Get(L10nKey.SplitOpaqueTransparent));
            var groupingSummary = Localization.Get(L10nKey.GroupingSummary, string.Join(" + ", groupingParts));
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
                    Localization.Get(L10nKey.CloneRootOnApplyTooltip));
                state.cloneRootOnApply = EditorGUILayout.ToggleLeft(cloneContent,
                    state.cloneRootOnApply, GUILayout.Width(150));
                using (new EditorGUI.DisabledScope(!state.cloneRootOnApply))
                {
                    var deactivateContent = new GUIContent(Localization.Get(L10nKey.DeactivateOriginalRoot),
                        Localization.Get(L10nKey.DeactivateOriginalRootTooltip));
                    state.deactivateOriginalRoot = EditorGUILayout.ToggleLeft(deactivateContent,
                        state.deactivateOriginalRoot, GUILayout.Width(160));
                    
                    var keepPrefabContent = new GUIContent(Localization.Get(L10nKey.KeepPrefabOnClone),
                        Localization.Get(L10nKey.KeepPrefabOnCloneTooltip));
                    state.keepPrefabOnClone = EditorGUILayout.ToggleLeft(keepPrefabContent,
                        state.keepPrefabOnClone, GUILayout.Width(150));
                }
            }

            string applySummary;
            if (state.cloneRootOnApply)
            {
                applySummary = state.deactivateOriginalRoot
                    ? Localization.Get(L10nKey.ApplySummaryCloneDeactivate)
                    : Localization.Get(L10nKey.ApplySummaryCloneKeep);
            }
            else
            {
                applySummary = Localization.Get(L10nKey.ApplySummaryDirect);
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
                    Localization.Get(L10nKey.AtlasSizeTooltip));
                state.atlasSize = EditorGUILayout.IntPopup(atlasContent, state.atlasSize,
                    new[] { new GUIContent("4096"), new GUIContent("8192") }, new[] { 4096, 8192 }, GUILayout.Width(220));
                var gridContent = new GUIContent(Localization.Get(L10nKey.Grid),
                    Localization.Get(L10nKey.GridTooltip));
                state.grid = EditorGUILayout.IntPopup(gridContent, state.grid,
                    new[] { new GUIContent("2"), new GUIContent("4") }, new[] { 2, 4 }, GUILayout.Width(200));
                GUILayout.FlexibleSpace();
            }

            var paddingContent = new GUIContent(Localization.Get(L10nKey.Padding),
                Localization.Get(L10nKey.PaddingTooltip));
            state.paddingPx = EditorGUILayout.IntSlider(paddingContent, state.paddingPx, 0, 64);

            int tilesPerPage = Mathf.Max(1, state.grid * state.grid);
            int cell = Mathf.Max(1, state.atlasSize / Mathf.Max(1, state.grid));
            int content = cell - state.paddingPx * 2;
            if (content <= 0) content = cell;
            var atlasSummary = Localization.Get(L10nKey.AtlasSummary, tilesPerPage, cell, content);
            EditorGUILayout.HelpBox(atlasSummary, MessageType.None);
        }

        /// <summary>
        /// 정책 설정
        /// </summary>
        private void DrawPolicySettings(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection(Localization.Get(L10nKey.Policy), Styles.stSection);
            var policyContent = new GUIContent(Localization.Get(L10nKey.UnresolvedDiffPolicy),
                Localization.Get(L10nKey.UnresolvedDiffPolicyTooltip));
            state.diffPolicy = (DiffPolicy)EditorGUILayout.EnumPopup(policyContent, state.diffPolicy);

            if (state.diffPolicy == DiffPolicy.UseSampleMaterial)
            {
                var sampleContent = new GUIContent(Localization.Get(L10nKey.SampleMaterial),
                    Localization.Get(L10nKey.SampleMaterialTooltip));
                state.diffSampleMaterial = (Material)EditorGUILayout.ObjectField(sampleContent,
                    state.diffSampleMaterial, typeof(Material), false);
            }

            string policySummary;
            var policyType = MessageType.None;
            if (state.diffPolicy == DiffPolicy.StopIfUnresolved)
            {
                policySummary = Localization.Get(L10nKey.PolicySummaryStop);
            }
            else if (state.diffPolicy == DiffPolicy.UseSampleMaterial)
            {
                if (state.diffSampleMaterial)
                {
                    policySummary = Localization.Get(L10nKey.PolicySummarySample);
                }
                else
                {
                    policySummary = Localization.Get(L10nKey.PolicySummarySampleMissing);
                    policyType = MessageType.Warning;
                }
            }
            else
            {
                policySummary = Localization.Get(L10nKey.PolicySummaryProceed);
            }

            EditorGUILayout.HelpBox(policySummary, policyType);
        }
    }
}
#endif
