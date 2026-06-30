using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SglDesigner
{
    public partial class SglProgressData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Colors.Transparent;
        [ObservableProperty] private Color _borderColor = Colors.DeepSkyBlue;
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _radius = 4;
        [ObservableProperty] private double _opacity = 255;

        [ObservableProperty] private int _value = 50;

        [ObservableProperty] private Color _fillColor = Colors.Gray;
        [ObservableProperty] private Color _trackColor = Colors.DeepSkyBlue;
        [ObservableProperty] private int _fillAlpha = 255;

        [ObservableProperty] private int _trackAlpha = 255;

        [ObservableProperty] private int _fillGap = 2;   //得2才能显示出来效果?
        [ObservableProperty] private int _fillRadius = 0;
        [ObservableProperty] private int _fillWidth = 5;

        public SglProgressData()
        {
            X = 50;
            Y = 50;
            W = 120;
            H = 12;

            FillColor = Colors.LightBlue;
        }
    }

    public partial class BaseBinder
    {
        public static void BindProgress(Border b, SglProgressData data)
        {
            b.ClipToBounds = true;
            var colorAlphaConv = new ColorAndAlphaToBrushConverter();

            MultiBinding trackBinding = new MultiBinding { Converter = colorAlphaConv };
            trackBinding.Bindings.Add(new Binding("TrackColor") { Source = data });
            trackBinding.Bindings.Add(new Binding("TrackAlpha") { Source = data });
            b.SetBinding(Border.BackgroundProperty, trackBinding);

            Grid container = new Grid
            {
                DataContext = null,
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };

            Border fillViewport = new Border
            {
                DataContext = null,
                IsHitTestVisible = false,
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Transparent
            };
            fillViewport.SetBinding(FrameworkElement.MarginProperty, new Binding("BorderWidth")
            {
                Source = data,
                Converter = new ProgressInnerMarginConverter()
            });
            fillViewport.SetBinding(FrameworkElement.WidthProperty, new MultiBinding
            {
                Converter = new ProgressFillLengthConverter(),
                Bindings =
                {
                    new Binding("Value") { Source = data },
                    new Binding("W") { Source = data },
                    new Binding("BorderWidth") { Source = data }
                }
            });

            MultiBinding fillBrushBinding = new MultiBinding { Converter = new ProgressChunkBrushConverter() };
            fillBrushBinding.Bindings.Add(new Binding("FillColor") { Source = data });
            fillBrushBinding.Bindings.Add(new Binding("FillAlpha") { Source = data });
            fillBrushBinding.Bindings.Add(new Binding("FillWidth") { Source = data });
            fillBrushBinding.Bindings.Add(new Binding("FillGap") { Source = data });
            fillBrushBinding.Bindings.Add(new Binding("FillRadius") { Source = data });
            fillBrushBinding.Bindings.Add(new Binding("Radius") { Source = data });
            fillBrushBinding.Bindings.Add(new Binding("BorderWidth") { Source = data });
            fillBrushBinding.Bindings.Add(new Binding("H") { Source = data });
            fillViewport.SetBinding(Border.BackgroundProperty, fillBrushBinding);

            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));

            container.Children.Add(fillViewport);
            b.Child = container;
        }
    }

    public class ProgressInnerMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double border = Math.Max(0.0, System.Convert.ToDouble(value));
            return new Thickness(border);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class ProgressFillLengthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || Array.Exists(values, v => v == DependencyProperty.UnsetValue))
            {
                return 0.0;
            }

            double percent = Math.Max(0.0, Math.Min(100.0, System.Convert.ToDouble(values[0])));
            double totalWidth = System.Convert.ToDouble(values[1]);
            double border = Math.Max(0.0, System.Convert.ToDouble(values[2]));
            double innerWidth = Math.Max(0.0, totalWidth - ((border) * 2.0));

            return innerWidth * percent / 100.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    public class ProgressChunkBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 8 || Array.Exists(values, v => v == DependencyProperty.UnsetValue))
            {
                return Brushes.Transparent;
            }

            Color color = values[0] is Color fillColor ? fillColor : Colors.Transparent;
            int alpha = System.Convert.ToInt32(values[1]);
            alpha = alpha < 0 ? 0 : (alpha > 255 ? 255 : alpha);
            double chunkWidth = Math.Max(1.0, System.Convert.ToDouble(values[2]));
            double gap = Math.Max(0.0, System.Convert.ToDouble(values[3]));
            double fillRadius = Math.Max(0.0, System.Convert.ToDouble(values[4]));
            double outerRadius = Math.Max(0.0, System.Convert.ToDouble(values[5]));
            double border = Math.Max(0.0, System.Convert.ToDouble(values[6]));
            double totalHeight = Math.Max(0.0, System.Convert.ToDouble(values[7]));
            double innerHeight = Math.Max(1.0, totalHeight - ((border) * 2.0));
            double radius = Math.Min(Math.Min(outerRadius, fillRadius), Math.Min(chunkWidth / 2.0, innerHeight / 2.0));
            double step = Math.Max(1.0, chunkWidth + gap);

            var fillBrush = new SolidColorBrush(Color.FromArgb((byte)alpha, color.R, color.G, color.B));
            var chunkGeometry = new RectangleGeometry(new Rect(0, 0, chunkWidth, innerHeight), radius, radius);
            var drawing = new GeometryDrawing(fillBrush, null, chunkGeometry);
            var drawingGroup = new DrawingGroup();
            drawingGroup.Children.Add(drawing);

            return new DrawingBrush(drawingGroup)
            {
                Stretch = Stretch.None,
                TileMode = TileMode.Tile,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                ViewportUnits = BrushMappingMode.Absolute,
                ViewboxUnits = BrushMappingMode.Absolute,
                Viewbox = new Rect(0, 0, step, innerHeight),
                Viewport = new Rect(0, 0, step, innerHeight),
                Transform = new TranslateTransform(-(gap * 2.0), 0)
            };
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
}
