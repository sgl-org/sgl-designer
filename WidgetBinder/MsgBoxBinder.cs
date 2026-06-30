using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{
    public partial class SglMsgBoxData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Color.FromRgb(0x28, 0x28, 0x28);
        [ObservableProperty] private Color _borderColor = Colors.Black;
        [ObservableProperty] private int _borderWidth = 1;

        [ObservableProperty] private int _radius = 8;
        [ObservableProperty] private int _opacity = 255;
        // 文本内容
        [ObservableProperty] private string _titleText = "Notice";
        [ObservableProperty] private string _msgText = "Are you sure?\nHello Word";
        [ObservableProperty] private string _leftBtnText = "YES";
        [ObservableProperty] private string _rightBtnText = "NO";

        // 颜色设置
        [ObservableProperty] private Color _titleColor = Colors.White;
        [ObservableProperty] private Color _msgColor = Colors.LightGray;
        [ObservableProperty] private Color _lbtnTextColor = Colors.White;
        [ObservableProperty] private Color _rbtnTextColor = Colors.White;
        [ObservableProperty] private Color _lbtnColor = Color.FromRgb(0x1E, 0x90, 0xFF);
        [ObservableProperty] private Color _rbtnColor = Color.FromRgb(0x20, 0xB2, 0xAA);


        // 布局与样式
        [ObservableProperty] private int _titleHeight = 20;
        [ObservableProperty] private int _msgLineMargin = 4;
        [ObservableProperty] private string _fontName = "consolas14";

        public SglMsgBoxData()
        {
            W = 240; H = 160;
            Type = SglMapping.SglType.MsgBox;
        }
    }
    public partial class BaseBinder
    {

        public static void BindMsgBox(Border b, SglMsgBoxData data)
        {
            // 创建主容器 Grid
            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 标题
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // 正文
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // 按钮

            // --- 背景与边框绑定 ---
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            Bind(b, UIElement.OpacityProperty, "Opacity", data, new OpacityConverter()); // 0-255转0-1.0
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));

            // --- 2. 标题栏部分 ---
            StackPanel titleStack = new StackPanel();

            // 标题文字
            TextBlock titleText = new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Normal,
                Margin = new Thickness(0)
            };
            Bind(titleText, TextBlock.TextProperty, "TitleText", data);
            Bind(titleText, TextBlock.ForegroundProperty, "TitleColor", data, GetConv("ColorToBrushConverter"));
            Bind(titleText, FrameworkElement.HeightProperty, "TitleHeight", data);

            // 水平分割线 (hline)
            Rectangle hline = new Rectangle { Height = 1, Margin = new Thickness(0) };
            Bind(hline, Shape.FillProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(hline, FrameworkElement.HeightProperty, "BorderWidth", data);

            titleStack.Children.Add(titleText);
            titleStack.Children.Add(hline);
            Grid.SetRow(titleStack, 0);
            mainGrid.Children.Add(titleStack);

            // --- 3. 正文部分 ---
            TextBlock msgText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0)
            };
            Bind(msgText, TextBlock.TextProperty, "MsgText", data);
            Bind(msgText, TextBlock.ForegroundProperty, "MsgColor", data, GetConv("ColorToBrushConverter"));
            // 绑定行间距 (SGL的margin对应WPF的LineHeight)
            Bind(msgText, TextBlock.LineHeightProperty, "MsgLineMargin", data, new MarginToLineHeightConverter());

            Grid.SetRow(msgText, 1);
            mainGrid.Children.Add(msgText);

            // --- 4. 按钮部分 (UniformGrid) ---
            UniformGrid btnGrid = new UniformGrid { Columns = 2, Margin = new Thickness(0) };

            // --- 左按钮 ---
            Border lBtn = new Border { Margin = new Thickness(0), Height = 24, Padding = new Thickness(5) };
            Bind(lBtn, Border.BackgroundProperty, "LbtnColor", data, GetConv("ColorToBrushConverter"));
            // 动态绑定圆角：跟随 Radius 属性，且只应用左下角
            // --- 左按钮圆角绑定 ---
            Binding lCornerBinding = new Binding("Radius")
            {
                Source = data,
                Converter = new MsgBoxButtonCornerConverter(),
                ConverterParameter = "Left"
            };
            lBtn.SetBinding(Border.CornerRadiusProperty, lCornerBinding);

            TextBlock lTxt = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Bind(lTxt, TextBlock.TextProperty, "LeftBtnText", data);
            Bind(lTxt, TextBlock.ForegroundProperty, "LbtnTextColor", data, GetConv("ColorToBrushConverter"));
            lBtn.Child = lTxt;

            // --- 右按钮 ---
            Border rBtn = new Border { Margin = new Thickness(0), Height = 24, Padding = new Thickness(5) };
            Bind(rBtn, Border.BackgroundProperty, "RbtnColor", data, GetConv("ColorToBrushConverter"));
            // 动态绑定圆角：跟随 Radius 属性，且只应用右下角
            Binding rCornerBinding = new Binding("Radius")
            {
                Source = data,
                Converter = new MsgBoxButtonCornerConverter(),
                ConverterParameter = "Right"
            };
            rBtn.SetBinding(Border.CornerRadiusProperty, rCornerBinding);

            TextBlock rTxt = new TextBlock { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Bind(rTxt, TextBlock.TextProperty, "RightBtnText", data);
            Bind(rTxt, TextBlock.ForegroundProperty, "RbtnTextColor", data, GetConv("ColorToBrushConverter"));
            rBtn.Child = rTxt;

            btnGrid.Children.Add(lBtn);
            btnGrid.Children.Add(rBtn);
            Grid.SetRow(btnGrid, 2);
            mainGrid.Children.Add(btnGrid);

            // 设置最终子元素
            b.Child = mainGrid;
        }


    }
}