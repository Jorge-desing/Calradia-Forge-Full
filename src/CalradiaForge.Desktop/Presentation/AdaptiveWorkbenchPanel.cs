using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CalradiaForge.Desktop.Presentation
{
    /// <summary>
    /// Keeps the selected route and optional secondary surfaces side-by-side when they fit;
    /// otherwise it stacks them in the workspace's vertical scroll viewport.
    /// Child order is primary route, pinned deck, contextual dossier.
    /// </summary>
    public sealed class AdaptiveWorkbenchPanel : Panel
    {
        const double DefaultStackedDeckHeight = 168;
        const double MinimumStackedDeckHeight = 124;
        const double MinimumPrimaryHeight = 120;

        public static readonly DependencyProperty MinimumPrimaryWidthProperty = DependencyProperty.Register(
            nameof(MinimumPrimaryWidth), typeof(double), typeof(AdaptiveWorkbenchPanel),
            new FrameworkPropertyMetadata(360d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange), IsFiniteNonNegative);

        public static readonly DependencyProperty PreferredSecondaryWidthProperty = DependencyProperty.Register(
            nameof(PreferredSecondaryWidth), typeof(double), typeof(AdaptiveWorkbenchPanel),
            new FrameworkPropertyMetadata(280d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange), IsFiniteNonNegative);

        public static readonly DependencyProperty MinimumSecondaryWidthProperty = DependencyProperty.Register(
            nameof(MinimumSecondaryWidth), typeof(double), typeof(AdaptiveWorkbenchPanel),
            new FrameworkPropertyMetadata(250d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange), IsFiniteNonNegative);

        public static readonly DependencyProperty PreferredContextWidthProperty = DependencyProperty.Register(
            nameof(PreferredContextWidth), typeof(double), typeof(AdaptiveWorkbenchPanel),
            new FrameworkPropertyMetadata(310d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange), IsFiniteNonNegative);

        public static readonly DependencyProperty MinimumContextWidthProperty = DependencyProperty.Register(
            nameof(MinimumContextWidth), typeof(double), typeof(AdaptiveWorkbenchPanel),
            new FrameworkPropertyMetadata(260d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange), IsFiniteNonNegative);

        public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
            nameof(Gap), typeof(double), typeof(AdaptiveWorkbenchPanel),
            new FrameworkPropertyMetadata(8d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange), IsFiniteNonNegative);

        public double MinimumPrimaryWidth { get => (double)GetValue(MinimumPrimaryWidthProperty); set => SetValue(MinimumPrimaryWidthProperty, value); }
        public double PreferredSecondaryWidth { get => (double)GetValue(PreferredSecondaryWidthProperty); set => SetValue(PreferredSecondaryWidthProperty, value); }
        public double MinimumSecondaryWidth { get => (double)GetValue(MinimumSecondaryWidthProperty); set => SetValue(MinimumSecondaryWidthProperty, value); }
        public double PreferredContextWidth { get => (double)GetValue(PreferredContextWidthProperty); set => SetValue(PreferredContextWidthProperty, value); }
        public double MinimumContextWidth { get => (double)GetValue(MinimumContextWidthProperty); set => SetValue(MinimumContextWidthProperty, value); }
        public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Children.Count == 0)
                return new Size(0, 0);

            var hasDeck = IsVisible(1);
            var hasContext = IsVisible(2);
            var finiteWidth = IsFinite(availableSize.Width);
            if (Children.Count <= 3 && CanFitHorizontally(availableSize.Width, hasDeck, hasContext))
            {
                GetHorizontalWidths(availableSize.Width, hasDeck, hasContext, out var primaryWidth, out var deckWidth, out var contextWidth);
                var viewportHeight = GetAvailableViewportHeight();
                var measureHeight = IsFinite(availableSize.Height) ? availableSize.Height : viewportHeight;
                Children[0].Measure(new Size(primaryWidth, measureHeight));
                if (hasDeck) Children[1].Measure(new Size(deckWidth, measureHeight));
                if (hasContext) Children[2].Measure(new Size(contextWidth, measureHeight));
                var height = Children[0].DesiredSize.Height;
                if (hasDeck) height = Math.Max(height, Children[1].DesiredSize.Height);
                if (hasContext) height = Math.Max(height, Children[2].DesiredSize.Height);
                if (IsFinite(viewportHeight) && viewportHeight > 0)
                    height = Math.Min(height, viewportHeight);
                return new Size(availableSize.Width, height);
            }

            var width = finiteWidth ? availableSize.Width : double.PositiveInfinity;
            var desiredWidth = 0d;
            var desiredHeight = 0d;
            var visibleCount = 0;
            for (var index = 0; index < Children.Count; index++)
            {
                var child = Children[index];
                if (child.Visibility == Visibility.Collapsed)
                {
                    child.Measure(new Size(0, 0));
                    continue;
                }

                child.Measure(new Size(width, double.PositiveInfinity));
                desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
                desiredHeight += GetStackedHeight(index, child);
                visibleCount++;
            }

            if (visibleCount > 1)
                desiredHeight += Gap * (visibleCount - 1);
            return new Size(finiteWidth ? availableSize.Width : desiredWidth, desiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count == 0)
                return finalSize;

            var hasDeck = IsVisible(1);
            var hasContext = IsVisible(2);
            if (Children.Count <= 3 && CanFitHorizontally(finalSize.Width, hasDeck, hasContext))
            {
                GetHorizontalWidths(finalSize.Width, hasDeck, hasContext, out var primaryWidth, out var deckWidth, out var contextWidth);
                var x = 0d;
                Children[0].Arrange(new Rect(x, 0, primaryWidth, finalSize.Height));
                x += primaryWidth;
                if (hasDeck)
                {
                    x += Gap;
                    Children[1].Arrange(new Rect(x, 0, deckWidth, finalSize.Height));
                    x += deckWidth;
                }
                else if (Children.Count > 1)
                {
                    Children[1].Arrange(new Rect(0, 0, 0, 0));
                }

                if (hasContext)
                {
                    x += Gap;
                    Children[2].Arrange(new Rect(x, 0, contextWidth, finalSize.Height));
                }
                else if (Children.Count > 2)
                {
                    Children[2].Arrange(new Rect(0, 0, 0, 0));
                }
                return finalSize;
            }

            var y = 0d;
            for (var index = 0; index < Children.Count; index++)
            {
                var child = Children[index];
                if (child.Visibility == Visibility.Collapsed)
                {
                    child.Arrange(new Rect(0, 0, 0, 0));
                    continue;
                }

                var height = GetStackedHeight(index, child);
                if (index > 0)
                    y += Gap;
                child.Arrange(new Rect(0, y, finalSize.Width, height));
                y += height;
            }
            return finalSize;
        }

        bool CanFitHorizontally(double width, bool hasDeck, bool hasContext)
        {
            if (!IsFinite(width))
                return false;
            var visibleCount = 1 + (hasDeck ? 1 : 0) + (hasContext ? 1 : 0);
            var required = MinimumPrimaryWidth + Gap * (visibleCount - 1);
            if (hasDeck) required += MinimumSecondaryWidth;
            if (hasContext) required += MinimumContextWidth;
            return width >= required;
        }

        void GetHorizontalWidths(double width, bool hasDeck, bool hasContext, out double primary, out double deck, out double context)
        {
            var gaps = Gap * ((hasDeck ? 1 : 0) + (hasContext ? 1 : 0));
            var remaining = Math.Max(MinimumPrimaryWidth, width - gaps);
            context = hasContext ? Math.Min(Math.Max(MinimumContextWidth, PreferredContextWidth), remaining - MinimumPrimaryWidth - (hasDeck ? MinimumSecondaryWidth : 0)) : 0;
            remaining -= context;
            deck = hasDeck ? Math.Min(Math.Max(MinimumSecondaryWidth, PreferredSecondaryWidth), remaining - MinimumPrimaryWidth) : 0;
            primary = Math.Max(0, width - gaps - deck - context);
        }

        double GetAvailableViewportHeight()
        {
            for (DependencyObject cur = VisualTreeHelper.GetParent(this); cur != null; cur = VisualTreeHelper.GetParent(cur))
            {
                if (cur is ScrollViewer sv)
                {
                    if (IsFinite(sv.ViewportHeight) && sv.ViewportHeight > 0)
                        return sv.ViewportHeight;
                    if (IsFinite(sv.ActualHeight) && sv.ActualHeight > 0)
                        return sv.ActualHeight;
                }
                if (cur is Grid g && IsFinite(g.ActualHeight) && g.ActualHeight > 0)
                    return g.ActualHeight;
                if (cur is Window w && IsFinite(w.ActualHeight) && w.ActualHeight > 0)
                    return Math.Max(MinimumPrimaryHeight, w.ActualHeight - 250);
            }
            return double.PositiveInfinity;
        }

        new bool IsVisible(int index) => index < Children.Count && Children[index].Visibility != Visibility.Collapsed;
        static bool IsFinite(double value) => !double.IsInfinity(value) && !double.IsNaN(value);
        static double NormalizeDesired(double value) => IsFinite(value) ? Math.Max(0, value) : 0;
        static double GetStackedHeight(int index, UIElement child)
        {
            var height = NormalizeDesired(child.DesiredSize.Height);
            return index == 1 ? Math.Min(Math.Max(height, MinimumStackedDeckHeight), DefaultStackedDeckHeight) : height;
        }
        static bool IsFiniteNonNegative(object value) => value is double number && IsFinite(number) && number >= 0;
    }
}
