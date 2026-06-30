using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{

    public enum LineDirection
    {
        [Description("水平方向")] Horizontal,
        [Description("垂直方向")] Vertical,
        [Description("左斜线 (\\)")] Diagonal,
        [Description("右斜线 (/)")] ReverseDiagonal
    }
    // 必须有 get

    public partial class SglLineData : SglWidgetData
    {
        [ObservableProperty] private int _opacity = 255;

        [ObservableProperty] private Color _color = Colors.White;
        [ObservableProperty] private int _lineWidth = 2;
        [ObservableProperty] private LineDirection _direction = LineDirection.Horizontal;

        public SglLineData()
        {
            X = 100; Y = 100;
            W = 100;
            H = 10;
            Type = SglMapping.SglType.Line;
        }
    }

    public partial class BaseBinder
    {
        public static void BindLine(Border b, SglLineData data)
        {
            b.Background = Brushes.Transparent;
            Line line = new Line { IsHitTestVisible = false };

            // 绑定颜色和线宽
            Bind(line, Shape.StrokeProperty, "Color", data, GetConv("ColorToBrushConverter"));
            Bind(line, Shape.StrokeThicknessProperty, "LineWidth", data, GetConv("IntToDoubleConverter"));

            // 使用 MultiBinding 动态计算 4 个坐标点
            // 这样无论拉伸成什么样，都能根据 Direction 模式对齐
            line.SetBinding(Line.X1Property, CreateLineBinding("X1", data));
            line.SetBinding(Line.Y1Property, CreateLineBinding("Y1", data));
            line.SetBinding(Line.X2Property, CreateLineBinding("X2", data));
            line.SetBinding(Line.Y2Property, CreateLineBinding("Y2", data));

            b.Child = line;
        }
        private static MultiBinding CreateLineBinding(string point, SglLineData data)
        {
            var mb = new MultiBinding
            {
                Converter = new SglLineCoordinateConverter(),
                ConverterParameter = point
            };
            mb.Bindings.Add(new Binding("W") { Source = data });
            mb.Bindings.Add(new Binding("H") { Source = data });
            mb.Bindings.Add(new Binding("Direction") { Source = data });
            return mb;
        }
        public class SglLineCoordinateConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length < 3 || values[0] == DependencyProperty.UnsetValue) return 0.0;

                double w = System.Convert.ToDouble(values[0]);
                double h = System.Convert.ToDouble(values[1]);
                LineDirection dir = (LineDirection)values[2];
                string point = parameter.ToString();

                return dir switch
                {
                    // 水平：Y 居中，X 从 0 到 W
                    LineDirection.Horizontal => point switch
                    {
                        "X1" => 0.0,
                        "X2" => w,
                        "Y1" => h / 2.0,
                        "Y2" => h / 2.0,
                        _ => 0.0
                    },
                    // 垂直：X 居中，Y 从 0 到 H
                    LineDirection.Vertical => point switch
                    {
                        "X1" => w / 2.0,
                        "X2" => w / 2.0,
                        "Y1" => 0.0,
                        "Y2" => h,
                        _ => 0.0
                    },
                    // 斜线 \
                    LineDirection.Diagonal => point switch
                    {
                        "X1" => 0.0,
                        "X2" => w,
                        "Y1" => 0.0,
                        "Y2" => h,
                        _ => 0.0
                    },
                    // 反斜线 /
                    LineDirection.ReverseDiagonal => point switch
                    {
                        "X1" => 0.0,
                        "X2" => w,
                        "Y1" => h,
                        "Y2" => 0.0,
                        _ => 0.0
                    },
                    _ => 0.0
                };
            }
            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
        }


    }
}