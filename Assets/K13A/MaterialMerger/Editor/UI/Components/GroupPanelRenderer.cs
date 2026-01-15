#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 개별 그룹 패널을 렌더링하는 컴포넌트
    /// </summary>
    public class GroupPanelRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public PropertyTableRenderer TableRenderer { get; set; }
        public ILocalizationService Localization { get; set; }
        private readonly System.Collections.Generic.Dictionary<GroupKey, Rect> materialsButtonRects =
            new System.Collections.Generic.Dictionary<GroupKey, Rect>();

        /// <summary>
        /// 그룹 패널 렌더링
        /// </summary>
        public void DrawGroup(
            GroupScan g,
            int index,
            bool isMergeChild,
            int mergeChildCount,
            Action<GroupScan, Rect> registerHeaderRect,
            Action<GroupScan, Rect> registerHandleRect)
        {
            using (new EditorGUILayout.VerticalScope(Styles.stBox))
            {
                DrawGroupHeader(g, isMergeChild, mergeChildCount, registerHeaderRect, registerHandleRect);

                if (g.foldout)
                {
                    using (new EditorGUI.DisabledScope(isMergeChild))
                    {
                        DrawGroupToolbar(g);
                        EditorGUILayout.Space(4);
                        TableRenderer.DrawTable(g);
                    }
                }
            }
        }

        /// <summary>
        /// 그룹 헤더 (체크박스, 폴드아웃, 이름, 통계)
        /// </summary>
        private void DrawGroupHeader(
            GroupScan g,
            bool isMergeChild,
            int mergeChildCount,
            Action<GroupScan, Rect> registerHeaderRect,
            Action<GroupScan, Rect> registerHandleRect)
        {
            var shaderName = Utilities.GUIUtility.GetGroupShaderName(g);
            bool isSingleMaterial = g.mats.Count == 1;
            if (string.IsNullOrEmpty(g.outputMaterialName))
                g.outputMaterialName = shaderName;

            using (new EditorGUILayout.HorizontalScope())
            {
                var dragContent = new GUIContent("::", Localization.Get(L10nKey.PlanDragHandleTooltip));
                GUILayout.Label(dragContent, Styles.stToolbarBtn, GUILayout.Width(18), GUILayout.Height(18));
                var handleRect = GUILayoutUtility.GetLastRect();
                if (Event.current.type == EventType.Repaint)
                    registerHandleRect?.Invoke(g, handleRect);

                // 단일 머티리얼은 체크박스 비활성화 (병합할 필요 없음)
                using (new EditorGUI.DisabledScope(isSingleMaterial || isMergeChild))
                {
                    g.enabled = EditorGUILayout.Toggle(g.enabled, GUILayout.Width(16));
                }

                if (isSingleMaterial)
                {
                    g.foldout = false;
                    GUILayout.Space(12);
                }
                else
                {
                    var foldRect = GUILayoutUtility.GetRect(12f, EditorGUIUtility.singleLineHeight, GUILayout.Width(12f));
                    g.foldout = EditorGUI.Foldout(foldRect, g.foldout, GUIContent.none, true);
                }

                using (new EditorGUI.DisabledScope(isMergeChild))
                {
                    var fieldRect = GUILayoutUtility.GetRect(160f, EditorGUIUtility.singleLineHeight,
                        GUILayout.MinWidth(160), GUILayout.ExpandWidth(true));
                    g.outputMaterialName = EditorGUI.TextField(fieldRect, g.outputMaterialName);
                    if (Event.current.type == EventType.Repaint)
                        GUI.Label(fieldRect, new GUIContent("", Localization.Get(L10nKey.OutputMaterialNameTooltip)));
                }

                if (!string.IsNullOrEmpty(g.tag))
                    GUILayout.Label($"[{g.tag}]", Styles.stMiniDim, GUILayout.Width(50));

                GUILayout.FlexibleSpace();

                // 단일 머티리얼이면 특별 표시
                if (isSingleMaterial)
                {
                    var c = GUI.color;
                    GUI.color = new Color(0.7f, 0.7f, 0.7f);
                    Utilities.GUIUtility.DrawPill(Localization.Get(L10nKey.SingleMaterial), false,
                        Styles.stPill, Styles.stPillWarn, Localization.Get(L10nKey.SingleMaterialTooltip));
                    GUI.color = c;
                }
                else
                {
                    Utilities.GUIUtility.DrawPill(Localization.Get(L10nKey.Page, g.pageCount), false,
                        Styles.stPill, Styles.stPillWarn, Localization.Get(L10nKey.PageTooltip));
                }

                if (g.skippedMultiMat > 0)
                    Utilities.GUIUtility.DrawPill(Localization.Get(L10nKey.Skip, g.skippedMultiMat), true,
                        Styles.stPill, Styles.stPillWarn, Localization.Get(L10nKey.MultiMatTooltip));

                if (mergeChildCount > 0)
                    Utilities.GUIUtility.DrawPill(Localization.Get(L10nKey.MergedTag, mergeChildCount), false,
                        Styles.stPill, Styles.stPillWarn, Localization.Get(L10nKey.MergedTagTooltip));

                using (new EditorGUI.DisabledScope(g.mats == null || g.mats.Count == 0))
                {
                    var materialsContent = Utilities.GUIUtility.MakeIconContent(
                        Localization.Get(L10nKey.PlanMaterials), "Material Icon", "d_Material Icon",
                        Localization.Get(L10nKey.PlanMaterialsTooltip));
                    bool clicked = GUILayout.Button(materialsContent, Styles.stToolbarBtn, GUILayout.Width(120), GUILayout.Height(18));
                    var buttonRect = GUILayoutUtility.GetLastRect();
                    if (Event.current.type == EventType.Repaint && buttonRect.width > 0f && buttonRect.height > 0f)
                        materialsButtonRects[g.key] = buttonRect;
                    if (clicked)
                    {
                        if (!materialsButtonRects.TryGetValue(g.key, out var anchorRect) || anchorRect.width <= 0f)
                        {
                            var mouse = Event.current.mousePosition;
                            anchorRect = new Rect(mouse.x, mouse.y, 1f, 1f);
                        }
                        MaterialGridPopup.Show(g, Localization, anchorRect);
                    }
                }
            }

            var headerRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
                headerRect = new Rect(0f, headerRect.y, EditorGUIUtility.currentViewWidth, headerRect.height);
                registerHeaderRect?.Invoke(g, headerRect);
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

                var relevantContent = Utilities.GUIUtility.MakeIconContent(
                    Localization.Get(L10nKey.RelevantOnly), "FilterByLabel", "d_FilterByLabel",
                    Localization.Get(L10nKey.FilterRelevantTooltip));
                g.onlyRelevant = GUILayout.Toggle(g.onlyRelevant, relevantContent,
                    Styles.stToolbarBtn, GUILayout.Width(100), GUILayout.Height(lineHeight));

                var texturesContent = Utilities.GUIUtility.MakeIconContent(
                    Localization.Get(L10nKey.TexturesOnly), "Texture Icon", "d_Texture Icon",
                    Localization.Get(L10nKey.FilterTexturesTooltip));
                g.showTexturesOnly = GUILayout.Toggle(g.showTexturesOnly, texturesContent,
                    Styles.stToolbarBtn, GUILayout.Width(110), GUILayout.Height(lineHeight));

                var scalarsContent = Utilities.GUIUtility.MakeIconContent(
                    Localization.Get(L10nKey.ScalarsOnly), "ScaleTool", "d_ScaleTool",
                    Localization.Get(L10nKey.FilterScalarsTooltip));
                g.showScalarsOnly = GUILayout.Toggle(g.showScalarsOnly, scalarsContent,
                    Styles.stToolbarBtn, GUILayout.Width(100), GUILayout.Height(lineHeight));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(Localization.Get(L10nKey.EnableAllTextureAtlas), Styles.stToolbarBtn,
                        GUILayout.Width(170), GUILayout.Height(lineHeight)))
                    SetAllTexActions(g, true);

                if (GUILayout.Button(Localization.Get(L10nKey.DisableAllTextureAtlas), Styles.stToolbarBtn,
                        GUILayout.Width(170), GUILayout.Height(lineHeight)))
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
