#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor
{
    public partial class MaterialMerger
    {
        const float TopLabelWidth = 90f;
        const float RowHeaderHeight = 24f;

        void EnsureStyles()
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
            stSection.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.90f, 0.90f, 0.90f, 1f) : new Color(0.20f, 0.20f, 0.20f, 1f);

            stMini = new GUIStyle(EditorStyles.miniLabel);
            stMini.alignment = TextAnchor.MiddleLeft;

            stMiniDim = new GUIStyle(EditorStyles.miniLabel);
            stMiniDim.alignment = TextAnchor.MiddleLeft;
            stMiniDim.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.75f, 0.75f, 0.75f, 1f) : new Color(0.35f, 0.35f, 0.35f, 1f);

            stMiniWarn = new GUIStyle(EditorStyles.miniLabel);
            stMiniWarn.alignment = TextAnchor.MiddleLeft;
            stMiniWarn.normal.textColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.78f, 0.35f, 1f) : new Color(0.75f, 0.45f, 0.05f, 1f);

            stPill = new GUIStyle(EditorStyles.miniButton);
            stPill.padding = new RectOffset(8, 8, 2, 2);
            stPillWarn = new GUIStyle(stPill);

            stToolbar = new GUIStyle(EditorStyles.toolbar);

            stToolbarBtn = new GUIStyle(EditorStyles.toolbarButton);
            stToolbarBtn.fixedHeight = 18;

            stRowMoreBtn = new GUIStyle(EditorStyles.miniButton);
            stRowMoreBtn.fixedHeight = 18;

            var baseBtn = (GUI.skin != null && GUI.skin.button != null) ? GUI.skin.button : EditorStyles.miniButton;
            stBigBtn = new GUIStyle(baseBtn);
            stBigBtn.fontSize = 12;
            stBigBtn.fontStyle = FontStyle.Bold;

            stylesReady = true;
        }

        void DrawTop()
        {
            using (new EditorGUILayout.VerticalScope(stBox))
            {
                float lineHeight = EditorGUIUtility.singleLineHeight;
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("멀티 아틀라스 머저", stTitle);
                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(!root))
                    {
                        var scanContent = MakeIconContent("스캔", "Refresh", "d_Refresh", "스캔");
                        if (GUILayout.Button(scanContent, stBigBtn, GUILayout.Width(140), GUILayout.Height(32)))
                            Scan();
                    }

                    using (new EditorGUI.DisabledScope(scans == null || scans.Count == 0 || (cloneRootOnApply && !root)))
                    {
                        var buildContent = MakeIconContent("빌드 & 적용", "PlayButton", "d_PlayButton", "빌드 & 적용");
                        if (GUILayout.Button(buildContent, stBigBtn, GUILayout.Width(180), GUILayout.Height(32)))
                            BuildAndApplyWithConfirm();
                    }
                }

                EditorGUILayout.Space(6);

                var rootRect = EditorGUILayout.GetControlRect(false, lineHeight);
                var rootLabelRect = new Rect(rootRect.x, rootRect.y, TopLabelWidth, rootRect.height);
                var rootFieldRect = new Rect(rootLabelRect.xMax + 6, rootRect.y, rootRect.width - TopLabelWidth - 6, rootRect.height);
                EditorGUI.LabelField(rootLabelRect, "루트");
                EditorGUI.BeginChangeCheck();
                var newRoot = (GameObject)EditorGUI.ObjectField(rootFieldRect, root, typeof(GameObject), true);
                if (EditorGUI.EndChangeCheck())
                    SetRoot(newRoot);

                var scanRect = EditorGUILayout.GetControlRect(false, lineHeight);
                var scanLabelRect = new Rect(scanRect.x, scanRect.y, TopLabelWidth, scanRect.height);
                var scanValueRect = new Rect(scanLabelRect.xMax + 6, scanRect.y, scanRect.width - TopLabelWidth - 6, scanRect.height);
                var scanLabelContent = new GUIContent("마지막 스캔");
                EditorGUI.LabelField(scanLabelRect, scanLabelContent, stMiniDim);
                EditorGUI.LabelField(scanValueRect, GetLastScanLabel(), stMiniDim);

                EditorGUILayout.Space(4);

                var outRect = EditorGUILayout.GetControlRect(false, lineHeight);
                var outFieldRect = new Rect(outRect.x, outRect.y, outRect.width - TopLabelWidth - 6, outRect.height);

                float buttonWidth = 90f;
                var outBtnRect = new Rect(outFieldRect.x, outFieldRect.y, buttonWidth, outFieldRect.height);
                var outPathRect = new Rect(outBtnRect.xMax + 6, outFieldRect.y, outFieldRect.width - buttonWidth - 6, outFieldRect.height);
                if (GUI.Button(outBtnRect, MakeIconContent("출력 폴더", "Folder Icon", "d_Folder Icon", "출력 폴더"), stToolbarBtn))
                {
                    var picked = EditorUtility.OpenFolderPanel("출력 폴더 선택", Application.dataPath, "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        picked = picked.Replace("\\", "/");
                        if (picked.Contains("/Assets/"))
                            outputFolder = "Assets/" + picked.Split(new[] { "/Assets/" }, StringSplitOptions.None)[1];
                        else
                            EditorUtility.DisplayDialog("출력 폴더", "Assets 폴더 내부만 가능합니다.", "OK");
                    }
                }

                EditorGUI.LabelField(outPathRect, outputFolder, stMiniDim);
            }
        }

        void DrawGlobal()
        {
            using (new EditorGUILayout.VerticalScope(stBox))
            {
                DrawSection("머테리얼 분리 규칙");
                using (new EditorGUILayout.HorizontalScope())
                {
                    groupByKeywords = EditorGUILayout.ToggleLeft("키워드로 분리", groupByKeywords, GUILayout.Width(130));
                    groupByRenderQueue = EditorGUILayout.ToggleLeft("RenderQueue로 분리", groupByRenderQueue, GUILayout.Width(150));
                    splitOpaqueTransparent = EditorGUILayout.ToggleLeft("불투명/투명 분리", splitOpaqueTransparent, GUILayout.Width(150));
                }

                DrawSeparator();

                DrawSection("적용 방식");
                using (new EditorGUILayout.HorizontalScope())
                {
                    cloneRootOnApply = EditorGUILayout.ToggleLeft("적용 시 루트 복제", cloneRootOnApply, GUILayout.Width(150));
                    using (new EditorGUI.DisabledScope(!cloneRootOnApply))
                        deactivateOriginalRoot = EditorGUILayout.ToggleLeft("원본 루트 비활성화", deactivateOriginalRoot, GUILayout.Width(160));
                }

                DrawSeparator();

                DrawSection("아틀라스");
                using (new EditorGUILayout.HorizontalScope())
                {
                    atlasSize = EditorGUILayout.IntPopup("크기", atlasSize, new[] { "4096", "8192" }, new[] { 4096, 8192 }, GUILayout.Width(220));
                    grid = EditorGUILayout.IntPopup("그리드", grid, new[] { "2", "4" }, new[] { 2, 4 }, GUILayout.Width(200));
                    GUILayout.FlexibleSpace();
                }

                paddingPx = EditorGUILayout.IntSlider("패딩(px)", paddingPx, 0, 64);

                DrawSeparator();

                DrawSection("정책");
                diffPolicy = (DiffPolicy)EditorGUILayout.EnumPopup("미해결 diff 처리", diffPolicy);
            }
        }

        void DrawGroups()
        {
            if (scans == null || scans.Count == 0)
            {
                using (new EditorGUILayout.VerticalScope(stBox))
                    EditorGUILayout.HelpBox("루트를 지정한 뒤 스캔을 실행하세요.", MessageType.Info);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            using (new EditorGUILayout.HorizontalScope(stBox))
            {
                GUILayout.Label("계획 목록", stSubTitle);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("전체 펼치기", stToolbarBtn, GUILayout.Width(95)))
                    foreach (var g in scans)
                        g.foldout = true;
                if (GUILayout.Button("전체 접기", stToolbarBtn, GUILayout.Width(95)))
                    foreach (var g in scans)
                        g.foldout = false;
                if (GUILayout.Button("전체 활성", stToolbarBtn, GUILayout.Width(90)))
                    foreach (var g in scans)
                        g.enabled = true;
                if (GUILayout.Button("전체 비활성", stToolbarBtn, GUILayout.Width(90)))
                    foreach (var g in scans)
                        g.enabled = false;
            }

            for (int gi = 0; gi < scans.Count; gi++)
                DrawGroup(scans[gi], gi);

            EditorGUILayout.EndScrollView();
        }

        void DrawGroup(GroupScan g, int index)
        {
            using (new EditorGUILayout.VerticalScope(stBox))
            {
                var shaderName = GetGroupShaderName(g);
                using (new EditorGUILayout.HorizontalScope())
                {
                    g.enabled = EditorGUILayout.Toggle(g.enabled, GUILayout.Width(16));
                    var foldoutContent = new GUIContent($"{shaderName}   [{g.tag}]");
                    g.foldout = EditorGUILayout.Foldout(g.foldout, foldoutContent, true);
                    GUILayout.FlexibleSpace();
                    DrawPill($"머티리얼 {g.mats.Count}", false);
                    DrawPill($"페이지 {g.pageCount}", false);
                    if (g.skippedMultiMat > 0) DrawPill($"스킵 {g.skippedMultiMat}", true);
                }

                if (!g.foldout) return;

                float lineHeight = EditorGUIUtility.singleLineHeight;
                using (new EditorGUILayout.HorizontalScope(stToolbar, GUILayout.Height(lineHeight)))
                {
                    g.search = DrawSearchField(g.search, 260, lineHeight);
                    g.onlyRelevant = GUILayout.Toggle(g.onlyRelevant, "관련만", stToolbarBtn, GUILayout.Width(70), GUILayout.Height(lineHeight));
                    g.showTexturesOnly = GUILayout.Toggle(g.showTexturesOnly, "텍스처만", stToolbarBtn, GUILayout.Width(80), GUILayout.Height(lineHeight));
                    g.showScalarsOnly = GUILayout.Toggle(g.showScalarsOnly, "스칼라만", stToolbarBtn, GUILayout.Width(80), GUILayout.Height(lineHeight));

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("텍스처 아틀라스 전체 켜기", stToolbarBtn, GUILayout.Width(170), GUILayout.Height(lineHeight))) SetAllTexActions(g, true);
                    if (GUILayout.Button("텍스처 아틀라스 전체 끄기", stToolbarBtn, GUILayout.Width(170), GUILayout.Height(lineHeight))) SetAllTexActions(g, false);
                }

                EditorGUILayout.Space(4);
                DrawTableHeader();

                int visible = 0;
                for (int i = 0; i < g.rows.Count; i++)
                {
                    var r = g.rows[i];
                    if (!PassRowFilter(g, r)) continue;
                    DrawRow(g, r, visible);
                    visible++;
                }

                int unresolved = CountUnresolvedDiffs(g);
                if (unresolved > 0)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.HelpBox($"미해결 diff 항목 {unresolved}개: 왼쪽 체크박스를 켠 뒤 액션을 지정하거나, 전역 설정에서 '첫번째기준으로진행'을 선택하세요.", MessageType.Warning);
                }
            }
        }

        void DrawSeparator()
        {
            var r = GUILayoutUtility.GetRect(1, 8, GUILayout.ExpandWidth(true));
            var line = new Rect(r.x + 6, r.y + 4, r.width - 12, 1);
            EditorGUI.DrawRect(line, EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.08f) : new Color(0, 0, 0, 0.15f));
        }

        void DrawSection(string title)
        {
            EditorGUILayout.Space(2);
            GUILayout.Label(title, stSection);
            EditorGUILayout.Space(2);
        }

        void DrawPill(string text, bool warn)
        {
            GUILayout.Label(text, warn ? stPillWarn : stPill, GUILayout.Height(18));
        }

        string DrawSearchField(string value, float width, float height)
        {
            var rect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width));
            var textStyle = GUI.skin != null ? (GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.textField) : EditorStyles.textField;
            var cancelStyle = GUI.skin != null ? (GUI.skin.FindStyle("ToolbarSearchCancelButton") ?? EditorStyles.miniButton) : EditorStyles.miniButton;
            var emptyCancelStyle = GUI.skin != null ? (GUI.skin.FindStyle("ToolbarSearchCancelButtonEmpty") ?? EditorStyles.miniButton) : EditorStyles.miniButton;

            var textRect = new Rect(rect.x, rect.y + 3, rect.width - 18, rect.height);
            var btnRect = new Rect(rect.x + rect.width - 18, rect.y, 18, rect.height);

            value = EditorGUI.TextField(textRect, value ?? "", textStyle);

            if (string.IsNullOrEmpty(value))
            {
                GUI.Button(btnRect, GUIContent.none, emptyCancelStyle);
            }
            else
            {
                if (GUI.Button(btnRect, GUIContent.none, cancelStyle))
                    value = "";
            }

            return value;
        }

        Texture GetIconTexture(string lightName, string darkName)
        {
            var iconName = EditorGUIUtility.isProSkin && !string.IsNullOrEmpty(darkName) ? darkName : lightName;
            if (string.IsNullOrEmpty(iconName)) return null;
            var content = EditorGUIUtility.IconContent(iconName);
            return content != null ? content.image : null;
        }

        GUIContent MakeIconContent(string text, string lightName, string darkName, string tooltip)
        {
            var img = GetIconTexture(lightName, darkName);
            return new GUIContent(text, img, tooltip ?? "");
        }

        string GetLastScanLabel()
        {
            if (!profile || profile.lastScanTicksUtc <= 0) return "(없음)";
            var dt = new DateTime(profile.lastScanTicksUtc, DateTimeKind.Utc).ToLocalTime();
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        string GetGroupShaderName(GroupScan g)
        {
            if (g == null) return "NULL_SHADER";
            if (g.key.shader) return g.key.shader.name;
            if (!string.IsNullOrEmpty(g.shaderName)) return g.shaderName;
            return "NULL_SHADER";
        }

        ColumnLayout CalcColumnLayout(Rect rect)
        {
            float pad = 6;
            rect = new Rect(rect.x + pad, rect.y, rect.width - pad * 2, rect.height);

            float wCheck = 18;
            float wType = 82;
            float wAction = 220;
            float wTarget = 240;
            float wInfo = 190;

            float minName = 240;
            float remain = rect.width - (wCheck + wType + wAction + wTarget + wInfo);
            float wName = Mathf.Max(minName, remain);

            float x = rect.x;
            var c = new ColumnLayout();
            c.check = new Rect(x, rect.y, wCheck, rect.height);
            x += wCheck;
            c.name = new Rect(x, rect.y, wName, rect.height);
            x += wName;
            c.type = new Rect(x, rect.y, wType, rect.height);
            x += wType;
            c.action = new Rect(x, rect.y, wAction, rect.height);
            x += wAction;
            c.target = new Rect(x, rect.y, wTarget, rect.height);
            x += wTarget;
            c.info = new Rect(x, rect.y, wInfo, rect.height);
            return c;
        }

        void DrawTableHeader()
        {
            var rect = GUILayoutUtility.GetRect(1, RowHeaderHeight, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.04f) : new Color(0, 0, 0, 0.04f));

            var cols = CalcColumnLayout(rect);
            float lineHeight = EditorGUIUtility.singleLineHeight;
            GUI.Label(CenterRect(cols.check, lineHeight), "", stMini);
            GUI.Label(CenterRect(cols.name, lineHeight), "프로퍼티", stMini);
            GUI.Label(CenterRect(cols.type, lineHeight), "타입", stMini);
            GUI.Label(CenterRect(cols.action, lineHeight), "액션", stMini);
            GUI.Label(CenterRect(cols.target, lineHeight), "대상", stMini);
            GUI.Label(CenterRect(cols.info, lineHeight), "정보", stMini);

            var line = new Rect(rect.x + 6, rect.yMax - 1, rect.width - 12, 1);
            EditorGUI.DrawRect(line, EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.08f) : new Color(0, 0, 0, 0.12f));
        }

        bool PassRowFilter(GroupScan g, Row r)
        {
            if (g.showTexturesOnly && r.type != ShaderUtil.ShaderPropertyType.TexEnv) return false;
            if (g.showScalarsOnly && r.type == ShaderUtil.ShaderPropertyType.TexEnv) return false;

            if (!string.IsNullOrEmpty(g.search))
            {
                var key = r.name ?? "";
                if (key.IndexOf(g.search, StringComparison.OrdinalIgnoreCase) < 0) return false;
            }

            if (!g.onlyRelevant) return true;

            if (r.type == ShaderUtil.ShaderPropertyType.TexEnv)
                return r.texNonNull > 0 || r.doAction;

            if (r.type == ShaderUtil.ShaderPropertyType.Color ||
                r.type == ShaderUtil.ShaderPropertyType.Float ||
                r.type == ShaderUtil.ShaderPropertyType.Range ||
                r.type == ShaderUtil.ShaderPropertyType.Vector)
                return r.distinctCount > 1 || r.doAction;

            return false;
        }

        float GetRowHeight(GroupScan g, Row row)
        {
            if (!row.expanded) return 26f;

            if (row.type == ShaderUtil.ShaderPropertyType.TexEnv)
                return 120f;

            bool needsTarget = row.doAction && (row.bakeMode == BakeMode.색상굽기_텍스처타일 || row.bakeMode == BakeMode.스칼라굽기_그레이타일 || row.bakeMode == BakeMode.색상곱_텍스처타일);

            float h = 26f;
            h += 8f;
            h += 20f;
            if (row.type == ShaderUtil.ShaderPropertyType.Color) h += 20f;
            if (needsTarget)
            {
                h += 18f;
                h += 20f;
                h += 20f;
                if (row.type == ShaderUtil.ShaderPropertyType.Color) h += 20f;
            }
            else
            {
                h += 18f;
            }

            return Mathf.Max(140f, h + 40f);
        }

        void DrawRow(GroupScan g, Row row, int visibleIndex)
        {
            float height = GetRowHeight(g, row);
            var rect = GUILayoutUtility.GetRect(1, height, GUILayout.ExpandWidth(true));

            var bg = (visibleIndex % 2 == 0)
                ? (EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.02f) : new Color(0, 0, 0, 0.02f))
                : (EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.012f) : new Color(0, 0, 0, 0.012f));
            EditorGUI.DrawRect(rect, bg);

            if (row.doAction)
            {
                var active = EditorGUIUtility.isProSkin ? new Color(0.25f, 0.7f, 0.3f, 0.06f) : new Color(0.2f, 0.6f, 0.25f, 0.08f);
                EditorGUI.DrawRect(rect, active);
            }

            if (Event.current != null && rect.Contains(Event.current.mousePosition))
            {
                var hover = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.06f) : new Color(0f, 0f, 0f, 0.06f);
                EditorGUI.DrawRect(rect, hover);
            }

            var top = new Rect(rect.x, rect.y, rect.width, RowHeaderHeight);
            var cols = CalcColumnLayout(top);
            float lineHeight = EditorGUIUtility.singleLineHeight;

            row.doAction = EditorGUI.Toggle(CenterRect(cols.check, lineHeight), row.doAction);

            EditorGUI.LabelField(CenterRect(cols.name, lineHeight), row.name ?? "");
            EditorGUI.LabelField(CenterRect(cols.type, lineHeight), TypeLabel(row.type), stMiniDim);

            if (row.type == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                using (new EditorGUI.DisabledScope(!row.doAction))
                    EditorGUI.LabelField(CenterRect(cols.action, lineHeight), "텍스처 아틀라스", stMiniDim);

                EditorGUI.LabelField(CenterRect(cols.target, lineHeight), row.isNormalLike ? "노말맵" : (row.isSRGB ? "sRGB" : "Linear"), stMiniDim);

                DrawRowRightInfoAndMore(row, cols.info, row.texNonNull > 0 ? $"tex:{row.texDistinct} ST:{row.stDistinct}" : "비어있음", row.texNonNull == 0);

                if (row.expanded)
                    DrawRowExpanded_Texture(row, rect);

                return;
            }

            bool differs = row.distinctCount > 1;

            if (!row.doAction)
            {
                EditorGUI.LabelField(CenterRect(cols.action, lineHeight), "미적용", stMiniDim);
                EditorGUI.LabelField(CenterRect(cols.target, lineHeight), "-", stMiniDim);
            }
            else
            {
                var allowed = AllowedModesUI(row.type);
                if (allowed.Length == 0)
                {
                    EditorGUI.LabelField(CenterRect(cols.action, lineHeight), "-", stMiniDim);
                    EditorGUI.LabelField(CenterRect(cols.target, lineHeight), "-", stMiniDim);
                }
                else
                {
                    if (!allowed.Contains(row.bakeMode) || row.bakeMode == BakeMode.유지)
                        row.bakeMode = allowed[0];

                    int idx = Array.IndexOf(allowed, row.bakeMode);
                    idx = Mathf.Max(0, idx);
                    idx = EditorGUI.Popup(CenterRect(cols.action, lineHeight), idx, allowed.Select(ModeLabel).ToArray());
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
                            row.targetTexIndex = EditorGUI.Popup(CenterRect(cols.target, lineHeight), row.targetTexIndex, g.shaderTexProps.ToArray());
                            row.targetTexProp = g.shaderTexProps[Mathf.Clamp(row.targetTexIndex, 0, g.shaderTexProps.Count - 1)];
                        }
                        else
                        {
                            EditorGUI.LabelField(CenterRect(cols.target, lineHeight), "(TexEnv 없음)", stMiniWarn);
                            row.targetTexProp = "";
                        }
                    }
                    else
                    {
                        EditorGUI.LabelField(CenterRect(cols.target, lineHeight), "-", stMiniDim);
                    }
                }
            }

            var unresolved = differs && !row.doAction;
            DrawRowRightInfoAndMore(row, cols.info, differs ? $"diff:{row.distinctCount}" : "동일", unresolved);

            if (row.expanded)
                DrawRowExpanded_NonTex(g, row, rect);
        }

        void DrawRowRightInfoAndMore(Row row, Rect infoRect, string info, bool warn)
        {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            var btn = CenterRect(new Rect(infoRect.x, infoRect.y, 54, infoRect.height), lineHeight);
            if (GUI.Button(btn, row.expanded ? "접기" : "더보기", stRowMoreBtn))
                row.expanded = !row.expanded;

            var txt = CenterRect(new Rect(infoRect.x + 60, infoRect.y, infoRect.width - 60, infoRect.height), lineHeight);
            EditorGUI.LabelField(txt, info, warn ? stMiniWarn : stMiniDim);
        }

        Rect CenterRect(Rect rect, float height)
        {
            float y = rect.y + (rect.height - height) * 0.5f;
            return new Rect(rect.x, y, rect.width, height);
        }

        void DrawRowExpanded_Texture(Row row, Rect rect)
        {
            var area = new Rect(rect.x + 12, rect.y + 28, rect.width - 24, rect.height - 32);
            GUI.BeginGroup(area);

            float y = 0;
            GUI.Label(new Rect(0, y, area.width, 18), "텍스처 아틀라싱", stSubTitle);
            y += 20;

            if (!row.doAction)
            {
                GUI.Label(new Rect(0, y, area.width, 18), "체크박스를 켜면 이 프로퍼티를 아틀라스에 포함합니다.", stMiniDim);
                GUI.EndGroup();
                return;
            }

            GUI.Label(new Rect(0, y, area.width, 18), "이 텍스처 프로퍼티를 아틀라스에 포함합니다.", stMiniDim);
            y += 18;
            GUI.Label(new Rect(0, y, area.width, 18), "노말/마스크 계열은 색공간이 자동 추정됩니다.", stMiniDim);

            GUI.EndGroup();
        }

        void DrawRowExpanded_NonTex(GroupScan g, Row row, Rect rect)
        {
            var area = new Rect(rect.x + 12, rect.y + 28, rect.width - 24, rect.height - 32);
            GUI.BeginGroup(area);

            float y = 0;

            if (!row.doAction)
            {
                GUI.Label(new Rect(0, y, area.width, 18), "체크박스를 켜면 이 프로퍼티에 액션을 적용합니다.", stMiniDim);
                GUI.EndGroup();
                return;
            }

            row.resetSourceAfterBake = GUI.Toggle(new Rect(0, y, 260, 18), row.resetSourceAfterBake, "굽기/곱 적용 후 원본 프로퍼티 리셋");
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
                GUI.Label(new Rect(0, y, area.width, 18), "모디파이어(옵션): 다른 float 프로퍼티로 곱/가산/감산", stMiniDim);
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
                GUI.Label(new Rect(0, y, area.width, 18), "굽기/곱 액션 선택 시 추가 옵션이 활성화됩니다.", stMiniDim);
            }

            GUI.EndGroup();
        }

        string TypeLabel(ShaderUtil.ShaderPropertyType t)
        {
            if (t == ShaderUtil.ShaderPropertyType.TexEnv) return "텍스처";
            if (t == ShaderUtil.ShaderPropertyType.Color) return "색상";
            if (t == ShaderUtil.ShaderPropertyType.Range) return "Range";
            if (t == ShaderUtil.ShaderPropertyType.Float) return "Float";
            if (t == ShaderUtil.ShaderPropertyType.Vector) return "Vector";
            return t.ToString();
        }

        BakeMode[] AllowedModesUI(ShaderUtil.ShaderPropertyType t)
        {
            if (t == ShaderUtil.ShaderPropertyType.Color)
                return new[] { BakeMode.리셋_쉐이더기본값, BakeMode.색상굽기_텍스처타일, BakeMode.색상곱_텍스처타일 };

            if (t == ShaderUtil.ShaderPropertyType.Float || t == ShaderUtil.ShaderPropertyType.Range)
                return new[] { BakeMode.리셋_쉐이더기본값, BakeMode.스칼라굽기_그레이타일 };

            if (t == ShaderUtil.ShaderPropertyType.Vector)
                return new[] { BakeMode.리셋_쉐이더기본값 };

            return Array.Empty<BakeMode>();
        }

        string ModeLabel(BakeMode m)
        {
            if (m == BakeMode.리셋_쉐이더기본값) return "리셋(쉐이더 기본값)";
            if (m == BakeMode.색상굽기_텍스처타일) return "색상 굽기 → 텍스처";
            if (m == BakeMode.스칼라굽기_그레이타일) return "스칼라 굽기 → 그레이";
            if (m == BakeMode.색상곱_텍스처타일) return "색상 곱 → 텍스처";
            if (m == BakeMode.유지) return "유지";
            return m.ToString();
        }

        int CountUnresolvedDiffs(GroupScan g)
        {
            int c = 0;
            for (int i = 0; i < g.rows.Count; i++)
            {
                var r = g.rows[i];
                if (r.type == ShaderUtil.ShaderPropertyType.TexEnv) continue;

                bool scalarOrColor = r.type == ShaderUtil.ShaderPropertyType.Color ||
                                     r.type == ShaderUtil.ShaderPropertyType.Float ||
                                     r.type == ShaderUtil.ShaderPropertyType.Range ||
                                     r.type == ShaderUtil.ShaderPropertyType.Vector;

                if (!scalarOrColor) continue;

                if (r.distinctCount > 1 && !r.doAction)
                    c++;
            }

            return c;
        }

        void SetAllTexActions(GroupScan g, bool on)
        {
            foreach (var r in g.rows)
                if (r.type == ShaderUtil.ShaderPropertyType.TexEnv && r.texDistinct > 1)
                    r.doAction = on;
        }
    }
}
#endif