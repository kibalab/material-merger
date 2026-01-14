#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 전역 설정 패널을 렌더링하는 컴포넌트
    /// </summary>
    public class GlobalSettingsPanelRenderer
    {
        public MaterialMergerStyles Styles { get; set; }

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
            Utilities.GUIUtility.DrawSection("머테리얼 분리 규칙", Styles.stSection);
            using (new EditorGUILayout.HorizontalScope())
            {
                state.groupByKeywords = EditorGUILayout.ToggleLeft("키워드로 분리", state.groupByKeywords,
                    GUILayout.Width(130));
                state.groupByRenderQueue = EditorGUILayout.ToggleLeft("RenderQueue로 분리",
                    state.groupByRenderQueue, GUILayout.Width(150));
                state.splitOpaqueTransparent = EditorGUILayout.ToggleLeft("불투명/투명 분리",
                    state.splitOpaqueTransparent, GUILayout.Width(150));
            }
        }

        /// <summary>
        /// 적용 방식
        /// </summary>
        private void DrawApplicationSettings(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection("적용 방식", Styles.stSection);
            using (new EditorGUILayout.HorizontalScope())
            {
                state.cloneRootOnApply = EditorGUILayout.ToggleLeft("적용 시 루트 복제", state.cloneRootOnApply,
                    GUILayout.Width(150));
                using (new EditorGUI.DisabledScope(!state.cloneRootOnApply))
                    state.deactivateOriginalRoot = EditorGUILayout.ToggleLeft("원본 루트 비활성화",
                        state.deactivateOriginalRoot, GUILayout.Width(160));
            }
        }

        /// <summary>
        /// 아틀라스 설정
        /// </summary>
        private void DrawAtlasSettings(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection("아틀라스", Styles.stSection);
            using (new EditorGUILayout.HorizontalScope())
            {
                state.atlasSize = EditorGUILayout.IntPopup("크기", state.atlasSize, new[] { "4096", "8192" },
                    new[] { 4096, 8192 }, GUILayout.Width(220));
                state.grid = EditorGUILayout.IntPopup("그리드", state.grid, new[] { "2", "4" }, new[] { 2, 4 },
                    GUILayout.Width(200));
                GUILayout.FlexibleSpace();
            }

            state.paddingPx = EditorGUILayout.IntSlider("패딩(px)", state.paddingPx, 0, 64);
        }

        /// <summary>
        /// 정책 설정
        /// </summary>
        private void DrawPolicySettings(MaterialMergerState state)
        {
            Utilities.GUIUtility.DrawSection("정책", Styles.stSection);
            state.diffPolicy = (DiffPolicy)EditorGUILayout.EnumPopup("미해결 diff 처리", state.diffPolicy);
        }
    }
}
#endif
