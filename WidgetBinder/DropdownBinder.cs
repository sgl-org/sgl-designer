using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{
    public partial class SglDropdownData : SglWidgetData
    {
        [ObservableProperty] private int _radius = 5;
        [ObservableProperty] private int _opticaty = 255;
        [ObservableProperty] private Color _bgColor = Color.FromRgb(0x28, 0x28, 0x28);
        [ObservableProperty] private Color _textColor = Color.FromRgb(0xFF, 0xFF, 0xFF);
        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private Color _borderColor = Color.FromRgb(0x40, 0x40, 0x40);
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _selectedIndex = 0;

        // 下拉选项列表
        public ObservableCollection<string> Options { get; set; } = new ObservableCollection<string>();

        public SglDropdownData()
        {
            W = 120; H = 30; // 默认高度通常是一行选项的高度
            Type = SglMapping.SglType.Dropdown;

            Options.CollectionChanged += Options_CollectionChanged;
        }

        private void Options_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {

            OnPropertyChanged(nameof(Options));
        }
        public static SglDropdownData CreateDefaultOptions()
        {
            var data = new SglDropdownData();
            data.Type = SglMapping.SglType.Dropdown;
            data.Options.Add("Option 1");
            data.Options.Add("Option 2");
            data.Options.Add("Option 3");
            return data;
        }
    }
    public partial class BaseBinder
    {

        public static void BindDropdown(Border b, SglDropdownData data)
        {
            // 基础容器绑定
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, UIElement.OpacityProperty, "Alpha", data, GetConv("IntToDoubleConverter"));

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 文字区
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // 箭头区

            // 2. 显示当前选中的文字
            TextBlock tb = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };

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


            // 绑定选中的文字（简单模拟：显示第一项或根据 Index 显示）
            tb.Text = data.Options.Count > 0 ? data.Options[0] : "Select...";
            Bind(tb, TextBlock.ForegroundProperty, "TextColor", data, GetConv("ColorToBrushConverter"));
            grid.Children.Add(tb);

            //  模拟下拉图标 (dropdown_icon)
            // SGL 源码中图标是 18x10 的 V 字型
            Path arrow = new Path
            {
                Data = Geometry.Parse("M 0,0 L 5,5 L 10,0"), // 简单的 V 型路径
                StrokeThickness = 2,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Bind(arrow, Shape.StrokeProperty, "TextColor", data, GetConv("ColorToBrushConverter"));
            Grid.SetColumn(arrow, 1);
            grid.Children.Add(arrow);

            b.Child = grid;
        }



    }
}