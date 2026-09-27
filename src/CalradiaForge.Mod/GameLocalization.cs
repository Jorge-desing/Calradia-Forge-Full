using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TaleWorlds.Localization;

namespace CalradiaForge.Mod
{
    internal static class GameLocalization
    {
        // Cache only tokens: translating on each call respects the game's current language.
        // ConcurrentDictionary ensures thread-safe reads and writes without collection modification exceptions.
        static readonly ConcurrentDictionary<string, string> tokens =
            new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        private static readonly PropertyInfo CachedCurrentLangProp =
            typeof(TextObject).Assembly.GetType("TaleWorlds.Localization.LocalizedTextManager")
                ?.GetProperty("CurrentLanguage", BindingFlags.Public | BindingFlags.Static);

        public static int RegisteredCount => tokens.Count;

        public static string CurrentLanguage
        {
            get
            {
                try
                {
                    var lang = CachedCurrentLangProp?.GetValue(null, null)?.ToString();
                    if (!string.IsNullOrEmpty(lang)) return lang;
                }
                catch { }
                return "English";
            }
        }

        public static string Text(string key)
        {
            if (string.IsNullOrEmpty(key)) return string.Empty;

            var token = tokens.GetOrAdd(key, k =>
            {
                using (var hash = SHA256.Create())
                {
                    var bytes = hash.ComputeHash(Encoding.UTF8.GetBytes(k));
                    var identifier = new StringBuilder("forge_");
                    for (var i = 0; i < 6; i++) identifier.Append(bytes[i].ToString("x2"));
                    return "{=" + identifier + "}" + k;
                }
            });

            return new TextObject(token).ToString();
        }
    }
}
