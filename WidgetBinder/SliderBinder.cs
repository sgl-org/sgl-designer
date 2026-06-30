using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace SglDesigner
{
    public partial class SglSliderData : SglWidgetData
    {
        [ObservableProperty] private Color _bgColor = Colors.Transparent;
        [ObservableProperty] private Color _borderColor = Colors.Transparent;
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _radius = 4;
        [ObservableProperty] private int _opacity = 255;

        [ObservableProperty] private int _value = 20;
        [ObservableProperty] private int _thickness = 4;  // 滑块厚度
        [ObservableProperty] private bool _isVertical = false; // direct: 0 水平, 1 垂直

        [ObservableProperty] private Color _trackColor = Colors.DarkGray;
        [ObservableProperty] private Color _fillColor = Colors.DeepSkyBlue;
        [ObservableProperty] private Color _knobColor = Colors.White;


        // --- 核心锚点真正绑定的“视觉属性” ---
        // 这样：拖拽虚线框宽度时，如果垂直则改H，水平则改W
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

        // 当底层数据 W, H, IsVertical 变化时，必须同步通知 UIWidth/UIHeight
        partial void OnIsVerticalChanged(bool value) => RefreshUISize();
        // 注意：基类 SglWidgetData 的 W 和 H 变化时也需要通知，
        // 如果基类没有提供虚方法，可以在这个类的 PropertyChanged 注入
        private void RefreshUISize()
        {
            OnPropertyChanged(nameof(UIWidth));
            OnPropertyChanged(nameof(UIHeight));

        }


        public SglSliderData()
        {
            X = 50; Y = 50;
            W = 100;
            H = 10;
            Type = SglMapping.SglType.Slider;
            // 监听自身所有属性变化
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(W) || e.PropertyName == nameof(H))
                {
                    // 当底层长宽变了，必须告诉 WPF：UI 映射层也变了
                    OnPropertyChanged(nameof(UIWidth));
                    OnPropertyChanged(nameof(UIHeight));
                }
            };
        }
    }
    public partial class BaseBinder
    {
        public static void BindSlider(Border b, SglSliderData data)
        {
            // 配置外层容器
            b.BorderThickness = new Thickness(0);
            b.Background = Brushes.Transparent;
            b.SetBinding(FrameworkElement.WidthProperty, new Binding("UIWidth") { Source = data, Mode = BindingMode.TwoWay });
            b.SetBinding(FrameworkElement.HeightProperty, new Binding("UIHeight") { Source = data, Mode = BindingMode.TwoWay });

            Canvas container = new Canvas { ClipToBounds = false, DataContext = null };
            container.SetBinding(FrameworkElement.LayoutTransformProperty, new Binding("IsVertical")
            {
                Source = data,
                Converter = new IsVerticalToRotateTransformConverter()
            });

            // --- 居中逻辑绑定 (用于轨道和填充层) ---
            var trackCenterBind = new MultiBinding { Converter = new SliderTrackCenterConverter() };
            trackCenterBind.Bindings.Add(new Binding("H") { Source = data });
            trackCenterBind.Bindings.Add(new Binding("Thickness") { Source = data });

            var trackInsetBind = new Binding("H")
            {
                Source = data,
                Converter = new SliderTrackInsetConverter()
            };

            // --- A. 轨道 (Track) ---
            Border track = new Border { IsHitTestVisible = false, DataContext = null };
            Bind(track, Border.BackgroundProperty, "TrackColor", data, GetConv("ColorToBrushConverter"));
            Bind(track, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            track.SetBinding(Canvas.LeftProperty, trackInsetBind);
            track.SetBinding(FrameworkElement.WidthProperty, new MultiBinding
            {
                Converter = new SliderTrackWidthConverter(),
                Bindings =
                {
                    new Binding("W") { Source = data },
                    new Binding("H") { Source = data }
                }
            });
            track.SetBinding(FrameworkElement.HeightProperty, new MultiBinding
            {
                Converter = new SliderTrackThicknessConverter(),
                Bindings =
                {
                    new Binding("H") { Source = data },
                    new Binding("Thickness") { Source = data }
                }
            });
            track.SetBinding(Canvas.TopProperty, trackCenterBind); // 动态居中

            // --- B. 填充层 (Fill) ---
            Border fill = new Border { IsHitTestVisible = false, DataContext = null };
            Bind(fill, Border.BackgroundProperty, "FillColor", data, GetConv("ColorToBrushConverter"));
            Bind(fill, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            fill.SetBinding(FrameworkElement.HeightProperty, new MultiBinding
            {
                Converter = new SliderTrackThicknessConverter(),
                Bindings =
                {
                    new Binding("H") { Source = data },
                    new Binding("Thickness") { Source = data }
                }
            });
            fill.SetBinding(Canvas.TopProperty, trackCenterBind); // 动态居中

            // 填充宽度逻辑
            fill.SetBinding(FrameworkElement.WidthProperty, new MultiBinding
            {
                Converter = new SliderFillWidthConverter(),
                Bindings =
                {
                    new Binding("W") { Source = data },
                    new Binding("H") { Source = data },
                    new Binding("Value") { Source = data }
                }
            });

            // 填充层位置 (处理垂直反转)
            var fillPosBind = new MultiBinding { Converter = new SliderFillPositionConverter() };
            fillPosBind.Bindings.Add(new Binding("W") { Source = data });
            fillPosBind.Bindings.Add(new Binding("H") { Source = data });
            fillPosBind.Bindings.Add(new Binding("Value") { Source = data });
            fillPosBind.Bindings.Add(new Binding("IsVertical") { Source = data });
            fill.SetBinding(Canvas.LeftProperty, fillPosBind);

            // --- C. 滑块 (Knob) ---
            Border knob = new Border { IsHitTestVisible = false, DataContext = null };
            Bind(knob, Border.BackgroundProperty, "KnobColor", data, GetConv("ColorToBrushConverter"));

            // 直径严格对齐 SGL: H - 2
            var knobSizeBind = new Binding("H") { Source = data, Converter = new SglNumericConverter(), ConverterParameter = "-2.0" };
            knob.SetBinding(FrameworkElement.WidthProperty, knobSizeBind);
            knob.SetBinding(FrameworkElement.HeightProperty, knobSizeBind);

            // 圆角：直径的一半
            knob.SetBinding(Border.CornerRadiusProperty, new Binding("H")
            {
                Source = data,
                Converter = new SglNumericConverter(),
                ConverterParameter = "-2.0/2.0"
            });

            // 滑块水平位置：基于 W 和 H 计算，不受 Thickness 影响
            var knobPosBind = new MultiBinding { Converter = new SliderKnobPositionConverter() };
            knobPosBind.Bindings.Add(new Binding("W") { Source = data });
            knobPosBind.Bindings.Add(new Binding("H") { Source = data });
            knobPosBind.Bindings.Add(new Binding("Value") { Source = data });
            knobPosBind.Bindings.Add(new Binding("IsVertical") { Source = data });
            knob.SetBinding(Canvas.LeftProperty, knobPosBind);

            // 滑块垂直位置：固定在 H 的中心 (由于高度是 H-2，所以 Top 始终为 1)
            knob.SetValue(Canvas.TopProperty, 1.0);

            // 组装
            container.Children.Add(track);
            container.Children.Add(fill);
            container.Children.Add(knob);
            b.Child = container;
        }



    }
}
