#if UNITY_EDITOR
namespace K13A.MaterialMerger.Editor.Services.Localization
{
    /// <summary>
    /// 현지화 서비스 인터페이스
    /// </summary>
    public interface ILocalizationService
    {
        /// <summary>
        /// 현재 언어
        /// </summary>
        Language CurrentLanguage { get; set; }

        /// <summary>
        /// 키로 번역된 문자열 가져오기
        /// </summary>
        string Get(string key);

        /// <summary>
        /// 키로 번역된 문자열 가져오기 (포맷 지원)
        /// </summary>
        string Get(string key, params object[] args);
    }
}
#endif
