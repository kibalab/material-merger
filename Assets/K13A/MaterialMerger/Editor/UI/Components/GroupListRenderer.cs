#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 그룹 목록 (툴바 + 스크롤뷰 + 그룹들)을 렌더링하는 컴포넌트
    /// </summary>
    public class GroupListRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public GroupPanelRenderer GroupRenderer { get; set; }
        public ILocalizationService Localization { get; set; }

        /// <summary>
        /// 그룹 목록 렌더링
        /// </summary>
        public void DrawGroupList(List<GroupScan> scans, ref Vector2 scroll)
        {
            if (scans == null || scans.Count == 0)
            {
                using (new EditorGUILayout.VerticalScope(Styles.stBox))
                    EditorGUILayout.HelpBox(Localization.Get(L10nKey.NoScanMessage), MessageType.Info);
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
                GUILayout.Label(Localization.Get(L10nKey.PlanList), Styles.stSubTitle);
                GUILayout.FlexibleSpace();

                var expandContent = new GUIContent(Localization.Get(L10nKey.ExpandAll),
                    Localization.Get(L10nKey.ExpandAllTooltip));
                if (GUILayout.Button(expandContent, Styles.stToolbarBtn, GUILayout.Width(95)))
                    foreach (var g in scans)
                        g.foldout = true;

                var collapseContent = new GUIContent(Localization.Get(L10nKey.CollapseAll),
                    Localization.Get(L10nKey.CollapseAllTooltip));
                if (GUILayout.Button(collapseContent, Styles.stToolbarBtn, GUILayout.Width(95)))
                    foreach (var g in scans)
                        g.foldout = false;

                var enableContent = new GUIContent(Localization.Get(L10nKey.EnableAll),
                    Localization.Get(L10nKey.EnableAllTooltip));
                if (GUILayout.Button(enableContent, Styles.stToolbarBtn, GUILayout.Width(90)))
                    foreach (var g in scans)
                        g.enabled = true;

                var disableContent = new GUIContent(Localization.Get(L10nKey.DisableAll),
                    Localization.Get(L10nKey.DisableAllTooltip));
                if (GUILayout.Button(disableContent, Styles.stToolbarBtn, GUILayout.Width(90)))
                    foreach (var g in scans)
                        g.enabled = false;
            }
        }
    }
}
#endif
