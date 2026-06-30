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

namespace SglDesigner
{

    public partial class SglPolygonData : SglWidgetData
    {
        // 顶点集合：对应 sgl_pos_t* vertices 和 vertex_count
        // 改用 SglPoint
        [ObservableProperty]
        private ObservableCollection<SglPoint> _vertices = new ObservableCollection<SglPoint>();

        // 填充颜色：对应 sgl_color_t fill_color
        [ObservableProperty] private Color _fillColor = Colors.LightGray;

        // 描边颜色：对应 sgl_color_t border_color
        [ObservableProperty] private Color _polygonBorderColor = Colors.DeepPink;

        // 描边宽度：对应 uint8_t border_width
        [ObservableProperty] private int _polygonBorderWidth = 1;

        // 透明度：对应 uint8_t alpha
        [ObservableProperty] private int _Opacity = 255;

        // 文本内容：对应 const char* text
        [ObservableProperty] private string _text = "Text";

        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private string _fontSize = "14";


        // 文本颜色：对应 sgl_color_t text_color
        [ObservableProperty] private Color _textColor = Colors.DeepPink;

        public SglPolygonData()
        {
            X = 0; Y = 0;
            W = 98; H = 90;
            Type = SglMapping.SglType.Polygon;




            // 监听集合内部变化
            _vertices.CollectionChanged += HandleVerticesCollectionChanged;
        }
        // 专门处理集合变化的方法，避免在构造函数写太多匿名函数
        private void HandleVerticesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SglPoint p in e.NewItems)
                {
                    p.PropertyChanged += (s1, e1) => OnPropertyChanged(nameof(Vertices));
                }
            }
            OnPropertyChanged(nameof(Vertices));
        }
        public static SglPolygonData CreateWithDefaults()
        {
            var data = new SglPolygonData();
            data.Vertices.Add(new SglPoint(10, 10));
            data.Vertices.Add(new SglPoint(90, 20));
            data.Vertices.Add(new SglPoint(80, 85));
            data.Vertices.Add(new SglPoint(15, 70));
            return data;
        }
    }
    public partial class BaseBinder
    {
        public static void BindPolygon(Border b, SglPolygonData data)
        {
            // 基础配置
            b.BorderThickness = new Thickness(0);
            b.Background = Brushes.Transparent;
            b.ClipToBounds = true;
            Bind(b, FrameworkElement.WidthProperty, "W", data);
            Bind(b, FrameworkElement.HeightProperty, "H", data);

            Canvas container = new Canvas { IsHitTestVisible = false };
            var colorAlphaConv = new ColorAndAlphaToBrushConverter();
            var centroidConv = new VerticesToCentroidConverter();

            // 2. 多边形绘制
            System.Windows.Shapes.Polygon polyShape = new System.Windows.Shapes.Polygon();
            polyShape.SetBinding(System.Windows.Shapes.Polygon.PointsProperty, new Binding("Vertices")
            {
                Source = data,
                Converter = new VerticesToPointCollectionConverter()
            });

            // 颜色透明度绑定
            MultiBinding fillBind = new MultiBinding { Converter = colorAlphaConv };
            fillBind.Bindings.Add(new Binding("FillColor") { Source = data });
            fillBind.Bindings.Add(new Binding("Opacity") { Source = data });
            polyShape.SetBinding(System.Windows.Shapes.Polygon.FillProperty, fillBind);

            MultiBinding strokeBind = new MultiBinding { Converter = colorAlphaConv };
            strokeBind.Bindings.Add(new Binding("PolygonBorderColor") { Source = data });
            strokeBind.Bindings.Add(new Binding("Opacity") { Source = data });
            polyShape.SetBinding(System.Windows.Shapes.Polygon.StrokeProperty, strokeBind);
            Bind(polyShape, System.Windows.Shapes.Polygon.StrokeThicknessProperty, "PolygonBorderWidth", data, GetConv("IntToDoubleConverter"));

            //  【核心修复】：文字定位锚点容器
            // 创建一个没有任何尺寸的 Grid。WPF 中，Grid 里的内容默认是相对于 Grid 中心对齐的。
            Grid textAnchor = new Grid
            {
                Width = 1,
                Height = 1,
                ClipToBounds = false // 允许文字溢出这个 1x1 的格子显示
            };

            TextBlock tb = new TextBlock
            {
                // 关键点：让 TextBlock 在这个 1x1 的格子中水平垂直居中
                // 这样文字的几何中心就永远锁死在 Grid 的 (0.5, 0.5) 坐标上
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                Padding = new Thickness(0),
                Margin = new Thickness(-500, -500, -500, -500) // 解决因容器太小导致的排版换行或隐藏问题
            };

            // 绑定内容、字体、颜色
            MultiBinding textFilterBind = new MultiBinding { Converter = new FontPreviewTextConverter() };
            textFilterBind.Bindings.Add(new Binding("Text") { Source = data });
            textFilterBind.Bindings.Add(new Binding("FontName") { Source = data });
            tb.SetBinding(TextBlock.TextProperty, textFilterBind);
            Bind(tb, TextBlock.ForegroundProperty, "TextColor", data, GetConv("ColorToBrushConverter"));
            Bind(tb, TextBlock.FontSizeProperty, "FontName", data, new FontNameToSizeConverter());
            Bind(tb, TextBlock.FontFamilyProperty, "FontName", data, new FontNameToFamilyConverter());

            //  定位：将 1x1 的锚点容器放在重心坐标上
            textAnchor.SetBinding(Canvas.LeftProperty, new Binding("Points")
            {
                Source = polyShape,
                Converter = centroidConv,
                ConverterParameter = "X"
            });
            textAnchor.SetBinding(Canvas.TopProperty, new Binding("Points")
            {
                Source = polyShape,
                Converter = centroidConv,
                ConverterParameter = "Y"
            });

            //  组装
            textAnchor.Children.Add(tb);
            container.Children.Add(polyShape);
            container.Children.Add(textAnchor);
            b.Child = container;
        }

        // 计算多边形重心坐标 (X 或 Y)
        public class VerticesToCentroidConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is PointCollection points && points.Count > 0)
                {
                    string axis = parameter?.ToString().ToUpper() ?? "X";
                    // SGL 源码算法：所有顶点的算术平均值
                    return axis == "X" ? points.Average(p => p.X) : points.Average(p => p.Y);
                }
                return 0.0;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
        }

        // 2. 颜色 + 透明度合成 Brush
        public class ColorAndAlphaToBrushConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length >= 2 && values[0] is Color color && values[1] is int alpha)
                {
                    // 兼容旧版，手动限制 0-255
                    byte a = (byte)(alpha < 0 ? 0 : (alpha > 255 ? 255 : alpha));
                    return new SolidColorBrush(Color.FromArgb(a, color.R, color.G, color.B));
                }
                return Brushes.Transparent;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
        }


    }
}