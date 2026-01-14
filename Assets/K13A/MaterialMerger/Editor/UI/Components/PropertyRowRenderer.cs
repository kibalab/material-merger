#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 개별 프로퍼티 행을 렌더링하는 컴포넌트
    /// </summary>
    public class PropertyRowRenderer
    {
        public MaterialMergerStyles Styles { get; set; }

        /// <summary>
        /// 프로퍼티 행의 높이 계산
        /// </summary>
        public float GetRowHeight(GroupScan g, Row row)
        {
            if (!row.expanded) return MaterialMergerStyles.RowHeaderHeight;

            float h = MaterialMergerStyles.RowHeaderHeight;

            if (row.type == ShaderUtil.ShaderPropertyType.TexEnv)
                return h + 60f;

            if (!row.doAction)
                return h + 30f;

            h += 40f;

            if (row.type == ShaderUtil.ShaderPropertyType.Color)
                h += 20f;

            bool needsTarget =
                row.bakeMode == BakeMode.색상굽기_텍스처타일 ||
                row.bakeMode == BakeMode.스칼라굽기_그레이타일 ||
                row.bakeMode == BakeMode.색상곱_텍스처타일;

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

            var top = new Rect(rect.x, rect.y, rect.width, MaterialMergerStyles.RowHeaderHeight);
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
                EditorGUI.LabelField(CenterRect(cols.action, lineHeight), "텍스처 아틀라스", Styles.stMiniDim);

            EditorGUI.LabelField(CenterRect(cols.target, lineHeight),
                row.isNormalLike ? "노말맵" : (row.isSRGB ? "sRGB" : "Linear"),
                Styles.stMiniDim);

            DrawRowRightInfoAndMore(row, cols.info,
                row.texNonNull > 0 ? $"tex:{row.texDistinct} ST:{row.stDistinct}" : "비어있음",
                row.texNonNull == 0);

            if (row.expanded)
                DrawRowExpanded_Texture(row, rect);
        }

        private void DrawNonTextureRow(GroupScan g, Row row, ColumnLayout cols, float lineHeight, Rect rect)
        {
            bool differs = row.distinctCount > 1;

            if (!row.doAction)
            {
                EditorGUI.LabelField(CenterRect(cols.action, lineHeight), "미적용", Styles.stMiniDim);
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
                    if (!allowed.Contains(row.bakeMode) || row.bakeMode == BakeMode.유지)
                        row.bakeMode = allowed[0];

                    int idx = Array.IndexOf(allowed, row.bakeMode);
                    idx = Mathf.Max(0, idx);
                    idx = EditorGUI.Popup(CenterRect(cols.action, lineHeight), idx,
                        allowed.Select(ModeLabel).ToArray());
                    row.bakeMode = allowed[Mathf.Clamp(idx, 0, allowed.Length - 1)];

                    bool needsTarget =
                        row.bakeMode == BakeMode.색상굽기_텍스처타일 ||
                        row.bakeMode == BakeMode.스칼라굽기_그레이타일 ||
                        row.bakeMode == BakeMode.색상곱_텍스처타일;

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
                            EditorGUI.LabelField(CenterRect(cols.target, lineHeight), "(TexEnv 없음)",
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
            DrawRowRightInfoAndMore(row, cols.info, differs ? $"diff:{row.distinctCount}" : "동일", unresolved);

            if (row.expanded)
                DrawRowExpanded_NonTex(g, row, rect);
        }

        private void DrawRowRightInfoAndMore(Row row, Rect infoRect, string info, bool warn)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var btn = CenterRect(new Rect(infoRect.x, infoRect.y, 54, infoRect.height), lineHeight);
            if (GUI.Button(btn, row.expanded ? "접기" : "더보기", Styles.stRowMoreBtn))
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
            GUI.Label(new Rect(0, y, area.width, 18), "텍스처 아틀라싱", Styles.stSubTitle);
            y += 20;

            if (!row.doAction)
            {
                GUI.Label(new Rect(0, y, area.width, 18), "체크박스를 켜면 이 프로퍼티를 아틀라스에 포함합니다.",
                    Styles.stMiniDim);
                GUI.EndGroup();
                return;
            }

            GUI.Label(new Rect(0, y, area.width, 18), "이 텍스처 프로퍼티를 아틀라스에 포함합니다.", Styles.stMiniDim);
            y += 18;
            GUI.Label(new Rect(0, y, area.width, 18), "노말/마스크 계열은 색공간이 자동 추정됩니다.", Styles.stMiniDim);

            GUI.EndGroup();
        }

        private void DrawRowExpanded_NonTex(GroupScan g, Row row, Rect rect)
        {
            var area = new Rect(rect.x + 12, rect.y + 28, rect.width - 24, rect.height - 32);
            GUI.BeginGroup(area);

            float y = 0;

            if (!row.doAction)
            {
                GUI.Label(new Rect(0, y, area.width, 18), "체크박스를 켜면 이 프로퍼티에 액션을 적용합니다.",
                    Styles.stMiniDim);
                GUI.EndGroup();
                return;
            }

            row.resetSourceAfterBake = GUI.Toggle(new Rect(0, y, 260, 18), row.resetSourceAfterBake,
                "굽기/곱 적용 후 원본 프로퍼티 리셋");
            y += 20;

            if (row.type == ShaderUtil.ShaderPropertyType.Color)
            {
                row.includeAlpha = GUI.Toggle(new Rect(0, y, 220, 18), row.includeAlpha, "알파 포함(기본 꺼짐)");
                y += 20;
            }

            bool needsTarget =
                row.bakeMode == BakeMode.색상굽기_텍스처타일 ||
                row.bakeMode == BakeMode.스칼라굽기_그레이타일 ||
                row.bakeMode == BakeMode.색상곱_텍스처타일;

            if (needsTarget)
            {
                GUI.Label(new Rect(0, y, area.width, 18), "모디파이어(옵션): 다른 float 프로퍼티로 곱/가산/감산",
                    Styles.stMiniDim);
                y += 18;

                row.modOp = (ModOp)EditorGUI.EnumPopup(new Rect(0, y, 160, 18), row.modOp);

                var modList = new List<string> { "(없음)" };
                modList.AddRange(g.shaderScalarProps);

                row.modPropIndex = Mathf.Clamp(row.modPropIndex, 0, modList.Count - 1);
                row.modPropIndex = EditorGUI.Popup(new Rect(170, y, 260, 18), row.modPropIndex, modList.ToArray());
                row.modProp = row.modPropIndex == 0 ? "" : modList[row.modPropIndex];
                y += 20;

                row.modClamp01 = GUI.Toggle(new Rect(0, y, 90, 18), row.modClamp01, "Clamp01");
                row.modScale = EditorGUI.FloatField(new Rect(100, y, 140, 18), row.modScale);
                row.modBias = EditorGUI.FloatField(new Rect(250, y, 140, 18), row.modBias);
                y += 20;

                if (row.type == ShaderUtil.ShaderPropertyType.Color)
                    row.modAffectsAlpha = GUI.Toggle(new Rect(0, y, 140, 18), row.modAffectsAlpha, "알파에도 적용");
            }
            else
            {
                GUI.Label(new Rect(0, y, area.width, 18), "굽기/곱 액션 선택 시 추가 옵션이 활성화됩니다.",
                    Styles.stMiniDim);
            }

            GUI.EndGroup();
        }

        // 헬퍼 메서드들
        private string TypeLabel(ShaderUtil.ShaderPropertyType t)
        {
            if (t == ShaderUtil.ShaderPropertyType.TexEnv) return "텍스처";
            if (t == ShaderUtil.ShaderPropertyType.Color) return "색상";
            if (t == ShaderUtil.ShaderPropertyType.Range) return "Range";
            if (t == ShaderUtil.ShaderPropertyType.Float) return "Float";
            if (t == ShaderUtil.ShaderPropertyType.Vector) return "Vector";
            return t.ToString();
        }

        private BakeMode[] AllowedModesUI(ShaderUtil.ShaderPropertyType t)
        {
            if (t == ShaderUtil.ShaderPropertyType.Color)
                return new[] { BakeMode.리셋_쉐이더기본값, BakeMode.색상굽기_텍스처타일, BakeMode.색상곱_텍스처타일 };

            if (t == ShaderUtil.ShaderPropertyType.Float || t == ShaderUtil.ShaderPropertyType.Range)
                return new[] { BakeMode.리셋_쉐이더기본값, BakeMode.스칼라굽기_그레이타일 };

            if (t == ShaderUtil.ShaderPropertyType.Vector)
                return new[] { BakeMode.리셋_쉐이더기본값 };

            return Array.Empty<BakeMode>();
        }

        private string ModeLabel(BakeMode m)
        {
            if (m == BakeMode.리셋_쉐이더기본값) return "리셋(쉐이더 기본값)";
            if (m == BakeMode.색상굽기_텍스처타일) return "색상 굽기 → 텍스처";
            if (m == BakeMode.스칼라굽기_그레이타일) return "스칼라 굽기 → 그레이";
            if (m == BakeMode.색상곱_텍스처타일) return "색상 곱 → 텍스처";
            if (m == BakeMode.유지) return "유지";
            return m.ToString();
        }
    }
}
#endif
