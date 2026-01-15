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
        private Texture2D tagBg;
        private Texture2D tagWarnBg;

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

            if (tagBg) Object.DestroyImmediate(tagBg);
            if (tagWarnBg) Object.DestroyImmediate(tagWarnBg);

            tagBg = CreateSolidTexture(EditorGUIUtility.isProSkin
                ? new Color(1f, 1f, 1f, 0.08f)
                : new Color(0f, 0f, 0f, 0.08f));
            tagWarnBg = CreateSolidTexture(EditorGUIUtility.isProSkin
                ? new Color(1f, 0.6f, 0.1f, 0.2f)
                : new Color(1f, 0.6f, 0.1f, 0.18f));

            stPill = new GUIStyle(EditorStyles.miniLabel);
            stPill.alignment = TextAnchor.MiddleCenter;
            stPill.padding = new RectOffset(8, 8, 2, 2);
            stPill.margin = new RectOffset(4, 4, 2, 2);
            stPill.fixedHeight = 18;
            stPill.normal.background = tagBg;
            stPill.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(0.86f, 0.86f, 0.86f, 1f)
                : new Color(0.2f, 0.2f, 0.2f, 1f);

            stPillWarn = new GUIStyle(stPill);
            stPillWarn.normal.background = tagWarnBg;
            stPillWarn.normal.textColor = EditorGUIUtility.isProSkin
                ? new Color(1f, 0.85f, 0.45f, 1f)
                : new Color(0.55f, 0.25f, 0.02f, 1f);

            stToolbar = new GUIStyle(EditorStyles.toolbar);

            stToolbarBtn = new GUIStyle(EditorStyles.toolbarButton);
            stToolbarBtn.fixedHeight = 18;
            stToolbarBtn.imagePosition = ImagePosition.ImageLeft;

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

        private Texture2D CreateSolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
#endif
