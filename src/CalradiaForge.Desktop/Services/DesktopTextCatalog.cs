using System;
using System.Linq;
using CalradiaForge.Core;

namespace CalradiaForge.Desktop.Services
{
    /// <summary>Resolves only a Bannerlord language code. Visible UI text comes from WPF resource dictionaries.</summary>
    internal static class DesktopTextCatalog
    {
        public static string ResolveCode(string displayOrCode)
        {
            var language = Localization.SupportedLanguages.FirstOrDefault(item =>
                string.Equals(item.Code, displayOrCode, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(item.DisplayName, displayOrCode, StringComparison.OrdinalIgnoreCase));
            return language == null ? Localization.DefaultLanguage : language.Code;
        }
    }
}
