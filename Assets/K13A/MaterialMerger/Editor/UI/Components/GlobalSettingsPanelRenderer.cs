#if UNITY_EDITOR
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
                state.groupByKeywords = EditorGUILayout.ToggleLeft(Localization.Get(L10nKey.GroupByKeywords),
                    state.groupByKeywords, GUILayout.Width(130));
                state.groupByRenderQueue = EditorGUILayout.ToggleLeft(Localization.Get(L10nKey.GroupByRenderQueue),
                    state.groupByRenderQueue, GUILayout.Width(150));
                state.splitOpaqueTransparent = EditorGUILayout.ToggleLeft(Localization.Get(L10nKey.SplitOpaqueTransparent),
                    state.splitOpaqueTransparent, GUILayout.Width(150));
            }
        }

        /// <summary>
        /// 적용 방식
        /// </summary>
        private void DrawApplicationSettings(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection(Localization.Get(L10nKey.ApplicationMethod), Styles.stSection);
            using (new EditorGUILayout.HorizontalScope())
            {
                state.cloneRootOnApply = EditorGUILayout.ToggleLeft(Localization.Get(L10nKey.CloneRootOnApply),
                    state.cloneRootOnApply, GUILayout.Width(150));
                using (new EditorGUI.DisabledScope(!state.cloneRootOnApply))
                    state.deactivateOriginalRoot = EditorGUILayout.ToggleLeft(Localization.Get(L10nKey.DeactivateOriginalRoot),
                        state.deactivateOriginalRoot, GUILayout.Width(160));
            }
        }

        /// <summary>
        /// 아틀라스 설정
        /// </summary>
        private void DrawAtlasSettings(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection(Localization.Get(L10nKey.Atlas), Styles.stSection);
            using (new EditorGUILayout.HorizontalScope())
            {
                state.atlasSize = EditorGUILayout.IntPopup(Localization.Get(L10nKey.AtlasSize), state.atlasSize,
                    new[] { "4096", "8192" }, new[] { 4096, 8192 }, GUILayout.Width(220));
                state.grid = EditorGUILayout.IntPopup(Localization.Get(L10nKey.Grid), state.grid,
                    new[] { "2", "4" }, new[] { 2, 4 }, GUILayout.Width(200));
                GUILayout.FlexibleSpace();
            }

            state.paddingPx = EditorGUILayout.IntSlider(Localization.Get(L10nKey.Padding), state.paddingPx, 0, 64);
        }

        /// <summary>
        /// 정책 설정
        /// </summary>
        private void DrawPolicySettings(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection(Localization.Get(L10nKey.Policy), Styles.stSection);
            state.diffPolicy = (DiffPolicy)EditorGUILayout.EnumPopup(Localization.Get(L10nKey.UnresolvedDiffPolicy),
                state.diffPolicy);
        }
    }
}
#endif
