using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SglDesigner
{
    public partial class SglSwitchData : SglWidgetData
    {

        [ObservableProperty] private Color _bgColor = Colors.Transparent;
        [ObservableProperty] private Color _borderColor = Colors.DeepSkyBlue;
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _radius = 8;
        [ObservableProperty] private double _opacity = 255;

        [ObservableProperty] private bool _status = false; // 状态：开/关

        [ObservableProperty] private Color _activeColor = Colors.Green; // 开启时的颜色 (color)
        [ObservableProperty] private Color _inactiveColor = Colors.Red;    // 关闭时的颜色 (bg_color)
        [ObservableProperty] private Color _knobColor = Colors.White;       // 圆钮颜色 (knob_color)

        [ObservableProperty] private int _knobRadius = 8;  // 圆钮半径
        [ObservableProperty] private int _knobMargin = 3;  // 圆钮与边框的间距

        public SglSwitchData()
        {
            X = 50; Y = 50;
            W = 50;  // 默认宽度
            H = 25;  // 默认高度
        }
    }
    public partial class BaseBinder
    {

        public static void BindSwitch(Border b, SglSwitchData data)
        {
            // 创建滑块 (Knob)
            Border knob = new Border
            {
                DataContext = null,
                IsHitTestVisible = false,
                VerticalAlignment = VerticalAlignment.Center
            };

            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));

            // 2. 绑定外层背景 (保持不变)
            MultiBinding bgBind = new MultiBinding { Converter = new SwitchBgColorConverter() };
            bgBind.Bindings.Add(new Binding("Status") { Source = data });
            bgBind.Bindings.Add(new Binding("ActiveColor") { Source = data });
            bgBind.Bindings.Add(new Binding("InactiveColor") { Source = data });
            b.SetBinding(Border.BackgroundProperty, bgBind);

            //  绑定滑块颜色
            Bind(knob, Border.BackgroundProperty, "KnobColor", data, GetConv("ColorToBrushConverter"));

            //  【尺寸逻辑】：滑块是一个正方形，其边长取决于容器高度 H 和边距 KnobMargin
            // 边长 = H - (KnobMargin * 2)
            MultiBinding sizeBind = new MultiBinding { Converter = new SwitchKnobSizeConverter() };
            sizeBind.Bindings.Add(new Binding("H") { Source = data });
            sizeBind.Bindings.Add(new Binding("KnobMargin") { Source = data });
            knob.SetBinding(FrameworkElement.WidthProperty, sizeBind);
            knob.SetBinding(FrameworkElement.HeightProperty, sizeBind);

            //  【圆角逻辑】：直接绑定到定义的 KnobRadius
            // 如果 KnobRadius = 0 是直角正方形；如果 KnobRadius 足够大则是圆形
            Bind(knob, Border.CornerRadiusProperty, "KnobRadius", data, new IntToCornerRadiusConverter());

            //  水平对齐（状态切换）
            Binding alignBind = new Binding("Status")
            {
                Source = data,
                Converter = new BooleanToHorizontalAlignmentConverter()
            };
            knob.SetBinding(FrameworkElement.HorizontalAlignmentProperty, alignBind);

            //  边距处理
            // 使用 KnobMargin 决定滑块离边缘的距离
            Bind(knob, FrameworkElement.MarginProperty, "KnobMargin", data, new IntToThicknessSideConverter());

            //  组装
            b.Child = knob;
        }



    }
}