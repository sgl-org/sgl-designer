using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{
    public partial class SglRollerData : SglWidgetData
    {
        [ObservableProperty] private string _options = "Option 1\nOption 2\nOption 3\nOption 4\nOption 5";
        [ObservableProperty] private int _visibleRows = 3;
        [ObservableProperty] private int _selectedIndex = 0;
        [ObservableProperty] private Color _textColor = Colors.White;
        [ObservableProperty] private Color _selectedColor = Color.FromRgb(0, 120, 215);
        [ObservableProperty] private Color _bgColor = Color.FromRgb(45, 45, 48);
        [ObservableProperty] private Color _borderColor = Colors.Gray;
        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private int _alpha = 255;
        [ObservableProperty] private int _radius = 4;
        [ObservableProperty] private int _borderWidth = 1;

        public SglRollerData()
        {
            Type = SglMapping.SglType.Roller;
            // Default size: SGL sgl_roller_create → width=100, height=visible_rows * (font_h + 6)
            int fontH = 14; // consolas14
            int itemH = fontH + 6;
            X = 50; Y = 50; W = 80; H = VisibleRows * itemH;
        }
    }

    public partial class BaseBinder
    {
        /// <summary>
        /// Bind a Roller widget preview matching SGL sgl_roller DRAW_MAIN:
        /// - Canvas fills the Border directly (no Viewbox), auto-scales with widget size
        /// - Selected band centered vertically in widget area
        /// - Items flow through the band from band_y + scroll_y
        /// - SelectedIndex controls which item appears in the band
        /// </summary>
        public static void BindRoller(Border b, SglRollerData data)
        {
            b.ClipToBounds = true;

            // Alpha: OneWay to avoid Opacity → Alpha back-conversion
            b.SetBinding(UIElement.OpacityProperty, new Binding("Alpha")
            {
                Source = data,
                Mode = BindingMode.OneWay,
                Converter = GetConv("IntToDoubleConverter")
            });

            // Canvas fills the Border directly, auto-resizes with widget
            Canvas container = new Canvas
            {
                DataContext = null,
                IsHitTestVisible = false,
                ClipToBounds = true
            };

            // --- Background fill (matches sgl_draw_rect with bg_color) ---
            Rectangle bg = new Rectangle { IsHitTestVisible = false };
            bg.SetBinding(Shape.FillProperty, new Binding("BgColor") { Source = data, Converter = GetConv("ColorToBrushConverter") });
            bg.SetBinding(Rectangle.RadiusXProperty, new Binding("Radius") { Source = data });
            bg.SetBinding(Rectangle.RadiusYProperty, new Binding("Radius") { Source = data });
            container.Children.Add(bg);

            // --- Selected band: centered vertically in widget ---
            // SGL: band_y1 = draw_y1 + (widget_h - item_h) / 2
            Rectangle band = new Rectangle { IsHitTestVisible = false };
            band.SetBinding(Shape.FillProperty, new Binding("SelectedColor") { Source = data, Converter = GetConv("ColorToBrushConverter") });
            container.Children.Add(band);

            // Redraw when properties or size change
            data.PropertyChanged += (s, e) =>
            {
                Application.Current?.Dispatcher.Invoke(() => Redraw(container, bg, band, data));
            };
            container.SizeChanged += (s, e) => Redraw(container, bg, band, data);
            container.Loaded += (s, e) => Redraw(container, bg, band, data);

            b.Child = container;
        }

        private static void Redraw(Canvas container, Rectangle bg, Rectangle band, SglRollerData data)
        {
            double cw = container.ActualWidth;
            double ch = container.ActualHeight;
            if (cw <= 0 || ch <= 0) return;

            // Remove old text blocks (keep bg and band)
            var toRemove = container.Children.OfType<TextBlock>().ToList();
            foreach (var tb in toRemove)
                container.Children.Remove(tb);

            // Size bg to fill canvas
            bg.Width = cw;
            bg.Height = ch;

            string[] lines = (data.Options ?? "").Split('\n');
            if (lines.Length == 0) return;

            // SGL: item_h = font_h + 6 — fixed, based on font, NOT widget height
            double sglFontH = GetSglFontHeight(data.FontName);
            double itemH = sglFontH + 6;

            // SGL: draw_h = max(widget_h, 3 * item_h) — at least 3 rows
            double drawH = Math.Max(ch, 3 * itemH);

            // SGL: band_y1 = draw_y1 + (widget_h - item_h) / 2 — band centered in widget
            double bandY = (ch - itemH) / 2.0;

            band.Width = cw;
            band.Height = itemH;
            Canvas.SetTop(band, bandY);

            // SGL: scroll_y = -selectedIdx * itemH → selected item at band_y1
            int selIdx = Math.Max(0, Math.Min(data.SelectedIndex, lines.Length - 1));
            double scrollY = -selIdx * itemH;

            // SGL: items drawn from band_y1 + scroll_y, continuing while within draw area
            double itemDrawY = bandY + scrollY;
            int itemIdx = 0;

            while (itemDrawY <= ch && itemIdx < lines.Length)
            {
                // Skip items above visible area (SGL: item_draw_y + item_h < draw_y1)
                if (itemDrawY + itemH < 0)
                {
                    itemIdx++;
                    itemDrawY += itemH;
                    continue;
                }

                string text = itemIdx < lines.Length ? lines[itemIdx] : "";
                if (string.IsNullOrWhiteSpace(text)) text = " ";

                // Truncate to fit
                if (text.Length > 20) text = text.Substring(0, 18) + "..";

                // WPF font size: scale SGL font height down for DPI difference (~90%)
                double wpfFontSize = sglFontH * 0.9;
                if (wpfFontSize < 7) wpfFontSize = 7;

                TextBlock tb = new TextBlock
                {
                    Text = text,
                    IsHitTestVisible = false,
                    Height = itemH,
                    Width = cw - 10,
                    TextAlignment = TextAlignment.Left,
                    FontSize = wpfFontSize,
                    LineHeight = itemH * 0.8,
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight
                };

                // Bind text color
                tb.SetBinding(TextBlock.ForegroundProperty, new Binding("TextColor")
                {
                    Source = data,
                    Converter = GetConv("ColorToBrushConverter")
                });

                // Bind font family
                tb.SetBinding(TextBlock.FontFamilyProperty, new Binding("FontName")
                {
                    Source = data,
                    Converter = new FontNameToFamilyConverter(),
                    Mode = BindingMode.OneWay
                });

                Canvas.SetTop(tb, itemDrawY);
                Canvas.SetLeft(tb, 5);
                container.Children.Add(tb);

                itemIdx++;
                itemDrawY += itemH;
            }
        }

        /// <summary>
        /// Get SGL font height from font name (e.g. consolas14 → 14)
        /// </summary>
        private static double GetSglFontHeight(string fontName)
        {
            var converter = new FontNameToSizeConverter();
            var result = converter.Convert(fontName, typeof(double), null, CultureInfo.InvariantCulture);
            return (result as double?) ?? 14.0;
        }
    }
}
