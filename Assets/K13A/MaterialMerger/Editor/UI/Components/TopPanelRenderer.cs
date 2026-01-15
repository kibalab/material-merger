#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 최상단 패널 (제목, 버튼, 루트 선택 등)을 렌더링하는 컴포넌트
    /// </summary>
    public class TopPanelRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public ILocalizationService Localization { get; set; }

        /// <summary>
        /// 최상단 패널 렌더링
        /// </summary>
        public void DrawTopPanel(
            MaterialMergerState state,
            Action onScanClicked,
            Action onBuildClicked,
            Action<GameObject> onRootChanged,
            Action<string> onOutputFolderChanged,
            Action<Language> onLanguageChanged)
        {
            using (new EditorGUILayout.VerticalScope(Styles.stBox))
            {
                DrawTitleAndButtons(state, onScanClicked, onBuildClicked);
                EditorGUILayout.Space(6);
                DrawRootField(state, onRootChanged);
                DrawLastScanLabel(state);
                EditorGUILayout.Space(4);
                DrawOutputFolderField(state, onOutputFolderChanged);
                DrawLanguageField(onLanguageChanged);
            }
        }

        /// <summary>
        /// 제목 및 버튼 (스캔, 빌드)
        /// </summary>
        private void DrawTitleAndButtons(MaterialMergerState state, Action onScanClicked, Action onBuildClicked)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(Localization.Get(L10nKey.WindowTitle), Styles.stTitle);
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!state.root))
                {
                    var scanText = Localization.Get(L10nKey.Scan);
                    var scanContent = Utilities.GUIUtility.MakeIconContent(scanText, "Refresh", "d_Refresh",
                        "Scan the selected root and build material plans.");
                    if (GUILayout.Button(scanContent, Styles.stBigBtn, GUILayout.Width(140), GUILayout.Height(32)))
                        onScanClicked?.Invoke();
                }

                using (new EditorGUI.DisabledScope(state.scans == null || state.scans.Count == 0 ||
                                                     (state.cloneRootOnApply && !state.root)))
                {
                    var buildText = Localization.Get(L10nKey.BuildAndApply);
                    var buildContent = Utilities.GUIUtility.MakeIconContent(buildText, "PlayButton", "d_PlayButton",
                        "Generate atlases and apply merged materials to meshes.");
                    if (GUILayout.Button(buildContent, Styles.stBigBtn, GUILayout.Width(180),
                            GUILayout.Height(32)))
                        onBuildClicked?.Invoke();
                }
            }
        }

        /// <summary>
        /// 루트 GameObject 필드
        /// </summary>
        private void DrawRootField(MaterialMergerState state, Action<GameObject> onRootChanged)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var rootRect = EditorGUILayout.GetControlRect(false, lineHeight);
            var rootLabelRect = new Rect(rootRect.x, rootRect.y, MaterialMergerStyles.TopLabelWidth, rootRect.height);
            var rootFieldRect = new Rect(rootLabelRect.xMax + 6, rootRect.y,
                rootRect.width - MaterialMergerStyles.TopLabelWidth - 6, rootRect.height);

            var rootLabel = new GUIContent(Localization.Get(L10nKey.Root), "Root GameObject to scan and apply.");
            EditorGUI.LabelField(rootLabelRect, rootLabel);
            EditorGUI.BeginChangeCheck();
            var newRoot = (GameObject)EditorGUI.ObjectField(rootFieldRect, state.root, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck())
                onRootChanged?.Invoke(newRoot);
        }

        /// <summary>
        /// 마지막 스캔 시각 레이블
        /// </summary>
        private void DrawLastScanLabel(MaterialMergerState state)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var scanRect = EditorGUILayout.GetControlRect(false, lineHeight);
            var scanLabelRect = new Rect(scanRect.x, scanRect.y, MaterialMergerStyles.TopLabelWidth,
                scanRect.height);
            var scanValueRect = new Rect(scanLabelRect.xMax + 6, scanRect.y,
                scanRect.width - MaterialMergerStyles.TopLabelWidth - 6, scanRect.height);

            var scanLabelContent = new GUIContent(Localization.Get(L10nKey.LastScan),
                "Time of the last scan saved to the profile.");
            EditorGUI.LabelField(scanLabelRect, scanLabelContent, Styles.stMiniDim);
            EditorGUI.LabelField(scanValueRect, Utilities.GUIUtility.GetLastScanLabel(state.profile, Localization),
                Styles.stMiniDim);
        }

        /// <summary>
        /// 출력 폴더 선택 필드
        /// </summary>
        private void DrawOutputFolderField(MaterialMergerState state, Action<string> onOutputFolderChanged)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var outRect = EditorGUILayout.GetControlRect(false, lineHeight);
            var outFieldRect = new Rect(outRect.x, outRect.y,
                outRect.width - MaterialMergerStyles.TopLabelWidth - 6, outRect.height);

            float buttonWidth = 90f;
            var outBtnRect = new Rect(outFieldRect.x, outFieldRect.y, buttonWidth, outFieldRect.height);
            var outPathRect = new Rect(outBtnRect.xMax + 6, outFieldRect.y, outFieldRect.width - buttonWidth - 6,
                outFieldRect.height);

            var outputFolderText = Localization.Get(L10nKey.OutputFolder);
            if (GUI.Button(outBtnRect, Utilities.GUIUtility.MakeIconContent(outputFolderText, "Folder Icon",
                    "d_Folder Icon", "Choose a folder under Assets to save generated files."), Styles.stToolbarBtn))
            {
                var picked = EditorUtility.OpenFolderPanel(Localization.Get(L10nKey.DialogOutputFolderTitle), Application.dataPath, "");
                if (!string.IsNullOrEmpty(picked))
                {
                    picked = picked.Replace("\\", "/");
                    if (picked.Contains("/Assets/"))
                    {
                        var newFolder = "Assets/" + picked.Split(new[] { "/Assets/" }, StringSplitOptions.None)[1];
                        onOutputFolderChanged?.Invoke(newFolder);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog(Localization.Get(L10nKey.OutputFolder),
                            Localization.Get(L10nKey.DialogOutputFolderError), "OK");
                    }
                }
            }

            EditorGUI.LabelField(outPathRect, state.outputFolder, Styles.stMiniDim);
        }

        private void DrawLanguageField(Action<Language> onLanguageChanged)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var langRect = EditorGUILayout.GetControlRect(false, lineHeight);
            var langLabelRect = new Rect(langRect.x, langRect.y, MaterialMergerStyles.TopLabelWidth, langRect.height);
            var langFieldRect = new Rect(langLabelRect.xMax + 6, langRect.y,
                langRect.width - MaterialMergerStyles.TopLabelWidth - 6, langRect.height);

            var label = new GUIContent(Localization.Get(L10nKey.LanguageSettings), "Change UI language.");
            EditorGUI.LabelField(langLabelRect, label);

            var options = new[]
            {
                new GUIContent(Localization.Get(L10nKey.LanguageKorean)),
                new GUIContent(Localization.Get(L10nKey.LanguageEnglish)),
                new GUIContent(Localization.Get(L10nKey.LanguageJapanese))
            };

            int current = (int)Localization.CurrentLanguage;
            int next = EditorGUI.Popup(langFieldRect, current, options);
            if (next != current && onLanguageChanged != null)
                onLanguageChanged((Language)next);
        }
    }
}
#endif
