#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Core;
using K13A.MaterialMerger.Editor.Services.Logging;
using K13A.MaterialMerger.Editor.Services.Localization;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 로그 콘솔 패널을 렌더링하는 컴포넌트
    /// </summary>
    public class LogConsoleRenderer
    {
        public MaterialMergerStyles Styles { get; set; }
        public ILocalizationService Localization { get; set; }
        public ILoggingService LoggingService { get; set; }

        private Vector2 logScroll;
        private bool autoScroll = true;
        private int selectedLogIndex = -1;

        private GUIStyle logEntryStyle;
        private GUIStyle logDetailsStyle;
        private GUIStyle toolbarStyle;
        private GUIStyle logTextStyle;

        /// <summary>
        /// 로그 콘솔 렌더링
        /// </summary>
        /// <returns>콘솔을 닫아야 하는지 여부</returns>
        public bool DrawLogConsole(Rect windowRect)
        {
            EnsureStyles();

            // 키보드 이벤트 처리 (Up/Down 화살표)
            HandleKeyboardNavigation();

            // Window 내부에 마진을 두고 콘솔 영역 계산
            float margin = 20f;
            Rect consoleRect = new Rect(
                margin,
                margin,
                windowRect.width - margin * 2,
                windowRect.height - margin * 2
            );

            bool shouldClose = false;

            GUILayout.BeginArea(consoleRect, GUI.skin.window);
            
            using (new EditorGUILayout.VerticalScope())
            {
                shouldClose = DrawToolbar();
                DrawLogList();
                if (selectedLogIndex >= 0 && selectedLogIndex < LoggingService.Entries.Count)
                {
                    DrawLogDetails(LoggingService.Entries[selectedLogIndex]);
                }
            }

            GUILayout.EndArea();

            return shouldClose;
        }

        /// <summary>
        /// 툴바 (제목, 클리어 버튼, 자동 스크롤 토글)
        /// </summary>
        /// <returns>닫기 버튼이 클릭되었는지 여부</returns>
        private bool DrawToolbar()
        {
            bool shouldClose = false;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(Localization.Get(L10nKey.LogConsole), EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                autoScroll = GUILayout.Toggle(autoScroll, Localization.Get(L10nKey.AutoScroll), EditorStyles.toolbarButton, GUILayout.Width(80));

                if (GUILayout.Button(Localization.Get(L10nKey.ClearLogs), EditorStyles.toolbarButton, GUILayout.Width(80)))
                {
                    LoggingService.Clear();
                    selectedLogIndex = -1;
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(25)))
                {
                    shouldClose = true;
                }
            }

            return shouldClose;
        }

        /// <summary>
        /// 로그 목록
        /// </summary>
        private void DrawLogList()
        {
            using (var scrollScope = new EditorGUILayout.ScrollViewScope(logScroll, GUILayout.ExpandHeight(true)))
            {
                logScroll = scrollScope.scrollPosition;

                var entries = LoggingService.Entries;
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    DrawLogEntry(entry, i);
                }

                // 자동 스크롤
                if (autoScroll && Event.current.type == EventType.Repaint)
                {
                    logScroll.y = float.MaxValue;
                }
            }
        }

        /// <summary>
        /// 개별 로그 엔트리 렌더링
        /// </summary>
        private void DrawLogEntry(LogEntry entry, int index)
        {
            Color originalColor = GUI.backgroundColor;
            Color originalContentColor = GUI.contentColor;

            // 선택된 항목 배경색
            if (index == selectedLogIndex)
            {
                GUI.backgroundColor = new Color(0.4f, 0.6f, 1f, 0.3f);
            }

            using (new EditorGUILayout.HorizontalScope(logEntryStyle, GUILayout.Height(20)))
            {
                // 시간
                GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
                GUILayout.Label(entry.TimeString, logTextStyle, GUILayout.Width(70));

                // 레벨 색상 및 텍스트
                Color levelColor = entry.GetLevelColor();
                GUI.contentColor = levelColor;
                GUILayout.Label(entry.GetLevelPrefix(), logTextStyle, GUILayout.Width(50));

                GUI.contentColor = originalContentColor;

                // 메시지 (버튼으로 클릭 가능)
                var buttonStyle = new GUIStyle(logTextStyle);
                buttonStyle.alignment = TextAnchor.MiddleLeft;
                if (GUILayout.Button(entry.message, buttonStyle, GUILayout.ExpandWidth(true)))
                {
                    selectedLogIndex = (selectedLogIndex == index) ? -1 : index;
                }
            }

            // 구분선 (단순한 1px 라인)
            Rect lineRect = GUILayoutUtility.GetLastRect();
            lineRect.y += lineRect.height;
            lineRect.height = 1;
            EditorGUI.DrawRect(lineRect, new Color(0.15f, 0.15f, 0.15f, 0.5f));

            GUI.backgroundColor = originalColor;
            GUI.contentColor = originalContentColor;
        }

        /// <summary>
        /// 선택된 로그의 상세 정보
        /// </summary>
        private void DrawLogDetails(LogEntry entry)
        {
            using (new EditorGUILayout.VerticalScope(Styles.stBox, GUILayout.Height(150)))
            {
                EditorGUILayout.LabelField(Localization.Get(L10nKey.LogDetails), EditorStyles.boldLabel);
                
                using (var detailsScroll = new EditorGUILayout.ScrollViewScope(Vector2.zero))
                {
                    if (!string.IsNullOrEmpty(entry.details))
                    {
                        EditorGUILayout.TextArea(entry.details, logDetailsStyle, GUILayout.ExpandHeight(true));
                    }
                    else
                    {
                        EditorGUILayout.LabelField(Localization.Get(L10nKey.NoDetails), EditorStyles.miniLabel);
                    }
                }
            }
        }

        /// <summary>
        /// 키보드 탐색 처리
        /// </summary>
        private void HandleKeyboardNavigation()
        {
            if (Event.current.type != EventType.KeyDown) return;

            var entries = LoggingService.Entries;
            if (entries.Count == 0) return;

            if (Event.current.keyCode == KeyCode.UpArrow)
            {
                if (selectedLogIndex < 0)
                    selectedLogIndex = entries.Count - 1;
                else
                    selectedLogIndex = Mathf.Max(0, selectedLogIndex - 1);
                
                Event.current.Use();
                ScrollToSelectedLog();
            }
            else if (Event.current.keyCode == KeyCode.DownArrow)
            {
                if (selectedLogIndex < 0)
                    selectedLogIndex = 0;
                else
                    selectedLogIndex = Mathf.Min(entries.Count - 1, selectedLogIndex + 1);
                
                Event.current.Use();
                ScrollToSelectedLog();
            }
        }

        /// <summary>
        /// 선택된 로그로 스크롤
        /// </summary>
        private void ScrollToSelectedLog()
        {
            if (selectedLogIndex < 0) return;
            
            // 간단한 스크롤 조정 (선택된 항목이 보이도록)
            float itemHeight = 22f; // 대략적인 로그 항목 높이
            float targetScrollY = selectedLogIndex * itemHeight;
            
            // 자동 스크롤 임시 비활성화
            autoScroll = false;
        }

        /// <summary>
        /// 스타일 초기화
        /// </summary>
        private void EnsureStyles()
        {
            if (logEntryStyle == null)
            {
                logEntryStyle = new GUIStyle()
                {
                    margin = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(4, 4, 2, 2),
                    border = new RectOffset(0, 0, 0, 1),
                    normal = { background = MakeSolidTexture(new Color(0.25f, 0.25f, 0.25f, 0.3f)) }
                };
            }

            if (logDetailsStyle == null)
            {
                logDetailsStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = true,
                    richText = false
                };
            }

            if (toolbarStyle == null)
            {
                toolbarStyle = new GUIStyle(EditorStyles.toolbar);
            }

            if (logTextStyle == null)
            {
                logTextStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = false,
                    clipping = TextClipping.Clip,
                    fontSize = 11
                };
            }
        }

        /// <summary>
        /// 단색 텍스처 생성
        /// </summary>
        private Texture2D MakeSolidTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
#endif
