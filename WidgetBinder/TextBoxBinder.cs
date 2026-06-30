using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{

    public partial class SglTextBoxData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Color.FromRgb(40, 40, 40);
        [ObservableProperty] private Color _borderColor = Colors.Gray;
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _radius = 4;
        [ObservableProperty] private int _opacity = 255;


        [ObservableProperty] private string _text = "This is a textbox with multiline support.";
        [ObservableProperty] private Color _textColor = Colors.White;

        [ObservableProperty] private int _lineMargin = 4;
        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private string _fontSize = "14";

        public SglTextBoxData()
        {
            X = 50; Y = 50;
            W = 150;
            H = 100;
            Type = SglMapping.SglType.TextBox;
        }
    }
    public partial class BaseBinder
    {
        /// <summary>
        /// 为 Icon 绑定特有属性（图标资源、颜色、透明度）
        /// </summary>
        public static void BindIcon(Border b, SglIconData data)
        {
            // 不设置 Background 的 Grid
            Grid grid = new Grid { IsHitTestVisible = false, Background = Brushes.Transparent };

            // 创建默认 Path
            Path defaultPath = new Path
            {
                Data = Geometry.Parse("m 8.062,10.565 a 2.5,2.5 0 1 1 2.5,-2.5 2.5,2.5 0 0 1 -2.5,2.5 z m 0,-4 a 1.5,1.5 0 1 0 1.5,1.5 1.5,1.5 0 0 0 -1.5,-1.5 z M 18.435,3.06 H 5.565 a 2.5,2.5 0 0 0 -2.5,2.5 v 12.88 a 2.507,2.507 0 0 0 2.5,2.5 h 12.87 a 2.507,2.507 0 0 0 2.5,-2.5 V 5.56 a 2.5,2.5 0 0 0 -2.5,-2.5 z m -14.37,2.5 a 1.5,1.5 0 0 1 1.5,-1.5 h 12.87 a 1.5,1.5 0 0 1 1.5,1.5 v 8.66 l -3.88,-3.88 a 1.509,1.509 0 0 0 -2.12,0 l -4.56,4.57 a 0.513,0.513 0 0 1 -0.71,0 l -0.56,-0.56 a 1.522,1.522 0 0 0 -2.12,0 l -1.92,1.92 z m 15.87,12.88 a 1.5,1.5 0 0 1 -1.5,1.5 H 5.565 a 1.5,1.5 0 0 1 -1.5,-1.5 V 17.69 L 6.7,15.06 a 0.5,0.5 0 0 1 0.35,-0.14 0.524,0.524 0 0 1 0.36,0.14 l 0.55,0.56 a 1.509,1.509 0 0 0 2.12,0 l 4.57,-4.57 a 0.5,0.5 0 0 1 0.71,0 l 4.58,4.58 z"),
                Stretch = Stretch.Uniform, // 保持 Path 比例
            };

            // 2. 创建图片矩形
            Rectangle colorRect = new Rectangle
            {
                Stretch = Stretch.Uniform, // 保持图片比例
            };

            // --- 统一尺寸绑定 ---
            // 绑定到 Grid 而不是直接绑定到子元素，或者让子元素 Stretch
            Bind(grid, FrameworkElement.WidthProperty, "W", data);
            Bind(grid, FrameworkElement.HeightProperty, "H", data);

            // --- 颜色与遮罩 ---
            Bind(defaultPath, Shape.FillProperty, "IconColor", data, GetConv("ColorToBrushConverter"));
            Bind(colorRect, Shape.FillProperty, "IconColor", data, GetConv("ColorToBrushConverter"));

            ImageBrush iconBrush = new ImageBrush { Stretch = Stretch.Uniform };
            Binding imgBind = new Binding("IconName")
            {
                Source = data,
                Converter = new ResNameToImageSourceConverter()
            };
            BindingOperations.SetBinding(iconBrush, ImageBrush.ImageSourceProperty, imgBind);
            colorRect.OpacityMask = iconBrush;

            // --- 用状态切换显示 (简单粗暴但有效) ---
            var visibilityConv = new IconVisibilityConverter();

            // 如果图片有效，Path 隐藏
            defaultPath.SetBinding(UIElement.VisibilityProperty, new Binding("IconName")
            {
                Source = data,
                Converter = visibilityConv,
                ConverterParameter = "Path"
            });
            // 如果图片有效，矩形显示
            colorRect.SetBinding(UIElement.VisibilityProperty, new Binding("IconName")
            {
                Source = data,
                Converter = visibilityConv,
                ConverterParameter = "Rect"
            });

            // --- 对齐与透明度 ---
            var alignConv = new AlignToAlignmentConverter();
            grid.SetBinding(FrameworkElement.HorizontalAlignmentProperty, new Binding("IconAlign") { Source = data, Converter = alignConv, ConverterParameter = "H" });
            grid.SetBinding(FrameworkElement.VerticalAlignmentProperty, new Binding("IconAlign") { Source = data, Converter = alignConv, ConverterParameter = "V" });

            grid.Children.Add(defaultPath);
            grid.Children.Add(colorRect);
            b.Child = grid;
        }
    }
}