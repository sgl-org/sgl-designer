using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SglDesigner
{
    // 补充枚举映射
    public enum SglBarchartOrientation { Vertical = 0, Horizontal = 1 }
    public enum SglBarchartOpenAnimDir { None = 0, FromLeft = 1, FromBottom = 2 }
    public partial class SglBarchartData : SglWidgetData
    {
        // --- 基础样式 (对应 sgl_barchart_t 的基础字段) ---
        [ObservableProperty] private Color _bgColor = Colors.Black;
        [ObservableProperty] private byte _bgAlpha = 255;
        [ObservableProperty] private Color _borderColor = Color.FromRgb(60, 60, 60);
        [ObservableProperty] private int _borderWidth = 1;
        [ObservableProperty] private int _radius = 0;
        [ObservableProperty] private byte _opacity = 255; // 全局 Alpha

        // --- 布局与边距 (对应 layout_xxx_margin) ---
        [ObservableProperty] private int _layoutLeftMargin = 30;
        [ObservableProperty] private int _layoutTopMargin = 4;
        [ObservableProperty] private int _layoutRightMargin = 4;
        [ObservableProperty] private int _layoutBottomMargin = 24;

        // --- 图表核心参数 ---
        [ObservableProperty] private int _barGap = 2;
        [ObservableProperty] private int _categoryGap = 8;
        [ObservableProperty] private int _seriesCount = 1;
        [ObservableProperty] private int _xLabelCount = 0;
        [ObservableProperty] private Color _barColor1 = Color.FromRgb(30, 144, 255);
        [ObservableProperty] private SglBarchartOrientation _orientation = SglBarchartOrientation.Vertical;

        // --- 绘图区控制 (对应 option_bits.custom_plot_rect) ---
        [ObservableProperty] private bool _customPlotRect = false;
        [ObservableProperty] private Rect _plotRelRect = new Rect(0, 0, 0, 0); // 对应 sgl_area_t

        // --- X 轴配置 (对应 sgl_barchart_axis_t x_axis) ---
        [ObservableProperty] private bool _xAutoScale = true; // 建议默认开启，自动匹配 Label 数量
        [ObservableProperty] private bool _showXGrid = false;
        [ObservableProperty] private bool _xGridDashed = false; // 补全：虚线支持
        [ObservableProperty] private bool _showXLabels = true;
        [ObservableProperty] private bool _showXTicks = true;
        [ObservableProperty] private int _xMin = 0;             // 补全：范围控制
        [ObservableProperty] private int _xMax = 4;             // 补全：对应 5 个点
        [ObservableProperty] private int _xStep = 0;             // 补全：关键！分类轴步进默认为 1
        [ObservableProperty] private Color _xGridColor = Color.FromRgb(80, 80, 80);
        [ObservableProperty] private Color _xLabelColor = Colors.White;
        [ObservableProperty] private string _xFontName = "consolas14"; // 补全：字体名称

        // --- Y 轴配置 (对应 sgl_barchart_axis_t y_axis) ---
        [ObservableProperty] private bool _yAutoScale = true;
        [ObservableProperty] private bool _showYGrid = true;
        [ObservableProperty] private bool _yGridDashed = true;
        [ObservableProperty] private bool _showYLabels = true;
        [ObservableProperty] private bool _showYTicks = true;
        [ObservableProperty] private int _yMin = 0;
        [ObservableProperty] private int _yMax = 100;
        [ObservableProperty] private int _yStep = 0; // 0 表示使用 AutoDivisions
        [ObservableProperty] private int _yAutoDivisions = 4;
        [ObservableProperty] private Color _yGridColor = Color.FromRgb(80, 80, 80);
        [ObservableProperty] private Color _yLabelColor = Colors.White;
        [ObservableProperty] private string _yFontName = "consolas14";



        // --- 动画 (对应 sgl_barchart_option_bits.open_anim_enable) ---
        [ObservableProperty] private bool _enableOpenAnim = true;
        [ObservableProperty] private SglBarchartOpenAnimDir _openAnimDir = SglBarchartOpenAnimDir.FromBottom;
        [ObservableProperty] private int _openAnimDuration = 600;


        partial void OnYMaxChanged(int value) => RequestRefresh();
        partial void OnXLabelCountChanged(int value) => RequestRefresh();

        partial void OnSeriesCountChanged(int value) => RequestRefresh();

        // 定义一个刷新请求事件，由 UI 预览层监听
        public event Action<SglBarchartData> RefreshRequested;

        private void RequestRefresh()
        {
            RefreshRequested?.Invoke(this);
        }
        public SglBarchartData()
        {
            Type = SglMapping.SglType.Barchart;
            W = 180;
            H = 120;

            // --- 关键修复：初始值不能为 0 ---
            XLabelCount = 5;  // 默认显示 5 组数据
            SeriesCount = 1;  // 默认 1 个序列
            YMax = 100;
            YMin = 0;

        }
    }
    public partial class BaseBinder
    {

        public static void BindBarchart(Border b, SglBarchartData data)
        {
            // 基础外观外观属性绑定
            Bind(b, Border.BackgroundProperty, "BgColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderBrushProperty, "BorderColor", data, GetConv("ColorToBrushConverter"));
            Bind(b, Border.BorderThicknessProperty, "BorderWidth", data, GetConv("IntToThicknessConverter"));
            Bind(b, Border.CornerRadiusProperty, "Radius", data, GetConv("IntToCornerRadiusConverter"));
            Bind(b, Border.OpacityProperty, "Opacity", data, GetConv("ByteToOpacityConverter"));

            // 2. 画布管理：如果已存在则清空，不存在则创建
            Canvas canvas;
            if (b.Child is Canvas oldCanvas)
            {
                canvas = oldCanvas;
                canvas.Children.Clear(); // 清空旧的柱体、标签和网格线
            }
            else
            {
                canvas = new Canvas { IsHitTestVisible = false, ClipToBounds = true };
                b.Child = canvas;
            }

            // --- 3. 坐标轴线 (仅保留底部 X 轴横线) ---
            Line xAxisLine = new Line { StrokeThickness = 1.0 };
            Bind(xAxisLine, Line.StrokeProperty, "YGridColor", data, GetConv("ColorToBrushConverter"));

            // 起点 X1
            xAxisLine.SetBinding(Line.X1Property, new Binding("LayoutLeftMargin") { Source = data });
            // 终点 X2 (W - RightMargin)
            MultiBinding x2AxisBind = new MultiBinding { Converter = new SglGridWidthConverter() };
            x2AxisBind.Bindings.Add(new Binding("W") { Source = data });
            x2AxisBind.Bindings.Add(new Binding("LayoutRightMargin") { Source = data });
            xAxisLine.SetBinding(Line.X2Property, x2AxisBind);
            // 垂直位置 Y (H - BottomMargin)
            MultiBinding yAxisPosBind = new MultiBinding { Converter = new SglAxisYPosConverter() };
            yAxisPosBind.Bindings.Add(new Binding("H") { Source = data });
            yAxisPosBind.Bindings.Add(new Binding("LayoutBottomMargin") { Source = data });
            xAxisLine.SetBinding(Line.Y1Property, yAxisPosBind);
            xAxisLine.SetBinding(Line.Y2Property, yAxisPosBind);

            canvas.Children.Add(xAxisLine);

            // --- 4. Y 轴刻度标签与网格线 ---
            int divisions = data.YStep > 0 ? (data.YMax - data.YMin) / data.YStep : data.YAutoDivisions;
            divisions = Math.Max(1, divisions);

            for (int i = 0; i <= divisions; i++)
            {
                double val = data.YMin + (i * (data.YMax - data.YMin) / (double)divisions);

                // 1 刻度文字
                TextBlock label = new TextBlock { FontSize = 10, Text = val.ToString("F0") };
                Bind(label, TextBlock.ForegroundProperty, "YLabelColor", data, GetConv("ColorToBrushConverter"));
                Bind(label, TextBlock.VisibilityProperty, "ShowYLabels", data, GetConv("BoolToVisConverter"));

                MultiBinding yBind = new MultiBinding { Converter = new SglYPositionConverter(), ConverterParameter = val };
                yBind.Bindings.Add(new Binding("H") { Source = data });
                yBind.Bindings.Add(new Binding("LayoutBottomMargin") { Source = data });
                yBind.Bindings.Add(new Binding("YMax") { Source = data });
                yBind.Bindings.Add(new Binding("YMin") { Source = data });
                yBind.Bindings.Add(new Binding("ActualHeight") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });
                label.SetBinding(Canvas.TopProperty, yBind);
                Canvas.SetLeft(label, 4);
                canvas.Children.Add(label);

                // 2 绘图区网格线
                if (data.ShowYGrid)
                {
                    Line gridLine = new Line { StrokeThickness = 0.8 };
                    Bind(gridLine, Line.StrokeProperty, "YGridColor", data, GetConv("ColorToBrushConverter"));
                    Bind(gridLine, Line.StrokeDashArrayProperty, "YGridDashed", data, GetConv("BoolToDashConverter"));
                    gridLine.SetBinding(Canvas.TopProperty, yBind);
                    Bind(gridLine, Line.X1Property, "LayoutLeftMargin", data);
                    gridLine.SetBinding(Line.X2Property, x2AxisBind); // 复用 X2 绑定
                    canvas.Children.Add(gridLine);
                }
            }

            // --- 5. X 轴刻度标签与柱体 (动态循环) ---
            for (int i = 0; i < data.XLabelCount; i++)
            {
                // 1 X 轴文字标签
                TextBlock xLabel = new TextBlock { FontSize = 10, Text = $"X{i + 1}" };
                Bind(xLabel, TextBlock.ForegroundProperty, "XLabelColor", data, GetConv("ColorToBrushConverter"));
                Bind(xLabel, TextBlock.VisibilityProperty, "ShowXLabels", data, GetConv("BoolToVisConverter"));

                MultiBinding xBind = new MultiBinding { Converter = new SglXPositionConverter(), ConverterParameter = i };
                xBind.Bindings.Add(new Binding("W") { Source = data });
                xBind.Bindings.Add(new Binding("LayoutLeftMargin") { Source = data });
                xBind.Bindings.Add(new Binding("XLabelCount") { Source = data });
                xBind.Bindings.Add(new Binding("ActualWidth") { RelativeSource = new RelativeSource(RelativeSourceMode.Self) });
                xLabel.SetBinding(Canvas.LeftProperty, xBind);

                MultiBinding yLabelBind = new MultiBinding { Converter = new SglXLabelTopConverter() };
                yLabelBind.Bindings.Add(new Binding("H") { Source = data });
                yLabelBind.Bindings.Add(new Binding("LayoutBottomMargin") { Source = data });
                xLabel.SetBinding(Canvas.TopProperty, yLabelBind);
                canvas.Children.Add(xLabel);

                // 2 柱体绘制 (支持多序列)
                for (int si = 0; si < data.SeriesCount; si++)
                {
                    Rectangle bar = new Rectangle();
                    // 根据序列选择颜色 (简单逻辑：序列0用BarColor1，序列1用BarColor2...)
                    string colorProp = si == 0 ? "BarColor1" : "BarColor2";
                    Bind(bar, Rectangle.FillProperty, colorProp, data, GetConv("ColorToBrushConverter"));

                    string[] propNames = { "Width", "Height", "Top", "Left" };
                    DependencyProperty[] dps = { FrameworkElement.WidthProperty, FrameworkElement.HeightProperty, Canvas.TopProperty, Canvas.LeftProperty };

                    for (int p = 0; p < dps.Length; p++)
                    {
                        MultiBinding mb = new MultiBinding
                        {
                            Converter = new SglBarRectConverter(),
                            ConverterParameter = $"{si}_{i}_{propNames[p]}" // si:序列索引, i:数据点索引
                        };

                        // 注入所有影响 Barchart 布局的依赖属性
                        mb.Bindings.Add(new Binding("W") { Source = data });                  // [0]
                        mb.Bindings.Add(new Binding("H") { Source = data });                  // [1]
                        mb.Bindings.Add(new Binding("LayoutLeftMargin") { Source = data });   // [2]
                        mb.Bindings.Add(new Binding("LayoutBottomMargin") { Source = data }); // [3]
                        mb.Bindings.Add(new Binding("CategoryGap") { Source = data });        // [4]
                        mb.Bindings.Add(new Binding("BarGap") { Source = data });             // [5]
                        mb.Bindings.Add(new Binding("SeriesCount") { Source = data });        // [6]
                        mb.Bindings.Add(new Binding("XLabelCount") { Source = data });        // [7]

                        // Mock 数据预览逻辑：基于 i 计算一个波动的百分比
                        double mockPercent = 0.3 + (i * 0.15) % 0.6;
                        mb.Bindings.Add(new Binding("YMax")
                        {
                            Source = data,
                            Converter = new PercentToValueConverter(),
                            ConverterParameter = mockPercent
                        });                                                                   // [8] Value
                        mb.Bindings.Add(new Binding("YMax") { Source = data });               // [9]
                        mb.Bindings.Add(new Binding("YMin") { Source = data });               // [10]

                        bar.SetBinding(dps[p], mb);
                    }
                    canvas.Children.Add(bar);
                }
            }
        }
    }
}