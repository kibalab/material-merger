#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace K13A.MaterialMerger.Editor.Core
{
    /// <summary>
    /// MaterialMerger 윈도우의 GUI 스타일을 관리하는 클래스
    /// </summary>
    public class MaterialMergerStyles
    {
        // GUI 스타일
        public GUIStyle stTitle;
        public GUIStyle stSubTitle;
        public GUIStyle stPill;
        public GUIStyle stPillWarn;
        public GUIStyle stMini;
        public GUIStyle stMiniDim;
        public GUIStyle stMiniWarn;
        public GUIStyle stToolbar;
        public GUIStyle stToolbarBtn;
        public GUIStyle stRowMoreBtn;
        public GUIStyle stBox;
        public GUIStyle stBigBtn;
        public GUIStyle stSection;

        // 스타일 상태
        private bool stylesReady;
        private bool lastProSkin;

        // 상수
        public const float TopLabelWidth = 90f;
        public const float RowHeaderHeight = 24f;

        /// <summary>
        /// 스타일이 초기화되지 않았거나 테마가 변경되었으면 스타일을 다시 생성
        /// </summary>
        public void EnsureStyles()
        {
            bool anyNull =
                stTitle == null || stSubTitle == null || stPill == null || stPillWarn == null || stMini == null ||
                stMiniDim == null || stMiniWarn == null || stToolbar == null || stToolbarBtn == null ||
                stRowMoreBtn == null || stBox == null || stBigBtn == null || stSection == null;

            if (!anyNull && stylesReady && lastProSkin == EditorGUIUtility.isProSkin) return;

            lastProSkin = EditorGUIUtility.isProSkin;

            stBox = new GUIStyle(EditorStyles.helpBox);

            stTitle = new GUIStyle(EditorStyles.boldLabel);
            stTitle.fontSize = 14;

            stSubTitle = new GUIStyle(EditorStyles.boldLabel);
            stSubTitle.fontSize = 11;

            stSection = new GUIStyle(EditorStyles.boldLabel);
            stSection.fontSize = 10;
            stSection.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.90f, 0.90f, 0.90f, 1f)
                : new Color(0.20f, 0.20f, 0.20f, 1f);

            stMini = new GUIStyle(EditorStyles.miniLabel);
            stMini.alignment = TextAnchor.MiddleLeft;

            stMiniDim = new GUIStyle(EditorStyles.miniLabel);
            stMiniDim.alignment = TextAnchor.MiddleLeft;
            stMiniDim.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.75f, 0.75f, 0.75f, 1f)
                : new Color(0.35f, 0.35f, 0.35f, 1f);

            stMiniWarn = new GUIStyle(EditorStyles.miniLabel);
            stMiniWarn.alignment = TextAnchor.MiddleLeft;
            stMiniWarn.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.78f, 0.35f, 1f)
                : new Color(0.75f, 0.45f, 0.05f, 1f);

            stPill = new GUIStyle(EditorStyles.miniButton);
            stPill.padding = new RectOffset(8, 8, 2, 2);
            stPillWarn = new GUIStyle(stPill);

            stToolbar = new GUIStyle(EditorStyles.toolbar);

            stToolbarBtn = new GUIStyle(EditorStyles.toolbarButton);
            stToolbarBtn.fixedHeight = 18;

            stRowMoreBtn = new GUIStyle(EditorStyles.miniButton);
            stRowMoreBtn.fixedHeight = 18;

            var baseBtn = (GUI.skin != null && GUI.skin.button != null)
                ? GUI.skin.button
                : EditorStyles.miniButton;
            stBigBtn = new GUIStyle(baseBtn);
            stBigBtn.fontSize = 12;
            stBigBtn.fontStyle = FontStyle.Bold;

            stylesReady = true;
        }
    }
}
#endif
