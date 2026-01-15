#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services.Logging
{
    /// <summary>
    /// 로깅 서비스 구현
    /// </summary>
    public class LoggingService : ILoggingService
    {
        private readonly List<LogEntry> entries = new List<LogEntry>();
        private const int MaxEntries = 1000;

        /// <summary>
        /// 모든 로그 엔트리 목록
        /// </summary>
        public List<LogEntry> Entries => entries;

        /// <summary>
        /// 새 로그가 추가될 때 호출되는 이벤트
        /// </summary>
        public event Action<LogEntry> OnLogAdded;

        /// <summary>
        /// 정보 로그 추가
        /// </summary>
        public void Info(string message, string details = null, bool alsoLogToUnity = false)
        {
            AddLog(LogEntry.Level.Info, message, details, alsoLogToUnity);
        }

        /// <summary>
        /// 경고 로그 추가
        /// </summary>
        public void Warning(string message, string details = null, bool alsoLogToUnity = false)
        {
            AddLog(LogEntry.Level.Warning, message, details, alsoLogToUnity);
        }

        /// <summary>
        /// 오류 로그 추가
        /// </summary>
        public void Error(string message, string details = null, bool alsoLogToUnity = true)
        {
            AddLog(LogEntry.Level.Error, message, details, alsoLogToUnity);
        }

        /// <summary>
        /// 성공 로그 추가
        /// </summary>
        public void Success(string message, string details = null, bool alsoLogToUnity = false)
        {
            AddLog(LogEntry.Level.Success, message, details, alsoLogToUnity);
        }

        /// <summary>
        /// 모든 로그 지우기
        /// </summary>
        public void Clear()
        {
            entries.Clear();
        }

        /// <summary>
        /// 로그 추가 내부 메서드
        /// </summary>
        private void AddLog(LogEntry.Level level, string message, string details, bool alsoLogToUnity)
        {
            var entry = new LogEntry(level, message, details);
            entries.Add(entry);

            // 최대 개수 제한
            if (entries.Count > MaxEntries)
            {
                entries.RemoveAt(0);
            }

            // Unity 콘솔에도 출력
            if (alsoLogToUnity)
            {
                string fullMessage = string.IsNullOrEmpty(details) 
                    ? $"[MaterialMerger] {message}" 
                    : $"[MaterialMerger] {message}\n{details}";

                switch (level)
                {
                    case LogEntry.Level.Info:
                    case LogEntry.Level.Success:
                        Debug.Log(fullMessage);
                        break;
                    case LogEntry.Level.Warning:
                        Debug.LogWarning(fullMessage);
                        break;
                    case LogEntry.Level.Error:
                        Debug.LogError(fullMessage);
                        break;
                }
            }

            // 이벤트 발생
            OnLogAdded?.Invoke(entry);
        }
    }
}
#endif
