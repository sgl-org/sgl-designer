using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{

    public partial class SglCircleData : SglWidgetData
    {

        [ObservableProperty] private Color _bgColor = Color.FromRgb(40, 40, 40);
        [ObservableProperty] private Color _borderColor = Color.FromRgb(41, 128, 185);
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _radius = 25;
        [ObservableProperty] private int _opacity = 255;



        [ObservableProperty] private Color _color = Color.FromRgb(52, 152, 219);

        [ObservableProperty] private string _pixmapVarName = ""; // 存储图片路径或资源名称

        // 增加一个逻辑判断：是否有图片
        public bool HasPixmap => !string.IsNullOrEmpty(PixmapVarName);



        public SglCircleData()
        {
            Type = SglMapping.SglType.Circle;
            // 初始化尺寸为半径的2倍
            W = 50;
            H = 50;

            // 监听属性改变事件
            this.PropertyChanged += SglCircleData_PropertyChanged;
        }
        private void SglCircleData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 当宽或高改变时，强制两者同步
            if (e.PropertyName == nameof(W))
            {
                if (H != W) H = W;
                // 更新半径，但不触发 OnRadiusChanged 递归
                Radius = W / 2;
                OnPropertyChanged(nameof(Radius));
            }
            else if (e.PropertyName == nameof(H))
            {
                if (W != H) W = H;
                Radius = H / 2;
                OnPropertyChanged(nameof(Radius));
            }
        }
        // 当半径改变时，同步更新 UI 上的宽高
        partial void OnRadiusChanged(int value)
        {
            W = value * 2;
            H = value * 2;
        }
    }
    public partial class BaseBinder
    {
        public static void BindCircle(Border b, SglCircleData data)
        {
            // 直接创建一个 Ellipse
            Ellipse circle = new Ellipse
            {
                // Stretch 设为 Fill，因为数据层已经保证了 Border 是正方形
                Stretch = Stretch.Fill
            };

            // 绑定边框属性
            Bind(circle, Shape.StrokeProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(circle, Shape.StrokeThicknessProperty, "BorderWidth", data);

            // 2. 绑定不透明度 (Opacity 映射到 Alpha)
            Bind(circle, UIElement.OpacityProperty, "Opacity", data, GetConv("IntToDoubleConverter"));

            //  核心填充逻辑：使用多重绑定处理【纯色】和【图片】的切换
            MultiBinding fillBinding = new MultiBinding
            {
                Converter = new CircleFillConverter() // 全能填充转换器
            };
            // 注入图片路径
            fillBinding.Bindings.Add(new Binding("PixmapVarName") { Source = data });
            // 注入颜色属性
            fillBinding.Bindings.Add(new Binding("Color") { Source = data });

            circle.SetBinding(Shape.FillProperty, fillBinding);

            // 将 Ellipse 设置为 Border 的唯一子元素
            b.Child = circle;
        }
    }
}