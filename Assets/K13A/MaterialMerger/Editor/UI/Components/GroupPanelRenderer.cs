#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 개별 그룹 패널을 렌더링하는 컴포넌트
    /// </summary>
    public class GroupPanelRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public PropertyTableRenderer TableRenderer { get; set; }
        public ILocalizationService Localization { get; set; }

        /// <summary>
        /// 그룹 패널 렌더링
        /// </summary>
        public void DrawGroup(GroupScan g, int index)
        {
            using (new EditorGUILayout.VerticalScope(Styles.stBox))
            {
                DrawGroupHeader(g);

                if (!g.foldout) return;

                DrawGroupToolbar(g);
                EditorGUILayout.Space(4);
                TableRenderer.DrawTable(g);
            }
        }

        /// <summary>
        /// 그룹 헤더 (체크박스, 폴드아웃, 이름, 통계)
        /// </summary>
        private void DrawGroupHeader(GroupScan g)
        {
            var shaderName = Utilities.GUIUtility.GetGroupShaderName(g);
            bool isSingleMaterial = g.mats.Count == 1;

            using (new EditorGUILayout.HorizontalScope())
            {
                // 단일 머티리얼은 체크박스 비활성화 (병합할 필요 없음)
                using (new EditorGUI.DisabledScope(isSingleMaterial))
                {
                    g.enabled = EditorGUILayout.Toggle(g.enabled, GUILayout.Width(16));
                }

                var foldoutContent = new GUIContent($"{shaderName}   [{g.tag}]");
                g.foldout = EditorGUILayout.Foldout(g.foldout, foldoutContent, true);
                GUILayout.FlexibleSpace();

                Utilities.GUIUtility.DrawPill(Localization.Get(L10nKey.Material, g.mats.Count), false,
                    Styles.stPill, Styles.stPillWarn);

                // 단일 머티리얼이면 특별 표시
                if (isSingleMaterial)
                {
                    var c = GUI.color;
                    GUI.color = new Color(0.7f, 0.7f, 0.7f);
                    Utilities.GUIUtility.DrawPill(Localization.Get(L10nKey.SingleMaterial), false,
                        Styles.stPill, Styles.stPillWarn);
                    GUI.color = c;
                }
                else
                {
                    Utilities.GUIUtility.DrawPill(Localization.Get(L10nKey.Page, g.pageCount), false,
                        Styles.stPill, Styles.stPillWarn);
                }

                if (g.skippedMultiMat > 0)
                    Utilities.GUIUtility.DrawPill(Localization.Get(L10nKey.Skip, g.skippedMultiMat), true,
                        Styles.stPill, Styles.stPillWarn);
            }
        }

        /// <summary>
        /// 그룹 툴바 (검색, 필터, 액션 버튼)
        /// </summary>
        private void DrawGroupToolbar(GroupScan g)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            using (new EditorGUILayout.HorizontalScope(Styles.stToolbar, GUILayout.Height(lineHeight)))
            {
                g.search = Utilities.GUIUtility.DrawSearchField(g.search, 260, lineHeight);

                g.onlyRelevant = GUILayout.Toggle(g.onlyRelevant, Localization.Get(L10nKey.RelevantOnly),
                    Styles.stToolbarBtn, GUILayout.Width(70), GUILayout.Height(lineHeight));
                g.showTexturesOnly = GUILayout.Toggle(g.showTexturesOnly, Localization.Get(L10nKey.TexturesOnly),
                    Styles.stToolbarBtn, GUILayout.Width(80), GUILayout.Height(lineHeight));
                g.showScalarsOnly = GUILayout.Toggle(g.showScalarsOnly, Localization.Get(L10nKey.ScalarsOnly),
                    Styles.stToolbarBtn, GUILayout.Width(80), GUILayout.Height(lineHeight));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(Localization.Get(L10nKey.EnableAllTextureAtlas), Styles.stToolbarBtn,
                        GUILayout.Width(170), GUILayout.Height(lineHeight)))
                    SetAllTexActions(g, true);

                if (GUILayout.Button(Localization.Get(L10nKey.DisableAllTextureAtlas), Styles.stToolbarBtn,
                        GUILayout.Width(170), GUILayout.Height(lineHeight)))
                    SetAllTexActions(g, false);
            }
        }

        /// <summary>
        /// 모든 텍스처 프로퍼티의 doAction 설정
        /// </summary>
        private void SetAllTexActions(GroupScan g, bool on)
        {
            foreach (var r in g.rows)
                if (r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.texDistinct > 1)
                    r.doAction = on;
        }
    }
}
#endif
