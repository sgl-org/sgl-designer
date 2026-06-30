using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{

    public partial class SglArcData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor;
        [ObservableProperty] private Color _borderColor;
        [ObservableProperty] private int _borderWidth = 0;
        [ObservableProperty] private int _radius = 4;
        [ObservableProperty] private int _opacity = 255;


        [ObservableProperty] private int _radiusIn = 40;  // 内半径
        [ObservableProperty] private int _radiusOut = 50; // 外半径 (通常等于 H/2)
        [ObservableProperty] private int _startAngle = 45; // 起始角度 (0-360)
        [ObservableProperty] private int _endAngle = 315; // 结束角度 (0-360)
        [ObservableProperty] private Color _arcColor = Colors.DodgerBlue; // 圆弧颜色
        // 当 W (宽度) 改变时，自动调整半径
        // 如果 W 在基类中，使用这种方式监听：
        private bool _isInternalUpdating = false;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            // 如果是内部同步引起的改变，直接退出，防止死循环
            if (_isInternalUpdating) return;

            if (e.PropertyName == nameof(W) || e.PropertyName == nameof(H))
            {
                _isInternalUpdating = true;
                try
                {
                    // 2. 判断哪个属性被用户拉动了
                    // 如果拉的是水平锚点(W)，就把 H 变成和 W 一样
                    // 如果拉的是垂直锚点(H)，就把 W 变成和 H 一样
                    int targetSize = (e.PropertyName == nameof(W)) ? W : H;

                    // 限制最小尺寸，防止拉没了
                    if (targetSize < 10) targetSize = 10;

                    //  强制同步：这步会让另一个轴的锚点自动跟着动
                    W = targetSize;
                    H = targetSize;

                    //  更新半径
                    UpdateRadiusByBounds();
                }
                finally
                {
                    _isInternalUpdating = false;
                }
            }
        }

        private void UpdateRadiusByBounds()
        {
            // 计算外径：容器的一半
            int newRadiusOut = (W / 2) - 2;

            // 内径不能是死数（40），必须随比例变化
            // 方案 A：保持比例（推荐，这样缩小时内径不会超过外径）
            RadiusIn = (int)(newRadiusOut * 0.8);
            RadiusOut = newRadiusOut;

        }
        public SglArcData()
        {
            X = 50; Y = 50;
            W = 70;
            H = 70;
            Type = SglMapping.SglType.Arc;
        }
    }
    public partial class BaseBinder
    {


        public static void BindArc(Border b, SglArcData data)
        {
            // HorizontalAlignment 和 VerticalAlignment 必须设为 Center
            // 配合 Path 的 Stretch="None"，确保坐标系原点(0,0)在 Border 内部
            Path arcPath = new Path
            {
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            // ... 绑定颜色和 Opacity (0-100 转 0-1.0) ...
            Bind(arcPath, Shape.FillProperty, "ArcColor", data, GetConv("ColorToBrushConverter"));
            Bind(arcPath, UIElement.OpacityProperty, "Opacity", data, GetConv("IntToDoubleConverter"));

            // 绑定形状
            MultiBinding pathBind = new MultiBinding { Converter = new ArcToPathDataConverter() };
            pathBind.Bindings.Add(new Binding("W") { Source = data });
            pathBind.Bindings.Add(new Binding("H") { Source = data });
            pathBind.Bindings.Add(new Binding("RadiusIn") { Source = data });
            pathBind.Bindings.Add(new Binding("RadiusOut") { Source = data });
            pathBind.Bindings.Add(new Binding("StartAngle") { Source = data });
            pathBind.Bindings.Add(new Binding("EndAngle") { Source = data });

            arcPath.SetBinding(Path.DataProperty, pathBind);

            b.Child = arcPath;
        }

    }
}