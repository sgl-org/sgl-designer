using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SglDesigner
{

    public partial class SglExtImageData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Color.FromRgb(40, 40, 40);
        [ObservableProperty] private Color _borderColor = Colors.Gray;
        [ObservableProperty] private int _borderWidth = 0;
        [ObservableProperty] private int _radius;
        [ObservableProperty] private int _opacity = 255;

        // 对应 sgl_ext_img_t 中的 pixmap (C语言中的结构体变量名)
        [ObservableProperty] private string _pixmapVarName = "";

        // 对应 sgl_ext_img_set_read_ops (读取 Flash 的函数指针名)
        [ObservableProperty] private string _readFuncName = "flash_read_data";



        // 对应 sgl_ext_img_set_pixmap_num
        [ObservableProperty] private int _pixmapNum = 1; // 帧数
        [ObservableProperty] private bool _autoRefresh = false; // 是否自动循环播放动画

        public SglExtImageData()
        {
            Type = SglMapping.SglType.Img_Ext; // 需在 SglType 枚举中添加 ExtImage
            W = 100;
            H = 100;
        }
    }
    public partial class BaseBinder
    {
        public static void BindExtImage(Border b, SglExtImageData data)
        {
            // 配置外层 Border
            b.HorizontalAlignment = HorizontalAlignment.Left;
            b.VerticalAlignment = VerticalAlignment.Top;
            b.Background = Brushes.Transparent;

            // 绑定宽高
            b.SetBinding(FrameworkElement.WidthProperty, new Binding("W") { Source = data, Mode = BindingMode.TwoWay });
            b.SetBinding(FrameworkElement.HeightProperty, new Binding("H") { Source = data, Mode = BindingMode.TwoWay });

            // 2. 创建一个容器 Grid，用来叠加默认图标和真实图片
            Grid container = new Grid();

            //  创建默认的 Path 图标
            Path defaultPath = new Path
            {
                Data = Geometry.Parse("m 8.062,10.565 a 2.5,2.5 0 1 1 2.5,-2.5 2.5,2.5 0 0 1 -2.5,2.5 z m 0,-4 a 1.5,1.5 0 1 0 1.5,1.5 1.5,1.5 0 0 0 -1.5,-1.5 z M 18.435,3.06 H 5.565 a 2.5,2.5 0 0 0 -2.5,2.5 v 12.88 a 2.507,2.507 0 0 0 2.5,2.5 h 12.87 a 2.507,2.507 0 0 0 2.5,-2.5 V 5.56 a 2.5,2.5 0 0 0 -2.5,-2.5 z m -14.37,2.5 a 1.5,1.5 0 0 1 1.5,-1.5 h 12.87 a 1.5,1.5 0 0 1 1.5,1.5 v 8.66 l -3.88,-3.88 a 1.509,1.509 0 0 0 -2.12,0 l -4.56,4.57 a 0.513,0.513 0 0 1 -0.71,0 l -0.56,-0.56 a 1.522,1.522 0 0 0 -2.12,0 l -1.92,1.92 z m 15.87,12.88 a 1.5,1.5 0 0 1 -1.5,1.5 H 5.565 a 1.5,1.5 0 0 1 -1.5,-1.5 V 17.69 L 6.7,15.06 a 0.5,0.5 0 0 1 0.35,-0.14 0.524,0.524 0 0 1 0.36,0.14 l 0.55,0.56 a 1.509,1.509 0 0 0 2.12,0 l 4.57,-4.57 a 0.5,0.5 0 0 1 0.71,0 l 4.58,4.58 z"),
                Stretch = Stretch.Uniform,
                Fill = new SolidColorBrush(Color.FromRgb(80, 80, 80)), // 默认灰色图标
                Margin = new Thickness(5),
                Opacity = 0.5
            };

            //  创建真实图片 Image
            Image img = new Image
            {
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            //  核心逻辑：控制显隐切换
            // 如果 PixmapVarName 为空或 Null，显示 Path，隐藏 Image
            var hasImageBinding = new Binding("PixmapVarName")
            {
                Source = data,
                Converter = new StringToVisibilityConverter(), // 需要实现或获取这个转换器
                ConverterParameter = "Inverse" // 取反：有字符串则隐藏 Path
            };
            defaultPath.SetBinding(UIElement.VisibilityProperty, hasImageBinding);

            var showImageBinding = new Binding("PixmapVarName")
            {
                Source = data,
                Converter = new StringToVisibilityConverter() // 有字符串则显示 Image
            };
            img.SetBinding(UIElement.VisibilityProperty, showImageBinding);

            //  绑定图片源
            img.SetBinding(Image.SourceProperty, new Binding("PixmapVarName")
            {
                Source = data,
                Converter = new ResNameToImageSourceConverter(),
                NotifyOnTargetUpdated = true
            });

            //  尺寸适配逻辑
            img.TargetUpdated += (s, e) =>
            {
                if (img.Source is BitmapSource bmp && (data.W == 0 || data.H == 0))
                {
                    data.W = (int)bmp.PixelWidth;
                    data.H = (int)bmp.PixelHeight;
                }
            };

            // 组装
            container.Children.Add(defaultPath);
            container.Children.Add(img);
            b.Child = container;
        }
    }
}