#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 그룹 목록 (툴바 + 스크롤뷰 + 그룹들)을 렌더링하는 컴포넌트
    /// </summary>
    public class GroupListRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public GroupPanelRenderer GroupRenderer { get; set; }

        /// <summary>
        /// 그룹 목록 렌더링
        /// </summary>
        public void DrawGroupList(List<GroupScan> scans, ref Vector2 scroll)
        {
            if (scans == null || scans.Count == 0)
            {
                using (new EditorGUILayout.VerticalScope(Styles.stBox))
                    EditorGUILayout.HelpBox("루트를 지정한 뒤 스캔을 실행하세요.", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawGroupListToolbar(scans);

            for (int gi = 0; gi < scans.Count; gi++)
                GroupRenderer.DrawGroup(scans[gi], gi);

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 그룹 목록 툴바 (전체 펼치기/접기/활성/비활성)
        /// </summary>
        private void DrawGroupListToolbar(List<GroupScan> scans)
        {
            using (new EditorGUILayout.HorizontalScope(Styles.stBox))
            {
                GUILayout.Label("계획 목록", Styles.stSubTitle);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("전체 펼치기", Styles.stToolbarBtn, GUILayout.Width(95)))
                    foreach (var g in scans)
                        g.foldout = true;

                if (GUILayout.Button("전체 접기", Styles.stToolbarBtn, GUILayout.Width(95)))
                    foreach (var g in scans)
                        g.foldout = false;

                if (GUILayout.Button("전체 활성", Styles.stToolbarBtn, GUILayout.Width(90)))
                    foreach (var g in scans)
                        g.enabled = true;

                if (GUILayout.Button("전체 비활성", Styles.stToolbarBtn, GUILayout.Width(90)))
                    foreach (var g in scans)
                        g.enabled = false;
            }
        }
    }
}
#endif
