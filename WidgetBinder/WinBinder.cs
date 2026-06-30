using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{
    public partial class SglWinData : SglWidgetData
    {
        // --- 基础外观 (继承自 BasePropertyPanel: BgColor, BorderColor, BorderWidth, Radius, Opacity) ---
        [ObservableProperty] private Color _bgColor = Color.FromRgb(0x28, 0x28, 0x28);
        [ObservableProperty] private Color _borderColor = Color.FromRgb(0x55, 0x55, 0x55);
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _radius = 8;
        [ObservableProperty] private int _opacity = 255;

        // --- 标题 ---
        [ObservableProperty] private string _titleText = "Window";
        [ObservableProperty] private Color _titleTextColor = Colors.White;
        [ObservableProperty] private Color _titleBgColor = Color.FromRgb(0x3D, 0x5A, 0x80);
        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private int _titleHeight = 24;
        [ObservableProperty] private int _titleAlign = 7;  // SGL_ALIGN_LEFT_MID

        // --- 关闭按钮 ---
        [ObservableProperty] private Color _closeColor = Color.FromRgb(255, 90, 80);

        public SglWinData()
        {
            W = 120; H = 90;
            Type = SglMapping.SglType.Win;
        }
    }

    public partial class BaseBinder
    {
        public static void BindWin(Border b, SglWinData data)
        {
            // --- 主容器 ---
            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 标题栏
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 内容区

            // --- 绑定基础外观 ---
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            Bind(b, UIElement.OpacityProperty, "Opacity", data, new OpacityConverter());
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));

            // --- 标题栏 ---
            Border titleBar = new Border
            {
                Height = 24,
                DataContext = null,
                Padding = new Thickness(8, 0, 0, 0)
            };
            // 标题栏上圆角跟随外框 Radius
            Bind(titleBar, Border.CornerRadiusProperty, "Radius", data, GetConv("TopCornerRadiusConverter"));
            Bind(titleBar, Border.BackgroundProperty, "TitleBgColor", data, GetConv("ColorToBrushConverter"));

            Grid titleGrid = new Grid();
            // 两列：标题文字 + 关闭按钮
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // 标题文字
            TextBlock titleText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                FontWeight = FontWeights.Normal
            };
            Bind(titleText, TextBlock.TextProperty, "TitleText", data);
            Bind(titleText, TextBlock.ForegroundProperty, "TitleTextColor", data, GetConv("ColorToBrushConverter"));
            // FontFamily + FontSize 绑定 (与 LabelBinder 一致)
            var fontFamBind = new Binding("FontName")
            {
                Source = data,
                Converter = new FontNameToFamilyConverter(),
                Mode = BindingMode.OneWay
            };
            titleText.SetBinding(TextBlock.FontFamilyProperty, fontFamBind);
            var fontSizeBind = new Binding("FontName")
            {
                Source = data,
                Converter = new FontNameToSizeConverter()
            };
            titleText.SetBinding(TextBlock.FontSizeProperty, fontSizeBind);

            Grid.SetColumn(titleText, 0);

            // 关闭按钮 (红色圆点)
            Ellipse closeBtn = new Ellipse
            {
                Width = 10,
                Height = 10,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Bind(closeBtn, Shape.FillProperty, "CloseColor", data, GetConv("ColorToBrushConverter"));
            Grid.SetColumn(closeBtn, 1);

            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(closeBtn);
            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);
            mainGrid.Children.Add(titleBar);

            // --- 内容区 (子控件容器) ---
            Canvas contentCanvas = new Canvas
            {
                ClipToBounds = false,
                Background = Brushes.Transparent
            };
            Grid.SetRow(contentCanvas, 1);
            mainGrid.Children.Add(contentCanvas);

            b.ClipToBounds = true; // 裁剪超出窗口的子控件
            b.Child = mainGrid;
            b.Tag = contentCanvas; // 标记内容 Canvas，供拖放子控件使用
        }
    }
}
