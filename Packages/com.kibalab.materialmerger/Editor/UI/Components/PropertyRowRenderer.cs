#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;
using K13A.MaterialMerger.Editor.Services.Localization;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 개별 프로퍼티 행을 렌더링하는 컴포넌트
    /// </summary>
    public class PropertyRowRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public ILocalizationService Localization { get; set; }

        /// <summary>
        /// 프로퍼티 행의 높이 계산
        /// </summary>
        public float GetRowHeight(GroupScan g, Row row)
        {
            if (!row.expanded) return Core.Constants.RowHeaderHeight;

            float h = Core.Constants.RowHeaderHeight;

            if (row.type == ShaderUtil.ShaderPropertyType.TexEnv)
                return h + 60f;

            if (!row.doAction)
                return h + 30f;

            h += 40f;

            if (row.type == ShaderUtil.ShaderPropertyType.Color)
                h += 20f;

            bool needsTarget =
                row.bakeMode == BakeMode.BakeColorToTexture ||
                row.bakeMode == BakeMode.BakeScalarToGrayscale ||
                row.bakeMode == BakeMode.MultiplyColorWithTexture;

            if (needsTarget)
                h += 60f;

            return Mathf.Max(140f, h + 40f);
        }

        /// <summary>
        /// 프로퍼티 행 렌더링
        /// </summary>
        public void DrawRow(GroupScan g, Row row, int visibleIndex)
        {
            float height = GetRowHeight(g, row);
            var rect = GUILayoutUtility.GetRect(1, height, GUILayout.ExpandWidth(true));

            // 배경색 (짝수/홀수)
            var bg = (visibleIndex % 2 == 0)
                ? (EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.02f) : new Color(0, 0, 0, 0.02f))
                : (EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.012f) : new Color(0, 0, 0, 0.012f));
            EditorGUI.DrawRect(rect, bg);

            // 활성화된 행 강조
            if (row.doAction)
            {
                var active = EditorGUIUtility.isProSkin
                    ? new Color(0.25f, 0.7f, 0.3f, 0.06f)
                    : new Color(0.2f, 0.6f, 0.25f, 0.08f);
                EditorGUI.DrawRect(rect, active);
            }

            // 호버 효과
            if (Event.current != null && rect.Contains(Event.current.mousePosition))
            {
                var hover = EditorGUIUtility.isProSkin
                    ? new Color(1f, 1f, 1f, 0.06f)
                    : new Color(0f, 0f, 0f, 0.06f);
                EditorGUI.DrawRect(rect, hover);
            }

            var top = new Rect(rect.x, rect.y, rect.width, Core.Constants.RowHeaderHeight);
            var cols = Utilities.GUIUtility.CalcColumnLayout(top);
            float lineHeight = EditorGUIUtility.singleLineHeight;

            // 체크박스
            row.doAction = EditorGUI.Toggle(CenterRect(cols.check, lineHeight), row.doAction);

            // 프로퍼티 이름
            EditorGUI.LabelField(CenterRect(cols.name, lineHeight), row.name ?? "");
            EditorGUI.LabelField(CenterRect(cols.type, lineHeight), TypeLabel(row.type), Styles.stMiniDim);

            // 텍스처 타입
            if (row.type == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                DrawTextureRow(row, cols, lineHeight, rect);
                return;
            }

            // 스칼라/색상 타입
            DrawNonTextureRow(g, row, cols, lineHeight, rect);
        }

        private void DrawTextureRow(Row row, ColumnLayout cols, float lineHeight, Rect rect)
        {
            using (new EditorGUI.DisabledScope(!row.doAction))
                EditorGUI.LabelField(CenterRect(cols.action, lineHeight), Localization.Get(L10nKey.TextureAtlas), Styles.stMiniDim);

            EditorGUI.LabelField(CenterRect(cols.target, lineHeight),
                row.isNormalLike ? Localization.Get(L10nKey.NormalMap) : (row.isSRGB ? Localization.Get(L10nKey.SRGB) : Localization.Get(L10nKey.Linear)),
                Styles.stMiniDim);

            DrawRowRightInfoAndMore(row, cols.info,
                row.texNonNull > 0 ? $"tex:{row.texDistinct} ST:{row.stDistinct}" : Localization.Get(L10nKey.Empty),
                row.texNonNull == 0);

            if (row.expanded)
                DrawRowExpanded_Texture(row, rect);
        }

        private void DrawNonTextureRow(GroupScan g, Row row, ColumnLayout cols, float lineHeight, Rect rect)
        {
            bool differs = row.distinctCount > 1;

            if (!row.doAction)
            {
                EditorGUI.LabelField(CenterRect(cols.action, lineHeight), Localization.Get(L10nKey.NotApplied), Styles.stMiniDim);
                EditorGUI.LabelField(CenterRect(cols.target, lineHeight), "-", Styles.stMiniDim);
            }
            else
            {
                var allowed = AllowedModesUI(row.type);
                if (allowed.Length == 0)
                {
                    EditorGUI.LabelField(CenterRect(cols.action, lineHeight), "-", Styles.stMiniDim);
                    EditorGUI.LabelField(CenterRect(cols.target, lineHeight), "-", Styles.stMiniDim);
                }
                else
                {
                    if (!allowed.Contains(row.bakeMode) || row.bakeMode == BakeMode.Keep)
                        row.bakeMode = allowed[0];

                    int idx = Array.IndexOf(allowed, row.bakeMode);
                    idx = Mathf.Max(0, idx);
                    idx = EditorGUI.Popup(CenterRect(cols.action, lineHeight), idx,
                        allowed.Select(ModeLabel).ToArray());
                    row.bakeMode = allowed[Mathf.Clamp(idx, 0, allowed.Length - 1)];

                    bool needsTarget =
                row.bakeMode == BakeMode.BakeColorToTexture ||
                row.bakeMode == BakeMode.BakeScalarToGrayscale ||
                row.bakeMode == BakeMode.MultiplyColorWithTexture;

                    if (needsTarget)
                    {
                        if (g.shaderTexProps.Count > 0)
                        {
                            row.targetTexIndex = Mathf.Clamp(row.targetTexIndex, 0, g.shaderTexProps.Count - 1);
                            row.targetTexIndex = EditorGUI.Popup(CenterRect(cols.target, lineHeight),
                                row.targetTexIndex, g.shaderTexProps.ToArray());
                            row.targetTexProp = g.shaderTexProps[Mathf.Clamp(row.targetTexIndex, 0,
                                g.shaderTexProps.Count - 1)];
                        }
                        else
                        {
                            EditorGUI.LabelField(CenterRect(cols.target, lineHeight), Localization.Get(L10nKey.NoTexEnv),
                                Styles.stMiniWarn);
                            row.targetTexProp = "";
                        }
                    }
                    else
                    {
                        EditorGUI.LabelField(CenterRect(cols.target, lineHeight), "-", Styles.stMiniDim);
                    }
                }
            }

            var unresolved = differs && !row.doAction;
            DrawRowRightInfoAndMore(row, cols.info, differs ? $"diff:{row.distinctCount}" : Localization.Get(L10nKey.Same), unresolved);

            if (row.expanded)
                DrawRowExpanded_NonTex(g, row, rect);
        }

        private void DrawRowRightInfoAndMore(Row row, Rect infoRect, string info, bool warn)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var btn = CenterRect(new Rect(infoRect.x, infoRect.y, 54, infoRect.height), lineHeight);
            if (GUI.Button(btn, row.expanded ? Localization.Get(L10nKey.Collapse) : Localization.Get(L10nKey.ShowMore), Styles.stRowMoreBtn))
                row.expanded = !row.expanded;

            var txt = CenterRect(new Rect(infoRect.x + 60, infoRect.y, infoRect.width - 60, infoRect.height),
                lineHeight);
            EditorGUI.LabelField(txt, info, warn ? Styles.stMiniWarn : Styles.stMiniDim);
        }

        private Rect CenterRect(Rect rect, float height)
        {
            float y = rect.y + (rect.height - height) * 0.5f;
            return new Rect(rect.x, y, rect.width, height);
        }

        private void DrawRowExpanded_Texture(Row row, Rect rect)
        {
            var area = new Rect(rect.x + 12, rect.y + 28, rect.width - 24, rect.height - 32);
            GUI.BeginGroup(area);

            float y = 0;
            GUI.Label(new Rect(0, y, area.width, 18), Localization.Get(L10nKey.TextureAtlasing), Styles.stSubTitle);
            y += 20;

            if (!row.doAction)
            {
                GUI.Label(new Rect(0, y, area.width, 18), Localization.Get(L10nKey.EnableCheckboxToInclude),
                    Styles.stMiniDim);
                GUI.EndGroup();
                return;
            }

            GUI.Label(new Rect(0, y, area.width, 18), Localization.Get(L10nKey.TextureWillBeIncluded), Styles.stMiniDim);
            y += 18;
            GUI.Label(new Rect(0, y, area.width, 18), Localization.Get(L10nKey.ColorSpaceAutoDetected), Styles.stMiniDim);

            GUI.EndGroup();
        }

        private void DrawRowExpanded_NonTex(GroupScan g, Row row, Rect rect)
        {
            var area = new Rect(rect.x + 12, rect.y + 28, rect.width - 24, rect.height - 32);
            GUI.BeginGroup(area);

            float y = 0;

            if (!row.doAction)
            {
                GUI.Label(new Rect(0, y, area.width, 18), Localization.Get(L10nKey.EnableCheckboxToApply),
                    Styles.stMiniDim);
                GUI.EndGroup();
                return;
            }

            row.resetSourceAfterBake = GUI.Toggle(new Rect(0, y, 260, 18), row.resetSourceAfterBake,
                Localization.Get(L10nKey.ResetAfterBake));
            y += 20;

            if (row.type == ShaderUtil.ShaderPropertyType.Color)
            {
                row.includeAlpha = GUI.Toggle(new Rect(0, y, 220, 18), row.includeAlpha, Localization.Get(L10nKey.IncludeAlpha));
                y += 20;
            }

            bool needsTarget =
                row.bakeMode == BakeMode.BakeColorToTexture ||
                row.bakeMode == BakeMode.BakeScalarToGrayscale ||
                row.bakeMode == BakeMode.MultiplyColorWithTexture;

            if (needsTarget)
            {
                GUI.Label(new Rect(0, y, area.width, 18), Localization.Get(L10nKey.ModifierOptional),
                    Styles.stMiniDim);
                y += 18;

                row.modOp = (ModOp)EditorGUI.EnumPopup(new Rect(0, y, 160, 18), row.modOp);

                var modList = new List<string> { Localization.Get(L10nKey.None) };
                modList.AddRange(g.shaderScalarProps);

                row.modPropIndex = Mathf.Clamp(row.modPropIndex, 0, modList.Count - 1);
                row.modPropIndex = EditorGUI.Popup(new Rect(170, y, 260, 18), row.modPropIndex, modList.ToArray());
                row.modProp = row.modPropIndex == 0 ? "" : modList[row.modPropIndex];
                y += 20;

                row.modClamp01 = GUI.Toggle(new Rect(0, y, 90, 18), row.modClamp01, Localization.Get(L10nKey.Clamp01));
                row.modScale = EditorGUI.FloatField(new Rect(100, y, 140, 18), row.modScale);
                row.modBias = EditorGUI.FloatField(new Rect(250, y, 140, 18), row.modBias);
                y += 20;

                if (row.type == ShaderUtil.ShaderPropertyType.Color)
                    row.modAffectsAlpha = GUI.Toggle(new Rect(0, y, 140, 18), row.modAffectsAlpha, Localization.Get(L10nKey.ApplyToAlpha));
            }
            else
            {
                GUI.Label(new Rect(0, y, area.width, 18), Localization.Get(L10nKey.BakeOptionsWhenSelected),
                    Styles.stMiniDim);
            }

            GUI.EndGroup();
        }

        // 헬퍼 메서드들
        private string TypeLabel(ShaderUtil.ShaderPropertyType t)
        {
            if (t == ShaderUtil.ShaderPropertyType.TexEnv) return Localization.Get(L10nKey.Texture);
            if (t == ShaderUtil.ShaderPropertyType.Color) return Localization.Get(L10nKey.Color);
            if (t == ShaderUtil.ShaderPropertyType.Range) return Localization.Get(L10nKey.Range);
            if (t == ShaderUtil.ShaderPropertyType.Float) return Localization.Get(L10nKey.Float);
            if (t == ShaderUtil.ShaderPropertyType.Vector) return Localization.Get(L10nKey.Vector);
            return t.ToString();
        }

        private BakeMode[] AllowedModesUI(ShaderUtil.ShaderPropertyType t)
        {
            if (t == ShaderUtil.ShaderPropertyType.Color)
                return new[] { BakeMode.ResetToDefault, BakeMode.BakeColorToTexture, BakeMode.MultiplyColorWithTexture };

            if (t == ShaderUtil.ShaderPropertyType.Float || t == ShaderUtil.ShaderPropertyType.Range)
                return new[] { BakeMode.ResetToDefault, BakeMode.BakeScalarToGrayscale };

            if (t == ShaderUtil.ShaderPropertyType.Vector)
                return new[] { BakeMode.ResetToDefault };

            return Array.Empty<BakeMode>();
        }

        private string ModeLabel(BakeMode m)
        {
            if (m == BakeMode.ResetToDefault) return Localization.Get(L10nKey.BakeModeReset);
            if (m == BakeMode.BakeColorToTexture) return Localization.Get(L10nKey.BakeModeColorBake);
            if (m == BakeMode.BakeScalarToGrayscale) return Localization.Get(L10nKey.BakeModeScalarBake);
            if (m == BakeMode.MultiplyColorWithTexture) return Localization.Get(L10nKey.BakeModeColorMultiply);
            if (m == BakeMode.Keep) return Localization.Get(L10nKey.BakeModeKeep);
            return m.ToString();
        }
    }
}
#endif
