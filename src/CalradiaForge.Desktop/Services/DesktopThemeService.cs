using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CalradiaForge.Desktop.Services
{
    internal sealed class DesktopThemeDescriptor(string id, string displayKey, string descriptionKey, string paletteUri)
    {
        public string Id { get; } = id;
        public string DisplayKey { get; } = displayKey;
        public string DescriptionKey { get; } = descriptionKey;
        public string PaletteUri { get; } = paletteUri;
    }

    internal sealed class ThemeApplyResult
    {
        public bool Succeeded { get; set; }
        public bool UsedFallback { get; set; }
        public string ThemeId { get; set; }
        public string MessageKey { get; set; }
        public string Message { get; set; }
    }

    /// <summary>Swaps Forge-owned palette dictionaries without walking or mutating WPF controls.</summary>
    internal sealed class DesktopThemeService
    {
        const string DefaultThemeId = "war-table";
        static readonly IReadOnlyList<DesktopThemeDescriptor> themes =
        [
            new(DefaultThemeId, "Ui.ThemeWarTable", "Ui.ThemeContrastWarTable", "/CalradiaForge.Desktop;component/Resources/TacticalPalette.xaml"),
            new("parchment", "Ui.ThemeParchment", "Ui.ThemeContrastParchment", "/CalradiaForge.Desktop;component/Resources/Themes/TacticalPalette.Parchment.xaml"),
            new("high-contrast", "Ui.ThemeHighContrast", "Ui.ThemeContrastHighContrast", "/CalradiaForge.Desktop;component/Resources/Themes/TacticalPalette.HighContrast.xaml")
        ];

        ResourceDictionary activePalette;

        public IReadOnlyList<DesktopThemeDescriptor> AvailableThemes => themes;
        public string SelectedThemeId { get; private set; } = DefaultThemeId;
        public string LastError { get; private set; }

        public void ApplyDefault()
        {
            var dictionaries = Application.Current?.Resources?.MergedDictionaries;
            activePalette = dictionaries?.FirstOrDefault(IsPaletteDictionary);
            SelectedThemeId = DefaultThemeId;
            LastError = null;
            FreezeDictionaryResources(activePalette);
            if (Application.Current?.Resources != null)
            {
                FreezeDictionaryResources(Application.Current.Resources);
            }
            BrushKeyConverter.InvalidateCache();
            DecorativeAccentVisibilityConverter.InvalidateCache();
        }

        public ThemeApplyResult Apply(string requestedThemeId)
        {
            var descriptor = themes.FirstOrDefault(item => string.Equals(item.Id, requestedThemeId, StringComparison.OrdinalIgnoreCase));
            var usedFallback = descriptor == null;
            descriptor ??= themes[0];
            var dictionaries = Application.Current?.Resources?.MergedDictionaries;
            if (dictionaries == null)
            {
                LastError = "Theme resources are unavailable.";
                return new ThemeApplyResult { Succeeded = false, UsedFallback = usedFallback, ThemeId = SelectedThemeId, MessageKey = "Ui.ThemeApplyFailed", Message = LastError };
            }

            var previousPalettes = dictionaries.Where(IsPaletteDictionary).ToArray();
            var insertIndex = previousPalettes.Length == 0
                ? Math.Min(1, dictionaries.Count)
                : Enumerable.Range(0, dictionaries.Count).First(index => IsPaletteDictionary(dictionaries[index]));
            var previous = activePalette;
            try
            {
                var next = new ResourceDictionary { Source = new Uri(descriptor.PaletteUri, UriKind.Relative) };
                FreezeDictionaryResources(next);
                // Remove all Forge palette copies before inserting one at the same shared resource slot.
                // This also repairs duplicated dictionaries left by older preview builds.
                for (var index = dictionaries.Count - 1; index >= 0; index--)
                    if (IsPaletteDictionary(dictionaries[index])) dictionaries.RemoveAt(index);
                dictionaries.Insert(Math.Min(insertIndex, dictionaries.Count), next);
                activePalette = next;
                SelectedThemeId = descriptor.Id;
                BrushKeyConverter.InvalidateCache();
                DecorativeAccentVisibilityConverter.InvalidateCache();
                LastError = usedFallback ? "Theme preference was unknown; War Table was restored." : null;
                return new ThemeApplyResult { Succeeded = true, UsedFallback = usedFallback, ThemeId = descriptor.Id, MessageKey = usedFallback ? "Ui.ThemeFallback" : null, Message = LastError };
            }
            catch (Exception error)
            {
                // A failed dictionary load or swap must not strand the application between palettes.
                try
                {
                    for (var index = dictionaries.Count - 1; index >= 0; index--)
                        if (IsPaletteDictionary(dictionaries[index])) dictionaries.RemoveAt(index);
                    for (var index = 0; index < previousPalettes.Length; index++)
                        dictionaries.Insert(Math.Min(insertIndex + index, dictionaries.Count), previousPalettes[index]);
                    activePalette = previous;
                    BrushKeyConverter.InvalidateCache();
                    DecorativeAccentVisibilityConverter.InvalidateCache();
                }
                catch { }
                LastError = "Theme change failed; the current theme was kept.";
                return new ThemeApplyResult { Succeeded = false, UsedFallback = usedFallback, ThemeId = SelectedThemeId, MessageKey = "Ui.ThemeApplyFailed", Message = LastError + " " + error.Message };
            }
        }

        public string ResolveThemeId(string requestedThemeId)
        {
            var match = themes.FirstOrDefault(item => string.Equals(item.Id, requestedThemeId, StringComparison.OrdinalIgnoreCase));
            return match?.Id ?? DefaultThemeId;
        }

        static bool IsPaletteDictionary(ResourceDictionary dictionary) =>
            dictionary?.Source?.OriginalString.IndexOf("TacticalPalette", StringComparison.OrdinalIgnoreCase) >= 0;

        static void FreezeDictionaryResources(ResourceDictionary dictionary)
        {
            if (dictionary == null) return;
            foreach (var key in dictionary.Keys)
            {
                if (dictionary[key] is Freezable freezable && freezable.CanFreeze && !freezable.IsFrozen)
                {
                    try { freezable.Freeze(); } catch { }
                }
            }
            if (dictionary.MergedDictionaries != null)
            {
                for (int i = 0; i < dictionary.MergedDictionaries.Count; i++)
                {
                    FreezeDictionaryResources(dictionary.MergedDictionaries[i]);
                }
            }
        }
    }
}
