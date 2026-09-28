using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CalradiaForge.Desktop.Presentation
{
    // =========================================================================
    // FORGE SPARKLINE CONTROL (LIGHTWEIGHT HARDWARE-ACCELERATED TIME-SERIES / AREA)
    // =========================================================================

    public sealed class ForgeSparkline : FrameworkElement
    {
        public static readonly DependencyProperty DataPointsProperty =
            DependencyProperty.Register(nameof(DataPoints), typeof(IReadOnlyList<double>), typeof(ForgeSparkline),
                new FrameworkPropertyMetadata(Array.Empty<double>(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(ForgeSparkline),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(ForgeSparkline),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(ForgeSparkline),
                new FrameworkPropertyMetadata(1.5, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShowGridLinesProperty =
            DependencyProperty.Register(nameof(ShowGridLines), typeof(bool), typeof(ForgeSparkline),
                new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GridBrushProperty =
            DependencyProperty.Register(nameof(GridBrush), typeof(Brush), typeof(ForgeSparkline),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValueUnitProperty =
            DependencyProperty.Register(nameof(ValueUnit), typeof(string), typeof(ForgeSparkline),
                new PropertyMetadata(string.Empty));

        public static readonly DependencyProperty FormatStringProperty =
            DependencyProperty.Register(nameof(FormatString), typeof(string), typeof(ForgeSparkline),
                new PropertyMetadata("N0"));

        public IReadOnlyList<double> DataPoints
        {
            get => (IReadOnlyList<double>)GetValue(DataPointsProperty);
            set => SetValue(DataPointsProperty, value);
        }

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public bool ShowGridLines
        {
            get => (bool)GetValue(ShowGridLinesProperty);
            set => SetValue(ShowGridLinesProperty, value);
        }

        public Brush GridBrush
        {
            get => (Brush)GetValue(GridBrushProperty);
            set => SetValue(GridBrushProperty, value);
        }

        public string ValueUnit
        {
            get => (string)GetValue(ValueUnitProperty);
            set => SetValue(ValueUnitProperty, value);
        }

        public string FormatString
        {
            get => (string)GetValue(FormatStringProperty);
            set => SetValue(FormatStringProperty, value);
        }

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
                var gridPen = new Pen(effectiveGrid, 0.8) { DashStyle = DashStyles.Dash };
                gridPen.Freeze();
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
            var linePen = new Pen(effectiveStroke, StrokeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            linePen.Freeze();
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
        public static readonly DependencyProperty AxisLabelsProperty =
            DependencyProperty.Register(nameof(AxisLabels), typeof(IReadOnlyList<string>), typeof(ForgeRadarChart),
                new FrameworkPropertyMetadata(Array.Empty<string>(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ValuesProperty =
            DependencyProperty.Register(nameof(Values), typeof(IReadOnlyList<double>), typeof(ForgeRadarChart),
                new FrameworkPropertyMetadata(Array.Empty<double>(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ComparisonValuesProperty =
            DependencyProperty.Register(nameof(ComparisonValues), typeof(IReadOnlyList<double>), typeof(ForgeRadarChart),
                new FrameworkPropertyMetadata(Array.Empty<double>(), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register(nameof(Stroke), typeof(Brush), typeof(ForgeRadarChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillProperty =
            DependencyProperty.Register(nameof(Fill), typeof(Brush), typeof(ForgeRadarChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ComparisonStrokeProperty =
            DependencyProperty.Register(nameof(ComparisonStroke), typeof(Brush), typeof(ForgeRadarChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ComparisonFillProperty =
            DependencyProperty.Register(nameof(ComparisonFill), typeof(Brush), typeof(ForgeRadarChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty GridBrushProperty =
            DependencyProperty.Register(nameof(GridBrush), typeof(Brush), typeof(ForgeRadarChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AxisLabelBrushProperty =
            DependencyProperty.Register(nameof(AxisLabelBrush), typeof(Brush), typeof(ForgeRadarChart),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        static readonly Typeface LabelTypeface = new("Segoe UI");

        public IReadOnlyList<string> AxisLabels
        {
            get => (IReadOnlyList<string>)GetValue(AxisLabelsProperty);
            set => SetValue(AxisLabelsProperty, value);
        }

        public IReadOnlyList<double> Values
        {
            get => (IReadOnlyList<double>)GetValue(ValuesProperty);
            set => SetValue(ValuesProperty, value);
        }

        public IReadOnlyList<double> ComparisonValues
        {
            get => (IReadOnlyList<double>)GetValue(ComparisonValuesProperty);
            set => SetValue(ComparisonValuesProperty, value);
        }

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public Brush Fill
        {
            get => (Brush)GetValue(FillProperty);
            set => SetValue(FillProperty, value);
        }

        public Brush ComparisonStroke
        {
            get => (Brush)GetValue(ComparisonStrokeProperty);
            set => SetValue(ComparisonStrokeProperty, value);
        }

        public Brush ComparisonFill
        {
            get => (Brush)GetValue(ComparisonFillProperty);
            set => SetValue(ComparisonFillProperty, value);
        }

        public Brush GridBrush
        {
            get => (Brush)GetValue(GridBrushProperty);
            set => SetValue(GridBrushProperty, value);
        }

        public Brush AxisLabelBrush
        {
            get => (Brush)GetValue(AxisLabelBrushProperty);
            set => SetValue(AxisLabelBrushProperty, value);
        }

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

            var gridPen = new Pen(effectiveGrid, 0.8) { DashStyle = DashStyles.Dash };
            gridPen.Freeze();

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
                var ringGeom = new StreamGeometry();
                using (var ctx = ringGeom.Open())
                {
                    ctx.BeginFigure(Vertex(0, rings[rIdx]), true, true);
                    for (int i = 1; i < n; i++) ctx.LineTo(Vertex(i, rings[rIdx]), true, false);
                }
                ringGeom.Freeze();
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
                var compGeom = new StreamGeometry();
                using (var ctx = compGeom.Open())
                {
                    ctx.BeginFigure(Vertex(0, Math.Clamp(comp[0], 0.05, 1.0)), true, true);
                    for (int i = 1; i < compCount; i++)
                    {
                        ctx.LineTo(Vertex(i, Math.Clamp(comp[i], 0.05, 1.0)), true, false);
                    }
                }
                compGeom.Freeze();

                var compFillBrush = ComparisonFill;
                if (compFillBrush == null)
                {
                    var col = (effectiveCompStroke as SolidColorBrush)?.Color ?? Colors.MediumSeaGreen;
                    var b = new SolidColorBrush(Color.FromArgb(40, col.R, col.G, col.B));
                    b.Freeze();
                    compFillBrush = b;
                }
                var compPen = new Pen(effectiveCompStroke, 1.4);
                compPen.Freeze();
                dc.DrawGeometry(compFillBrush, compPen, compGeom);
            }

            // Primary Polygon
            if (vals != null && vals.Count >= 3)
            {
                var vCount = Math.Min(n, vals.Count);
                var polyGeom = new StreamGeometry();
                using (var ctx = polyGeom.Open())
                {
                    ctx.BeginFigure(Vertex(0, Math.Clamp(vals[0], 0.05, 1.0)), true, true);
                    for (int i = 1; i < vCount; i++)
                    {
                        ctx.LineTo(Vertex(i, Math.Clamp(vals[i], 0.05, 1.0)), true, false);
                    }
                }
                polyGeom.Freeze();

                var fillBrush = Fill;
                if (fillBrush == null)
                {
                    var col = (effectiveStroke as SolidColorBrush)?.Color ?? Colors.Goldenrod;
                    var b = new SolidColorBrush(Color.FromArgb(70, col.R, col.G, col.B));
                    b.Freeze();
                    fillBrush = b;
                }
                var strokePen = new Pen(effectiveStroke, 1.6);
                strokePen.Freeze();
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
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(ForgeRingGauge),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register(nameof(Maximum), typeof(double), typeof(ForgeRingGauge),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ThresholdProperty =
            DependencyProperty.Register(nameof(Threshold), typeof(double), typeof(ForgeRingGauge),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(ForgeRingGauge),
                new FrameworkPropertyMetadata(6.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AccentBrushProperty =
            DependencyProperty.Register(nameof(AccentBrush), typeof(Brush), typeof(ForgeRingGauge),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BackgroundBrushProperty =
            DependencyProperty.Register(nameof(BackgroundBrush), typeof(Brush), typeof(ForgeRingGauge),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ThresholdBrushProperty =
            DependencyProperty.Register(nameof(ThresholdBrush), typeof(Brush), typeof(ForgeRingGauge),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CenterTextProperty =
            DependencyProperty.Register(nameof(CenterText), typeof(string), typeof(ForgeRingGauge),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SubTextProperty =
            DependencyProperty.Register(nameof(SubText), typeof(string), typeof(ForgeRingGauge),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TextBrushProperty =
            DependencyProperty.Register(nameof(TextBrush), typeof(Brush), typeof(ForgeRingGauge),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        static readonly Typeface BoldTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal);
        static readonly Typeface NormalTypeface = new("Segoe UI");

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double Maximum
        {
            get => (double)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        public double Threshold
        {
            get => (double)GetValue(ThresholdProperty);
            set => SetValue(ThresholdProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public Brush AccentBrush
        {
            get => (Brush)GetValue(AccentBrushProperty);
            set => SetValue(AccentBrushProperty, value);
        }

        public Brush BackgroundBrush
        {
            get => (Brush)GetValue(BackgroundBrushProperty);
            set => SetValue(BackgroundBrushProperty, value);
        }

        public Brush ThresholdBrush
        {
            get => (Brush)GetValue(ThresholdBrushProperty);
            set => SetValue(ThresholdBrushProperty, value);
        }

        public string CenterText
        {
            get => (string)GetValue(CenterTextProperty);
            set => SetValue(CenterTextProperty, value);
        }

        public string SubText
        {
            get => (string)GetValue(SubTextProperty);
            set => SetValue(SubTextProperty, value);
        }

        public Brush TextBrush
        {
            get => (Brush)GetValue(TextBrushProperty);
            set => SetValue(TextBrushProperty, value);
        }

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
            var bgPen = new Pen(effectiveBg, t);
            bgPen.Freeze();
            dc.DrawEllipse(null, bgPen, new Point(cx, cy), r, r);

            // Active Value Arc (starts at top: -90 deg)
            var maxVal = Math.Max(0.001, Maximum);
            var ratio = Math.Clamp(Value / maxVal, 0.0, 1.0);

            if (ratio >= 0.999)
            {
                var fullPen = new Pen(effectiveAccent, t);
                fullPen.Freeze();
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

                var arcPen = new Pen(effectiveAccent, t) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                arcPen.Freeze();
                dc.DrawGeometry(null, arcPen, arcGeom);
            }

            // Threshold Tick Marker (e.g. 45% danger zone)
            if (Threshold > 0.0 && Threshold < Maximum)
            {
                var thRatio = Math.Clamp(Threshold / maxVal, 0.0, 1.0);
                var thAngle = thRatio * 2.0 * Math.PI;
                var p1 = new Point(cx + (r - t * 0.7) * Math.Sin(thAngle), cy - (r - t * 0.7) * Math.Cos(thAngle));
                var p2 = new Point(cx + (r + t * 0.7) * Math.Sin(thAngle), cy - (r + t * 0.7) * Math.Cos(thAngle));
                var thPen = new Pen(ThresholdBrush ?? Brushes.IndianRed, 1.6);
                thPen.Freeze();
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
}
