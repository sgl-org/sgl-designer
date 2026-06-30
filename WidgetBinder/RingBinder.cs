using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{

    public partial class SglRingData : SglWidgetData
    {
        [ObservableProperty] private int _radiusIn = 20;
        [ObservableProperty] private int _radiusOut = 30;
        [ObservableProperty] private Color _color = Color.FromRgb(46, 204, 113);
        [ObservableProperty] private int _opacity = 255;

        public SglRingData()
        {
            Type = SglMapping.SglType.Ring;
            W = 50;
            H = 50;

            this.PropertyChanged += SglRingData_PropertyChanged;
        }

        private void SglRingData_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // 与 CircleBinder 同样模式：W/H 变化时强制正方形，反向推算半径
            if (e.PropertyName == nameof(W))
            {
                if (H != W) H = W;
                UpdateRadiusFromSize(W);
            }
            else if (e.PropertyName == nameof(H))
            {
                if (W != H) W = H;
                UpdateRadiusFromSize(H);
            }
        }

        private void UpdateRadiusFromSize(int size)
        {
            int newRadiusOut = size / 2;
            if (newRadiusOut <= 0) newRadiusOut = 1;

            // 按原有比例缩放内半径，保留一位小数精度避免累积误差
            double ratio = RadiusOut > 0 ? (double)RadiusIn / RadiusOut : 0;
            int newRadiusIn = (int)Math.Round(newRadiusOut * ratio, MidpointRounding.AwayFromZero);
            if (newRadiusIn >= newRadiusOut) newRadiusIn = newRadiusOut - 2;
            if (newRadiusIn < 0) newRadiusIn = 0;

            RadiusOut = newRadiusOut;
            RadiusIn = newRadiusIn;
        }

        // 当外径改变时，同步更新 UI 控件的宽高
        partial void OnRadiusOutChanged(int value)
        {
            W = value * 2;
            H = value * 2;
            // 确保内径不会超过外径
            if (RadiusIn >= value) RadiusIn = value - 2;
        }
    }
    public partial class BaseBinder
    {
        public static void BindRing(Border b, SglRingData data)
        {
            Path ringPath = new Path { Stretch = Stretch.Uniform, DataContext = null };

            // 绑定颜色和透明度
            Bind(ringPath, Shape.FillProperty, "Color", data, GetConv("ColorToBrushConverter"));
            Bind(ringPath, UIElement.OpacityProperty, "Alpha", data, GetConv("IntToDoubleConverter"));

            // 监听内径和外径变化，动态更新几何体数据
            void UpdateGeometry()
            {
                double ro = data.RadiusOut;
                double ri = data.RadiusIn;
                if (ro <= 0) ro = 1;

                // 创建环形几何体：大圆减去小圆
                CombinedGeometry ringGeo = new CombinedGeometry
                {
                    GeometryCombineMode = GeometryCombineMode.Exclude,
                    Geometry1 = new EllipseGeometry(new Point(ro, ro), ro, ro),
                    Geometry2 = new EllipseGeometry(new Point(ro, ro), ri, ri)
                };
                ringPath.Data = ringGeo;
            }

            // 简单起见，这里可以利用 PropertyChanged 监听
            data.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == "RadiusIn" || e.PropertyName == "RadiusOut") UpdateGeometry();
            };

            UpdateGeometry();
            b.Child = ringPath;
        }
    }
}