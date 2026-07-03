using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SglDesigner
{
    public partial class SglLabelData : SglWidgetData
    {

        [ObservableProperty] private Color _bgColor = Colors.Transparent;
        [ObservableProperty] private Color _borderColor;
        [ObservableProperty] private int _borderWidth = 0;
        [ObservableProperty] private int _radius = 4;
        [ObservableProperty] private double _opacity = 255;

        [ObservableProperty] private string _text = "Label";
        [ObservableProperty] private Color _textColor = Colors.White;
        [ObservableProperty] private int _textAlign = 0; // SGL_ALIGN_CENTER
                                                         // 对应 sgl_button_t 中的 text
        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private string _fontSize = "14";
        // Label 特有属性
        [ObservableProperty] private int _textRotation = 0;
        [ObservableProperty] private sbyte _offsetX = 0;
        [ObservableProperty] private sbyte _offsetY = 0;

        // Label 的背景色（注意：SGL Label 有专门的 set_bg_color）
        public SglLabelData()
        {
            X = 50; Y = 50;
            W = 40;  // 默认宽度
            H = 20;  // 默认高度
        }
    }
    public partial class BaseBinder
    {



        /// <summary>
        /// 为 Label 绑定特有属性（旋转、偏移、文字颜色）
        /// </summary>
        public static void BindLabel(Border b, SglLabelData data)
        {
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            // 绑定外框的边框属性 (BorderColor & BorderWidth)
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            // 注意：动态属性里叫 "BorderSize"，但 Data 里叫 "BorderWidth"，这里要统一
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            
            b.VerticalAlignment = VerticalAlignment.Center;
            b.Margin = new Thickness(0, -4, 0, 0);
            // 创建内部 TextBlock
            TextBlock tb = new TextBlock
            {
                
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                
                IsHitTestVisible = false // 防止干扰选中
            };

            //  实现 TextAlign 功能
            var alignConv = new AlignToAlignmentConverter();

            // 绑定水平对齐
            Binding horizAlignBind = new Binding("TextAlign")
            {
                Source = data,
                Converter = alignConv,
                ConverterParameter = "H"
            };
            tb.SetBinding(TextBlock.HorizontalAlignmentProperty, horizAlignBind);

            // 绑定垂直对齐
            Binding vertAlignBind = new Binding("TextAlign")
            {
                Source = data,
                Converter = alignConv,
                ConverterParameter = "V"
            };
            tb.SetBinding(TextBlock.VerticalAlignmentProperty, vertAlignBind);

            // 针对文本内容的对齐（如果 TextBlock 宽度被撑满时起作用）
            Binding textInAlignBind = new Binding("TextAlign")
            {
                Source = data,
                Converter = new AlignToAlignmentConverter() // 下面补充这个转换器
            };
            tb.SetBinding(TextBlock.TextAlignmentProperty, textInAlignBind);

            Bind(tb, TextBlock.ForegroundProperty, "TextColor", data, GetConv("ColorToBrushConverter"));
            // 绑定字体大小：对应 sgl 中的预览尺寸
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


            // 2. 旋转预览
            RotateTransform rt = new RotateTransform();
            Bind(rt, RotateTransform.AngleProperty, "TextRotation", data);
            tb.RenderTransform = rt;
            tb.RenderTransformOrigin = new Point(0.5, 0.5);

            //  偏移预览 (利用 Margin)
            MultiBinding marginBind = new MultiBinding { Converter = new OffsetToMarginConverter() };
            marginBind.Bindings.Add(new Binding("OffsetX") { Source = data });
            marginBind.Bindings.Add(new Binding("OffsetY") { Source = data });
            tb.SetBinding(FrameworkElement.MarginProperty, marginBind);

            // 将 TextBlock 塞入 Border
            b.Child = tb;
        }




    }
}