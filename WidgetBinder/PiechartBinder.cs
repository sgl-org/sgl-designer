using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{
    // 对应源码中的枚举
    public enum SglPieLegendPos { Left, Right, Top, Bottom }
    public enum SglPieLegendDir { Horizontal, Vertical }

    public partial class SglPieSlice : ObservableObject
    {
        [ObservableProperty] private int _value = 0;
        [ObservableProperty] private Color _color = Colors.DeepSkyBlue;
        [ObservableProperty] private int _opacity = 255;
        [ObservableProperty] private string _label = "Slice";
    }

    // 子类：按钮独有属性
    public partial class SglPiechartData : SglWidgetData
    {

        // --- 基础样式 ---
        [ObservableProperty] private int _opacity = 255;
        [ObservableProperty] private int _startAngle = 0;

        // 半径设置：对应源码中的 radius_out 和 inner_radius_rate
        [ObservableProperty] private int _radiusOut = 0; // 为0时自动计算
        [ObservableProperty] private int _innerRadiusRate = 0; // 0-100, 为>0时是环形图

        // --- 选项位 (对应源码 option_bits) ---
        [ObservableProperty] private bool _isSmooth = false;
        [ObservableProperty] private bool _openAnimEnable = true;

        // --- 图例设置 (对应源码 layout_bits & legend_font 等) ---
        [ObservableProperty] private bool _legendEnable = true;
        [ObservableProperty] private SglPieLegendPos _legendPos = SglPieLegendPos.Right;
        [ObservableProperty] private SglPieLegendDir _legendDir = SglPieLegendDir.Vertical;
        [ObservableProperty] private int _legendAreaSize = 60;
        [ObservableProperty] private int _legendBoxSize = 10;
        [ObservableProperty] private Color _legendTextColor = Colors.White;
        [ObservableProperty] private string _fontName = "consolas14";
        [ObservableProperty] private string _fontSize = "14";
        [ObservableProperty] private Color _legendBgColor = Colors.Transparent;
        [ObservableProperty] private bool _legendBgEnable = false;


        // --- 颜色与样式 ---
        [ObservableProperty] private Color _legendBorderColor = Color.FromRgb(60, 60, 60);
        [ObservableProperty] private int _legendAlpha = 255;
        [ObservableProperty] private int _legendPadding = 4;   // 对应 SGL_PIECHART_DEFAULT_PADDING
        [ObservableProperty] private int _legendItemGap = 4;   // 对应 SGL_PIECHART_DEFAULT_GAP



        // --- 数据源 ---
        // 使用 ObservableCollection 方便在 UI 上增删切片
        public ObservableCollection<SglPieSlice> Slices { get; set; } = new();


        [RelayCommand]
        private void AddSlice()
        {
            // 添加一个默认切片，触发 UI 刷新和重绘
            Slices.Add(new SglPieSlice
            {
                Value = 10,
                Color = Colors.Gray,
                Label = $"Part {Slices.Count + 1}"
            });
        }

        [RelayCommand]
        private void DeleteSlice(SglPieSlice slice)
        {
            if (slice != null && Slices.Contains(slice))
            {
                Slices.Remove(slice);
            }
        }


        public SglPiechartData()
        {

            W = 160;
            H = 100;

            // 构造时添加 3 个默认切片
            Slices.Add(new SglPieSlice { Value = 30, Color = Colors.Red, Label = "Part 1" });
            Slices.Add(new SglPieSlice { Value = 40, Color = Colors.Green, Label = "Part 2" });
            Slices.Add(new SglPieSlice { Value = 30, Color = Colors.Blue, Label = "Part 3" });

            Slices.CollectionChanged += HandleSlicesCollectionChanged;

        }
        private void HandleSlicesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (SglPieSlice p in e.NewItems)
                {
                    // 当单个切片的 Value 或其他属性变化时，通知整个 Data 刷新
                    p.PropertyChanged += (s1, e1) =>
                    {
                        // 这里通知 StartAngle 或 RadiusOut 发生变化，
                        // 即使值没变，也会强制触发 MultiBinding 重新计算
                        OnPropertyChanged(nameof(Slices));
                    };
                }
            }
            // 通知 Slices 集合已更新
            OnPropertyChanged(nameof(Slices));
        }
        public static SglPiechartData CreateDefault()
        {
            return new SglPiechartData();
        }

    }





    public partial class BaseBinder
    {
        private static void UpdateLayoutGrid(Grid grid, SglPiechartData data)
        {
            grid.RowDefinitions.Clear();
            grid.ColumnDefinitions.Clear();

            // 根据位置分配行列定义
            switch (data.LegendPos)
            {
                case SglPieLegendPos.Top:
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(data.LegendAreaSize) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    break;
                case SglPieLegendPos.Bottom:
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(data.LegendAreaSize) });
                    break;
                case SglPieLegendPos.Left:
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(data.LegendAreaSize) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    break;
                case SglPieLegendPos.Right:
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(data.LegendAreaSize) });
                    break;
            }

            // 重新遍历 Grid 的子元素，更新它们的行列索引
            // 假设：Children[0] 是 Canvas (饼图), Children[1] 是 ItemsControl (图例)
            if (grid.Children.Count >= 2)
            {
                var pieCanvas = grid.Children[0] as UIElement;
                var legendControl = grid.Children[1] as UIElement;

                Grid.SetRow(pieCanvas, GetPieRow(data.LegendPos));
                Grid.SetColumn(pieCanvas, GetPieColumn(data.LegendPos));

                Grid.SetRow(legendControl, GetLegendRow(data.LegendPos));
                Grid.SetColumn(legendControl, GetLegendColumn(data.LegendPos));
            }
        }
        private static int GetPieRow(SglPieLegendPos pos) => pos == SglPieLegendPos.Top ? 1 : 0;
        private static int GetPieColumn(SglPieLegendPos pos) => pos == SglPieLegendPos.Left ? 1 : 0;

        private static int GetLegendRow(SglPieLegendPos pos) => pos == SglPieLegendPos.Bottom ? 1 : (pos == SglPieLegendPos.Top ? 0 : 0);
        private static int GetLegendColumn(SglPieLegendPos pos) => pos == SglPieLegendPos.Right ? 1 : (pos == SglPieLegendPos.Left ? 0 : 0);

        private static ItemsControl CreateLegendControl(SglPiechartData data)
        {
            ItemsControl legend = new ItemsControl
            {
                ItemsSource = data.Slices,
                Margin = new Thickness(data.LegendPadding),
                DataContext = null
            };

            // 设置图例排列方向 (对应源码 legend_dir)
            FrameworkElementFactory stackFactory = new FrameworkElementFactory(typeof(StackPanel));
            Binding dirBind = new Binding("LegendDir")
            {
                Source = data,
                Converter = new LegendDirToOrientationConverter()
            };
            stackFactory.SetBinding(StackPanel.OrientationProperty, dirBind);
            legend.ItemsPanel = new ItemsPanelTemplate(stackFactory);

            // 图例项模板：[小方块] [文字]
            DataTemplate itemTemp = new DataTemplate(typeof(SglPieSlice));
            FrameworkElementFactory grid = new FrameworkElementFactory(typeof(Grid));
            grid.SetValue(Grid.MarginProperty, new Thickness(0, 0, 0, data.LegendItemGap));

            FrameworkElementFactory col1 = new FrameworkElementFactory(typeof(ColumnDefinition));
            col1.SetValue(ColumnDefinition.WidthProperty, new GridLength(data.LegendBoxSize + 4));
            FrameworkElementFactory col2 = new FrameworkElementFactory(typeof(ColumnDefinition));

            grid.AppendChild(col1);
            grid.AppendChild(col2);

            // 颜色方块 (对应源码 legend_box_size)
            FrameworkElementFactory rect = new FrameworkElementFactory(typeof(Rectangle));
            rect.SetValue(Rectangle.WidthProperty, (double)data.LegendBoxSize);
            rect.SetValue(Rectangle.HeightProperty, (double)data.LegendBoxSize);
            rect.SetBinding(Rectangle.FillProperty, new Binding("Color") { Converter = GetConv("ColorToBrushConverter") });
            rect.SetValue(Grid.ColumnProperty, 0);
            grid.AppendChild(rect);

            // 标签文字
            FrameworkElementFactory txt = new FrameworkElementFactory(typeof(TextBlock));
            txt.SetBinding(TextBlock.TextProperty, new Binding("Label"));
            txt.SetBinding(TextBlock.ForegroundProperty, new Binding("LegendTextColor") { Source = data, Converter = GetConv("ColorToBrushConverter") });
            //txt.SetValue(TextBlock.FontSizeProperty, double.Parse(data.FontSize));
            txt.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);
            txt.SetValue(Grid.ColumnProperty, 1);
            grid.AppendChild(txt);

            itemTemp.VisualTree = grid;
            legend.ItemTemplate = itemTemp;

            return legend;
        }


        public static void BindPiechart(Border b, SglPiechartData data)
        {
            // 基础样式配置
            b.SnapsToDevicePixels = true;
            b.ClipToBounds = true;

            // 同步 Border 与 Data 的宽高 (TwoWay 确保拖拽同步)
            b.SetBinding(FrameworkElement.WidthProperty, new Binding("W") { Source = data, Mode = BindingMode.TwoWay });
            b.SetBinding(FrameworkElement.HeightProperty, new Binding("H") { Source = data, Mode = BindingMode.TwoWay });
            Bind(b, Border.BackgroundProperty, "LegendBgColor", data, GetConv("ColorToBrushConverter"));

            // 2. 创建主布局 Grid (解决图例和饼图并列问题)
            Grid mainGrid = new Grid();
            UpdateLayoutGrid(mainGrid, data);

            //  饼图绘制区域 (Canvas)
            // Canvas 必须设置 ClipToBounds，防止半径计算错误导致溢出
            Canvas drawingCanvas = new Canvas
            {
                DataContext = null,
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            ItemsControl itemsControl = new ItemsControl
            {
                ItemsSource = data.Slices,
                DataContext = null
            };

            //  居中变换逻辑
            // 确保饼图在 Canvas 分配到的 Star 区域内居中
            MultiBinding transformBind = new MultiBinding { Converter = new PieCenterTransformConverter() };
            transformBind.Bindings.Add(new Binding("ActualWidth") { Source = drawingCanvas });
            transformBind.Bindings.Add(new Binding("ActualHeight") { Source = drawingCanvas });
            itemsControl.SetBinding(UIElement.RenderTransformProperty, transformBind);

            //  切片模板与几何图形生成
            FrameworkElementFactory gridFactory = new FrameworkElementFactory(typeof(Grid));
            itemsControl.ItemsPanel = new ItemsPanelTemplate(gridFactory);

            DataTemplate sliceTemplate = new DataTemplate(typeof(SglPieSlice));
            FrameworkElementFactory pathFactory = new FrameworkElementFactory(typeof(Path));
            pathFactory.SetBinding(Path.FillProperty, new Binding("Color") { Converter = GetConv("ColorToBrushConverter") });

            // 绑定抗锯齿模式 (Smooth 属性)
            pathFactory.SetBinding(RenderOptions.EdgeModeProperty, new Binding("IsSmooth") { Source = data, Converter = new BoolToEdgeModeConverter() });

            MultiBinding geometryBind = new MultiBinding { Converter = new SglPieSliceGeometryConverter() };
            geometryBind.Bindings.Add(new Binding(".")); // 当前 Slice
            geometryBind.Bindings.Add(new Binding("RadiusOut") { Source = data });
            geometryBind.Bindings.Add(new Binding("InnerRadiusRate") { Source = data });
            geometryBind.Bindings.Add(new Binding("StartAngle") { Source = data });
            geometryBind.Bindings.Add(new Binding("ActualWidth") { Source = drawingCanvas });
            geometryBind.Bindings.Add(new Binding("ActualHeight") { Source = drawingCanvas });
            // --- 增加对集合数量的监听 ---
            geometryBind.Bindings.Add(new Binding("Slices.Count") { Source = data });
            geometryBind.ConverterParameter = data;

            pathFactory.SetBinding(Path.DataProperty, geometryBind);

            pathFactory.SetValue(Path.StretchProperty, Stretch.None);

            sliceTemplate.VisualTree = pathFactory;
            itemsControl.ItemTemplate = sliceTemplate;

            // 组装饼图层
            drawingCanvas.Children.Add(itemsControl);
            mainGrid.Children.Add(drawingCanvas);
            Grid.SetRow(drawingCanvas, GetPieRow(data.LegendPos));
            Grid.SetColumn(drawingCanvas, GetPieColumn(data.LegendPos));

            //  创建图例层
            ItemsControl legendControl = CreateLegendControl(data);
            mainGrid.Children.Add(legendControl);
            Grid.SetRow(legendControl, GetLegendRow(data.LegendPos));
            Grid.SetColumn(legendControl, GetLegendColumn(data.LegendPos));

            b.Child = mainGrid;
        }

        public class SglPieSliceGeometryConverter : IMultiValueConverter
        {
            public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
            {
                if (values.Length < 6 || values[0] is not SglPieSlice currentSlice || parameter is not SglPiechartData data)
                    return Geometry.Empty;

                double radiusOutSet = System.Convert.ToDouble(values[1]);
                double innerRate = System.Convert.ToDouble(values[2]) / 100.0;
                double baseAngle = System.Convert.ToDouble(values[3]);
                double containerW = System.Convert.ToDouble(values[4]);
                double containerH = System.Convert.ToDouble(values[5]);

                // 计算当前物理限制的最大半径
                double maxRadius = Math.Min(containerW, containerH) / 2.0;

                // 如果设置的半径为 0，则自动填满 Border；如果设置了半径，则取最小值防止溢出
                double rOut = (radiusOutSet <= 0) ? maxRadius : Math.Min(radiusOutSet, maxRadius);
                double rIn = rOut * innerRate;

                // --- 计算角度逻辑 ---
                double total = data.Slices.Sum(s => s.Value);
                if (total <= 0) return Geometry.Empty;

                double startAngle = baseAngle + 90;
                foreach (var s in data.Slices)
                {
                    if (s == currentSlice) break;
                    startAngle += (s.Value / total) * 360.0;
                }
                double sweepAngle = (currentSlice.Value / total) * 360.0;

                return CreatePieSliceGeometry(rOut, rIn, startAngle, startAngle + sweepAngle);
            }

            private Geometry CreatePieSliceGeometry(double rOut, double rIn, double startDeg, double endDeg)
            {
                StreamGeometry geometry = new StreamGeometry();
                using (StreamGeometryContext ctx = geometry.Open())
                {
                    Point pOutStart = DegreeToPoint(rOut, startDeg);
                    Point pOutEnd = DegreeToPoint(rOut, endDeg);
                    Point pInStart = DegreeToPoint(rIn, startDeg);
                    Point pInEnd = DegreeToPoint(rIn, endDeg);

                    bool isLargeArc = (endDeg - startDeg) > 180;

                    // 绘图开始
                    ctx.BeginFigure(rIn > 0 ? pInStart : new Point(0, 0), true, true);
                    ctx.LineTo(pOutStart, true, false);
                    ctx.ArcTo(pOutEnd, new Size(rOut, rOut), 0, isLargeArc, SweepDirection.Clockwise, true, false);
                    ctx.LineTo(rIn > 0 ? pInEnd : new Point(0, 0), true, false);

                    if (rIn > 0)
                    {
                        ctx.ArcTo(pInStart, new Size(rIn, rIn), 0, isLargeArc, SweepDirection.Counterclockwise, true, false);
                    }
                }
                geometry.Freeze();
                return geometry;
            }

            private Point DegreeToPoint(double r, double deg)
            {
                double rad = (Math.PI / 180.0) * deg;
                return new Point(r * Math.Cos(rad), r * Math.Sin(rad));
            }

            public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}