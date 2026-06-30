using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SglDesigner
{
    public partial class SglBarData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Colors.Transparent;
        [ObservableProperty] private Color _borderColor = Colors.DeepSkyBlue;
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _radius = 6;
        [ObservableProperty] private int _opacity = 255;
        [ObservableProperty] private int _value = 50;
        [ObservableProperty] private Color _fillColor = Colors.DeepSkyBlue;
        [ObservableProperty] private Color _trackColor = Color.FromRgb(45, 45, 45);
        [ObservableProperty] private bool _isVertical = false;
        [ObservableProperty] private string _pixmapVarName = "";

        public double UIWidth
        {
            get => IsVertical ? H : W;
            set
            {
                if (IsVertical) H = (int)value; else W = (int)value;
                OnPropertyChanged(nameof(UIWidth));
            }
        }

        public double UIHeight
        {
            get => IsVertical ? W : H;
            set
            {
                if (IsVertical) W = (int)value; else H = (int)value;
                OnPropertyChanged(nameof(UIHeight));
            }
        }

        partial void OnIsVerticalChanged(bool value) => RefreshUISize();

        private void RefreshUISize()
        {
            OnPropertyChanged(nameof(UIWidth));
            OnPropertyChanged(nameof(UIHeight));
        }

        public SglBarData()
        {
            Type = SglMapping.SglType.Bar;
            X = 50;
            Y = 50;
            W = 120;
            H = 16;

            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(W) || e.PropertyName == nameof(H))
                {
                    OnPropertyChanged(nameof(UIWidth));
                    OnPropertyChanged(nameof(UIHeight));
                }
            };
        }
    }

    public partial class BaseBinder
    {
        public static void BindBar(Border b, SglBarData data)
        {
            b.ClipToBounds = true;
            b.SetBinding(FrameworkElement.WidthProperty, new Binding("UIWidth") { Source = data, Mode = BindingMode.TwoWay });
            b.SetBinding(FrameworkElement.HeightProperty, new Binding("UIHeight") { Source = data, Mode = BindingMode.TwoWay });

            var colorAlphaConv = new ColorAndAlphaToBrushConverter();

            MultiBinding trackBinding = new MultiBinding { Converter = colorAlphaConv };
            trackBinding.Bindings.Add(new Binding("TrackColor") { Source = data });
            trackBinding.Bindings.Add(new Binding("Opacity") { Source = data });
            b.SetBinding(Border.BackgroundProperty, trackBinding);

            MultiBinding borderBinding = new MultiBinding { Converter = colorAlphaConv };
            borderBinding.Bindings.Add(new Binding("BorderColor") { Source = data });
            borderBinding.Bindings.Add(new Binding("Opacity") { Source = data });
            b.SetBinding(Border.BorderBrushProperty, borderBinding);

            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));

            Grid container = new Grid
            {
                DataContext = null,
                Margin = new Thickness(0),
                IsHitTestVisible = false,
                ClipToBounds = true
            };

            Grid barHost = new Grid
            {
                DataContext = null,
                IsHitTestVisible = false,
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            barHost.SetBinding(FrameworkElement.WidthProperty, new Binding("W") { Source = data });
            barHost.SetBinding(FrameworkElement.HeightProperty, new Binding("H") { Source = data });
            barHost.SetBinding(FrameworkElement.LayoutTransformProperty, new Binding("IsVertical")
            {
                Source = data,
                Converter = new BarIsVerticalToRotateTransformConverter()
            });

            Image img = new Image
            {
                Stretch = Stretch.Fill,
                Opacity = 0.5,
                IsHitTestVisible = false
            };
            img.SetBinding(Image.SourceProperty, new Binding("PixmapVarName")
            {
                Source = data,
                Converter = new ResNameToImageSourceConverter(),
                Mode = BindingMode.OneWay
            });

            Border fillBorder = new Border
            {
                Margin = new Thickness(0),
                IsHitTestVisible = false,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            MultiBinding fillBinding = new MultiBinding { Converter = colorAlphaConv };
            fillBinding.Bindings.Add(new Binding("FillColor") { Source = data });
            fillBinding.Bindings.Add(new Binding("Opacity") { Source = data });
            fillBorder.SetBinding(Border.BackgroundProperty, fillBinding);
            fillBorder.SetBinding(FrameworkElement.MarginProperty, new Binding("BorderWidth")
            {
                Source = data,
                Converter = new IntToThicknessAllConverter()
            });
            Bind(fillBorder, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            fillBorder.SetBinding(FrameworkElement.WidthProperty, new MultiBinding
            {
                Converter = new BarFillLengthConverter(),
                Bindings =
                {
                    new Binding("W") { Source = data },
                    new Binding("Value") { Source = data },
                    new Binding("BorderWidth") { Source = data },
                    new Binding("IsVertical") { Source = data }
                }
            });

            barHost.Children.Add(img);
            barHost.Children.Add(fillBorder);
            container.Children.Add(barHost);
            b.Child = container;
        }
    }

    public class BarIsVerticalToRotateTransformConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVertical = value is bool flag && flag;
            return isVertical ? new RotateTransform(-90) : new RotateTransform(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class BarFillLengthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 4 || Array.Exists(values, v => v == DependencyProperty.UnsetValue)) return 0.0;

            double width = System.Convert.ToDouble(values[0]);
            double percent = Math.Max(0.0, Math.Min(100.0, System.Convert.ToDouble(values[1])));
            double border = System.Convert.ToDouble(values[2]);
            bool isVertical = System.Convert.ToBoolean(values[3]);
            double compensation = isVertical ? 2.0 : 0.0;
            double innerLength = Math.Max(0.0, width - border * 2.0 - compensation);

            return innerLength * percent / 100.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
}
