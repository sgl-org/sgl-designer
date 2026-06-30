using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SglDesigner
{
    // 子类：按钮独有属性
    public partial class SglButtonData : SglWidgetData
    {

        [ObservableProperty] private Color _bgColor;
        [ObservableProperty] private Color _borderColor;
        [ObservableProperty] private int _borderWidth;
        [ObservableProperty] private int _radius;
        [ObservableProperty] private int _opacity = 255;

        // 对应 sgl_button_t 中的 text
        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private string _fontSize = "14";

        // 对应 sgl_button_t 中的 text
        [ObservableProperty] private string _text = "Button";
        [ObservableProperty] private int _textAlign = 0; // SGL_ALIGN_CENTER

        // 对应 sgl_button_t 中的 text_color
        [ObservableProperty] private Color _textColor = Colors.White;


        public SglButtonData()
        {

            X = 50; Y = 50;
            W = 50;
            H = 24;
            BgColor = Color.FromRgb(0x02, 0x88, 0xD1);
            BorderWidth = 1;
            BorderColor = Color.FromRgb(0x02, 0x88, 0xD1);
            Radius = 4;
        }

    }

    public partial class BaseBinder
    {



        /// <summary>
        /// 为 Button 绑定特有属性（文本、字体大小、颜色）
        /// </summary>
        public static void BindButton(Border b, SglButtonData data)
        {
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            // 绑定外框的边框属性 (BorderColor & BorderWidth)
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            // 注意：动态属性里叫 "BorderSize"，但 Data 里叫 "BorderWidth"，这里要统一
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));

            // 创建内部 TextBlock
            TextBlock tb = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsHitTestVisible = false // 防止干扰选中
            };

            // 绑定文本内容、颜色和大小
            Bind(tb, TextBlock.ForegroundProperty, "TextColor", data, GetConv("ColorToBrushConverter"));
            //Bind(tb, TextBlock.FontSizeProperty, "FontSize", data);
            // 如果是内置字体 Consolas24，预览时自动设为 24
            Binding sizeBinding = new Binding("FontName")
            {
                Source = data,
                Converter = new FontNameToSizeConverter() // 新建一个简单的数字提取转换器
            };
            tb.SetBinding(TextBlock.FontSizeProperty, sizeBinding);

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

            // 2. 绑定按钮自身的边框圆角预览 (对应 sgl_button_set_radius)
            Bind(b, Border.CornerRadiusProperty, "Radius", data, new IntToCornerRadiusConverter());

            // 将 TextBlock 塞入 Border
            b.Child = tb;
        }

    }
}