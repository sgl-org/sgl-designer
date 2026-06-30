using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;

namespace SglDesigner
{
    /// <summary>
    /// 为 Xceed ColorPicker 添加屏幕取色（吸管）功能的附加行为。
    ///
    /// 用法：在 ColorPicker 上设置 attached property:
    ///   behaviors:ColorPickerEyedropper.IsEnabled="True"
    ///
    /// 或在全局 Style 中启用，使所有 ColorPicker 自动获得吸管按钮。
    /// </summary>
    public static class ColorPickerEyedropper
    {
        #region IsEnabled Attached Property

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached(
                "IsEnabled",
                typeof(bool),
                typeof(ColorPickerEyedropper),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj)
            => (bool)obj.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject obj, bool value)
            => obj.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPicker colorPicker)
            {
                if ((bool)e.NewValue)
                {
                    colorPicker.Loaded += OnColorPickerLoaded;
                    colorPicker.Unloaded += OnColorPickerUnloaded;
                }
                else
                {
                    colorPicker.Loaded -= OnColorPickerLoaded;
                    colorPicker.Unloaded -= OnColorPickerUnloaded;
                    RemoveAdorner(colorPicker);
                }
            }
        }

        #endregion

        #region Loaded / Unloaded

        private static void OnColorPickerLoaded(object sender, RoutedEventArgs e)
        {
            var colorPicker = (ColorPicker)sender;

            // 延迟添加 Adorner，确保 AdornerLayer 已可用
            colorPicker.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(() => AttachAdorner(colorPicker)));
        }

        private static void OnColorPickerUnloaded(object sender, RoutedEventArgs e)
        {
            RemoveAdorner((ColorPicker)sender);
        }

        #endregion

        #region Adorner Management

        private static void AttachAdorner(ColorPicker colorPicker)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(colorPicker);
            if (adornerLayer == null)
                return;

            // 防止重复添加
            var existingAdorners = adornerLayer.GetAdorners(colorPicker);
            if (existingAdorners != null)
            {
                foreach (var ad in existingAdorners)
                {
                    if (ad is EyedropperAdorner)
                        return;
                }
            }

            adornerLayer.Add(new EyedropperAdorner(colorPicker));
        }

        private static void RemoveAdorner(ColorPicker colorPicker)
        {
            var adornerLayer = AdornerLayer.GetAdornerLayer(colorPicker);
            if (adornerLayer == null)
                return;

            var adorners = adornerLayer.GetAdorners(colorPicker);
            if (adorners != null)
            {
                foreach (var ad in adorners)
                {
                    if (ad is EyedropperAdorner)
                        adornerLayer.Remove(ad);
                }
            }
        }

        #endregion

        #region EyedropperAdorner

        /// <summary>
        /// 在 ColorPicker 上叠加渲染的吸管按钮 Adorner
        /// </summary>
        private class EyedropperAdorner : Adorner
        {
            private readonly EyedropperButton _button;
            private readonly ColorPicker _colorPicker;

            public EyedropperAdorner(ColorPicker colorPicker) : base(colorPicker)
            {
                _colorPicker = colorPicker;

                _button = new EyedropperButton();
                _button.Click += OnEyedropperClick;

                AddVisualChild(_button);
                AddLogicalChild(_button);
            }

            private void OnEyedropperClick(object sender, RoutedEventArgs e)
            {
                var window = Window.GetWindow(_colorPicker);
                _ = DoPickColorAsync(window);
            }

            private async Task DoPickColorAsync(Window window)
            {
                // 点击时禁用按钮，防止重复触发
                _button.IsEnabled = false;
                try
                {
                    var result = await ScreenColorPickerHelper.PickColorAsync(window);
                    if (result.HasValue)
                    {
                        _colorPicker.SelectedColor = result.Value;
                    }
                }
                finally
                {
                    _button.IsEnabled = true;
                }
            }

            #region Visual Tree

            protected override int VisualChildrenCount => 1;

            protected override Visual GetVisualChild(int index)
            {
                if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
                return _button;
            }

            #endregion

            #region Layout

            protected override Size MeasureOverride(Size constraint)
            {
                var buttonSize = EyedropperButton.ButtonSize;
                _button.Measure(new Size(buttonSize, buttonSize));
                return _button.DesiredSize;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                double btnSize = EyedropperButton.ButtonSize;
                double elementWidth = AdornedElement.RenderSize.Width;
                double elementHeight = AdornedElement.RenderSize.Height;

                // 将吸管按钮放在 ColorPicker 右侧、下拉箭头之前
                // 通常下拉箭头占据约 16-18px，吸管按钮放在箭头左侧
                double x = Math.Max(2, elementWidth - btnSize - 20);
                double y = Math.Max(0, (elementHeight - btnSize) / 2);

                _button.Arrange(new Rect(x, y, btnSize, btnSize));
                return finalSize;
            }

            #endregion
        }

        #endregion

        #region EyedropperButton

        /// <summary>
        /// 吸管按钮控件 — 扁平的图标按钮，悬浮时显示背景
        /// </summary>
        private class EyedropperButton : Button
        {
            public const double ButtonSize = 18;

            public EyedropperButton()
            {
                Width = ButtonSize;
                Height = ButtonSize;
                Cursor = Cursors.Hand;
                ToolTip = "屏幕取色 (吸管)\n点击后在屏幕上任意位置选取颜色，按 ESC 取消";
                Focusable = false;

                // 扁平样式：无边框无背景
                Background = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)); ;
                BorderBrush = Brushes.Transparent;
                BorderThickness = new Thickness(0);
                Padding = new Thickness(0);
                HorizontalContentAlignment = HorizontalAlignment.Center;
                VerticalContentAlignment = VerticalAlignment.Center;

                Content = CreateEyedropperIcon();

                // 悬浮效果
                MouseEnter += (s, e) =>
                {
                    //Background = new SolidColorBrush(Color.FromArgb(0x30, 0x40, 0x80, 0xFF));
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0x60, 0x40, 0x80, 0xFF));
                    BorderThickness = new Thickness(1);
                };
                MouseLeave += (s, e) =>
                {
                    //Background = Brushes.Transparent;
                    BorderBrush = Brushes.Transparent;
                    BorderThickness = new Thickness(0);
                };
            }

            /// <summary>
            /// 创建吸管图标 — 圆角挤压泡 + 细管 + 椭圆吸头
            /// </summary>
            private static UIElement CreateEyedropperIcon()
            {
                // 使用你提供的 SVG 路径字符串，并设置填充规则为 EvenOdd 以确保内部镂空正确
                string pathData = "M224,67.3a35.79,35.79,0,0,0-11.26-25.66c-14-13.28-36.72-12.78-50.62,1.13L138.8,66.2a24,24,0,0,0-33.14.77l-5,5a16,16,0,0,0,0,22.64l2,2.06-51,51a39.75,39.75,0,0,0-10.53,38l-8,18.41A13.68,13.68,0,0,0,36,219.3a15.92,15.92,0,0,0,17.71,3.35L71.23,215a39.89,39.89,0,0,0,37.06-10.75l51-51,2.06,2.06a16,16,0,0,0,22.62,0l5-5a24,24,0,0,0,.74-33.18l23.75-23.87A35.75,35.75,0,0,0,224,67.3ZM97,193a24,24,0,0,1-24,6,8,8,0,0,0-5.55.31l-18.1,7.91L57,189.41a8,8,0,0,0,.25-5.75A23.88,23.88,0,0,1,63,159l51-51,33.94,34Z";

                // 解析路径
                Geometry geometry = Geometry.Parse(pathData);

                //设置 EvenOdd 规则，防止吸管的“液体透明视窗”和“手柄边缘”被实心填充
                //geometry.StandardFlatteningTolerance = 0.25;
                if (geometry is PathGeometry pathGeometry)
                {
                    pathGeometry.FillRule = FillRule.EvenOdd;
                }

                // 创建 Path 控件
                var path = new Path
                {
                    Data = geometry,
                    Fill = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)), // 图标颜色（灰色）
                    Stretch = Stretch.Uniform,                                  // 等比例缩放
                    Width = 14,                                                 // 限制图标视口大小
                    Height = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    SnapsToDevicePixels = true,
                };

                return path;
            }

        }

        #endregion
    }
}
