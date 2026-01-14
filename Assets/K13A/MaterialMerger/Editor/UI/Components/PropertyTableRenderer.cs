#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 프로퍼티 테이블 (헤더 + 행들)을 렌더링하는 컴포넌트
    /// </summary>
    public class PropertyTableRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public PropertyRowRenderer RowRenderer { get; set; }

        /// <summary>
        /// 프로퍼티 테이블 렌더링 (헤더 + 필터링된 행들)
        /// </summary>
        public void DrawTable(GroupScan g)
        {
            DrawTableHeader();

            int visible = 0;
            foreach (var r in g.rows)
            {
                if (!PassRowFilter(g, r)) continue;
                RowRenderer.DrawRow(g, r, visible);
                visible++;
            }

            if (visible == 0)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox("필터 조건에 맞는 프로퍼티가 없습니다.", MessageType.Info);
            }
        }

        /// <summary>
        /// 테이블 헤더 그리기
        /// </summary>
        private void DrawTableHeader()
        {
            var rect = GUILayoutUtility.GetRect(1, MaterialMergerStyles.RowHeaderHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color(1, 1, 1, 0.04f)
                : new Color(0, 0, 0, 0.04f));

            var cols = Utilities.GUIUtility.CalcColumnLayout(rect);
            float lineHeight = EditorGUIUtility.singleLineHeight;

            Rect CenterRect(Rect r, float h)
            {
                float y = r.y + (r.height - h) * 0.5f;
                return new Rect(r.x, y, r.width, h);
            }

            GUI.Label(CenterRect(cols.check, lineHeight), "", Styles.stMini);
            GUI.Label(CenterRect(cols.name, lineHeight), "프로퍼티", Styles.stMini);
            GUI.Label(CenterRect(cols.type, lineHeight), "타입", Styles.stMini);
            GUI.Label(CenterRect(cols.action, lineHeight), "액션", Styles.stMini);
            GUI.Label(CenterRect(cols.target, lineHeight), "대상", Styles.stMini);
            GUI.Label(CenterRect(cols.info, lineHeight), "정보", Styles.stMini);

            var line = new Rect(rect.x + 6, rect.yMax - 1, rect.width - 12, 1);
            EditorGUI.DrawRect(line, EditorGUIUtility.isProSkin
                ? new Color(1, 1, 1, 0.08f)
                : new Color(0, 0, 0, 0.12f));
        }

        /// <summary>
        /// 행 필터링 조건 확인
        /// </summary>
        private bool PassRowFilter(GroupScan g, Row r)
        {
            if (g.showTexturesOnly && r.type != ShaderUtil.ShaderPropertyType.TexEnv) return false;
            if (g.showScalarsOnly && r.type == ShaderUtil.ShaderPropertyType.TexEnv) return false;

            if (!string.IsNullOrEmpty(g.search))
            {
                var key = r.name ?? "";
                if (key.IndexOf(g.search, StringComparison.OrdinalIgnoreCase) < 0) return false;
            }

            if (!g.onlyRelevant) return true;

            if (r.type == ShaderUtil.ShaderPropertyType.TexEnv)
                return r.texNonNull > 0 || r.doAction;

            if (r.type == ShaderUtil.ShaderPropertyType.Color ||
                r.type == ShaderUtil.ShaderPropertyType.Float ||
                r.type == ShaderUtil.ShaderPropertyType.Range ||
                r.type == ShaderUtil.ShaderPropertyType.Vector)
                return r.distinctCount > 1 || r.doAction;

            return false;
        }
    }
}
#endif
