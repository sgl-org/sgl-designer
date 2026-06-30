using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{

    public partial class SglLedData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Colors.Black; // 底座背景色;
        [ObservableProperty] private int _radius = 14;
        [ObservableProperty] private int _opacity = 255;

        [ObservableProperty] private bool _status = true;
        [ObservableProperty] private Color _onColor = Color.FromRgb(0x00, 0xFF, 0x00);     // 开启时的发光颜色
        [ObservableProperty] private Color _offColor = Color.FromRgb(0xFF, 0x00, 0x00); // 关闭时的暗色

        partial void OnRadiusChanged(int value)
        {
            if (value < 1) value = 1;
            W = H = value * 2 + 2;
        }
        public SglLedData()
        {
            X = 50; Y = 50;
            W = 30;
            H = 30;
            Radius = 14; // 默认圆形
            Type = SglMapping.SglType.Led; // 确保基类有这个 Type 赋值
                                           //订阅属性变更通知
            this.PropertyChanged += (s, e) =>
            {
                Radius = W / 2 - 1; // 圆形半径等于宽高的 1/2 - 1
            };
        }


    }
    public partial class BaseBinder
    {

        public static void BindLed(Border b, SglLedData data)
        {

            // --- 1. 最外层 Border 处理 ---
            // 强制把 Border 设为透明（它只作为交互和拖拽的占位符）
            b.ClipToBounds = false; // 允许子元素（光晕）超出 Border 边界
            Binding cornerRadiusBind = new Binding("W") { Source = data, Converter = new WidthToCornerRadiusConverter() };
            b.SetBinding(Border.CornerRadiusProperty, cornerRadiusBind);

            // --- 2. 创建 Viewbox ---
            Viewbox vb = new Viewbox
            {
                DataContext = null,
                Stretch = Stretch.Uniform, // 强制等比例，不拉伸
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            // --- 3. 内部渲染层 (固定基准 100x100) ---
            Grid contentGrid = new Grid { Width = 100, Height = 100 };

            // --- 4. 黑色底座 (原本是 Border，现在改为 Ellipse) ---
            Ellipse baseShell = new Ellipse
            {
                DataContext = null,
                Fill = new SolidColorBrush(Color.FromRgb(0x11, 0x11, 0x11)),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            // 绑定底座背景色（如果需要根据数据改变底座颜色）
            Bind(baseShell, Shape.FillProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            contentGrid.Children.Add(baseShell);

            // --- 5. 发光圆点 ---
            Ellipse light = new Ellipse
            {
                DataContext = null,
                Margin = new Thickness(1),
                IsHitTestVisible = false
            };

            // 绑定发光画刷
            MultiBinding brushBind = new MultiBinding { Converter = new LedRadialGradientConverter() };
            brushBind.Bindings.Add(new Binding("Status") { Source = data });
            brushBind.Bindings.Add(new Binding("OnColor") { Source = data });
            brushBind.Bindings.Add(new Binding("OffColor") { Source = data });
            brushBind.Bindings.Add(new Binding("BgColor") { Source = data });
            light.SetBinding(Shape.FillProperty, brushBind);
            contentGrid.Children.Add(light);

            // --- 7. 组装 ---
            vb.Child = contentGrid;
            b.Child = vb;

            b.MouseDown += (s, e) => data.Status = !data.Status;
        }
        public class LedRadialGradientConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length < 3) return Brushes.Gray;

                bool status = (bool)values[0];
                Color onCol = (Color)values[1];
                Color offCol = (Color)values[2];
                Color bgCol = (Color)values[3];

                Color baseColor = status ? onCol : offCol;

                RadialGradientBrush brush = new RadialGradientBrush
                {
                    RadiusX = 0.55,
                    RadiusY = 0.55,
                    Center = new Point(0.5, 0.5),
                    GradientOrigin = new Point(0.5, 0.5)
                };


                // 中心：原色 (最亮)
                brush.GradientStops.Add(new GradientStop(baseColor, 0.1));
                //// 边缘：变暗 (乘以 0.4)
                Color edgeColor = Color.FromRgb(
                    (byte)(bgCol.R * 0.5),
                    (byte)(bgCol.G * 0.5),
                    (byte)(bgCol.B * 0.5));
                brush.GradientStops.Add(new GradientStop(edgeColor, 1.0));


                return brush;
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
        }
    }

    public class WidthToCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int w)
            {
                return new CornerRadius(w / 2.0);
            }
            return new CornerRadius(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
}