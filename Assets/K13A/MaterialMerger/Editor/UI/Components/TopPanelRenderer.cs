#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 최상단 패널 (제목, 버튼, 루트 선택 등)을 렌더링하는 컴포넌트
    /// </summary>
    public class TopPanelRenderer
    {
        public MaterialMergerStyles Styles { get; set; }

        /// <summary>
        /// 최상단 패널 렌더링
        /// </summary>
        public void DrawTopPanel(
            MaterialMergerState state,
            Action onScanClicked,
            Action onBuildClicked,
            Action<GameObject> onRootChanged,
            Action<string> onOutputFolderChanged)
        {
            using (new EditorGUILayout.VerticalScope(Styles.stBox))
            {
                DrawTitleAndButtons(state, onScanClicked, onBuildClicked);
                EditorGUILayout.Space(6);
                DrawRootField(state, onRootChanged);
                DrawLastScanLabel(state);
                EditorGUILayout.Space(4);
                DrawOutputFolderField(state, onOutputFolderChanged);
            }
        }

        /// <summary>
        /// 제목 및 버튼 (스캔, 빌드)
        /// </summary>
        private void DrawTitleAndButtons(MaterialMergerState state, Action onScanClicked, Action onBuildClicked)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("멀티 아틀라스 머저", Styles.stTitle);
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!state.root))
                {
                    var scanContent = Utilities.GUIUtility.MakeIconContent("스캔", "Refresh", "d_Refresh", "스캔");
                    if (GUILayout.Button(scanContent, Styles.stBigBtn, GUILayout.Width(140), GUILayout.Height(32)))
                        onScanClicked?.Invoke();
                }

                using (new EditorGUI.DisabledScope(state.scans == null || state.scans.Count == 0 ||
                                                     (state.cloneRootOnApply && !state.root)))
                {
                    var buildContent = Utilities.GUIUtility.MakeIconContent("빌드 & 적용", "PlayButton", "d_PlayButton",
                        "빌드 & 적용");
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

            EditorGUI.LabelField(rootLabelRect, "루트");
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

            var scanLabelContent = new GUIContent("마지막 스캔");
            EditorGUI.LabelField(scanLabelRect, scanLabelContent, Styles.stMiniDim);
            EditorGUI.LabelField(scanValueRect, Utilities.GUIUtility.GetLastScanLabel(state.profile),
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

            if (GUI.Button(outBtnRect, Utilities.GUIUtility.MakeIconContent("출력 폴더", "Folder Icon",
                    "d_Folder Icon", "출력 폴더"), Styles.stToolbarBtn))
            {
                var picked = EditorUtility.OpenFolderPanel("출력 폴더 선택", Application.dataPath, "");
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
                        EditorUtility.DisplayDialog("출력 폴더", "Assets 폴더 내부만 가능합니다.", "OK");
                    }
                }
            }

            EditorGUI.LabelField(outPathRect, state.outputFolder, Styles.stMiniDim);
        }
    }
}
#endif
