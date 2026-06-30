using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SglDesigner
{

    public partial class SglKeyboardData : SglWidgetData
    {
        // --- 主体属性 (body_desc) ---
        [ObservableProperty] private Color _bgColor = Color.FromRgb(30, 30, 30);
        [ObservableProperty] private int _opacity = 255;
        [ObservableProperty] private int _radius = 4;
        [ObservableProperty] private Color _borderColor = Color.FromRgb(50, 50, 50);
        [ObservableProperty] private int _borderWidth = 1;

        // --- 按键属性 (btn_desc) ---
        [ObservableProperty] private Color _btnColor = Color.FromRgb(60, 60, 60);
        [ObservableProperty] private int _btnAlpha = 255;
        [ObservableProperty] private int _btnRadius = 4;
        [ObservableProperty] private Color _btnBorderColor = Color.FromRgb(80, 80, 80);
        [ObservableProperty] private int _btnBorderWidth = 0;

        // --- 文本与布局 ---
        [ObservableProperty] private Color _textColor = Color.FromRgb(255, 255, 255);
        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private int _keyMargin = 2;
        [ObservableProperty] private string _targetTextArea = string.Empty;
        public SglKeyboardData()
        {
            Type = SglMapping.SglType.Keyboard;
            W = 320; // 键盘通常较宽
            H = 120;
        }
    }
    public partial class BaseBinder
    {
        private static void RefreshKeyboardInternal(Border parent, SglKeyboardData data)
        {
            Grid mainGrid = new Grid { Margin = new Thickness(data.KeyMargin) };
            for (int i = 0; i < 4; i++)
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 适配左图样式的比例 (根据像素占位估算)
            double[][] allWeights = new double[][] {
        new double[] { 5, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 7 },    // Row 0: 1# 到 退格
        new double[] { 8, 4, 4, 4, 4, 4, 4, 4, 4, 4, 9 },       // Row 1: ABC 到 回车
        new double[] { 5, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 5 },    // Row 2: 下划线 到 分号
        new double[] { 8, 8, 24, 8, 8 }                         // Row 3: 底部5键 [隐藏][左][空格][右][确认]
    };

            string[][] allLabels = new string[][] {
        new string[] { "1#", "q", "w", "e", "r", "t", "y", "u", "i", "o", "p", "✕" },
        new string[] { "ABC", "a", "s", "d", "f", "g", "h", "j", "k", "l", "↵" },
        new string[] { "_", "-", "z", "x", "c", "v", "b", "n", "m", ".", ",", ":" },
        new string[] { "⌨", "＜", " ", "＞", "✓" }
    };

            for (int r = 0; r < 4; r++)
            {
                Grid rowGrid = new Grid();
                double[] weights = allWeights[r];
                string[] labels = allLabels[r];

                for (int c = 0; c < weights.Length; c++)
                {
                    rowGrid.ColumnDefinitions.Add(new ColumnDefinition
                    {
                        Width = new GridLength(weights[c], GridUnitType.Star)
                    });

                    Border btn = new Border
                    {
                        DataContext = null,
                        Margin = new Thickness(data.KeyMargin),
                        Background = new SolidColorBrush(data.BtnColor),
                        CornerRadius = new CornerRadius(data.BtnRadius),
                        BorderBrush = new SolidColorBrush(data.BtnBorderColor),
                        BorderThickness = new Thickness(data.BtnBorderWidth)
                    };

                    TextBlock txt = new TextBlock
                    {
                        DataContext = null,
                        Text = labels[c],
                        Foreground = new SolidColorBrush(data.TextColor),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        FontSize = 12,
                        FontWeight = FontWeights.Medium
                    };

                    btn.Child = txt;
                    rowGrid.Children.Add(btn);
                    Grid.SetColumn(btn, c);
                }

                mainGrid.Children.Add(rowGrid);
                Grid.SetRow(rowGrid, r);
            }

            parent.Child = mainGrid;
        }
        public static void BindKeyboard(Border b, SglKeyboardData data)
        {
            // 绑定主体位置和尺寸
            Bind(b, FrameworkElement.WidthProperty, "W", data);
            Bind(b, FrameworkElement.HeightProperty, "H", data);

            // 2. 绑定主体视觉样式 (使用已有的 Converter)
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            Bind(b, UIElement.OpacityProperty, "Opacity", data, new OpacityConverter());

            //  初始化并监听内部布局变化
            RefreshKeyboardInternal(b, data);

            // 当间距、字体颜色或按键样式改变时，必须刷新整个内部 Grid
            data.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(data.KeyMargin) ||
                    e.PropertyName.Contains("Btn") ||
                    e.PropertyName == nameof(data.TextColor))
                {
                    RefreshKeyboardInternal(b, data);
                }
            };
        }
    }
}