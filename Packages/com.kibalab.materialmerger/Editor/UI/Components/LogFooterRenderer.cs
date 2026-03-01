#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using K13A.MaterialMerger.Editor.Services.Logging;
using K13A.MaterialMerger.Editor.Services.Localization;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.UI.Components
{
    /// <summary>
    /// 로그 Footer 바를 렌더링하는 컴포넌트
    /// </summary>
    public class LogFooterRenderer
    {
        public ILocalizationService Localization { get; set; }
        public ILoggingService LoggingService { get; set; }

        private GUIStyle footerStyle;
        private GUIStyle footerTextStyle;
        private const float FooterHeight = 24f;

        /// <summary>
        /// Footer 바 렌더링
        /// </summary>
        /// <param name="windowRect">윈도우 영역</param>
        /// <returns>Footer가 클릭되었는지 여부</returns>
        public bool DrawFooter(Rect windowRect)
        {
            EnsureStyles();

            // Footer 영역 계산 (화면 최하단)
            Rect footerRect = new Rect(0, windowRect.height - FooterHeight, windowRect.width, FooterHeight);

            bool clicked = false;

            // 마지막 로그 가져오기
            LogEntry lastEntry = null;
            if (LoggingService.Entries.Count > 0)
            {
                lastEntry = LoggingService.Entries[LoggingService.Entries.Count - 1];
            }

            // Footer 배경
            Color originalBgColor = GUI.backgroundColor;
            if (lastEntry != null)
            {
                // 로그 레벨에 따라 미묘하게 배경색 변경
                switch (lastEntry.level)
                {
                    case LogEntry.Level.Error:
                        GUI.backgroundColor = new Color(0.5f, 0.2f, 0.2f, 1f);
                        break;
                    case LogEntry.Level.Warning:
                        GUI.backgroundColor = new Color(0.5f, 0.4f, 0.2f, 1f);
                        break;
                    case LogEntry.Level.Success:
                        GUI.backgroundColor = new Color(0.2f, 0.5f, 0.2f, 1f);
                        break;
                    default:
                        GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                        break;
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
            }

            // Footer를 버튼으로 만들어 클릭 가능하게
            if (GUI.Button(footerRect, "", footerStyle))
            {
                clicked = true;
            }

            GUI.backgroundColor = originalBgColor;

            // 텍스트 표시
            Rect textRect = new Rect(footerRect.x + 8, footerRect.y, footerRect.width - 16, footerRect.height);
            
            if (lastEntry != null)
            {
                // 레벨 아이콘 + 메시지
                Color originalContentColor = GUI.contentColor;
                GUI.contentColor = lastEntry.GetLevelColor();

                string displayText = $"{lastEntry.GetLevelPrefix()} {lastEntry.message}";
                
                using (new GUI.GroupScope(textRect))
                {
                    Rect labelRect = new Rect(0, 0, textRect.width - 100, textRect.height);
                    GUI.Label(labelRect, displayText, footerTextStyle);
                    
                    // 시간 표시 (우측)
                    Rect timeRect = new Rect(textRect.width - 95, 0, 90, textRect.height);
                    GUI.contentColor = new Color(0.6f, 0.6f, 0.6f);
                    GUI.Label(timeRect, lastEntry.TimeString, footerTextStyle);
                }

                GUI.contentColor = originalContentColor;
            }
            else
            {
                // 로그가 없을 때
                GUI.contentColor = new Color(0.5f, 0.5f, 0.5f);
                GUI.Label(textRect, "No logs. Click to open console.", footerTextStyle);
                GUI.contentColor = Color.white;
            }

            return clicked;
        }

        /// <summary>
        /// 스타일 초기화
        /// </summary>
        private void EnsureStyles()
        {
            if (footerStyle == null)
            {
                footerStyle = new GUIStyle(GUI.skin.box)
                {
                    margin = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(8, 8, 4, 4),
                    border = new RectOffset(1, 1, 1, 1)
                };
            }

            if (footerTextStyle == null)
            {
                footerTextStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                    fontSize = 11,
                    wordWrap = false,
                    clipping = TextClipping.Clip
                };
            }
        }

        /// <summary>
        /// Footer 높이 반환
        /// </summary>
        public float GetHeight()
        {
            return FooterHeight;
        }
    }
}
#endif
