using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
namespace CalradiaForge.Core
{
    public sealed class ForgeLanguage
    {
        public ForgeLanguage(string code,string displayName) { Code=code; DisplayName=displayName; }
        public string Code { get; }
        public string DisplayName { get; }
        public override string ToString() => DisplayName;
    }

    public static class Localization
    {
        public const string DefaultLanguage = "en";
        static readonly Dictionary<string,Dictionary<string,string>> Catalogs=LoadCatalogs();
        static readonly Dictionary<string,string> English=Catalogs[DefaultLanguage];
        // These are the language identifiers used by Bannerlord's installed Native language packs.
        // Keep the source language first so English remains the deterministic desktop default.
        static readonly ForgeLanguage[] Languages =
        {
            new ForgeLanguage("en","English"),
            new ForgeLanguage("es","Español (LA)"),
            new ForgeLanguage("pt","Português (BR)"),
            new ForgeLanguage("de","Deutsch"),
            new ForgeLanguage("fr","Français"),
            new ForgeLanguage("it","Italiano"),
            new ForgeLanguage("pl","Polski"),
            new ForgeLanguage("ru","Русский"),
            new ForgeLanguage("tr","Türkçe"),
            new ForgeLanguage("zh-HANS","简体中文"),
            new ForgeLanguage("zh-HANT","繁體中文"),
            new ForgeLanguage("ja","日本語"),
            new ForgeLanguage("ko","한국어")
        };
        public static IReadOnlyList<ForgeLanguage> SupportedLanguages => Languages;
        static readonly string[] CachedEnglishKeys = English.Keys.OrderBy(key => key, StringComparer.Ordinal).ToArray();
        public static IReadOnlyList<string> EnglishKeys => CachedEnglishKeys;

        static Dictionary<string,Dictionary<string,string>> LoadCatalogs()
        {
            var catalogs=new Dictionary<string,Dictionary<string,string>>(StringComparer.OrdinalIgnoreCase);
            var assembly=typeof(Localization).Assembly;
            foreach(var resource in assembly.GetManifestResourceNames().Where(name=>name.StartsWith("CalradiaForge.Localization.",StringComparison.Ordinal) && name.EndsWith(".xml",StringComparison.OrdinalIgnoreCase)))
            {
                var code=resource.Substring("CalradiaForge.Localization.".Length,resource.Length-"CalradiaForge.Localization.".Length-4);
                using(var stream=assembly.GetManifestResourceStream(resource))
                {
                    if(stream==null) continue;
                    var entries=XDocument.Load(stream).Root.Elements("string").ToDictionary(e=>(string)e.Attribute("key"),e=>(string)e.Attribute("value"),StringComparer.Ordinal);
                    catalogs[code]=entries;
                }
            }
            if(!catalogs.ContainsKey(DefaultLanguage)) throw new InvalidOperationException("The English localization catalog is missing.");
            return catalogs;
        }

        static Dictionary<string,string> Load(string language)
        {
            using(var stream=typeof(Localization).Assembly.GetManifestResourceStream("CalradiaForge.Localization."+language+".xml"))
                return XDocument.Load(stream).Root.Elements("string").ToDictionary(e=>(string)e.Attribute("key"),e=>(string)e.Attribute("value"));
        }
        public static string Text(string key,string language)
        {
            if(key==null)return string.Empty;
            if(language!=null && Catalogs.TryGetValue(language,out var catalog) && catalog.TryGetValue(key,out var translated) && !string.IsNullOrWhiteSpace(translated))return translated;
            return English.TryGetValue(key,out var original)?original:key;
        }

        /// <summary>Maps a visible catalog value back to its stable English key for UI language switching.</summary>
        public static bool TryGetKeyForText(string value,out string key)
        {
            key=null;
            if(string.IsNullOrWhiteSpace(value)) return false;
            foreach(var catalog in Catalogs.Values)
            {
                foreach(var pair in catalog)
                {
                    if(string.Equals(pair.Value, value, StringComparison.Ordinal))
                    {
                        key = pair.Key;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
