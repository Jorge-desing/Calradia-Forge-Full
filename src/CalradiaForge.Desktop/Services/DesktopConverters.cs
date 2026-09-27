using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using CalradiaForge.Desktop.Presentation;

namespace CalradiaForge.Desktop.Services
{
    /// <summary>Used only for explicit empty states; no visual-tree traversal is needed.</summary>
    internal sealed class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool flag && !flag ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>Hides passive image details when accents are off or the active theme suppresses them.</summary>
    internal sealed class DecorativeAccentVisibilityConverter : IMultiValueConverter
    {
        static readonly Dictionary<string, Visibility> visibilityCache = new(StringComparer.Ordinal);
        static readonly object syncRoot = new();

        internal static void InvalidateCache()
        {
            lock (syncRoot)
            {
                visibilityCache.Clear();
            }
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is not [bool enabled, ..] || !enabled) return Visibility.Collapsed;
            var key = parameter?.ToString();
            if (string.IsNullOrEmpty(key)) return Visibility.Collapsed;
            lock (syncRoot)
            {
                if (visibilityCache.TryGetValue(key, out var cached)) return cached;
                var res = Application.Current?.TryFindResource(key);
                var result = res is Visibility.Visible ? Visibility.Visible : Visibility.Collapsed;
                visibilityCache[key] = result;
                return result;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>Keeps the summary artwork large on normal screens and proportionally compact at the minimum window width.</summary>
    internal sealed class ResponsiveDecorationDimensionConverter : IValueConverter
    {
        const double CompactWindowBreakpoint = 1120;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var compact = value is double width && width > 0 && width < CompactWindowBreakpoint;
            return parameter?.ToString() switch
            {
                "corner-width" => compact ? 88d : 136d,
                "corner-height" => compact ? 59d : 90d,
                "board-width" => compact ? 92d : 160d,
                "board-height" => compact ? 52d : 90d,
                "shield-size" => compact ? 16d : 20d,
                _ => DependencyProperty.UnsetValue
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>Renders a favorite state without mutating the visual tree or duplicating tool rows.</summary>
    internal sealed class PinnedGlyphConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values is [ToolDefinition tool, IEnumerable pinned, ..])
            {
                if (pinned is IList<ToolDefinition> list)
                {
                    if (list.Count == 0) return "☆";
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (ReferenceEquals(list[i], tool)) return "★";
                    }
                    return "☆";
                }
                if (pinned is IReadOnlyList<ToolDefinition> roList)
                {
                    if (roList.Count == 0) return "☆";
                    for (int i = 0; i < roList.Count; i++)
                    {
                        if (ReferenceEquals(roList[i], tool)) return "★";
                    }
                    return "☆";
                }
                foreach (var item in pinned) if (ReferenceEquals(item, tool)) return "★";
            }
            return "☆";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>Resolves a semantic geometry key to a font-independent WPF vector with high-performance frozen caching.</summary>
    internal sealed class GeometryKeyConverter : IValueConverter
    {
        static readonly Dictionary<string, Geometry> geometryCache = new(StringComparer.Ordinal);
        static readonly object syncRoot = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string key) return null;
            lock (syncRoot)
            {
                if (geometryCache.TryGetValue(key, out var cached)) return cached;
                if (Application.Current?.TryFindResource(key) is Geometry geom)
                {
                    if (geom.CanFreeze && !geom.IsFrozen) geom.Freeze();
                    geometryCache[key] = geom;
                    return geom;
                }
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    internal sealed class ThemeDisplayConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is DesktopThemeDescriptor theme ? Application.Current?.TryFindResource(theme.DisplayKey) as string ?? theme.Id : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    internal sealed class ThemeDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is DesktopThemeDescriptor theme ? Application.Current?.TryFindResource(theme.DescriptionKey) as string ?? string.Empty : string.Empty;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>Resolves a semantic brush key to a dynamic WPF SolidColorBrush with active theme palette caching.</summary>
    internal sealed class BrushKeyConverter : IValueConverter
    {
        static readonly Dictionary<string, Brush> brushCache = new(StringComparer.Ordinal);
        static readonly object syncRoot = new();

        internal static void InvalidateCache()
        {
            lock (syncRoot)
            {
                brushCache.Clear();
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string key) return Brushes.Goldenrod;
            lock (syncRoot)
            {
                if (brushCache.TryGetValue(key, out var cached)) return cached;
                var brush = Application.Current?.TryFindResource(key) as Brush ?? Brushes.Goldenrod;
                if (brush.CanFreeze && !brush.IsFrozen)
                {
                    try { brush.Freeze(); } catch { }
                }
                brushCache[key] = brush;
                return brush;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }
}
