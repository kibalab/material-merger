#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace K13A.MaterialMerger.Editor
{
    public class ConfirmWindow : EditorWindow
    {
        [Serializable]
        public class GroupInfo
        {
            public string title;
            public bool willRun;
            public string skipReason;
            public List<string> atlasProps = new List<string>();
            public List<string> generatedProps = new List<string>();
        }

        dynamic owner;
        List<GroupInfo> groups;
        Vector2 scroll;

        public static void Open(dynamic owner, List<GroupInfo> groups)
        {
            var w = CreateInstance<ConfirmWindow>();
            w.owner = owner;
            w.groups = groups ?? new List<GroupInfo>();
            w.titleContent = new GUIContent("빌드 확인");
            w.minSize = new Vector2(680, 520);
            w.ShowUtility();
        }

        void OnGUI()
        {
            if (owner == null)
            {
                EditorGUILayout.HelpBox("원본 창이 닫혀서 실행할 수 없습니다.", MessageType.Error);
                if (GUILayout.Button("닫기", GUILayout.Height(28))) Close();
                return;
            }

            var runCount = groups.Count(x => x.willRun);
            var skipCount = groups.Count - runCount;

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("아틀라스 빌드 & 적용 실행 전 확인", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.HelpBox($"실행 대상 Material Plan: {runCount} / 스킵: {skipCount}\n아래 프로퍼티 목록이 실제로 텍스처 아틀라싱/생성 대상입니다.", MessageType.Info);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var g in groups)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(g.title, EditorStyles.boldLabel);
                        GUILayout.FlexibleSpace();
                        var st = g.willRun ? "실행" : "스킵";
                        var c = GUI.color;
                        GUI.color = g.willRun ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.85f, 0.55f);
                        GUILayout.Label(st, EditorStyles.miniButton, GUILayout.Width(64));
                        GUI.color = c;
                    }

                    if (!g.willRun && !string.IsNullOrEmpty(g.skipReason))
                        EditorGUILayout.HelpBox(g.skipReason, MessageType.Warning);

                    EditorGUILayout.Space(2);

                    EditorGUILayout.LabelField($"아틀라싱 포함 TexEnv ({g.atlasProps.Count})", EditorStyles.miniBoldLabel);
                    if (g.atlasProps.Count == 0) EditorGUILayout.LabelField("(없음)", EditorStyles.miniLabel);
                    else EditorGUILayout.LabelField(string.Join(", ", g.atlasProps), EditorStyles.wordWrappedMiniLabel);

                    EditorGUILayout.Space(4);

                    EditorGUILayout.LabelField($"텍스처 생성/타겟 TexEnv ({g.generatedProps.Count})", EditorStyles.miniBoldLabel);
                    if (g.generatedProps.Count == 0) EditorGUILayout.LabelField("(없음)", EditorStyles.miniLabel);
                    else EditorGUILayout.LabelField(string.Join(", ", g.generatedProps), EditorStyles.wordWrappedMiniLabel);
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("취소", GUILayout.Height(32)))
                {
                    Close();
                    return;
                }

                using (new EditorGUI.DisabledScope(runCount == 0))
                {
                    if (GUILayout.Button("실행", GUILayout.Height(32)))
                    {
                        Close();
                        owner.BuildAndApply();
                    }
                }
            }
        }
    }
}
#endif
