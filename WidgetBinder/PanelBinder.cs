using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SglDesigner
{
    public partial class SglPanelData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Colors.DarkGray;
        [ObservableProperty] private Color _borderColor = Colors.Black;
        [ObservableProperty] private int _borderWidth = 0;
        [ObservableProperty] private int _radius = 0;
        [ObservableProperty] private int _opacity = 255;
        [ObservableProperty] private int _borderOpacity = 255;
        // 对应 sgl_rectangle_t 中的 pixmap (C语言中的结构体变量名)
        [ObservableProperty] private string _pixmapVarName = "";

        public SglPanelData()
        {
            X = 50; Y = 50;
            W = 100;
            H = 80;
            Type = SglMapping.SglType.Rectangle; // 对应 SGL 后台类型
        }
    }
    public partial class BaseBinder
    {

        public static void BindPanel(Border b, SglPanelData data)
        {
            Canvas panel = new Canvas { IsHitTestVisible = false, ClipToBounds = true };

            // 基础尺寸绑定
            Bind(panel, FrameworkElement.WidthProperty, "W", data);
            Bind(panel, FrameworkElement.HeightProperty, "H", data);

            var colorAlphaConv = new ColorAndAlphaToBrushConverter();

            // --- 核心修改：绑定背景色 (Color + Opacity) ---
            MultiBinding bgBinding = new MultiBinding { Converter = colorAlphaConv };
            bgBinding.Bindings.Add(new Binding("BgColor") { Source = data });
            bgBinding.Bindings.Add(new Binding("Opacity") { Source = data });
            b.SetBinding(Border.BackgroundProperty, bgBinding);

            // --- 核心修改：绑定边框色 (BorderColor + BorderOpacity) ---
            MultiBinding borderBinding = new MultiBinding { Converter = colorAlphaConv };
            borderBinding.Bindings.Add(new Binding("BorderColor") { Source = data });
            borderBinding.Bindings.Add(new Binding("BorderOpacity") { Source = data });
            b.SetBinding(Border.BorderBrushProperty, borderBinding);

            // 绑定边框粗细和圆角
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));

            // 图片控件处理
            Image img = new Image
            {
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            // 让图片宽高跟随 Canvas/Border
            Bind(img, FrameworkElement.WidthProperty, "W", data);
            Bind(img, FrameworkElement.HeightProperty, "H", data);

            var imgBinding = new Binding("PixmapVarName")
            {
                Source = data,
                Converter = new ResNameToImageSourceConverter(),
                Mode = BindingMode.OneWay
            };
            img.SetBinding(Image.SourceProperty, imgBinding);

            panel.Children.Add(img);
            b.Child = panel;
            b.ClipToBounds = true;
        }
    }

    public class ColorAndAlphaToBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is Color color)
            {
                int alpha;
                try
                {
                    alpha = System.Convert.ToInt32(values[1]);
                }
                catch
                {
                    return Brushes.Transparent;
                }

                int safeAlpha = alpha < 0 ? 0 : (alpha > 255 ? 255 : alpha);

                return new SolidColorBrush(Color.FromArgb((byte)safeAlpha, color.R, color.G, color.B));
            }
            return Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null; // 通常不需要反向绑定
        }
    }
}
