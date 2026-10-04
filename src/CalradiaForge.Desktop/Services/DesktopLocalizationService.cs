using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CalradiaForge.Desktop.Services
{
    /// <summary>Swaps only Forge-owned language dictionaries; raw diagnostic evidence is never transformed.</summary>
    internal sealed class DesktopLocalizationService
    {
        const string DictionaryPrefix = "/CalradiaForge.Desktop;component/Resources/Strings.";
        ResourceDictionary activeDictionary;
        public string CurrentLanguageCode { get; private set; } = "en";

        public string GetText(string resourceKey, string fallback) =>
            Application.Current?.TryFindResource(resourceKey) as string ?? fallback;

        public void Apply(string languageCode)
        {
            var code = DesktopTextCatalog.ResolveCode(languageCode);
            var dictionaries = Application.Current?.Resources?.MergedDictionaries;
            if (dictionaries == null) return;
            var englishFallback = dictionaries.FirstOrDefault(IsEnglishDictionary);
            var stale = dictionaries.Where(IsForgeLanguageDictionary).Where(item => item != englishFallback).ToArray();
            foreach (var item in stale) dictionaries.Remove(item);
            if (string.Equals(code, "en", StringComparison.OrdinalIgnoreCase))
            {
                activeDictionary = englishFallback;
                CurrentLanguageCode = code;
                return;
            }
            try
            {
                var next = new ResourceDictionary { Source = new Uri(DictionaryPrefix + code + ".xaml", UriKind.Relative) };
                dictionaries.Add(next);
                activeDictionary = next;
                CurrentLanguageCode = code;
            }
            catch
            {
                activeDictionary = englishFallback;
                CurrentLanguageCode = "en";
            }
        }

        static bool IsForgeLanguageDictionary(ResourceDictionary dictionary) =>
            dictionary?.Source?.OriginalString.IndexOf("Resources/Strings.", StringComparison.OrdinalIgnoreCase) >= 0;

        static bool IsEnglishDictionary(ResourceDictionary dictionary) =>
            dictionary?.Source?.OriginalString.EndsWith("Strings.en.xaml", StringComparison.OrdinalIgnoreCase) == true;

        public static IReadOnlyCollection<string> SupportedResourceCodes => ["en", "es", "pt", "de", "fr", "it", "pl", "ru", "tr", "zh-HANS", "zh-HANT", "ja", "ko"];
    }
}
