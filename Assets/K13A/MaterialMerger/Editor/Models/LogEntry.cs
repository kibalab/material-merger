#if UNITY_EDITOR
using System;
using UnityEngine;

namespace K13A.MaterialMerger.Editor.Models
{
    /// <summary>
    /// 로그 엔트리 데이터 모델
    /// </summary>
    [Serializable]
    public class LogEntry
    {
        /// <summary>
        /// 로그 레벨
        /// </summary>
        public enum Level
        {
            Info,
            Warning,
            Error,
            Success
        }

        /// <summary>
        /// 로그가 생성된 시간
        /// </summary>
        public DateTime timestamp;

        /// <summary>
        /// 로그 레벨
        /// </summary>
        public Level level;

        /// <summary>
        /// 로그 메시지
        /// </summary>
        public string message;

        /// <summary>
        /// 상세 정보 (선택적)
        /// </summary>
        public string details;

        public LogEntry(Level level, string message, string details = null)
        {
            this.timestamp = DateTime.Now;
            this.level = level;
            this.message = message;
            this.details = details;
        }

        /// <summary>
        /// 시간 포맷팅된 문자열
        /// </summary>
        public string TimeString => timestamp.ToString("HH:mm:ss.fff");

        /// <summary>
        /// 레벨에 따른 색상 가져오기
        /// </summary>
        public Color GetLevelColor()
        {
            switch (level)
            {
                case Level.Info:
                    return new Color(0.8f, 0.8f, 0.8f);
                case Level.Warning:
                    return new Color(1f, 0.8f, 0.2f);
                case Level.Error:
                    return new Color(1f, 0.3f, 0.3f);
                case Level.Success:
                    return new Color(0.3f, 1f, 0.3f);
                default:
                    return Color.white;
            }
        }

        /// <summary>
        /// 레벨에 따른 접두사 가져오기
        /// </summary>
        public string GetLevelPrefix()
        {
            switch (level)
            {
                case Level.Info:
                    return "[INFO]";
                case Level.Warning:
                    return "[WARN]";
                case Level.Error:
                    return "[ERRO]";
                case Level.Success:
                    return "[SUCC]";
                default:
                    return "[LOGS]";
            }
        }
    }
}
#endif
