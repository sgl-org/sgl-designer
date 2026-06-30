using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{
    public enum SglBoxScrollMode
    {
        VerticalOnly = 1,
        HorizontalOnly = 2,
        Both = 3
    }

    public partial class SglBoxData : SglPanelData
    {
        [ObservableProperty] private Color _scrollbarColor = Colors.Silver;
        [ObservableProperty] private SglBoxScrollMode _scrollMode = SglBoxScrollMode.Both;
        [ObservableProperty] private bool _showVerticalScrollbar = true;
        [ObservableProperty] private bool _showHorizontalScrollbar = true;
        [ObservableProperty] private int _elasticScrollUp = 0;
        [ObservableProperty] private int _elasticScrollDown = 0;
        [ObservableProperty] private int _elasticScrollLeft = 0;
        [ObservableProperty] private int _elasticScrollRight = 0;

        public SglBoxData()
        {
            Type = SglMapping.SglType.Box;
            W = 140;
            H = 100;
            BgColor = Color.FromRgb(60, 60, 60);
            BorderColor = Colors.DimGray;
            BorderWidth = 1;
            Radius = 8;
            Opacity = 255;
        }
    }

    public partial class BaseBinder
    {
        public static void BindBox(Border b, SglBoxData data)
        {
            Canvas panel = new Canvas { IsHitTestVisible = false, ClipToBounds = true };

            Bind(panel, FrameworkElement.WidthProperty, "W", data);
            Bind(panel, FrameworkElement.HeightProperty, "H", data);

            var colorAlphaConv = new ColorAndAlphaToBrushConverter();

            MultiBinding bgBinding = new MultiBinding { Converter = colorAlphaConv };
            bgBinding.Bindings.Add(new Binding("BgColor") { Source = data });
            bgBinding.Bindings.Add(new Binding("Opacity") { Source = data });
            b.SetBinding(Border.BackgroundProperty, bgBinding);

            MultiBinding borderBinding = new MultiBinding { Converter = colorAlphaConv };
            borderBinding.Bindings.Add(new Binding("BorderColor") { Source = data });
            borderBinding.Bindings.Add(new Binding("Opacity") { Source = data });
            b.SetBinding(Border.BorderBrushProperty, borderBinding);

            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));

            Image img = new Image
            {
                Stretch = Stretch.UniformToFill,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false
            };
            Bind(img, FrameworkElement.WidthProperty, "W", data);
            Bind(img, FrameworkElement.HeightProperty, "H", data);
            img.SetBinding(Image.SourceProperty, new Binding("PixmapVarName")
            {
                Source = data,
                Converter = new ResNameToImageSourceConverter(),
                Mode = BindingMode.OneWay
            });
            panel.Children.Add(img);

            Rectangle vScroll = new Rectangle
            {
                Width = 4,
                RadiusX = 2,
                RadiusY = 2,
                IsHitTestVisible = false
            };
            Panel.SetZIndex(vScroll, 999);
            Bind(vScroll, Shape.FillProperty, "ScrollbarColor", data, GetConv("ColorToBrushConverter"));
            vScroll.SetBinding(UIElement.VisibilityProperty, new MultiBinding
            {
                Converter = new BoxScrollbarVisibilityConverter(),
                ConverterParameter = "Vertical",
                Bindings =
                {
                    new Binding("ShowVerticalScrollbar") { Source = data },
                    new Binding("ScrollMode") { Source = data }
                }
            });
            vScroll.SetBinding(Canvas.LeftProperty, new MultiBinding
            {
                Converter = new BoxVerticalScrollbarLeftConverter(),
                Bindings =
                {
                    new Binding("W") { Source = data },
                    new Binding("Radius") { Source = data }
                }
            });
            vScroll.SetBinding(Canvas.TopProperty, new Binding("Radius") { Source = data });
            vScroll.SetBinding(FrameworkElement.HeightProperty, new Binding("H")
            {
                Source = data,
                Converter = new BoxScrollbarLengthConverter()
            });

            Rectangle hScroll = new Rectangle
            {
                Height = 4,
                RadiusX = 2,
                RadiusY = 2,
                IsHitTestVisible = false
            };
            Panel.SetZIndex(hScroll, 999);
            Bind(hScroll, Shape.FillProperty, "ScrollbarColor", data, GetConv("ColorToBrushConverter"));
            hScroll.SetBinding(UIElement.VisibilityProperty, new MultiBinding
            {
                Converter = new BoxScrollbarVisibilityConverter(),
                ConverterParameter = "Horizontal",
                Bindings =
                {
                    new Binding("ShowHorizontalScrollbar") { Source = data },
                    new Binding("ScrollMode") { Source = data }
                }
            });
            hScroll.SetBinding(Canvas.LeftProperty, new Binding("Radius") { Source = data });
            hScroll.SetBinding(Canvas.TopProperty, new MultiBinding
            {
                Converter = new BoxHorizontalScrollbarTopConverter(),
                Bindings =
                {
                    new Binding("H") { Source = data },
                    new Binding("Radius") { Source = data }
                }
            });
            hScroll.SetBinding(FrameworkElement.WidthProperty, new Binding("W")
            {
                Source = data,
                Converter = new BoxScrollbarLengthConverter()
            });

            panel.Children.Add(vScroll);
            panel.Children.Add(hScroll);
            b.Child = panel;
            b.ClipToBounds = true;
        }
    }

    public class BoxScrollbarLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue) return 8.0;
            double total = System.Convert.ToDouble(value);
            return Math.Max(8.0, total / 8.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    public class BoxScrollbarVisibilityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return Visibility.Collapsed;

            bool enabled = System.Convert.ToBoolean(values[0]);
            if (!enabled) return Visibility.Collapsed;

            SglBoxScrollMode mode = (SglBoxScrollMode)values[1];
            string axis = parameter as string;
            bool visible = axis == "Vertical"
                ? mode == SglBoxScrollMode.VerticalOnly || mode == SglBoxScrollMode.Both
                : mode == SglBoxScrollMode.HorizontalOnly || mode == SglBoxScrollMode.Both;

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    public class BoxVerticalScrollbarLeftConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || Array.Exists(values, v => v == DependencyProperty.UnsetValue)) return 0.0;
            double width = System.Convert.ToDouble(values[0]);
            double radius = System.Convert.ToDouble(values[1]);
            return Math.Max(0.0, width - radius - 4.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    public class BoxHorizontalScrollbarTopConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || Array.Exists(values, v => v == DependencyProperty.UnsetValue)) return 0.0;
            double height = System.Convert.ToDouble(values[0]);
            double radius = System.Convert.ToDouble(values[1]);
            return Math.Max(0.0, height - radius - 4.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
}
