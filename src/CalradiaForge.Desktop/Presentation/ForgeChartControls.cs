#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CalradiaForge.Desktop.Presentation
{
    static class ChartPropertyRegistration
    {
        internal static DependencyProperty Render(string name, Type propertyType, Type ownerType, object? defaultValue) =>
            DependencyProperty.Register(name, propertyType, ownerType,
                new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.AffectsRender));
    }

    internal static class ChartRenderHelper
    {
        internal static Brush CreateAlphaBrush(Brush? source, byte alpha, Color fallback)
        {
            if (source is SolidColorBrush scb)
            {
                var col = scb.Color;
                var b = new SolidColorBrush(Color.FromArgb(alpha, col.R, col.G, col.B));
                b.Freeze();
                return b;
            }
            var fb = new SolidColorBrush(Color.FromArgb(alpha, fallback.R, fallback.G, fallback.B));
            fb.Freeze();
            return fb;
        }

        internal static Pen CreateFrozenPen(Brush brush, double thickness, DashStyle? dash = null, PenLineCap cap = PenLineCap.Flat)
        {
            var pen = new Pen(brush, thickness)
            {
                DashStyle = dash ?? DashStyles.Solid,
                StartLineCap = cap,
                EndLineCap = cap
            };
            pen.Freeze();
            return pen;
        }

        internal static StreamGeometry BuildClosedPolygon(IReadOnlyList<Point> points)
        {
            var geom = new StreamGeometry();
            if (points == null || points.Count == 0)
            {
                geom.Freeze();
                return geom;
            }
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(points[0], true, true);
                for (int i = 1; i < points.Count; i++)
                {
                    ctx.LineTo(points[i], true, false);
                }
            }
            geom.Freeze();
            return geom;
        }

        internal static StreamGeometry BuildPolyline(IReadOnlyList<Point> points)
        {
            var geom = new StreamGeometry();
            if (points == null || points.Count == 0)
            {
                geom.Freeze();
                return geom;
            }
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(points[0], false, false);
                for (int i = 1; i < points.Count; i++)
                {
                    ctx.LineTo(points[i], true, false);
                }
            }
            geom.Freeze();
            return geom;
        }

        internal static Point PolarToCartesian(double cx, double cy, double radius, double angleRad) =>
            new Point(cx + radius * Math.Cos(angleRad), cy + radius * Math.Sin(angleRad));
    }

    // =========================================================================
    // FORGE SPARKLINE CONTROL (LIGHTWEIGHT HARDWARE-ACCELERATED TIME-SERIES / AREA)
    // =========================================================================

    public sealed class ForgeSparkline : FrameworkElement
    {
        public static readonly DependencyProperty DataPointsProperty = ChartPropertyRegistration.Render(
            nameof(DataPoints), typeof(IReadOnlyList<double>), typeof(ForgeSparkline), Array.Empty<double>());

        public static readonly DependencyProperty StrokeProperty = ChartPropertyRegistration.Render(
            nameof(Stroke), typeof(Brush), typeof(ForgeSparkline), null);

        public static readonly DependencyProperty FillProperty = ChartPropertyRegistration.Render(
            nameof(Fill), typeof(Brush), typeof(ForgeSparkline), null);

        public static readonly DependencyProperty StrokeThicknessProperty = ChartPropertyRegistration.Render(
            nameof(StrokeThickness), typeof(double), typeof(ForgeSparkline), 1.5);

        public static readonly DependencyProperty ShowGridLinesProperty = ChartPropertyRegistration.Render(
            nameof(ShowGridLines), typeof(bool), typeof(ForgeSparkline), true);

        public static readonly DependencyProperty GridBrushProperty = ChartPropertyRegistration.Render(
            nameof(GridBrush), typeof(Brush), typeof(ForgeSparkline), null);

        public static readonly DependencyProperty ValueUnitProperty =
            DependencyProperty.Register(nameof(ValueUnit), typeof(string), typeof(ForgeSparkline),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FormatStringProperty =
            DependencyProperty.Register(nameof(FormatString), typeof(string), typeof(ForgeSparkline),
                new PropertyMetadata("N0"));

        public IReadOnlyList<double> DataPoints { get => (IReadOnlyList<double>)GetValue(DataPointsProperty); set => SetValue(DataPointsProperty, value); }

        public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

        public Brush Fill { get => (Brush)GetValue(FillProperty); set => SetValue(FillProperty, value); }

        public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

        public bool ShowGridLines { get => (bool)GetValue(ShowGridLinesProperty); set => SetValue(ShowGridLinesProperty, value); }

        public Brush GridBrush { get => (Brush)GetValue(GridBrushProperty); set => SetValue(GridBrushProperty, value); }

        public string ValueUnit { get => (string)GetValue(ValueUnitProperty); set => SetValue(ValueUnitProperty, value); }

        public string FormatString { get => (string)GetValue(FormatStringProperty); set => SetValue(FormatStringProperty, value); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var points = DataPoints;
            if (points == null || points.Count < 2 || ActualWidth <= 12)
            {
                ToolTip = null;
                return;
            }

            var pos = e.GetPosition(this);
            const double padX = 6.0;
            var w = Math.Max(1.0, ActualWidth - 2 * padX);
            var step = w / (points.Count - 1);
            var idx = (int)Math.Clamp(Math.Round((pos.X - padX) / step), 0, points.Count - 1);
            var val = points[idx];
            var unit = string.IsNullOrEmpty(ValueUnit) ? string.Empty : " " + ValueUnit;
            ToolTip = $"Index #{idx + 1}: {val.ToString(FormatString, CultureInfo.InvariantCulture)}{unit}";
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var w = ActualWidth;
            var h = ActualHeight;
            if (w <= 8 || h <= 8) return;

            var points = DataPoints;
            var effectiveStroke = Stroke ?? Brushes.Goldenrod;
            var effectiveGrid = GridBrush ?? new SolidColorBrush(Color.FromArgb(40, 200, 200, 200));

            // Background border / bounds
            const double padX = 6.0;
            const double padY = 6.0;
            var usableW = w - 2 * padX;
            var usableH = h - 2 * padY;

            // Subtle Grid lines
            if (ShowGridLines && usableH > 16)
            {
                var gridPen = ChartRenderHelper.CreateFrozenPen(effectiveGrid, 0.8, DashStyles.Dash);
                dc.DrawLine(gridPen, new Point(padX, padY + usableH * 0.25), new Point(w - padX, padY + usableH * 0.25));
                dc.DrawLine(gridPen, new Point(padX, padY + usableH * 0.50), new Point(w - padX, padY + usableH * 0.50));
                dc.DrawLine(gridPen, new Point(padX, padY + usableH * 0.75), new Point(w - padX, padY + usableH * 0.75));
            }

            if (points == null || points.Count < 2) return;

            // Min and Max calculation
            double min = double.MaxValue;
            double max = double.MinValue;
            for (int i = 0; i < points.Count; i++)
            {
                var v = points[i];
                if (v < min) min = v;
                if (v > max) max = v;
            }

            var range = Math.Max(0.001, max - min);
            var stepX = usableW / (points.Count - 1);

            Point MapPoint(int i)
            {
                var x = padX + i * stepX;
                var norm = (points[i] - min) / range;
                var y = padY + (1.0 - norm) * usableH;
                return new Point(x, y);
            }

            var firstPt = MapPoint(0);
            var lastPt = MapPoint(points.Count - 1);

            // Shaded Area (gradient under line)
            var areaBrush = Fill;
            if (areaBrush == null)
            {
                var sc = (effectiveStroke as SolidColorBrush)?.Color ?? Colors.Goldenrod;
                var grad = new LinearGradientBrush(
                    Color.FromArgb(70, sc.R, sc.G, sc.B),
                    Color.FromArgb(5, sc.R, sc.G, sc.B),
                    new Point(0, 0),
                    new Point(0, 1));
                grad.Freeze();
                areaBrush = grad;
            }

            var areaGeom = new StreamGeometry();
            using (var ctx = areaGeom.Open())
            {
                ctx.BeginFigure(new Point(padX, h - padY), true, true);
                ctx.LineTo(firstPt, true, false);
                for (int i = 1; i < points.Count; i++)
                {
                    ctx.LineTo(MapPoint(i), true, false);
                }
                ctx.LineTo(new Point(lastPt.X, h - padY), true, false);
            }
            areaGeom.Freeze();
            dc.DrawGeometry(areaBrush, null, areaGeom);

            // Polyline Stroke
            var lineGeom = new StreamGeometry();
            using (var ctx = lineGeom.Open())
            {
                ctx.BeginFigure(firstPt, false, false);
                for (int i = 1; i < points.Count; i++)
                {
                    ctx.LineTo(MapPoint(i), true, false);
                }
            }
            lineGeom.Freeze();
            var linePen = ChartRenderHelper.CreateFrozenPen(effectiveStroke, StrokeThickness, cap: PenLineCap.Round);
            dc.DrawGeometry(null, linePen, lineGeom);

            // Milestone Marker on final point
            dc.DrawEllipse(effectiveStroke, null, lastPt, 2.5, 2.5);
        }
    }

    // =========================================================================
    // FORGE RADAR CHART (POLYGONAL MULTIAXIAL COMBAT & POWER BALANCE RADAR)
    // =========================================================================

    public sealed class ForgeRadarChart : FrameworkElement
    {
        public static readonly DependencyProperty AxisLabelsProperty = ChartPropertyRegistration.Render(
            nameof(AxisLabels), typeof(IReadOnlyList<string>), typeof(ForgeRadarChart), Array.Empty<string>());

        public static readonly DependencyProperty ValuesProperty = ChartPropertyRegistration.Render(
            nameof(Values), typeof(IReadOnlyList<double>), typeof(ForgeRadarChart), Array.Empty<double>());

        public static readonly DependencyProperty ComparisonValuesProperty = ChartPropertyRegistration.Render(
            nameof(ComparisonValues), typeof(IReadOnlyList<double>), typeof(ForgeRadarChart), Array.Empty<double>());

        public static readonly DependencyProperty StrokeProperty = ChartPropertyRegistration.Render(
            nameof(Stroke), typeof(Brush), typeof(ForgeRadarChart), null);

        public static readonly DependencyProperty FillProperty = ChartPropertyRegistration.Render(
            nameof(Fill), typeof(Brush), typeof(ForgeRadarChart), null);

        public static readonly DependencyProperty ComparisonStrokeProperty = ChartPropertyRegistration.Render(
            nameof(ComparisonStroke), typeof(Brush), typeof(ForgeRadarChart), null);

        public static readonly DependencyProperty ComparisonFillProperty = ChartPropertyRegistration.Render(
            nameof(ComparisonFill), typeof(Brush), typeof(ForgeRadarChart), null);

        public static readonly DependencyProperty GridBrushProperty = ChartPropertyRegistration.Render(
            nameof(GridBrush), typeof(Brush), typeof(ForgeRadarChart), null);

        public static readonly DependencyProperty AxisLabelBrushProperty = ChartPropertyRegistration.Render(
            nameof(AxisLabelBrush), typeof(Brush), typeof(ForgeRadarChart), null);

        static readonly Typeface LabelTypeface = new("Segoe UI");

        public IReadOnlyList<string> AxisLabels { get => (IReadOnlyList<string>)GetValue(AxisLabelsProperty); set => SetValue(AxisLabelsProperty, value); }

        public IReadOnlyList<double> Values { get => (IReadOnlyList<double>)GetValue(ValuesProperty); set => SetValue(ValuesProperty, value); }

        public IReadOnlyList<double> ComparisonValues { get => (IReadOnlyList<double>)GetValue(ComparisonValuesProperty); set => SetValue(ComparisonValuesProperty, value); }

        public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

        public Brush Fill { get => (Brush)GetValue(FillProperty); set => SetValue(FillProperty, value); }

        public Brush ComparisonStroke { get => (Brush)GetValue(ComparisonStrokeProperty); set => SetValue(ComparisonStrokeProperty, value); }

        public Brush ComparisonFill { get => (Brush)GetValue(ComparisonFillProperty); set => SetValue(ComparisonFillProperty, value); }

        public Brush GridBrush { get => (Brush)GetValue(GridBrushProperty); set => SetValue(GridBrushProperty, value); }

        public Brush AxisLabelBrush { get => (Brush)GetValue(AxisLabelBrushProperty); set => SetValue(AxisLabelBrushProperty, value); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var labels = AxisLabels;
            var vals = Values;
            if (labels == null || vals == null || labels.Count == 0 || vals.Count == 0)
            {
                ToolTip = null;
                return;
            }

            var count = Math.Min(labels.Count, vals.Count);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.Append(" · ");
                sb.Append(labels[i]).Append(": ").Append((vals[i] * 100.0).ToString("F0", CultureInfo.InvariantCulture)).Append('%');
            }
            ToolTip = sb.ToString();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var w = ActualWidth;
            var h = ActualHeight;
            if (w <= 16 || h <= 16) return;

            var labels = AxisLabels;
            var vals = Values;
            int n = labels != null && labels.Count >= 3 ? labels.Count : (vals != null && vals.Count >= 3 ? vals.Count : 5);

            var cx = w / 2.0;
            var cy = h / 2.0;
            var maxR = Math.Max(10.0, Math.Min(cx, cy) - 18.0);

            var effectiveGrid = GridBrush ?? new SolidColorBrush(Color.FromArgb(45, 180, 180, 180));
            var effectiveLabel = AxisLabelBrush ?? new SolidColorBrush(Color.FromArgb(200, 220, 220, 220));
            var effectiveStroke = Stroke ?? Brushes.Goldenrod;
            var effectiveCompStroke = ComparisonStroke ?? Brushes.MediumSeaGreen;

            var gridPen = ChartRenderHelper.CreateFrozenPen(effectiveGrid, 0.8, DashStyles.Dash);

            Point Vertex(int i, double rFraction)
            {
                var angle = -Math.PI / 2.0 + i * (2.0 * Math.PI / n);
                var r = maxR * rFraction;
                return new Point(cx + r * Math.Cos(angle), cy + r * Math.Sin(angle));
            }

            // Concentric Polygon Rings (25%, 50%, 75%, 100%)
            double[] rings = [0.25, 0.50, 0.75, 1.0];
            for (int rIdx = 0; rIdx < rings.Length; rIdx++)
            {
                var ringPts = new Point[n];
                for (int i = 0; i < n; i++) ringPts[i] = Vertex(i, rings[rIdx]);
                var ringGeom = ChartRenderHelper.BuildClosedPolygon(ringPts);
                dc.DrawGeometry(null, gridPen, ringGeom);
            }

            // Radial Axis Lines & Labels
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            for (int i = 0; i < n; i++)
            {
                var outer = Vertex(i, 1.0);
                dc.DrawLine(gridPen, new Point(cx, cy), outer);

                if (labels != null && i < labels.Count)
                {
                    var text = new FormattedText(labels[i], CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        LabelTypeface, 7.5, effectiveLabel, dpi);
                    var labelAngle = -Math.PI / 2.0 + i * (2.0 * Math.PI / n);
                    var lx = cx + (maxR + 10.0) * Math.Cos(labelAngle) - (text.Width / 2.0);
                    var ly = cy + (maxR + 10.0) * Math.Sin(labelAngle) - (text.Height / 2.0);
                    dc.DrawText(text, new Point(lx, ly));
                }
            }

            // Secondary / Comparison Polygon (e.g. Captain Perks active)
            var comp = ComparisonValues;
            if (comp != null && comp.Count >= 3)
            {
                var compCount = Math.Min(n, comp.Count);
                var compPts = new Point[compCount];
                for (int i = 0; i < compCount; i++)
                {
                    compPts[i] = Vertex(i, Math.Clamp(comp[i], 0.05, 1.0));
                }
                var compGeom = ChartRenderHelper.BuildClosedPolygon(compPts);
                var compFillBrush = ComparisonFill ?? ChartRenderHelper.CreateAlphaBrush(effectiveCompStroke, 40, Colors.MediumSeaGreen);
                var compPen = ChartRenderHelper.CreateFrozenPen(effectiveCompStroke, 1.4);
                dc.DrawGeometry(compFillBrush, compPen, compGeom);
            }

            // Primary Polygon
            if (vals != null && vals.Count >= 3)
            {
                var vCount = Math.Min(n, vals.Count);
                var polyPts = new Point[vCount];
                for (int i = 0; i < vCount; i++)
                {
                    polyPts[i] = Vertex(i, Math.Clamp(vals[i], 0.05, 1.0));
                }
                var polyGeom = ChartRenderHelper.BuildClosedPolygon(polyPts);
                var fillBrush = Fill ?? ChartRenderHelper.CreateAlphaBrush(effectiveStroke, 70, Colors.Goldenrod);
                var strokePen = ChartRenderHelper.CreateFrozenPen(effectiveStroke, 1.6);
                dc.DrawGeometry(fillBrush, strokePen, polyGeom);

                // Small circular nodes on vertices
                for (int i = 0; i < vCount; i++)
                {
                    var pt = Vertex(i, Math.Clamp(vals[i], 0.05, 1.0));
                    dc.DrawEllipse(effectiveStroke, null, pt, 2.0, 2.0);
                }
            }
        }
    }

    // =========================================================================
    // FORGE RING GAUGE (CIRCULAR GAUGE FOR REBELLION, SENATE & SLOTS)
    // =========================================================================

    public sealed class ForgeRingGauge : FrameworkElement
    {
        public static readonly DependencyProperty ValueProperty = ChartPropertyRegistration.Render(
            nameof(Value), typeof(double), typeof(ForgeRingGauge), 0.0);

        public static readonly DependencyProperty MaximumProperty = ChartPropertyRegistration.Render(
            nameof(Maximum), typeof(double), typeof(ForgeRingGauge), 100.0);

        public static readonly DependencyProperty ThresholdProperty = ChartPropertyRegistration.Render(
            nameof(Threshold), typeof(double), typeof(ForgeRingGauge), 0.0);

        public static readonly DependencyProperty StrokeThicknessProperty = ChartPropertyRegistration.Render(
            nameof(StrokeThickness), typeof(double), typeof(ForgeRingGauge), 6.0);

        public static readonly DependencyProperty AccentBrushProperty = ChartPropertyRegistration.Render(
            nameof(AccentBrush), typeof(Brush), typeof(ForgeRingGauge), null);

        public static readonly DependencyProperty BackgroundBrushProperty = ChartPropertyRegistration.Render(
            nameof(BackgroundBrush), typeof(Brush), typeof(ForgeRingGauge), null);

        public static readonly DependencyProperty ThresholdBrushProperty = ChartPropertyRegistration.Render(
            nameof(ThresholdBrush), typeof(Brush), typeof(ForgeRingGauge), null);

        public static readonly DependencyProperty CenterTextProperty = ChartPropertyRegistration.Render(
            nameof(CenterText), typeof(string), typeof(ForgeRingGauge), string.Empty);

        public static readonly DependencyProperty SubTextProperty = ChartPropertyRegistration.Render(
            nameof(SubText), typeof(string), typeof(ForgeRingGauge), string.Empty);

        public static readonly DependencyProperty TextBrushProperty = ChartPropertyRegistration.Render(
            nameof(TextBrush), typeof(Brush), typeof(ForgeRingGauge), null);

        static readonly Typeface BoldTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        static readonly Typeface NormalTypeface = new("Segoe UI");

        public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

        public double Threshold { get => (double)GetValue(ThresholdProperty); set => SetValue(ThresholdProperty, value); }

        public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

        public Brush AccentBrush { get => (Brush)GetValue(AccentBrushProperty); set => SetValue(AccentBrushProperty, value); }

        public Brush BackgroundBrush { get => (Brush)GetValue(BackgroundBrushProperty); set => SetValue(BackgroundBrushProperty, value); }

        public Brush ThresholdBrush { get => (Brush)GetValue(ThresholdBrushProperty); set => SetValue(ThresholdBrushProperty, value); }

        public string CenterText { get => (string)GetValue(CenterTextProperty); set => SetValue(CenterTextProperty, value); }

        public string SubText { get => (string)GetValue(SubTextProperty); set => SetValue(SubTextProperty, value); }

        public Brush TextBrush { get => (Brush)GetValue(TextBrushProperty); set => SetValue(TextBrushProperty, value); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var max = Math.Max(0.001, Maximum);
            var pct = (Value / max) * 100.0;
            var sub = string.IsNullOrEmpty(SubText) ? "Value" : SubText;
            ToolTip = $"{sub}: {Value:0.0} / {Maximum:0.0} ({pct:0.0}%)";
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var w = ActualWidth;
            var h = ActualHeight;
            if (w <= 8 || h <= 8) return;

            var cx = w / 2.0;
            var cy = h / 2.0;
            var t = Math.Max(2.0, StrokeThickness);
            var r = Math.Max(4.0, (Math.Min(w, h) / 2.0) - t);

            var effectiveAccent = AccentBrush ?? Brushes.Goldenrod;
            var effectiveBg = BackgroundBrush ?? new SolidColorBrush(Color.FromArgb(40, 160, 160, 160));
            var effectiveText = TextBrush ?? Brushes.WhiteSmoke;

            // Background Full Ring
            var bgPen = ChartRenderHelper.CreateFrozenPen(effectiveBg, t);
            dc.DrawEllipse(null, bgPen, new Point(cx, cy), r, r);

            // Active Value Arc (starts at top: -90 deg)
            var maxVal = Math.Max(0.001, Maximum);
            var ratio = Math.Clamp(Value / maxVal, 0.0, 1.0);

            if (ratio >= 0.999)
            {
                var fullPen = ChartRenderHelper.CreateFrozenPen(effectiveAccent, t);
                dc.DrawEllipse(null, fullPen, new Point(cx, cy), r, r);
            }
            else if (ratio > 0.002)
            {
                var sweepAngle = ratio * 2.0 * Math.PI;
                var startPt = new Point(cx, cy - r);
                var endPt = new Point(cx + r * Math.Sin(sweepAngle), cy - r * Math.Cos(sweepAngle));
                var isLargeArc = ratio > 0.5;

                var arcFigure = new PathFigure { StartPoint = startPt, IsClosed = false };
                arcFigure.Segments.Add(new ArcSegment(endPt, new Size(r, r), 0, isLargeArc, SweepDirection.Clockwise, true));

                var arcGeom = new PathGeometry();
                arcGeom.Figures.Add(arcFigure);
                arcGeom.Freeze();

                var arcPen = ChartRenderHelper.CreateFrozenPen(effectiveAccent, t, cap: PenLineCap.Round);
                dc.DrawGeometry(null, arcPen, arcGeom);
            }

            // Threshold Tick Marker (e.g. 45% danger zone)
            if (Threshold > 0.0 && Threshold < Maximum)
            {
                var thRatio = Math.Clamp(Threshold / maxVal, 0.0, 1.0);
                var thAngle = thRatio * 2.0 * Math.PI;
                var p1 = new Point(cx + (r - t * 0.7) * Math.Sin(thAngle), cy - (r - t * 0.7) * Math.Cos(thAngle));
                var p2 = new Point(cx + (r + t * 0.7) * Math.Sin(thAngle), cy - (r + t * 0.7) * Math.Cos(thAngle));
                var thPen = ChartRenderHelper.CreateFrozenPen(ThresholdBrush ?? Brushes.IndianRed, 1.6);
                dc.DrawLine(thPen, p1, p2);
            }

            // Centered Text Labels
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var centerStr = !string.IsNullOrEmpty(CenterText) ? CenterText : $"{ratio * 100.0:0.0}%";
            var centerFt = new FormattedText(centerStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                BoldTypeface, Math.Max(8.0, r * 0.5), effectiveText, dpi);

            var textY = cy - (centerFt.Height / 2.0);
            if (!string.IsNullOrEmpty(SubText)) textY -= 4.0;

            dc.DrawText(centerFt, new Point(cx - (centerFt.Width / 2.0), textY));

            if (!string.IsNullOrEmpty(SubText) && r > 16)
            {
                var subFt = new FormattedText(SubText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    NormalTypeface, Math.Max(6.5, r * 0.28), effectiveText, dpi);
                dc.DrawText(subFt, new Point(cx - (subFt.Width / 2.0), textY + centerFt.Height + 1.0));
            }
        }
    }

    // =========================================================================
    // FORGE BAR DATA POINT & BAR CHART CONTROL (HIGH-DENSITY TACTICAL BARS)
    // =========================================================================

    public sealed class ForgeBarDataPoint
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
        public Brush? CustomBrush { get; set; }
        public string? DetailText { get; set; }

        public ForgeBarDataPoint() { }
        public ForgeBarDataPoint(string label, double value, Brush? customBrush = null, string? detailText = null)
        {
            Label = label;
            Value = value;
            CustomBrush = customBrush;
            DetailText = detailText;
        }
    }

    public sealed class ForgeBarChart : FrameworkElement
    {
        public static readonly DependencyProperty BarsProperty = ChartPropertyRegistration.Render(
            nameof(Bars), typeof(IReadOnlyList<ForgeBarDataPoint>), typeof(ForgeBarChart), Array.Empty<ForgeBarDataPoint>());

        public static readonly DependencyProperty BarBrushProperty = ChartPropertyRegistration.Render(
            nameof(BarBrush), typeof(Brush), typeof(ForgeBarChart), null);

        public static readonly DependencyProperty BackgroundBarBrushProperty = ChartPropertyRegistration.Render(
            nameof(BackgroundBarBrush), typeof(Brush), typeof(ForgeBarChart), null);

        public static readonly DependencyProperty LabelBrushProperty = ChartPropertyRegistration.Render(
            nameof(LabelBrush), typeof(Brush), typeof(ForgeBarChart), null);

        public static readonly DependencyProperty ValueBrushProperty = ChartPropertyRegistration.Render(
            nameof(ValueBrush), typeof(Brush), typeof(ForgeBarChart), null);

        public static readonly DependencyProperty MaximumProperty = ChartPropertyRegistration.Render(
            nameof(Maximum), typeof(double), typeof(ForgeBarChart), 0.0);

        public static readonly DependencyProperty BarThicknessProperty = ChartPropertyRegistration.Render(
            nameof(BarThickness), typeof(double), typeof(ForgeBarChart), 12.0);

        public static readonly DependencyProperty ValueUnitProperty =
            DependencyProperty.Register(nameof(ValueUnit), typeof(string), typeof(ForgeBarChart),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FormatStringProperty =
            DependencyProperty.Register(nameof(FormatString), typeof(string), typeof(ForgeBarChart),
                new PropertyMetadata("N0"));

        static readonly Typeface LabelTypeface = new("Segoe UI");
        static readonly Typeface ValueTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

        public IReadOnlyList<ForgeBarDataPoint> Bars { get => (IReadOnlyList<ForgeBarDataPoint>)GetValue(BarsProperty); set => SetValue(BarsProperty, value); }

        public Brush BarBrush { get => (Brush)GetValue(BarBrushProperty); set => SetValue(BarBrushProperty, value); }

        public Brush BackgroundBarBrush { get => (Brush)GetValue(BackgroundBarBrushProperty); set => SetValue(BackgroundBarBrushProperty, value); }

        public Brush LabelBrush { get => (Brush)GetValue(LabelBrushProperty); set => SetValue(LabelBrushProperty, value); }

        public Brush ValueBrush { get => (Brush)GetValue(ValueBrushProperty); set => SetValue(ValueBrushProperty, value); }

        public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }

        public double BarThickness { get => (double)GetValue(BarThicknessProperty); set => SetValue(BarThicknessProperty, value); }

        public string ValueUnit { get => (string)GetValue(ValueUnitProperty); set => SetValue(ValueUnitProperty, value); }

        public string FormatString { get => (string)GetValue(FormatStringProperty); set => SetValue(FormatStringProperty, value); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var bars = Bars;
            if (bars == null || bars.Count == 0 || ActualHeight <= 8)
            {
                ToolTip = null;
                return;
            }

            var pos = e.GetPosition(this);
            var rowH = ActualHeight / bars.Count;
            var idx = (int)Math.Clamp(Math.Floor(pos.Y / rowH), 0, bars.Count - 1);
            var b = bars[idx];
            var unit = string.IsNullOrEmpty(ValueUnit) ? string.Empty : " " + ValueUnit;
            var detail = string.IsNullOrEmpty(b.DetailText) ? string.Empty : $" ({b.DetailText})";
            ToolTip = $"{b.Label}: {b.Value.ToString(FormatString, CultureInfo.InvariantCulture)}{unit}{detail}";
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var w = ActualWidth;
            var h = ActualHeight;
            if (w <= 16 || h <= 8) return;

            var bars = Bars;
            if (bars == null || bars.Count == 0) return;

            var effectiveBar = BarBrush ?? Brushes.Goldenrod;
            var effectiveBg = BackgroundBarBrush ?? new SolidColorBrush(Color.FromArgb(35, 140, 140, 140));
            var effectiveLabel = LabelBrush ?? new SolidColorBrush(Color.FromArgb(220, 230, 230, 230));
            var effectiveVal = ValueBrush ?? Brushes.Goldenrod;

            double max = Maximum;
            if (max <= 0.0)
            {
                for (int i = 0; i < bars.Count; i++)
                {
                    if (bars[i].Value > max) max = bars[i].Value;
                }
                if (max <= 0.0) max = 1.0;
            }

            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            const double labelColWidth = 64.0;
            const double valueColWidth = 44.0;
            var barTrackWidth = Math.Max(10.0, w - labelColWidth - valueColWidth - 8.0);
            var rowH = h / bars.Count;
            var barH = Math.Clamp(BarThickness, 4.0, rowH - 4.0);

            for (int i = 0; i < bars.Count; i++)
            {
                var pt = bars[i];
                var y = i * rowH;
                var barY = y + (rowH - barH) / 2.0;

                // Category Label
                var lblFt = new FormattedText(pt.Label ?? string.Empty, CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, LabelTypeface, 8.0, effectiveLabel, dpi)
                {
                    MaxTextWidth = labelColWidth - 4.0,
                    MaxTextHeight = rowH,
                    Trimming = TextTrimming.CharacterEllipsis
                };
                dc.DrawText(lblFt, new Point(2.0, y + (rowH - lblFt.Height) / 2.0));

                // Background Track
                var trackRect = new Rect(labelColWidth, barY, barTrackWidth, barH);
                dc.DrawRoundedRectangle(effectiveBg, null, trackRect, 2.0, 2.0);

                // Active Bar
                var fillRatio = Math.Clamp(pt.Value / max, 0.0, 1.0);
                if (fillRatio > 0.001)
                {
                    var fillW = Math.Max(2.0, barTrackWidth * fillRatio);
                    var fillBrush = pt.CustomBrush ?? effectiveBar;
                    var fillRect = new Rect(labelColWidth, barY, fillW, barH);
                    dc.DrawRoundedRectangle(fillBrush, null, fillRect, 2.0, 2.0);
                }

                // Value Label
                var valStr = pt.Value.ToString(FormatString, CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(ValueUnit)) valStr += ValueUnit;
                var valFt = new FormattedText(valStr, CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, ValueTypeface, 8.0, effectiveVal, dpi);
                dc.DrawText(valFt, new Point(labelColWidth + barTrackWidth + 4.0, y + (rowH - valFt.Height) / 2.0));
            }
        }
    }

    // =========================================================================
    // FORGE HEATMAP GRID CONTROL (TACTICAL MATRIX & TIME CORRELATION MAP)
    // =========================================================================

    public sealed class ForgeHeatmapGrid : FrameworkElement
    {
        public static readonly DependencyProperty MatrixProperty = ChartPropertyRegistration.Render(
            nameof(Matrix), typeof(IReadOnlyList<IReadOnlyList<double>>), typeof(ForgeHeatmapGrid), Array.Empty<IReadOnlyList<double>>());

        public static readonly DependencyProperty RowHeadersProperty = ChartPropertyRegistration.Render(
            nameof(RowHeaders), typeof(IReadOnlyList<string>), typeof(ForgeHeatmapGrid), Array.Empty<string>());

        public static readonly DependencyProperty ColumnHeadersProperty = ChartPropertyRegistration.Render(
            nameof(ColumnHeaders), typeof(IReadOnlyList<string>), typeof(ForgeHeatmapGrid), Array.Empty<string>());

        public static readonly DependencyProperty BaseBrushProperty = ChartPropertyRegistration.Render(
            nameof(BaseBrush), typeof(Brush), typeof(ForgeHeatmapGrid), null);

        public static readonly DependencyProperty HotBrushProperty = ChartPropertyRegistration.Render(
            nameof(HotBrush), typeof(Brush), typeof(ForgeHeatmapGrid), null);

        public static readonly DependencyProperty CellRadiusProperty = ChartPropertyRegistration.Render(
            nameof(CellRadius), typeof(double), typeof(ForgeHeatmapGrid), 2.0);

        static readonly Typeface HeaderTypeface = new("Segoe UI");

        public IReadOnlyList<IReadOnlyList<double>> Matrix { get => (IReadOnlyList<IReadOnlyList<double>>)GetValue(MatrixProperty); set => SetValue(MatrixProperty, value); }

        public IReadOnlyList<string> RowHeaders { get => (IReadOnlyList<string>)GetValue(RowHeadersProperty); set => SetValue(RowHeadersProperty, value); }

        public IReadOnlyList<string> ColumnHeaders { get => (IReadOnlyList<string>)GetValue(ColumnHeadersProperty); set => SetValue(ColumnHeadersProperty, value); }

        public Brush BaseBrush { get => (Brush)GetValue(BaseBrushProperty); set => SetValue(BaseBrushProperty, value); }

        public Brush HotBrush { get => (Brush)GetValue(HotBrushProperty); set => SetValue(HotBrushProperty, value); }

        public double CellRadius { get => (double)GetValue(CellRadiusProperty); set => SetValue(CellRadiusProperty, value); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var m = Matrix;
            if (m == null || m.Count == 0 || m[0].Count == 0)
            {
                ToolTip = null;
                return;
            }

            var rows = m.Count;
            var cols = m[0].Count;
            const double rowHeaderW = 38.0;
            const double colHeaderH = 14.0;

            var pos = e.GetPosition(this);
            if (pos.X < rowHeaderW || pos.Y < colHeaderH)
            {
                ToolTip = null;
                return;
            }

            var gridW = Math.Max(1.0, ActualWidth - rowHeaderW);
            var gridH = Math.Max(1.0, ActualHeight - colHeaderH);
            var cW = gridW / cols;
            var cH = gridH / rows;

            var c = (int)Math.Clamp(Math.Floor((pos.X - rowHeaderW) / cW), 0, cols - 1);
            var r = (int)Math.Clamp(Math.Floor((pos.Y - colHeaderH) / cH), 0, rows - 1);

            var val = m[r][c];
            var rName = RowHeaders != null && r < RowHeaders.Count ? RowHeaders[r] : $"R{r + 1}";
            var cName = ColumnHeaders != null && c < ColumnHeaders.Count ? ColumnHeaders[c] : $"C{c + 1}";
            ToolTip = $"{rName} · {cName}: {(val * 100.0):0.0}%";
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var w = ActualWidth;
            var h = ActualHeight;
            if (w <= 20 || h <= 20) return;

            var m = Matrix;
            if (m == null || m.Count == 0 || m[0].Count == 0) return;

            var rows = m.Count;
            var cols = m[0].Count;
            const double rowHeaderW = 38.0;
            const double colHeaderH = 14.0;

            var gridW = Math.Max(1.0, w - rowHeaderW);
            var gridH = Math.Max(1.0, h - colHeaderH);
            var cW = gridW / cols;
            var cH = gridH / rows;

            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var headerBrush = new SolidColorBrush(Color.FromArgb(170, 200, 200, 200));
            headerBrush.Freeze();

            // Column Headers
            var colHdrs = ColumnHeaders;
            if (colHdrs != null)
            {
                for (int c = 0; c < Math.Min(cols, colHdrs.Count); c++)
                {
                    var ft = new FormattedText(colHdrs[c], CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        HeaderTypeface, 7.0, headerBrush, dpi);
                    var x = rowHeaderW + c * cW + (cW - ft.Width) / 2.0;
                    dc.DrawText(ft, new Point(x, 1.0));
                }
            }

            // Row Headers & Matrix Cells
            var baseCol = (BaseBrush as SolidColorBrush)?.Color ?? Color.FromArgb(40, 45, 55, 72);
            var hotCol = (HotBrush as SolidColorBrush)?.Color ?? Color.FromArgb(255, 212, 175, 55);
            var rowHdrs = RowHeaders;

            for (int r = 0; r < rows; r++)
            {
                var y = colHeaderH + r * cH;
                if (rowHdrs != null && r < rowHdrs.Count)
                {
                    var ft = new FormattedText(rowHdrs[r], CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        HeaderTypeface, 7.0, headerBrush, dpi)
                    {
                        MaxTextWidth = rowHeaderW - 4.0,
                        Trimming = TextTrimming.CharacterEllipsis
                    };
                    dc.DrawText(ft, new Point(2.0, y + (cH - ft.Height) / 2.0));
                }

                for (int c = 0; c < cols; c++)
                {
                    var x = rowHeaderW + c * cW;
                    var val = Math.Clamp(m[r][c], 0.0, 1.0);

                    var cellR = (byte)(baseCol.R + (hotCol.R - baseCol.R) * val);
                    var cellG = (byte)(baseCol.G + (hotCol.G - baseCol.G) * val);
                    var cellB = (byte)(baseCol.B + (hotCol.B - baseCol.B) * val);
                    var cellA = (byte)(baseCol.A + (hotCol.A - baseCol.A) * val);

                    var brush = new SolidColorBrush(Color.FromArgb(cellA, cellR, cellG, cellB));
                    brush.Freeze();

                    var cellRect = new Rect(x + 1.0, y + 1.0, Math.Max(1.0, cW - 2.0), Math.Max(1.0, cH - 2.0));
                    dc.DrawRoundedRectangle(brush, null, cellRect, CellRadius, CellRadius);
                }
            }
        }
    }

    // =========================================================================
    // FORGE ARC GAUGE CONTROL (TACTICAL DIAL / CIRCULAR TELEMETRY METER)
    // =========================================================================

    public sealed class ForgeArcGauge : FrameworkElement
    {
        private static readonly Typeface ValueTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        private static readonly Typeface LabelTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

        public static readonly DependencyProperty ValueProperty = ChartPropertyRegistration.Render(
            nameof(Value), typeof(double), typeof(ForgeArcGauge), 0.0);

        public static readonly DependencyProperty MinValueProperty = ChartPropertyRegistration.Render(
            nameof(MinValue), typeof(double), typeof(ForgeArcGauge), 0.0);

        public static readonly DependencyProperty MaxValueProperty = ChartPropertyRegistration.Render(
            nameof(MaxValue), typeof(double), typeof(ForgeArcGauge), 100.0);

        public static readonly DependencyProperty ArcThicknessProperty = ChartPropertyRegistration.Render(
            nameof(ArcThickness), typeof(double), typeof(ForgeArcGauge), 6.0);

        public static readonly DependencyProperty TrackBrushProperty = ChartPropertyRegistration.Render(
            nameof(TrackBrush), typeof(Brush), typeof(ForgeArcGauge), null);

        public static readonly DependencyProperty ProgressBrushProperty = ChartPropertyRegistration.Render(
            nameof(ProgressBrush), typeof(Brush), typeof(ForgeArcGauge), null);

        public static readonly DependencyProperty ValueBrushProperty = ChartPropertyRegistration.Render(
            nameof(ValueBrush), typeof(Brush), typeof(ForgeArcGauge), null);

        public static readonly DependencyProperty UnitsBrushProperty = ChartPropertyRegistration.Render(
            nameof(UnitsBrush), typeof(Brush), typeof(ForgeArcGauge), null);

        public static readonly DependencyProperty ValueFormatProperty = ChartPropertyRegistration.Render(
            nameof(ValueFormat), typeof(string), typeof(ForgeArcGauge), "0");

        public static readonly DependencyProperty UnitsTextProperty = ChartPropertyRegistration.Render(
            nameof(UnitsText), typeof(string), typeof(ForgeArcGauge), "%");

        public static readonly DependencyProperty GaugeTitleProperty = ChartPropertyRegistration.Render(
            nameof(GaugeTitle), typeof(string), typeof(ForgeArcGauge), string.Empty);

        public static readonly DependencyProperty StartAngleProperty = ChartPropertyRegistration.Render(
            nameof(StartAngle), typeof(double), typeof(ForgeArcGauge), 135.0);

        public static readonly DependencyProperty SweepAngleProperty = ChartPropertyRegistration.Render(
            nameof(SweepAngle), typeof(double), typeof(ForgeArcGauge), 270.0);

        public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

        public double MinValue { get => (double)GetValue(MinValueProperty); set => SetValue(MinValueProperty, value); }

        public double MaxValue { get => (double)GetValue(MaxValueProperty); set => SetValue(MaxValueProperty, value); }

        public double ArcThickness { get => (double)GetValue(ArcThicknessProperty); set => SetValue(ArcThicknessProperty, value); }

        public Brush? TrackBrush { get => (Brush?)GetValue(TrackBrushProperty); set => SetValue(TrackBrushProperty, value); }

        public Brush? ProgressBrush { get => (Brush?)GetValue(ProgressBrushProperty); set => SetValue(ProgressBrushProperty, value); }

        public Brush? ValueBrush { get => (Brush?)GetValue(ValueBrushProperty); set => SetValue(ValueBrushProperty, value); }

        public Brush? UnitsBrush { get => (Brush?)GetValue(UnitsBrushProperty); set => SetValue(UnitsBrushProperty, value); }

        public string ValueFormat { get => (string)GetValue(ValueFormatProperty); set => SetValue(ValueFormatProperty, value); }

        public string UnitsText { get => (string)GetValue(UnitsTextProperty); set => SetValue(UnitsTextProperty, value); }

        public string GaugeTitle { get => (string)GetValue(GaugeTitleProperty); set => SetValue(GaugeTitleProperty, value); }

        public double StartAngle { get => (double)GetValue(StartAngleProperty); set => SetValue(StartAngleProperty, value); }

        public double SweepAngle { get => (double)GetValue(SweepAngleProperty); set => SetValue(SweepAngleProperty, value); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var title = string.IsNullOrWhiteSpace(GaugeTitle) ? "Value" : GaugeTitle;
            ToolTip = $"{title}: {Value.ToString(ValueFormat, CultureInfo.InvariantCulture)}{UnitsText}";
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var w = ActualWidth;
            var h = ActualHeight;
            if (w < 24 || h < 24) return;

            var cx = w / 2.0;
            var cy = h / 2.0;
            var thick = Math.Max(2.0, ArcThickness);
            var radius = Math.Max(8.0, Math.Min(cx, cy) - thick / 2.0 - 2.0);

            var startDeg = StartAngle;
            var sweepDeg = SweepAngle;
            var startRad = (startDeg * Math.PI) / 180.0;
            var totalEndRad = ((startDeg + sweepDeg) * Math.PI) / 180.0;

            // Draw Background Track Arc
            var trackGeo = new StreamGeometry();
            using (var ctx = trackGeo.Open())
            {
                var ptStart = new Point(cx + radius * Math.Cos(startRad), cy + radius * Math.Sin(startRad));
                var ptEnd = new Point(cx + radius * Math.Cos(totalEndRad), cy + radius * Math.Sin(totalEndRad));
                ctx.BeginFigure(ptStart, isFilled: false, isClosed: false);
                ctx.ArcTo(ptEnd, new Size(radius, radius), 0.0, sweepDeg > 180.0, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: true);
            }
            trackGeo.Freeze();

            var trkBrush = TrackBrush ?? new SolidColorBrush(Color.FromArgb(40, 200, 200, 200));
            trkBrush.Freeze();
            var trkPen = ChartRenderHelper.CreateFrozenPen(trkBrush, thick, cap: PenLineCap.Round);
            dc.DrawGeometry(null, trkPen, trackGeo);

            // Draw Progress Arc
            var min = MinValue;
            var max = Math.Max(min + 0.001, MaxValue);
            var norm = Math.Clamp((Value - min) / (max - min), 0.0, 1.0);
            var progSweep = sweepDeg * norm;

            if (progSweep > 0.5)
            {
                var progEndRad = ((startDeg + progSweep) * Math.PI) / 180.0;
                var progGeo = new StreamGeometry();
                using (var ctx = progGeo.Open())
                {
                    var ptStart = new Point(cx + radius * Math.Cos(startRad), cy + radius * Math.Sin(startRad));
                    var ptEnd = new Point(cx + radius * Math.Cos(progEndRad), cy + radius * Math.Sin(progEndRad));
                    ctx.BeginFigure(ptStart, isFilled: false, isClosed: false);
                    ctx.ArcTo(ptEnd, new Size(radius, radius), 0.0, progSweep > 180.0, SweepDirection.Clockwise, isStroked: true, isSmoothJoin: true);
                }
                progGeo.Freeze();

                var progBrush = ProgressBrush ?? new SolidColorBrush(Color.FromArgb(240, 212, 175, 55));
                progBrush.Freeze();
                var progPen = ChartRenderHelper.CreateFrozenPen(progBrush, thick, cap: PenLineCap.Round);
                dc.DrawGeometry(null, progPen, progGeo);
            }

            // Draw Center Text (Value & Units/Title)
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var valBrush = ValueBrush ?? new SolidColorBrush(Color.FromArgb(240, 245, 240, 230));
            valBrush.Freeze();

            var valFontSize = Math.Clamp(radius * 0.46, 9.0, 18.0);
            var valStr = Value.ToString(ValueFormat, CultureInfo.InvariantCulture);
            var ftVal = new FormattedText(valStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                ValueTypeface, valFontSize, valBrush, dpi);

            var untBrush = UnitsBrush ?? new SolidColorBrush(Color.FromArgb(180, 190, 185, 170));
            untBrush.Freeze();

            var untFontSize = Math.Clamp(radius * 0.24, 6.5, 11.0);
            var untStr = string.IsNullOrWhiteSpace(GaugeTitle) ? UnitsText : $"{UnitsText} · {GaugeTitle}";
            var ftUnt = new FormattedText(untStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                LabelTypeface, untFontSize, untBrush, dpi)
            {
                MaxTextWidth = Math.Max(20.0, radius * 1.7),
                Trimming = TextTrimming.CharacterEllipsis
            };

            var totalTextH = ftVal.Height + ftUnt.Height - 1.0;
            var textStartY = cy - totalTextH / 2.0;

            dc.DrawText(ftVal, new Point(cx - ftVal.Width / 2.0, textStartY));
            dc.DrawText(ftUnt, new Point(cx - ftUnt.Width / 2.0, textStartY + ftVal.Height - 1.0));
        }
    }

    // =========================================================================
    // FORGE AREA CHART (TACTICAL VERTICAL GRADIENT AREA CHART WITH GRID & LABELS)
    // =========================================================================

    public sealed class ForgeAreaChart : FrameworkElement
    {
        public static readonly DependencyProperty DataPointsProperty = ChartPropertyRegistration.Render(
            nameof(DataPoints), typeof(IReadOnlyList<double>), typeof(ForgeAreaChart), Array.Empty<double>());

        public static readonly DependencyProperty StrokeProperty = ChartPropertyRegistration.Render(
            nameof(Stroke), typeof(Brush), typeof(ForgeAreaChart), null);

        public static readonly DependencyProperty FillProperty = ChartPropertyRegistration.Render(
            nameof(Fill), typeof(Brush), typeof(ForgeAreaChart), null);

        public static readonly DependencyProperty StrokeThicknessProperty = ChartPropertyRegistration.Render(
            nameof(StrokeThickness), typeof(double), typeof(ForgeAreaChart), 1.8);

        public static readonly DependencyProperty ShowGridLinesProperty = ChartPropertyRegistration.Render(
            nameof(ShowGridLines), typeof(bool), typeof(ForgeAreaChart), true);

        public static readonly DependencyProperty GridBrushProperty = ChartPropertyRegistration.Render(
            nameof(GridBrush), typeof(Brush), typeof(ForgeAreaChart), null);

        public static readonly DependencyProperty AxisLabelBrushProperty = ChartPropertyRegistration.Render(
            nameof(AxisLabelBrush), typeof(Brush), typeof(ForgeAreaChart), null);

        public static readonly DependencyProperty ShowMinMaxLabelsProperty = ChartPropertyRegistration.Render(
            nameof(ShowMinMaxLabels), typeof(bool), typeof(ForgeAreaChart), true);

        public static readonly DependencyProperty ShowBaselineProperty = ChartPropertyRegistration.Render(
            nameof(ShowBaseline), typeof(bool), typeof(ForgeAreaChart), true);

        public static readonly DependencyProperty ShowDataPointsProperty = ChartPropertyRegistration.Render(
            nameof(ShowDataPoints), typeof(bool), typeof(ForgeAreaChart), true);

        public static readonly DependencyProperty HighlightPointBrushProperty = ChartPropertyRegistration.Render(
            nameof(HighlightPointBrush), typeof(Brush), typeof(ForgeAreaChart), null);

        public static readonly DependencyProperty ValueUnitProperty =
            DependencyProperty.Register(nameof(ValueUnit), typeof(string), typeof(ForgeAreaChart),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FormatStringProperty =
            DependencyProperty.Register(nameof(FormatString), typeof(string), typeof(ForgeAreaChart),
                new PropertyMetadata("N0"));

        public IReadOnlyList<double> DataPoints { get => (IReadOnlyList<double>)GetValue(DataPointsProperty); set => SetValue(DataPointsProperty, value); }

        public Brush Stroke { get => (Brush)GetValue(StrokeProperty); set => SetValue(StrokeProperty, value); }

        public Brush Fill { get => (Brush)GetValue(FillProperty); set => SetValue(FillProperty, value); }

        public double StrokeThickness { get => (double)GetValue(StrokeThicknessProperty); set => SetValue(StrokeThicknessProperty, value); }

        public bool ShowGridLines { get => (bool)GetValue(ShowGridLinesProperty); set => SetValue(ShowGridLinesProperty, value); }

        public Brush GridBrush { get => (Brush)GetValue(GridBrushProperty); set => SetValue(GridBrushProperty, value); }

        public Brush AxisLabelBrush { get => (Brush)GetValue(AxisLabelBrushProperty); set => SetValue(AxisLabelBrushProperty, value); }

        public bool ShowMinMaxLabels { get => (bool)GetValue(ShowMinMaxLabelsProperty); set => SetValue(ShowMinMaxLabelsProperty, value); }

        public bool ShowBaseline { get => (bool)GetValue(ShowBaselineProperty); set => SetValue(ShowBaselineProperty, value); }

        public bool ShowDataPoints { get => (bool)GetValue(ShowDataPointsProperty); set => SetValue(ShowDataPointsProperty, value); }

        public Brush HighlightPointBrush { get => (Brush)GetValue(HighlightPointBrushProperty); set => SetValue(HighlightPointBrushProperty, value); }

        public string ValueUnit { get => (string)GetValue(ValueUnitProperty); set => SetValue(ValueUnitProperty, value); }

        public string FormatString { get => (string)GetValue(FormatStringProperty); set => SetValue(FormatStringProperty, value); }

        static readonly Typeface LabelTypeface = new(new FontFamily("Consolas, Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var points = DataPoints;
            if (points == null || points.Count < 2 || ActualWidth <= 16)
            {
                ToolTip = null;
                return;
            }

            var padLeft = ShowMinMaxLabels ? 36.0 : 8.0;
            const double padRight = 8.0;
            var w = Math.Max(1.0, ActualWidth - padLeft - padRight);
            var pos = e.GetPosition(this);
            var step = w / (points.Count - 1);
            var idx = (int)Math.Clamp(Math.Round((pos.X - padLeft) / step), 0, points.Count - 1);
            var val = points[idx];
            var unit = string.IsNullOrEmpty(ValueUnit) ? string.Empty : " " + ValueUnit;
            ToolTip = $"Sample #{idx + 1}: {val.ToString(FormatString, CultureInfo.InvariantCulture)}{unit}";
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var w = ActualWidth;
            var h = ActualHeight;
            if (w <= 16 || h <= 16) return;

            var points = DataPoints;
            var effectiveStroke = Stroke ?? Brushes.Goldenrod;
            var effectiveGrid = GridBrush ?? new SolidColorBrush(Color.FromArgb(35, 200, 200, 200));
            var effectiveLabel = AxisLabelBrush ?? new SolidColorBrush(Color.FromArgb(160, 220, 215, 200));
            effectiveGrid.Freeze();
            effectiveLabel.Freeze();

            var padLeft = ShowMinMaxLabels ? 34.0 : 8.0;
            const double padRight = 8.0;
            const double padTop = 8.0;
            const double padBottom = 16.0;

            var usableW = Math.Max(1.0, w - padLeft - padRight);
            var usableH = Math.Max(1.0, h - padTop - padBottom);

            // Compute Min and Max
            double min = double.MaxValue;
            double max = double.MinValue;
            if (points != null && points.Count > 0)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    var v = points[i];
                    if (v < min) min = v;
                    if (v > max) max = v;
                }
            }
            if (min >= max)
            {
                max = min + 1.0;
            }

            var range = Math.Max(0.001, max - min);

            // Subtle Grid lines (0%, 33%, 66%, 100%)
            if (ShowGridLines && usableH > 20)
            {
                var gridPen = ChartRenderHelper.CreateFrozenPen(effectiveGrid, 0.8, DashStyles.Dash);
                for (int g = 0; g <= 3; g++)
                {
                    var frac = g / 3.0;
                    var gy = padTop + usableH * (1.0 - frac);
                    dc.DrawLine(gridPen, new Point(padLeft, gy), new Point(w - padRight, gy));
                }

                // Vertical graduation markers
                if (points != null && points.Count >= 4)
                {
                    var vertPen = ChartRenderHelper.CreateFrozenPen(effectiveGrid, 0.6, DashStyles.Dot);
                    var numVert = Math.Min(6, points.Count);
                    for (int v = 1; v < numVert - 1; v++)
                    {
                        var vx = padLeft + (v / (double)(numVert - 1)) * usableW;
                        dc.DrawLine(vertPen, new Point(vx, padTop), new Point(vx, padTop + usableH));
                    }
                }
            }

            // Baseline
            if (ShowBaseline)
            {
                var basePen = ChartRenderHelper.CreateFrozenPen(effectiveGrid, 1.0);
                dc.DrawLine(basePen, new Point(padLeft, padTop + usableH), new Point(w - padRight, padTop + usableH));
            }

            // Min/Max Text Labels
            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            if (ShowMinMaxLabels && usableH > 24)
            {
                var maxStr = max.ToString(FormatString, CultureInfo.InvariantCulture);
                var minStr = min.ToString(FormatString, CultureInfo.InvariantCulture);

                var ftMax = new FormattedText(maxStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    LabelTypeface, 7.5, effectiveLabel, dpi)
                {
                    MaxTextWidth = padLeft - 4.0,
                    Trimming = TextTrimming.CharacterEllipsis,
                    TextAlignment = TextAlignment.Right
                };
                var ftMin = new FormattedText(minStr, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    LabelTypeface, 7.5, effectiveLabel, dpi)
                {
                    MaxTextWidth = padLeft - 4.0,
                    Trimming = TextTrimming.CharacterEllipsis,
                    TextAlignment = TextAlignment.Right
                };

                dc.DrawText(ftMax, new Point(0, padTop - 2));
                dc.DrawText(ftMin, new Point(0, padTop + usableH - ftMin.Height));
            }

            if (points == null || points.Count < 2) return;

            var stepX = usableW / (points.Count - 1);
            Point MapPoint(int i)
            {
                var x = padLeft + i * stepX;
                var norm = (points[i] - min) / range;
                var y = padTop + (1.0 - norm) * usableH;
                return new Point(x, y);
            }

            var firstPt = MapPoint(0);
            var lastPt = MapPoint(points.Count - 1);

            // Shaded Area (Linear Gradient Brush)
            var areaBrush = Fill;
            if (areaBrush == null)
            {
                var sc = (effectiveStroke as SolidColorBrush)?.Color ?? Color.FromRgb(212, 175, 55);
                var grad = new LinearGradientBrush(
                    Color.FromArgb(90, sc.R, sc.G, sc.B),
                    Color.FromArgb(8, sc.R, sc.G, sc.B),
                    new Point(0, 0),
                    new Point(0, 1));
                grad.Freeze();
                areaBrush = grad;
            }

            var areaGeom = new StreamGeometry();
            using (var ctx = areaGeom.Open())
            {
                ctx.BeginFigure(new Point(padLeft, padTop + usableH), true, true);
                ctx.LineTo(firstPt, true, false);
                for (int i = 1; i < points.Count; i++)
                {
                    ctx.LineTo(MapPoint(i), true, false);
                }
                ctx.LineTo(new Point(lastPt.X, padTop + usableH), true, false);
            }
            areaGeom.Freeze();
            dc.DrawGeometry(areaBrush, null, areaGeom);

            // Stroke Polyline
            var lineGeom = new StreamGeometry();
            using (var ctx = lineGeom.Open())
            {
                ctx.BeginFigure(firstPt, false, false);
                for (int i = 1; i < points.Count; i++)
                {
                    ctx.LineTo(MapPoint(i), true, false);
                }
            }
            lineGeom.Freeze();
            var linePen = ChartRenderHelper.CreateFrozenPen(effectiveStroke, StrokeThickness, cap: PenLineCap.Round);
            dc.DrawGeometry(null, linePen, lineGeom);

            // Markers on Data Points
            if (ShowDataPoints && points.Count <= 32)
            {
                var ptBrush = HighlightPointBrush ?? effectiveStroke;
                var ptPen = ChartRenderHelper.CreateFrozenPen(new SolidColorBrush(Color.FromArgb(200, 20, 24, 28)), 1.0);

                for (int i = 0; i < points.Count; i++)
                {
                    var pt = MapPoint(i);
                    var radius = (i == points.Count - 1) ? 3.5 : 2.0;
                    dc.DrawEllipse(ptBrush, ptPen, pt, radius, radius);
                }

                // Prominent outer halo ring on last point
                var haloPen = ChartRenderHelper.CreateFrozenPen(effectiveStroke, 1.2, DashStyles.Dash);
                dc.DrawEllipse(null, haloPen, lastPt, 6.0, 6.0);
            }
        }
    }

    // =========================================================================
    // FORGE STEP PROGRESS (TACTICAL SEQUENTIAL PIPELINE & WORKFLOW TRACKER)
    // =========================================================================

    public enum ForgeStepStatus
    {
        Pending = 0,
        Active = 1,
        Completed = 2,
        Failed = 3
    }

    public sealed class ForgeStepItem
    {
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
        public ForgeStepStatus Status { get; set; } = ForgeStepStatus.Pending;
        public string? BadgeText { get; set; }

        public ForgeStepItem() { }

        public ForgeStepItem(string title, ForgeStepStatus status, string? subtitle = null, string? badgeText = null)
        {
            Title = title;
            Status = status;
            Subtitle = subtitle;
            BadgeText = badgeText;
        }

        public ForgeStepItem(string title, string? subtitle, ForgeStepStatus status, string? badgeText = null)
        {
            Title = title;
            Subtitle = subtitle;
            Status = status;
            BadgeText = badgeText;
        }
    }

    public sealed class ForgeStepProgress : FrameworkElement
    {
        public static readonly DependencyProperty StepsProperty = ChartPropertyRegistration.Render(
            nameof(Steps), typeof(IReadOnlyList<ForgeStepItem>), typeof(ForgeStepProgress), Array.Empty<ForgeStepItem>());

        public static readonly DependencyProperty ActiveBrushProperty = ChartPropertyRegistration.Render(
            nameof(ActiveBrush), typeof(Brush), typeof(ForgeStepProgress), null);

        public static readonly DependencyProperty CompletedBrushProperty = ChartPropertyRegistration.Render(
            nameof(CompletedBrush), typeof(Brush), typeof(ForgeStepProgress), null);

        public static readonly DependencyProperty PendingBrushProperty = ChartPropertyRegistration.Render(
            nameof(PendingBrush), typeof(Brush), typeof(ForgeStepProgress), null);

        public static readonly DependencyProperty FailedBrushProperty = ChartPropertyRegistration.Render(
            nameof(FailedBrush), typeof(Brush), typeof(ForgeStepProgress), null);

        public static readonly DependencyProperty ConnectorBrushProperty = ChartPropertyRegistration.Render(
            nameof(ConnectorBrush), typeof(Brush), typeof(ForgeStepProgress), null);

        public static readonly DependencyProperty TextBrushProperty = ChartPropertyRegistration.Render(
            nameof(TextBrush), typeof(Brush), typeof(ForgeStepProgress), null);

        public static readonly DependencyProperty SubtitleBrushProperty = ChartPropertyRegistration.Render(
            nameof(SubtitleBrush), typeof(Brush), typeof(ForgeStepProgress), null);

        public static readonly DependencyProperty NodeRadiusProperty = ChartPropertyRegistration.Render(
            nameof(NodeRadius), typeof(double), typeof(ForgeStepProgress), 10.0);

        public static readonly DependencyProperty OrientationProperty = ChartPropertyRegistration.Render(
            nameof(Orientation), typeof(Orientation), typeof(ForgeStepProgress), Orientation.Horizontal);

        public IReadOnlyList<ForgeStepItem> Steps { get => (IReadOnlyList<ForgeStepItem>)GetValue(StepsProperty); set => SetValue(StepsProperty, value); }

        public Brush ActiveBrush { get => (Brush)GetValue(ActiveBrushProperty); set => SetValue(ActiveBrushProperty, value); }

        public Brush CompletedBrush { get => (Brush)GetValue(CompletedBrushProperty); set => SetValue(CompletedBrushProperty, value); }

        public Brush PendingBrush { get => (Brush)GetValue(PendingBrushProperty); set => SetValue(PendingBrushProperty, value); }

        public Brush FailedBrush { get => (Brush)GetValue(FailedBrushProperty); set => SetValue(FailedBrushProperty, value); }

        public Brush ConnectorBrush { get => (Brush)GetValue(ConnectorBrushProperty); set => SetValue(ConnectorBrushProperty, value); }

        public Brush TextBrush { get => (Brush)GetValue(TextBrushProperty); set => SetValue(TextBrushProperty, value); }

        public Brush SubtitleBrush { get => (Brush)GetValue(SubtitleBrushProperty); set => SetValue(SubtitleBrushProperty, value); }

        public double NodeRadius { get => (double)GetValue(NodeRadiusProperty); set => SetValue(NodeRadiusProperty, value); }

        public Orientation Orientation { get => (Orientation)GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

        static readonly Typeface TitleTypeface = new(new FontFamily("Segoe UI, Arial"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        static readonly Typeface SubtitleTypeface = new(new FontFamily("Segoe UI, Arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        static readonly Typeface GlyphTypeface = new(new FontFamily("Consolas, Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var steps = Steps;
            if (steps == null || steps.Count == 0)
            {
                ToolTip = null;
                return;
            }

            var pos = e.GetPosition(this);
            var count = steps.Count;
            var isHoriz = Orientation == Orientation.Horizontal;
            var totalLength = isHoriz ? ActualWidth : ActualHeight;
            var pad = NodeRadius + 14.0;
            var usable = Math.Max(1.0, totalLength - 2 * pad);
            var stepDist = count > 1 ? usable / (count - 1) : 0.0;

            for (int i = 0; i < count; i++)
            {
                var center = count == 1 ? totalLength / 2.0 : pad + i * stepDist;
                var dist = isHoriz ? Math.Abs(pos.X - center) : Math.Abs(pos.Y - center);
                if (dist <= NodeRadius * 2.0)
                {
                    var item = steps[i];
                    var sub = string.IsNullOrEmpty(item.Subtitle) ? string.Empty : $" — {item.Subtitle}";
                    ToolTip = $"[Step {i + 1}/{count}] {item.Title} ({item.Status}){sub}";
                    return;
                }
            }
            ToolTip = null;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var w = ActualWidth;
            var h = ActualHeight;
            if (w < 20 || h < 20) return;

            var steps = Steps;
            if (steps == null || steps.Count == 0) return;

            var count = steps.Count;
            var isHoriz = Orientation == Orientation.Horizontal;
            var r = Math.Clamp(NodeRadius, 6.0, 20.0);

            var effActive = ActiveBrush ?? new SolidColorBrush(Color.FromRgb(212, 175, 55));      // Brass/Gold
            var effCompleted = CompletedBrush ?? new SolidColorBrush(Color.FromRgb(46, 139, 87)); // SeaGreen / Verdigris
            var effPending = PendingBrush ?? new SolidColorBrush(Color.FromArgb(90, 160, 160, 160)); // Muted
            var effFailed = FailedBrush ?? new SolidColorBrush(Color.FromRgb(178, 34, 34));       // Crimson / Ember
            var effConn = ConnectorBrush ?? new SolidColorBrush(Color.FromArgb(60, 200, 200, 200));
            var effText = TextBrush ?? new SolidColorBrush(Color.FromRgb(230, 225, 215));         // Paper
            var effSub = SubtitleBrush ?? new SolidColorBrush(Color.FromArgb(180, 170, 165, 155));

            effActive.Freeze();
            effCompleted.Freeze();
            effPending.Freeze();
            effFailed.Freeze();
            effConn.Freeze();
            effText.Freeze();
            effSub.Freeze();

            var coalBg = new SolidColorBrush(Color.FromRgb(24, 28, 32));
            coalBg.Freeze();

            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            // Layout coordinates
            var pad = r + 14.0;
            var totalLength = isHoriz ? w : h;
            var usable = Math.Max(1.0, totalLength - 2 * pad);
            var stepDist = count > 1 ? usable / (count - 1) : 0.0;

            var nodeCenterY = isHoriz ? r + 6.0 : h / 2.0;
            var nodeCenterX = isHoriz ? w / 2.0 : r + 6.0;

            Point GetNodeCenter(int i)
            {
                if (count == 1)
                {
                    return isHoriz ? new Point(w / 2.0, nodeCenterY) : new Point(nodeCenterX, h / 2.0);
                }
                var coord = pad + i * stepDist;
                return isHoriz ? new Point(coord, nodeCenterY) : new Point(nodeCenterX, coord);
            }

            // 1. Draw Connecting Lines between steps
            for (int i = 0; i < count - 1; i++)
            {
                var ptA = GetNodeCenter(i);
                var ptB = GetNodeCenter(i + 1);

                var stepA = steps[i];
                var stepB = steps[i + 1];

                Brush connBrush;
                if (stepA.Status == ForgeStepStatus.Completed && (stepB.Status == ForgeStepStatus.Completed || stepB.Status == ForgeStepStatus.Active))
                {
                    connBrush = effCompleted;
                }
                else if (stepA.Status == ForgeStepStatus.Failed || stepB.Status == ForgeStepStatus.Failed)
                {
                    connBrush = effFailed;
                }
                else
                {
                    connBrush = effConn;
                }

                var pen = new Pen(connBrush, 2.0)
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };
                pen.Freeze();

                if (isHoriz)
                {
                    dc.DrawLine(pen, new Point(ptA.X + r + 2.0, ptA.Y), new Point(ptB.X - r - 2.0, ptB.Y));
                }
                else
                {
                    dc.DrawLine(pen, new Point(ptA.X, ptA.Y + r + 2.0), new Point(ptB.X, ptB.Y - r - 2.0));
                }
            }

            // 2. Draw Nodes and Labels
            for (int i = 0; i < count; i++)
            {
                var pt = GetNodeCenter(i);
                var item = steps[i];

                Brush nodeStroke;
                Brush nodeFill;
                string glyph;
                Brush glyphBrush;

                switch (item.Status)
                {
                    case ForgeStepStatus.Completed:
                        nodeStroke = effCompleted;
                        nodeFill = effCompleted;
                        glyph = "✓";
                        glyphBrush = Brushes.White;
                        break;
                    case ForgeStepStatus.Active:
                        nodeStroke = effActive;
                        nodeFill = coalBg;
                        glyph = (i + 1).ToString(CultureInfo.InvariantCulture);
                        glyphBrush = effActive;
                        break;
                    case ForgeStepStatus.Failed:
                        nodeStroke = effFailed;
                        nodeFill = effFailed;
                        glyph = "✕";
                        glyphBrush = Brushes.White;
                        break;
                    case ForgeStepStatus.Pending:
                    default:
                        nodeStroke = effPending;
                        nodeFill = coalBg;
                        glyph = (i + 1).ToString(CultureInfo.InvariantCulture);
                        glyphBrush = effPending;
                        break;
                }

                // If active, draw outer glowing ring
                if (item.Status == ForgeStepStatus.Active)
                {
                    var haloPen = new Pen(effActive, 1.2) { DashStyle = DashStyles.Dash };
                    haloPen.Freeze();
                    dc.DrawEllipse(null, haloPen, pt, r + 4.0, r + 4.0);
                }

                var nodePen = new Pen(nodeStroke, 1.6);
                nodePen.Freeze();
                dc.DrawEllipse(nodeFill, nodePen, pt, r, r);

                // Glyph inside node
                glyphBrush.Freeze();
                var ftGlyph = new FormattedText(glyph, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    GlyphTypeface, r * 1.1, glyphBrush, dpi);
                dc.DrawText(ftGlyph, new Point(pt.X - ftGlyph.Width / 2.0, pt.Y - ftGlyph.Height / 2.0));

                // Text labels
                if (isHoriz)
                {
                    var maxLabelW = Math.Max(40.0, stepDist > 0 ? stepDist - 4.0 : 90.0);
                    var labelY = pt.Y + r + 4.0;

                    var ftTitle = new FormattedText(item.Title, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        TitleTypeface, 8.5, effText, dpi)
                    {
                        MaxTextWidth = maxLabelW,
                        TextAlignment = TextAlignment.Center,
                        Trimming = TextTrimming.CharacterEllipsis
                    };
                    dc.DrawText(ftTitle, new Point(pt.X - ftTitle.Width / 2.0, labelY));

                    if (!string.IsNullOrEmpty(item.Subtitle))
                    {
                        var ftSub = new FormattedText(item.Subtitle, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                            SubtitleTypeface, 7.5, effSub, dpi)
                        {
                            MaxTextWidth = maxLabelW,
                            TextAlignment = TextAlignment.Center,
                            Trimming = TextTrimming.CharacterEllipsis
                        };
                        dc.DrawText(ftSub, new Point(pt.X - ftSub.Width / 2.0, labelY + ftTitle.Height));
                    }
                }
                else
                {
                    var labelX = pt.X + r + 8.0;
                    var ftTitle = new FormattedText(item.Title, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                        TitleTypeface, 9.0, effText, dpi)
                    {
                        MaxTextWidth = Math.Max(40.0, w - labelX - 6.0),
                        Trimming = TextTrimming.CharacterEllipsis
                    };
                    dc.DrawText(ftTitle, new Point(labelX, pt.Y - ftTitle.Height / 2.0 - (string.IsNullOrEmpty(item.Subtitle) ? 0 : 4.0)));

                    if (!string.IsNullOrEmpty(item.Subtitle))
                    {
                        var ftSub = new FormattedText(item.Subtitle, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                            SubtitleTypeface, 7.5, effSub, dpi)
                        {
                            MaxTextWidth = Math.Max(40.0, w - labelX - 6.0),
                            Trimming = TextTrimming.CharacterEllipsis
                        };
                        dc.DrawText(ftSub, new Point(labelX, pt.Y + (ftTitle.Height / 2.0) - 2.0));
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tactical graduated timeline and phase ruler control rendered directly via DrawingContext.
    /// Provides calibrated millimetric tick marks, major/minor divisions, time labels, and active marker indicators.
    /// </summary>
    public sealed class ForgeTimelineRuler : FrameworkElement
    {
        public static readonly DependencyProperty TotalDurationProperty = ChartPropertyRegistration.Render(
            nameof(TotalDuration), typeof(double), typeof(ForgeTimelineRuler), 100.0);

        public static readonly DependencyProperty CurrentTimeProperty = ChartPropertyRegistration.Render(
            nameof(CurrentTime), typeof(double), typeof(ForgeTimelineRuler), 0.0);

        public static readonly DependencyProperty MajorIntervalProperty = ChartPropertyRegistration.Render(
            nameof(MajorInterval), typeof(double), typeof(ForgeTimelineRuler), 20.0);

        public static readonly DependencyProperty MinorIntervalProperty = ChartPropertyRegistration.Render(
            nameof(MinorInterval), typeof(double), typeof(ForgeTimelineRuler), 5.0);

        public static readonly DependencyProperty UnitLabelProperty = ChartPropertyRegistration.Render(
            nameof(UnitLabel), typeof(string), typeof(ForgeTimelineRuler), "ms");

        public static readonly DependencyProperty ShowLabelsProperty = ChartPropertyRegistration.Render(
            nameof(ShowLabels), typeof(bool), typeof(ForgeTimelineRuler), true);

        public static readonly DependencyProperty RulerBrushProperty = ChartPropertyRegistration.Render(
            nameof(RulerBrush), typeof(Brush), typeof(ForgeTimelineRuler), null);

        public static readonly DependencyProperty TickBrushProperty = ChartPropertyRegistration.Render(
            nameof(TickBrush), typeof(Brush), typeof(ForgeTimelineRuler), null);

        public static readonly DependencyProperty MarkerBrushProperty = ChartPropertyRegistration.Render(
            nameof(MarkerBrush), typeof(Brush), typeof(ForgeTimelineRuler), null);

        public static readonly DependencyProperty LabelBrushProperty = ChartPropertyRegistration.Render(
            nameof(LabelBrush), typeof(Brush), typeof(ForgeTimelineRuler), null);

        public static readonly DependencyProperty MarkerLabelProperty = ChartPropertyRegistration.Render(
            nameof(MarkerLabel), typeof(string), typeof(ForgeTimelineRuler), string.Empty);

        static readonly Typeface RulerTypeface = new("Segoe UI");
        static readonly Typeface MarkerTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);

        static readonly SolidColorBrush DefaultRuler = new(Color.FromRgb(0x42, 0x4B, 0x48));
        static readonly SolidColorBrush DefaultTick = new(Color.FromRgb(0xC5, 0xA0, 0x59));
        static readonly SolidColorBrush DefaultMarker = new(Color.FromRgb(0x4E, 0x9A, 0x78));
        static readonly SolidColorBrush DefaultLabel = new(Color.FromRgb(0xD0, 0xC8, 0xB8));

        static ForgeTimelineRuler()
        {
            DefaultRuler.Freeze();
            DefaultTick.Freeze();
            DefaultMarker.Freeze();
            DefaultLabel.Freeze();
        }

        public double TotalDuration { get => (double)GetValue(TotalDurationProperty); set => SetValue(TotalDurationProperty, value); }

        public double CurrentTime { get => (double)GetValue(CurrentTimeProperty); set => SetValue(CurrentTimeProperty, value); }

        public double MajorInterval { get => (double)GetValue(MajorIntervalProperty); set => SetValue(MajorIntervalProperty, value); }

        public double MinorInterval { get => (double)GetValue(MinorIntervalProperty); set => SetValue(MinorIntervalProperty, value); }

        public string UnitLabel { get => (string)GetValue(UnitLabelProperty); set => SetValue(UnitLabelProperty, value); }

        public bool ShowLabels { get => (bool)GetValue(ShowLabelsProperty); set => SetValue(ShowLabelsProperty, value); }

        public Brush RulerBrush { get => (Brush)GetValue(RulerBrushProperty); set => SetValue(RulerBrushProperty, value); }

        public Brush TickBrush { get => (Brush)GetValue(TickBrushProperty); set => SetValue(TickBrushProperty, value); }

        public Brush MarkerBrush { get => (Brush)GetValue(MarkerBrushProperty); set => SetValue(MarkerBrushProperty, value); }

        public Brush LabelBrush { get => (Brush)GetValue(LabelBrushProperty); set => SetValue(LabelBrushProperty, value); }

        public string MarkerLabel { get => (string)GetValue(MarkerLabelProperty); set => SetValue(MarkerLabelProperty, value); }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var w = ActualWidth;
            var maxD = Math.Max(1.0, TotalDuration);
            var padX = 14.0;
            var rulerW = w - padX * 2.0;

            if (rulerW <= 10.0)
            {
                ToolTip = null;
                return;
            }

            var pos = e.GetPosition(this);
            var ratio = Math.Max(0.0, Math.Min(1.0, (pos.X - padX) / rulerW));
            var hoverVal = ratio * maxD;
            var unit = UnitLabel ?? "ms";
            ToolTip = $"Timeline: {hoverVal:F1}{unit} (Current: {CurrentTime:F1}{unit} / {maxD:F0}{unit})";
        }

        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            ToolTip = null;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var w = ActualWidth;
            var h = ActualHeight;
            if (w < 20.0 || h < 12.0)
                return;

            var padX = 14.0;
            var rulerW = w - padX * 2.0;
            if (rulerW <= 10.0)
                return;

            var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            var maxD = Math.Max(1.0, TotalDuration);
            var major = Math.Max(0.1, MajorInterval);
            var minor = Math.Max(0.05, MinorInterval);

            var effRulerBrush = RulerBrush ?? DefaultRuler;
            var effTickBrush = TickBrush ?? DefaultTick;
            var effMarkerBrush = MarkerBrush ?? DefaultMarker;
            var effLabelBrush = LabelBrush ?? DefaultLabel;

            var baselineY = ShowLabels ? Math.Max(8.0, h - 14.0) : h * 0.7;

            // Draw horizontal baseline
            var baselinePen = ChartRenderHelper.CreateFrozenPen(effRulerBrush, 1.4);
            dc.DrawLine(baselinePen, new Point(padX, baselineY), new Point(padX + rulerW, baselineY));

            // Draw tick marks
            var majorTickPen = ChartRenderHelper.CreateFrozenPen(effTickBrush, 1.2);
            var minorTickPen = ChartRenderHelper.CreateFrozenPen(effTickBrush, 0.7);

            var steps = (int)Math.Floor(maxD / minor);
            for (var i = 0; i <= steps; i++)
            {
                var val = i * minor;
                if (val > maxD + 0.001)
                    break;

                var ratio = val / maxD;
                var x = padX + ratio * rulerW;
                var isMajor = Math.Abs(val % major) < 0.001 || Math.Abs(val - maxD) < 0.001 || Math.Abs((val % major) - major) < 0.001;

                if (isMajor)
                {
                    dc.DrawLine(majorTickPen, new Point(x, baselineY - 7.0), new Point(x, baselineY + 2.0));
                    if (ShowLabels)
                    {
                        var text = val.ToString("0");
                        if (Math.Abs(val - maxD) < 0.001 && !string.IsNullOrEmpty(UnitLabel))
                            text += UnitLabel;

                        var ft = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                            RulerTypeface, 7.5, effLabelBrush, dpi);
                        dc.DrawText(ft, new Point(x - ft.Width / 2.0, baselineY + 3.0));
                    }
                }
                else
                {
                    dc.DrawLine(minorTickPen, new Point(x, baselineY - 4.0), new Point(x, baselineY));
                }
            }

            // Draw current marker indicator
            if (CurrentTime >= 0.0)
            {
                var clampedTime = Math.Max(0.0, Math.Min(maxD, CurrentTime));
                var markerX = padX + (clampedTime / maxD) * rulerW;

                var markerPen = ChartRenderHelper.CreateFrozenPen(effMarkerBrush, 1.8);
                dc.DrawLine(markerPen, new Point(markerX, 4.0), new Point(markerX, baselineY + 2.0));

                // Top pointer (downward pointing triangle)
                var pointerGeom = ChartRenderHelper.BuildClosedPolygon(new[]
                {
                    new Point(markerX - 4.5, 0.0),
                    new Point(markerX + 4.5, 0.0),
                    new Point(markerX, 5.5)
                });
                dc.DrawGeometry(effMarkerBrush, null, pointerGeom);

                // Optional text label above or alongside pointer
                var labelText = !string.IsNullOrEmpty(MarkerLabel)
                    ? MarkerLabel
                    : $"{clampedTime:F0}{UnitLabel}";

                var ftMarker = new FormattedText(labelText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    MarkerTypeface, 8.0, effMarkerBrush, dpi);

                var labelX = Math.Max(padX, Math.Min(w - padX - ftMarker.Width, markerX - ftMarker.Width / 2.0));
                dc.DrawText(ftMarker, new Point(labelX, 0.0));
            }
        }
    }
}



