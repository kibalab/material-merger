#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.UI.Utilities
{
    /// <summary>
    /// MaterialMerger UI에서 공통적으로 사용되는 GUI 유틸리티 메서드
    /// </summary>
    public static class GUIUtility
    {
        /// <summary>
        /// 수평 구분선 그리기
        /// </summary>
        public static void DrawSeparator()
        {
            var r = GUILayoutUtility.GetRect(1, 8, GUILayout.ExpandWidth(true));
            var line = new Rect(r.x + 6, r.y + 4, r.width - 12, 1);
            EditorGUI.DrawRect(line, EditorGUIUtility.isProSkin
                ? new Color(1, 1, 1, 0.08f)
                : new Color(0, 0, 0, 0.15f));
        }

        /// <summary>
        /// 섹션 제목 그리기
        /// </summary>
        public static void DrawSection(string title, GUIStyle style)
        {
            EditorGUILayout.Space(2);
            GUILayout.Label(title, style);
            EditorGUILayout.Space(2);
        }

        /// <summary>
        /// 작은 레이블 필 그리기 (경고 표시 가능)
        /// </summary>
        public static void DrawPill(string text, bool warn, GUIStyle normalStyle, GUIStyle warnStyle)
        {
            GUILayout.Label(text, warn ? warnStyle : normalStyle, GUILayout.Height(18));
        }

        /// <summary>
        /// 검색 필드 그리기 (취소 버튼 포함)
        /// </summary>
        public static string DrawSearchField(string value, float width, float height)
        {
            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width));
            var textStyle = GUI.skin != null
                ? (GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.textField)
                : EditorStyles.textField;
            var cancelStyle = GUI.skin != null
                ? (GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.miniButton)
                : EditorStyles.miniButton;
            var emptyCancelStyle = GUI.skin != null
                ? (GUI.skin.FindStyle("ToolbarSearchCancelButtonEmpty") ?? EditorStyles.miniButton)
                : EditorStyles.miniButton;

            var textRect = new Rect(rect.x, rect.y + 3, rect.width - 18, rect.height);
            var btnRect = new Rect(rect.x + rect.width - 18, rect.y, 18, rect.height);

            value = EditorGUI.TextField(textRect, value ?? "", textStyle);

            if (string.IsNullOrEmpty(value))
            {
                GUI.Button(btnRect, GUIContent.none, emptyCancelStyle);
            }
            else
            {
                if (GUI.Button(btnRect, GUIContent.none, cancelStyle))
                    value = "";
            }

            return value;
        }

        /// <summary>
        /// 아이콘 텍스처 가져오기 (라이트/다크 테마 대응)
        /// </summary>
        public static Texture GetIconTexture(string lightName, string darkName)
        {
            var iconName = EditorGUIUtility.isProSkin && !string.IsNullOrEmpty(darkName)
                ? darkName
                : lightName;
            if (string.IsNullOrEmpty(iconName)) return null;
            var content = EditorGUIUtility.IconContent(iconName);
            return content != null ? content.image : null;
        }

        /// <summary>
        /// 아이콘이 포함된 GUIContent 생성
        /// </summary>
        public static GUIContent MakeIconContent(string text, string lightName, string darkName, string tooltip)
        {
            var img = GetIconTexture(lightName, darkName);
            return new GUIContent(text, img, tooltip ?? "");
        }

        /// <summary>
        /// 마지막 스캔 시각 레이블 생성
        /// </summary>
        public static string GetLastScanLabel(MaterialMergeProfile profile)
        {
            if (!profile || profile.lastScanTicksUtc <= 0) return "(없음)";
            var dt = new DateTime(profile.lastScanTicksUtc, DateTimeKind.Utc).ToLocalTime();
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// GroupScan의 셰이더 이름 가져오기
        /// </summary>
        public static string GetGroupShaderName(GroupScan g)
        {
            if (g == null) return "NULL_SHADER";
            if (g.key.shader) return g.key.shader.name;
            if (!string.IsNullOrEmpty(g.shaderName)) return g.shaderName;
            return "NULL_SHADER";
        }

        /// <summary>
        /// 프로퍼티 테이블의 컬럼 레이아웃 계산
        /// </summary>
        public static ColumnLayout CalcColumnLayout(Rect rect)
        {
            float pad = 6;
            rect = new Rect(rect.x + pad, rect.y, rect.width - pad * 2, rect.height);

            float wCheck = 18;
            float wType = 82;
            float wAction = 220;
            float wTarget = 240;
            float wInfo = 190;

            float minName = 240;
            float remain = rect.width - (wCheck + wType + wAction + wTarget + wInfo);
            float wName = Mathf.Max(minName, remain);

            float x = rect.x;
            var c = new ColumnLayout();
            c.check = new Rect(x, rect.y, wCheck, rect.height);
            x += wCheck;
            c.name = new Rect(x, rect.y, wName, rect.height);
            x += wName;
            c.type = new Rect(x, rect.y, wType, rect.height);
            x += wType;
            c.action = new Rect(x, rect.y, wAction, rect.height);
            x += wAction;
            c.target = new Rect(x, rect.y, wTarget, rect.height);
            x += wTarget;
            c.info = new Rect(x, rect.y, wInfo, rect.height);
            return c;
        }
    }
}
#endif
