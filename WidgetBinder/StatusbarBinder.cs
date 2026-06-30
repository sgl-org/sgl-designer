using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SglDesigner
{
    public partial class StatusbarSlotData : ObservableObject
    {
        [ObservableProperty] private string _text = "";
        [ObservableProperty] private Color _color = Colors.White;
        [ObservableProperty] private int _alpha = 255;
    }

    public partial class SglStatusbarData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Color.FromRgb(20, 20, 20);
        [ObservableProperty] private int _bgAlpha = 128;
        [ObservableProperty] private int _radius = 4;
        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private int _slotSpace = 4;
        [ObservableProperty] private int _leftMargin = 5;
        [ObservableProperty] private int _rightMargin = 5;

        public ObservableCollection<StatusbarSlotData> LeftSlots { get; } = new();
        public ObservableCollection<StatusbarSlotData> RightSlots { get; } = new();

        public SglStatusbarData()
        {
            W = 320; H = 24;
            Type = SglMapping.SglType.Statusbar;

            // 默认槽位示例
            LeftSlots.Add(new StatusbarSlotData { Text = "⌂" });     // ⌂ home
            LeftSlots.Add(new StatusbarSlotData { Text = "⛭" });     // ⛭ settings

            RightSlots.Add(new StatusbarSlotData { Text = "⚡" });    // ⚡
            RightSlots.Add(new StatusbarSlotData { Text = "100%" });
        }
    }

    public partial class BaseBinder
    {
        public static void BindStatusbar(Border b, SglStatusbarData data)
        {
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, UIElement.OpacityProperty, "BgAlpha", data, new OpacityConverter());
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));

            Grid mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 左侧槽位
            StackPanel leftPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(data.LeftMargin, 0, 0, 0)
            };
            Grid.SetColumn(leftPanel, 0);

            // 右侧槽位
            StackPanel rightPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, data.RightMargin, 0)
            };
            Grid.SetColumn(rightPanel, 1);

            // ---- 属性变更 → 更新 UI ----
            data.PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(SglStatusbarData.LeftMargin):
                    case nameof(SglStatusbarData.RightMargin):
                        leftPanel.Margin = new Thickness(data.LeftMargin, 0, 0, 0);
                        rightPanel.Margin = new Thickness(0, 0, data.RightMargin, 0);
                        break;
                    case nameof(SglStatusbarData.SlotSpace):
                    case nameof(SglStatusbarData.FontName):
                        SyncSlots(leftPanel, data.LeftSlots, data.SlotSpace, data);
                        SyncSlots(rightPanel, data.RightSlots, data.SlotSpace, data);
                        break;
                }
            };

            // ---- 同步左侧槽位 ----
            data.LeftSlots.CollectionChanged += (s, e) =>
                SyncSlots(leftPanel, data.LeftSlots, data.SlotSpace, data);
            SyncSlots(leftPanel, data.LeftSlots, data.SlotSpace, data);

            // ---- 同步右侧槽位 ----
            data.RightSlots.CollectionChanged += (s, e) =>
                SyncSlots(rightPanel, data.RightSlots, data.SlotSpace, data);
            SyncSlots(rightPanel, data.RightSlots, data.SlotSpace, data);

            mainGrid.Children.Add(leftPanel);
            mainGrid.Children.Add(rightPanel);
            b.Child = mainGrid;
        }

        private static void SyncSlots(StackPanel panel, ObservableCollection<StatusbarSlotData> slots,
                                       int spacing, SglStatusbarData data)
        {
            panel.Children.Clear();
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                var tb = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(i > 0 ? spacing : 0, 0, 0, 0)
                };
                Bind(tb, TextBlock.TextProperty, "Text", slot);
                Bind(tb, TextBlock.ForegroundProperty, "Color", slot, GetConv("ColorToBrushConverter"));
                Bind(tb, UIElement.OpacityProperty, "Alpha", slot, new OpacityConverter());
                // 字体绑定
                var fontFamBind = new Binding("FontName")
                {
                    Source = data,
                    Converter = new FontNameToFamilyConverter(),
                    Mode = BindingMode.OneWay
                };
                tb.SetBinding(TextBlock.FontFamilyProperty, fontFamBind);
                var fontSizeBind = new Binding("FontName")
                {
                    Source = data,
                    Converter = new FontNameToSizeConverter()
                };
                tb.SetBinding(TextBlock.FontSizeProperty, fontSizeBind);
                panel.Children.Add(tb);
            }
        }
    }
}
