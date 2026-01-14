#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 개별 그룹 패널을 렌더링하는 컴포넌트
    /// </summary>
    public class GroupPanelRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public PropertyTableRenderer TableRenderer { get; set; }

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
            using (new EditorGUILayout.HorizontalScope())
            {
                g.enabled = EditorGUILayout.Toggle(g.enabled, GUILayout.Width(16));
                var foldoutContent = new GUIContent($"{shaderName}   [{g.tag}]");
                g.foldout = EditorGUILayout.Foldout(g.foldout, foldoutContent, true);
                GUILayout.FlexibleSpace();

                Utilities.GUIUtility.DrawPill($"머티리얼 {g.mats.Count}", false, Styles.stPill, Styles.stPillWarn);
                Utilities.GUIUtility.DrawPill($"페이지 {g.pageCount}", false, Styles.stPill, Styles.stPillWarn);

                if (g.skippedMultiMat > 0)
                    Utilities.GUIUtility.DrawPill($"스킵 {g.skippedMultiMat}", true, Styles.stPill,
                        Styles.stPillWarn);
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

                g.onlyRelevant = GUILayout.Toggle(g.onlyRelevant, "관련만", Styles.stToolbarBtn,
                    GUILayout.Width(70), GUILayout.Height(lineHeight));
                g.showTexturesOnly = GUILayout.Toggle(g.showTexturesOnly, "텍스처만", Styles.stToolbarBtn,
                    GUILayout.Width(80), GUILayout.Height(lineHeight));
                g.showScalarsOnly = GUILayout.Toggle(g.showScalarsOnly, "스칼라만", Styles.stToolbarBtn,
                    GUILayout.Width(80), GUILayout.Height(lineHeight));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("텍스처 아틀라스 전체 켜기", Styles.stToolbarBtn, GUILayout.Width(170),
                        GUILayout.Height(lineHeight)))
                    SetAllTexActions(g, true);

                if (GUILayout.Button("텍스처 아틀라스 전체 끄기", Styles.stToolbarBtn, GUILayout.Width(170),
                        GUILayout.Height(lineHeight)))
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
