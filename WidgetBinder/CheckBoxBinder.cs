using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{

    public partial class SglCheckboxData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor;
        [ObservableProperty] private Color _borderColor;
        [ObservableProperty] private int _borderWidth;
        [ObservableProperty] private int _radius;
        [ObservableProperty] private double _opacity = 1.0;

        [ObservableProperty] private bool _status = false; // 勾选状态
        [ObservableProperty] private string _text = "Checkbox"; // 文本内容
        [ObservableProperty] private Color _textColor = Colors.White; // 文本与勾选框颜色
        [ObservableProperty] private int _alpha = 255;

        // 字体相关（假设已有 Font 处理逻辑）
        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private string _fontSize = "12";
        public SglCheckboxData()
        {
            X = 50; Y = 50;
            W = 100;
            H = 32;
        }
    }
    public partial class BaseBinder
    {


        public static void BindCheckbox(Border b, SglCheckboxData data)
        {
            StackPanel sp = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                IsHitTestVisible = false
            };

            // 左侧勾选方框
            Border box = new Border
            {
                Width = 18,
                Height = 18,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                VerticalAlignment = VerticalAlignment.Center
            };
            // 绑定方框边框颜色
            Bind(box, Border.BorderBrushProperty, "TextColor", data, GetConv("ColorToBrushConverter"));

            // 2. 勾选标志 (使用 Path 绘制一个简单的对勾)
            Path checkMark = new Path
            {
                Data = Geometry.Parse("M 3,8 L 7,12 L 15,4"),
                StrokeThickness = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(2)
            };
            Bind(checkMark, Shape.StrokeProperty, "TextColor", data, GetConv("ColorToBrushConverter"));
            // 只有 Status 为 true 时显示对勾
            checkMark.SetBinding(UIElement.VisibilityProperty, new Binding("Status")
            {
                Source = data,
                Converter = new BooleanToVisibilityConverter()
            });

            box.Child = checkMark;

            //  右侧文本
            TextBlock tb = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };

            //Bind(tb, TextBlock.TextProperty, "Text", data);
            Bind(tb, TextBlock.ForegroundProperty, "TextColor", data, GetConv("ColorToBrushConverter"));
            Bind(tb, UIElement.OpacityProperty, "Opacity", data, new OpacityConverter());

            // 使用多重绑定实现字符过滤预览
            MultiBinding textFilterBind = new MultiBinding { Converter = new FontPreviewTextConverter() };
            textFilterBind.Bindings.Add(new Binding("Text") { Source = data });
            textFilterBind.Bindings.Add(new Binding("FontName") { Source = data });
            tb.SetBinding(TextBlock.TextProperty, textFilterBind);

            // 确保 "FontName" 字符串与 SglLabelData 中的属性名完全一致
            Binding fontFamBind = new Binding("FontName")
            {
                Source = data,
                Converter = new FontNameToFamilyConverter(), // 确保的转换器类名是这个
                Mode = BindingMode.OneWay
            };
            tb.SetBinding(TextBlock.FontFamilyProperty, fontFamBind);
            // 如果是内置字体 Consolas24，预览时自动设为 24
            Binding sizeBinding = new Binding("FontName")
            {
                Source = data,
                Converter = new FontNameToSizeConverter() // 新建一个简单的数字提取转换器
            };
            tb.SetBinding(TextBlock.FontSizeProperty, sizeBinding);

            sp.Children.Add(box);
            sp.Children.Add(tb);
            b.Child = sp;

            // 点击整个区域切换状态
            b.MouseDown += (s, e) =>
            {
                data.Status = !data.Status;
            };
        }



    }
}