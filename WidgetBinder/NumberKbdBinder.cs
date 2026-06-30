using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SglDesigner
{

    public partial class SglNumberKbdData : SglWidgetData
    {
        // --- 主体属性 (body_desc) ---
        [ObservableProperty] private Color _bgColor = Color.FromRgb(40, 40, 40);
        [ObservableProperty] private int _opacity = 255;
        [ObservableProperty] private int _radius = 4;
        [ObservableProperty] private Color _borderColor = Color.FromRgb(60, 60, 60);
        [ObservableProperty] private int _borderWidth = 1;

        // --- 按键属性 (btn_desc) ---
        [ObservableProperty] private Color _btnColor = Color.FromRgb(60, 60, 60);
        [ObservableProperty] private int _btnAlpha = 255;
        [ObservableProperty] private int _btnRadius = 2;
        [ObservableProperty] private Color _btnBorderColor = Color.FromRgb(80, 80, 80);
        [ObservableProperty] private int _btnBorderWidth = 1;
        [ObservableProperty] private int _btnMargin = 4;

        // --- 文本与布局 ---
        [ObservableProperty] private Color _textColor = Color.FromRgb(255, 255, 255);
        [ObservableProperty] private string _fontName = "consolas14";

        [ObservableProperty] private string _targetTextArea = string.Empty;

        public SglNumberKbdData()
        {
            Type = SglMapping.SglType.NumberKbd;
            // 默认数字键盘尺寸，通常比全键盘窄
            W = 140;
            H = 160;
        }
    }
    public partial class BaseBinder
    {
        public static void BindNumberKbd(Border b, SglNumberKbdData data)
        {
            // 基础绑定
            Bind(b, Border.BackgroundProperty, nameof(data.BgColor), data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.CornerRadiusProperty, nameof(data.Radius), data, GetConv("IntToCornerRadiusConverter"));

            RefreshNumberKbdInternal(b, data);

            data.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(data.BtnMargin) || e.PropertyName.Contains("Btn") || e.PropertyName == nameof(data.TextColor))
                {
                    RefreshNumberKbdInternal(b, data);
                }
            };
        }

        private static void RefreshNumberKbdInternal(Border parent, SglNumberKbdData data)
        {
            Grid mainGrid = new Grid { Margin = new Thickness(data.BtnMargin - 2) };
            for (int i = 0; i < 5; i++) mainGrid.RowDefinitions.Add(new RowDefinition());
            for (int i = 0; i < 4; i++) mainGrid.ColumnDefinitions.Add(new ColumnDefinition());

            string[,] labels = {
                { "+", "-", "*", "/" },
                { "7", "8", "9", "=" },
                { "4", "5", "6", "⌫" },
                { "1", "2", "3", "OK" },
                { ".", "0", "%", "OK" }
            };

            // 记录哪些单元格已经被占用（用于处理跨行按钮）
            bool[,] occupied = new bool[5, 4];

            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 4; c++)
                {
                    if (occupied[r, c]) continue;

                    Border btn = new Border
                    {
                        DataContext = null,
                        Margin = new Thickness(data.BtnMargin - 2),
                        Background = new SolidColorBrush(data.BtnColor),
                        CornerRadius = new CornerRadius(data.BtnRadius),
                        BorderBrush = new SolidColorBrush(data.BtnBorderColor),
                        BorderThickness = new Thickness(data.BtnBorderWidth)
                    };

                    TextBlock txt = new TextBlock
                    {
                        Text = labels[r, c],
                        Foreground = new SolidColorBrush(data.TextColor),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    // 特殊逻辑：处理两行高的 OK 键 (对应源码 btn_row == 3 && btn_col == 3)
                    if (r == 3 && c == 3)
                    {
                        Grid.SetRowSpan(btn, 2);
                        occupied[4, 3] = true;
                    }

                    btn.Child = txt;
                    mainGrid.Children.Add(btn);
                    Grid.SetRow(btn, r);
                    Grid.SetColumn(btn, c);
                }
            }
            parent.Child = mainGrid;
        }
    }
}