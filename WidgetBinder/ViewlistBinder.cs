using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SglDesigner
{
    public partial class SglViewlistData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Color.FromRgb(0x28, 0x28, 0x28);
        [ObservableProperty] private Color _borderColor = Color.FromRgb(0x55, 0x55, 0x55);
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _radius = 4;
        [ObservableProperty] private int _opacity = 255;

        [ObservableProperty] private int _itemHeight = 24;
        [ObservableProperty] private int _marginX = 2;
        [ObservableProperty] private int _marginY = 2;

        public SglViewlistData()
        {
            W = 120; H = 90;
            Type = SglMapping.SglType.Viewlist;

            // 子控件增删 → 重新流式布局
            Children.CollectionChanged += (s, e) => ApplyFlowLayout();

            // 属性变更 → 重新流式布局
            PropertyChanged += (s, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(ItemHeight):
                    case nameof(MarginX):
                    case nameof(MarginY):
                    case nameof(BorderWidth):
                    case nameof(W):
                        ApplyFlowLayout();
                        break;
                }
            };
        }

        /// <summary>
        /// 流式布局：强制接管所有子控件的 X/Y/W/H
        /// 宽度=填满减边距, 高度=ItemHeight, Y=按索引累加
        /// </summary>
        public void ApplyFlowLayout()
        {
            int border = BorderWidth;
            int mx = MarginX;
            int my = MarginY;
            int itemH = ItemHeight;
            int contentW = W - border * 2 - mx * 2;

            int index = 0;
            foreach (var child in Children)
            {
                child.X = border + mx;
                child.Y = border + my + index * (itemH + my);
                child.W = contentW > 0 ? contentW : 10;
                child.H = itemH;
                index++;
            }
        }
    }

    public partial class BaseBinder
    {
        public static void BindViewlist(Border b, SglViewlistData data)
        {
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            Bind(b, UIElement.OpacityProperty, "Opacity", data, new OpacityConverter());

            b.ClipToBounds = true;

            // 内容区 Canvas（子控件容器）
            Canvas contentCanvas = new Canvas
            {
                ClipToBounds = true,
                Background = Brushes.Transparent
            };
            b.Child = contentCanvas;
            b.Tag = contentCanvas;
        }
    }
}
