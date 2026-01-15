#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using K13A.MaterialMerger.Editor.Models;

namespace K13A.MaterialMerger.Editor.Services.Logging
{
    /// <summary>
    /// 로깅 서비스 인터페이스
    /// </summary>
    public interface ILoggingService
    {
        /// <summary>
        /// 모든 로그 엔트리 목록
        /// </summary>
        List<LogEntry> Entries { get; }

        /// <summary>
        /// 정보 로그 추가
        /// </summary>
        /// <param name="message">메시지</param>
        /// <param name="details">상세 정보 (선택적)</param>
        /// <param name="alsoLogToUnity">Unity 콘솔에도 출력할지 여부</param>
        void Info(string message, string details = null, bool alsoLogToUnity = false);

        /// <summary>
        /// 경고 로그 추가
        /// </summary>
        /// <param name="message">메시지</param>
        /// <param name="details">상세 정보 (선택적)</param>
        /// <param name="alsoLogToUnity">Unity 콘솔에도 출력할지 여부</param>
        void Warning(string message, string details = null, bool alsoLogToUnity = false);

        /// <summary>
        /// 오류 로그 추가
        /// </summary>
        /// <param name="message">메시지</param>
        /// <param name="details">상세 정보 (선택적)</param>
        /// <param name="alsoLogToUnity">Unity 콘솔에도 출력할지 여부</param>
        void Error(string message, string details = null, bool alsoLogToUnity = true);

        /// <summary>
        /// 성공 로그 추가
        /// </summary>
        /// <param name="message">메시지</param>
        /// <param name="details">상세 정보 (선택적)</param>
        /// <param name="alsoLogToUnity">Unity 콘솔에도 출력할지 여부</param>
        void Success(string message, string details = null, bool alsoLogToUnity = false);

        /// <summary>
        /// 모든 로그 지우기
        /// </summary>
        void Clear();

        /// <summary>
        /// 새 로그가 추가될 때 호출되는 이벤트
        /// </summary>
        event Action<LogEntry> OnLogAdded;
    }
}
#endif
