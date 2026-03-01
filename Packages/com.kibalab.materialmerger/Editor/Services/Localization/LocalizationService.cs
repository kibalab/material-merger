#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

namespace K13A.MaterialMerger.Editor.Services.Localization
{
    /// <summary>
    /// 현지화 서비스 구현
    /// </summary>
    public class LocalizationService : ILocalizationService
    {
        private const string EditorPrefsKey = "K13A.MaterialMerger.Language";
        private Language currentLanguage;
        private Dictionary<Language, Dictionary<string, string>> translations;

        public Language CurrentLanguage
        {
            get => currentLanguage;
            set
            {
                if (currentLanguage != value)
                {
                    currentLanguage = value;
                    EditorPrefs.SetInt(EditorPrefsKey, (int)currentLanguage);
                }
            }
        }

        public LocalizationService()
        {
            translations = LocalizationData.GetAllTranslations();
            LoadLanguageFromPrefs();
        }

        public string Get(string key)
        {
            if (translations.TryGetValue(currentLanguage, out var langDict))
            {
                if (langDict.TryGetValue(key, out var value))
                    return value;
            }

            // Fallback to Korean if translation not found
            if (currentLanguage != Language.Korean && translations.TryGetValue(Language.Korean, out var fallbackDict))
            {
                if (fallbackDict.TryGetValue(key, out var fallbackValue))
                    return fallbackValue;
            }

            return $"[{key}]";
        }

        public string Get(string key, params object[] args)
        {
            var format = Get(key);
            try
            {
                return string.Format(format, args);
            }
            catch
            {
                return format;
            }
        }

        private void LoadLanguageFromPrefs()
        {
            if (EditorPrefs.HasKey(EditorPrefsKey))
            {
                int savedLang = EditorPrefs.GetInt(EditorPrefsKey, 0);
                currentLanguage = (Language)savedLang;
            }
            else
            {
                // Auto-detect system language
                currentLanguage = DetectSystemLanguage();
                EditorPrefs.SetInt(EditorPrefsKey, (int)currentLanguage);
            }
        }

        private Language DetectSystemLanguage()
        {
            var systemLang = UnityEngine.Application.systemLanguage;
            switch (systemLang)
            {
                case UnityEngine.SystemLanguage.Korean:
                    return Language.Korean;
                case UnityEngine.SystemLanguage.Japanese:
                    return Language.Japanese;
                case UnityEngine.SystemLanguage.English:
                default:
                    return Language.English;
            }
        }
    }
}
#endif
