using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{
    public enum SglLinePointType
    {
        None = 0,
        Circle = 1,
        Square = 2
    }

    public enum SglLineOpenAnimDir
    {
        None = 0,
        FromLeft = 1,
        FromTop = 2
    }

    public partial class SglLinechartData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Colors.Black;
        [ObservableProperty] private byte _bgAlpha = 255;
        [ObservableProperty] private Color _borderColor = Color.FromRgb(60, 60, 60);
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _radius = 0;
        [ObservableProperty] private byte _opacity = 255;

        [ObservableProperty] private int _plotOffsetX = 44;
        [ObservableProperty] private int _plotOffsetY = 4;
        [ObservableProperty] private int _plotWidth = 152;
        [ObservableProperty] private int _plotHeight = 112;

        [ObservableProperty] private bool _showXGrid = true;
        [ObservableProperty] private bool _xGridDashed = true;
        [ObservableProperty] private bool _showXLabels = true;
        [ObservableProperty] private bool _showXTicks = true;
        [ObservableProperty] private Color _xGridColor = Color.FromRgb(60, 60, 60);
        [ObservableProperty] private int _xGridAlpha = 80;
        [ObservableProperty] private Color _xLabelColor = Colors.White;
        [ObservableProperty] private int _xLabelAlpha = 255;
        [ObservableProperty] private string _xFontName = "consolas14";

        [ObservableProperty] private int _yMin = 0;
        [ObservableProperty] private int _yMax = 100;
        [ObservableProperty] private bool _yAutoScale = true;
        [ObservableProperty] private bool _showYLabels = true;
        [ObservableProperty] private bool _showYGrid = true;
        [ObservableProperty] private bool _showYTicks = true;
        [ObservableProperty] private bool _yGridDashed = true;
        [ObservableProperty] private int _yGridAlpha = 80;
        [ObservableProperty] private int _yLabelAlpha = 255;
        [ObservableProperty] private int _yStep = 0;
        [ObservableProperty] private int _yDivisions = 4;
        [ObservableProperty] private Color _yGridColor = Color.FromRgb(60, 60, 60);
        [ObservableProperty] private Color _yLabelColor = Colors.White;
        [ObservableProperty] private string _yFontName = "consolas14";

        [ObservableProperty] private int _lineThickness = 2;
        [ObservableProperty] private Color _lineColor = Colors.DodgerBlue;
        [ObservableProperty] private int _lineOpacity = 255;
        [ObservableProperty] private SglLinePointType _pointType = SglLinePointType.Circle;
        [ObservableProperty] private int _pointRadius = 3;
        [ObservableProperty] private bool _fillUnder = false;
        [ObservableProperty] private Color _fillColor = Colors.LightSkyBlue;
        [ObservableProperty] private int _fillOpacity = 96;

        [ObservableProperty] private bool _enableOpenAnim = false;
        [ObservableProperty] private SglLineOpenAnimDir _openAnimDir = SglLineOpenAnimDir.FromLeft;
        [ObservableProperty] private int _openAnimDuration = 600;

        [ObservableProperty] private ObservableCollection<int> _values = new ObservableCollection<int> { 10, 40, 25, 60, 30 };

        public event Action<SglLinechartData> RefreshRequested;

        public SglLinechartData()
        {
            Type = SglMapping.SglType.Linechart;
            W = 200;
            H = 140;

            HookValuesCollection(Values);

            PropertyChanged += (s, e) =>
            {
                if (ShouldRefreshFor(e.PropertyName))
                {
                    RequestRefresh();
                }
            };
        }

        partial void OnValuesChanged(ObservableCollection<int> oldValue, ObservableCollection<int> newValue)
        {
            UnhookValuesCollection(oldValue);
            HookValuesCollection(newValue);
            RequestRefresh();
        }

        private void HookValuesCollection(ObservableCollection<int> values)
        {
            if (values != null)
            {
                values.CollectionChanged += Values_CollectionChanged;
            }
        }

        private void UnhookValuesCollection(ObservableCollection<int> values)
        {
            if (values != null)
            {
                values.CollectionChanged -= Values_CollectionChanged;
            }
        }

        private void Values_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) => RequestRefresh();

        private bool ShouldRefreshFor(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return true;
            }

            switch (propertyName)
            {
                case nameof(Id):
                case nameof(Tag):
                case nameof(Type):
                case nameof(X):
                case nameof(Y):
                case nameof(IsSelected):
                case nameof(EventCallbackName):
                case nameof(IsClickable):
                    return false;
                default:
                    return true;
            }
        }

        private void RequestRefresh()
        {
            RefreshRequested?.Invoke(this);
        }
    }

    public partial class BaseBinder
    {
        public static void BindLinechart(Border b, SglLinechartData data)
        {
            var colorAlphaConv = new ColorAndAlphaToBrushConverter();

            MultiBinding bgBinding = new MultiBinding { Converter = colorAlphaConv };
            bgBinding.Bindings.Add(new Binding("BgAlpha") { Source = data });
            b.SetBinding(Border.BackgroundProperty, bgBinding);

            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            Bind(b, UIElement.OpacityProperty, "Opacity", data, GetConv("ByteToOpacityConverter"));



            Canvas canvas;
            if (b.Child is Canvas existingCanvas)
            {
                canvas = existingCanvas;
                canvas.Children.Clear();
            }
            else
            {
                canvas = new Canvas
                {
                    DataContext = null,
                    IsHitTestVisible = false,
                    ClipToBounds = true,
                    Background = Brushes.Transparent
                };
                b.Child = canvas;
            }
            canvas.SetBinding(Canvas.BackgroundProperty, bgBinding);

            if (data.W <= 0 || data.H <= 0)
            {
                return;
            }

            Rect plotRect = GetPlotRect(data);
            if (plotRect.Width <= 1 || plotRect.Height <= 1)
            {
                return;
            }

            var values = (data.Values ?? new ObservableCollection<int>()).ToList();
            if (values.Count == 0)
            {
                values.Add(0);
            }

            var yRange = ResolveYRange(data, values);
            var points = BuildSeriesPoints(data, values, plotRect, yRange.min, yRange.max);

            DrawAxes(canvas, data, plotRect, values.Count, yRange.min, yRange.max);
            DrawSeries(canvas, data, plotRect, points);
        }

        private static Rect GetPlotRect(SglLinechartData data)
        {
            double left = Math.Max(0, data.PlotOffsetX);
            double top = Math.Max(0, data.PlotOffsetY);
            double width = Math.Max(10, Math.Min(data.PlotWidth, data.W - left - 1));
            double height = Math.Max(10, Math.Min(data.PlotHeight, data.H - top - 1));

            return new Rect(left, top, width, height);
        }

        private static (int min, int max) ResolveYRange(SglLinechartData data, System.Collections.Generic.IReadOnlyList<int> values)
        {
            if (!data.YAutoScale)
            {
                int fixedMin = data.YMin;
                int fixedMax = data.YMax <= fixedMin ? fixedMin + 1 : data.YMax;
                return (fixedMin, fixedMax);
            }

            int minVal = values.Min();
            int maxVal = values.Max();

            if (minVal == maxVal)
            {
                return (minVal - 1, maxVal + 1);
            }

            int range = maxVal - minVal;
            int margin = Math.Max(1, range / 10);
            return (minVal - margin, maxVal + margin);
        }

        private static PointCollection BuildSeriesPoints(SglLinechartData data, System.Collections.Generic.IReadOnlyList<int> values, Rect plotRect, int yMin, int yMax)
        {
            var points = new PointCollection();
            double range = Math.Max(1.0, yMax - yMin);

            if (values.Count == 1)
            {
                double pct = (values[0] - yMin) / range;
                double y = plotRect.Bottom - (pct * plotRect.Height);
                points.Add(new Point(plotRect.Left + plotRect.Width / 2.0, y));
                return points;
            }

            double stepX = plotRect.Width / Math.Max(1, values.Count - 1);
            for (int i = 0; i < values.Count; i++)
            {
                double pct = (values[i] - yMin) / range;
                double x = plotRect.Left + (stepX * i);
                double y = plotRect.Bottom - (pct * plotRect.Height);
                points.Add(new Point(x, y));
            }

            return points;
        }

        private static void DrawAxes(Canvas canvas, SglLinechartData data, Rect plotRect, int pointCount, int yMin, int yMax)
        {
            int yStep = GetEffectiveStep(yMin, yMax, data.YStep, data.YDivisions);
            int yTickCount = 0;

            for (int value = yMin; value <= yMax && yTickCount < 64; value += yStep, yTickCount++)
            {
                double ratio = (value - yMin) / (double)Math.Max(1, yMax - yMin);
                double y = plotRect.Bottom - (ratio * plotRect.Height);

                if (data.ShowYGrid)
                {
                    var gridLine = CreateLine(plotRect.Left, y, plotRect.Right, y, data.YGridColor, data.YGridAlpha, data.YGridDashed);
                    canvas.Children.Add(gridLine);
                }

                if (data.ShowYTicks)
                {
                    var tickLine = CreateLine(plotRect.Left - 4, y, plotRect.Left - 1, y, data.YGridColor, data.YGridAlpha, false);
                    canvas.Children.Add(tickLine);
                }

                if (data.ShowYLabels)
                {
                    var label = CreateAxisLabel(value.ToString(CultureInfo.InvariantCulture), data.YLabelColor, data.YLabelAlpha, data.YFontName);
                    Canvas.SetLeft(label, 2);
                    Canvas.SetTop(label, y - 8);
                    canvas.Children.Add(label);
                }
            }

            if (pointCount <= 0)
            {
                return;
            }

            int xStep = Math.Max(1, (int)Math.Ceiling(pointCount / 8.0));
            for (int i = 0; i < pointCount; i += xStep)
            {
                double x = pointCount == 1
                    ? plotRect.Left + plotRect.Width / 2.0
                    : plotRect.Left + (plotRect.Width * i / Math.Max(1, pointCount - 1));

                if (data.ShowXGrid)
                {
                    var gridLine = CreateLine(x, plotRect.Top, x, plotRect.Bottom, data.XGridColor, data.XGridAlpha, data.XGridDashed);
                    canvas.Children.Add(gridLine);
                }

                if (data.ShowXTicks)
                {
                    var tickLine = CreateLine(x, plotRect.Bottom + 1, x, plotRect.Bottom + 4, data.XGridColor, data.XGridAlpha, false);
                    canvas.Children.Add(tickLine);
                }

                if (data.ShowXLabels)
                {
                    var label = CreateAxisLabel(i.ToString(CultureInfo.InvariantCulture), data.XLabelColor, data.XLabelAlpha, data.XFontName);
                    label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    Canvas.SetLeft(label, x - label.DesiredSize.Width / 2.0);
                    Canvas.SetTop(label, plotRect.Bottom + 4);
                    canvas.Children.Add(label);
                }
            }
        }

        private static void DrawSeries(Canvas canvas, SglLinechartData data, Rect plotRect, PointCollection points)
        {
            if (points.Count == 0)
            {
                return;
            }

            RectangleGeometry clip = new RectangleGeometry(plotRect);

            if (data.FillUnder)
            {
                var fillGeometry = new StreamGeometry();
                using (var ctx = fillGeometry.Open())
                {
                    ctx.BeginFigure(points[0], true, true);
                    ctx.PolyLineTo(points.Skip(1).ToList(), true, true);
                    ctx.LineTo(new Point(points[points.Count - 1].X, plotRect.Bottom), true, false);
                    ctx.LineTo(new Point(points[0].X, plotRect.Bottom), true, false);
                }
                fillGeometry.Freeze();

                var fillPath = new Path
                {
                    Data = fillGeometry,
                    Fill = new SolidColorBrush(data.FillColor),
                    Opacity = Math.Max(0.0, Math.Min(1.0, data.FillOpacity / 255.0)),
                    IsHitTestVisible = false,
                    Clip = clip
                };
                canvas.Children.Add(fillPath);
            }

            var lineGeometry = new StreamGeometry();
            using (var ctx = lineGeometry.Open())
            {
                ctx.BeginFigure(points[0], false, false);
                ctx.PolyLineTo(points.Skip(1).ToList(), true, true);
            }
            lineGeometry.Freeze();

            var linePath = new Path
            {
                Data = lineGeometry,
                Stroke = new SolidColorBrush(data.LineColor),
                StrokeThickness = Math.Max(1.0, data.LineThickness),
                Opacity = Math.Max(0.0, Math.Min(1.0, data.LineOpacity / 255.0)),
                IsHitTestVisible = false,
                Clip = clip
            };
            canvas.Children.Add(linePath);

            if (data.PointType == SglLinePointType.None || data.PointRadius <= 0)
            {
                return;
            }

            foreach (Point point in points)
            {
                Shape marker = data.PointType == SglLinePointType.Circle
                    ? (Shape)new Ellipse()
                    : new Rectangle();

                marker.Width = data.PointRadius * 2.0;
                marker.Height = data.PointRadius * 2.0;
                marker.Fill = new SolidColorBrush(data.LineColor);
                marker.Opacity = Math.Max(0.0, Math.Min(1.0, data.LineOpacity / 255.0));
                marker.IsHitTestVisible = false;

                Canvas.SetLeft(marker, point.X - data.PointRadius);
                Canvas.SetTop(marker, point.Y - data.PointRadius);
                canvas.Children.Add(marker);
            }
        }

        private static int GetEffectiveStep(int min, int max, int step, int divisions)
        {
            int range = Math.Max(1, max - min);
            if (step > 0)
            {
                return step;
            }

            int safeDivisions = Math.Max(1, divisions);
            int computed = range / safeDivisions;
            return Math.Max(1, computed);
        }

        private static Line CreateLine(double x1, double y1, double x2, double y2, Color color, int alpha, bool dashed)
        {
            int safeAlpha = alpha < 0 ? 0 : (alpha > 255 ? 255 : alpha);
            var line = new Line
            {
                X1 = x1,
                Y1 = y1,
                X2 = x2,
                Y2 = y2,
                Stroke = new SolidColorBrush(color),
                Opacity = safeAlpha / 255.0,
                StrokeThickness = 1.0,
                IsHitTestVisible = false
            };

            if (dashed)
            {
                line.StrokeDashArray = new DoubleCollection(new[] { 4.0, 4.0 });
            }

            return line;
        }

        private static TextBlock CreateAxisLabel(string text, Color color, int alpha, string fontName)
        {
            int safeAlpha = alpha < 0 ? 0 : (alpha > 255 ? 255 : alpha);
            return new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontFamily = ResolvePreviewFontFamily(fontName),
                Foreground = new SolidColorBrush(color),
                Opacity = safeAlpha / 255.0,
                IsHitTestVisible = false
            };
        }

        private static FontFamily ResolvePreviewFontFamily(string fontName)
        {
            if (string.IsNullOrWhiteSpace(fontName))
            {
                return SystemFonts.MessageFontFamily;
            }

            string familyName = new string(fontName.TakeWhile(c => !char.IsDigit(c)).ToArray());
            if (string.IsNullOrWhiteSpace(familyName) || familyName.Equals("sgl_font_default", StringComparison.OrdinalIgnoreCase))
            {
                return SystemFonts.MessageFontFamily;
            }

            try
            {
                return new FontFamily(familyName);
            }
            catch
            {
                return SystemFonts.MessageFontFamily;
            }
        }
    }
}
