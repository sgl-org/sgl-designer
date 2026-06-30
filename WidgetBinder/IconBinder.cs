using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SglDesigner
{

    // 子类：图标独有属性
    public partial class SglIconData : SglWidgetData
    {

        [ObservableProperty] private Color _bgColor = Colors.Transparent;
        [ObservableProperty] private Color _borderColor = Colors.Transparent;
        [ObservableProperty] private int _borderWidth = 0;
        [ObservableProperty] private int _radius = 0;


        [ObservableProperty] private string _iconName = "";

        // 对应 sgl_icon_set_color
        [ObservableProperty] private Color _iconColor = Colors.DarkGray;

        // 对应 sgl_icon_set_alpha (0-255)
        [ObservableProperty] private int _opacity = 255;

        // 对应 sgl_icon_set_align
        [ObservableProperty] private int _iconAlign = 0; // 0: Center, 1: Left...

        public SglIconData()
        {
            Type = SglMapping.SglType.Icon; 
            W = 36;
            H = 36;
        }
    }
    public partial class BaseBinder
    {
        public static void BindTextBox(Border b, SglTextBoxData data)
        {
            // 绑定外层 Border 基础外观
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));

            // 2. 内部文本容器 (带内边距)
            TextBlock tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(5),
                IsHitTestVisible = false
            };

            //  绑定文本属性
            //Bind(tb, TextBlock.TextProperty, "Text", data);
            Bind(tb, TextBlock.ForegroundProperty, "TextColor", data, GetConv("ColorToBrushConverter"));

            // 使用多重绑定实现字符过滤预览
            MultiBinding textFilterBind = new MultiBinding { Converter = new FontPreviewTextConverter() };
            textFilterBind.Bindings.Add(new Binding("Text") { Source = data });
            textFilterBind.Bindings.Add(new Binding("FontName") { Source = data });
            tb.SetBinding(TextBlock.TextProperty, textFilterBind);

            // 如果是内置字体 Consolas24，预览时自动设为 24
            Binding sizeBinding = new Binding("FontName")
            {
                Source = data,
                Converter = new FontNameToSizeConverter() // 新建一个简单的数字提取转换器
            };
            tb.SetBinding(TextBlock.FontSizeProperty, sizeBinding);


            // 确保 "FontName" 字符串与 SglLabelData 中的属性名完全一致
            Binding fontFamBind = new Binding("FontName")
            {
                Source = data,
                Converter = new FontNameToFamilyConverter(), // 确保的转换器类名是这个
                Mode = BindingMode.OneWay
            };
            tb.SetBinding(TextBlock.FontFamilyProperty, fontFamBind);

            // 绑定行间距 (WPF 的 LineHeight 是基准，需要转换)
            tb.SetBinding(TextBlock.LineHeightProperty, new Binding("LineMargin")
            {
                Source = data,
                Converter = new SglNumericConverter(),
                ConverterParameter = "+8" // 假设基础字高16，加上 margin
            });

            b.Child = tb;
        }
    }
}