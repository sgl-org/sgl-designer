using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace SglDesigner
{
    public partial class BaseBinder
    {
        // 预取转换器资源
        public static IValueConverter GetConv(string key)
        {
            // 优先从当前主窗口找资源，找不到再从全局找
            var conv = Application.Current.MainWindow?.TryFindResource(key)
                       ?? Application.Current.TryFindResource(key);

            if (conv == null)
            {
                // 说明 XAML 里确实没写，或者 Key 写错了
                throw new Exception($"致命错误：XAML 中未定义资源 {key}，请检查 Window.Resources");
            }
            return (IValueConverter)conv;
        }

        public static void Bind(DependencyObject target, DependencyProperty dp, string path, object source, IValueConverter conv = null)
        {
            Binding bnd = new Binding(path)
            {
                Source = source,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = conv
            };
            BindingOperations.SetBinding(target, dp, bnd);
        }
        /// <summary>
        /// 为所有 SGL 控件绑定共有基础属性
        /// </summary>
        public static void BindBase(Border b, SglWidgetData data)
        {
            b.DataContext = data;

            // 位置与尺寸 (双向绑定)
            Bind(b, Canvas.LeftProperty, "X", data);
            Bind(b, Canvas.TopProperty, "Y", data);
            Bind(b, FrameworkElement.WidthProperty, "W", data);
            Bind(b, FrameworkElement.HeightProperty, "H", data);

            // 颜色 (需要 ColorToBrush 转换器)
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));

            //  边框与圆角 (需要 Thickness 和 CornerRadius 转换器)
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));


            Bind(b, Border.VisibilityProperty, "IsHidden", data, new InverseBooleanToVisibilityConverter());

       


            // 交互反馈  让所有控件在被点击时都有视觉缩放或透明度变化
            b.Cursor = Cursors.Hand;

            b.MouseDown += (s, e) =>
            {
                b.RenderTransform = new ScaleTransform(0.98, 0.98); // 轻微缩小
                b.RenderTransformOrigin = new Point(0.5, 0.5);
                b.Opacity = 0.8;
            };

            b.MouseUp += (s, e) =>
            {
                b.RenderTransform = new ScaleTransform(1.0, 1.0); // 恢复
                b.Opacity = 1.0;


            };


        }


    }
}