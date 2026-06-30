using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SglDesigner
{
    public class LegendDirToOrientationConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (SglPieLegendDir)value == SglPieLegendDir.Horizontal ? Orientation.Horizontal : Orientation.Vertical;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class BoolToEdgeModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? EdgeMode.Unspecified : EdgeMode.Aliased;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class CircleFillConverter : IMultiValueConverter
    {
        // 实例化那个“不要动”的转换器
        private readonly ResNameToImageSourceConverter _resConverter = new ResNameToImageSourceConverter();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] -> PixmapVarName
            // values[1] -> Color
            string resName = values[0] as string;

            // 优先尝试获取图片
            if (!string.IsNullOrEmpty(resName))
            {
                // 直接调用那个函数
                var imageSource = _resConverter.Convert(resName, typeof(ImageSource), null, culture) as ImageSource;

                if (imageSource != null)
                {
                    return new ImageBrush
                    {
                        ImageSource = imageSource,
                        Stretch = Stretch.UniformToFill
                    };
                }
            }

            // 2. 如果没图片，使用颜色
            if (values[1] is Color color)
            {
                return new SolidColorBrush(color);
            }

            return Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class CircleBackgroundConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // values[0] 是 PixmapVarName
            // values[1] 是 Color
            string pixmapName = values[0] as string;

            // 如果有图片，背景直接透明
            if (!string.IsNullOrEmpty(pixmapName))
            {
                return Brushes.Transparent;
            }

            // 如果没有图片，返回绑定的 Color
            if (values[1] is Color color)
            {
                return new SolidColorBrush(color);
            }

            return Brushes.Transparent;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BackgroundToTransparentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var pixmapPath = value as string;
            var data = parameter as SglCircleData;

            // 如果路径不为空（即选择了图片），背景设为透明，避免干扰
            if (!string.IsNullOrEmpty(pixmapPath))
            {
                return Brushes.Transparent;
            }

            // 如果没有图片，显示 data 中定义的 Color
            if (data != null)
            {
                return new SolidColorBrush(data.Color);
            }

            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class SglLinechartGeometryConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 健壮性检查：防止设计器加载时的 UnsetValue 或 null
            if (values == null || values.Length < 9) return null;
            if (values.Any(v => v == DependencyProperty.UnsetValue || v == null)) return null;

            var pts = values[0] as IEnumerable<int>;
            if (pts == null || !pts.Any()) return null;

            try
            {
                // 2. 解析参数：全部改用 System.Convert 以处理装箱和类型转换
                float yMin = System.Convert.ToSingle(values[1]);
                float yMax = System.Convert.ToSingle(values[2]);
                bool autoScale = System.Convert.ToBoolean(values[3]); // 解决 bool 转换无效
                float offX = System.Convert.ToSingle(values[4]);
                float offY = System.Convert.ToSingle(values[5]);
                float pW = System.Convert.ToSingle(values[6]);
                float pH = System.Convert.ToSingle(values[7]);
                bool smooth = System.Convert.ToBoolean(values[8]); // 解决 bool 转换无效

                //  自动缩放逻辑
                if (autoScale)
                {
                    int minVal = pts.Min();
                    int maxVal = pts.Max();
                    // 模拟 SGL 内部逻辑：如果相等则撑开区间，防止除零
                    if (minVal == maxVal) { yMin = minVal - 1; yMax = maxVal + 1; }
                    else { yMin = minVal; yMax = maxVal; }
                }

                //  计算坐标点
                var points = new List<Point>();
                int count = pts.Count();
                float xStep = count > 1 ? pW / (count - 1) : 0;

                int i = 0;
                foreach (var v in pts)
                {
                    double x = offX + (i * xStep);
                    // 线性归一化计算 Y 坐标
                    double range = yMax - yMin;
                    double pct = range == 0 ? 0.5 : (v - yMin) / range;

                    // 翻转 Y 轴：WPF 0在顶部，SGL 绘图区域需要向上增长
                    double y = offY + pH - (pct * pH);
                    points.Add(new Point(x, y));
                    i++;
                }

                //  构建几何路径
                StreamGeometry geo = new StreamGeometry();
                using (StreamGeometryContext ctx = geo.Open())
                {
                    ctx.BeginFigure(points[0], isFilled: true, isClosed: false);

                    if (smooth && points.Count > 2)
                    {
                        // 贝塞尔平滑：保持与设计器其他组件风格一致
                        for (int j = 0; j < points.Count - 1; j++)
                        {
                            var p1 = points[j];
                            var p2 = points[j + 1];
                            double weight = (p2.X - p1.X) / 2;
                            ctx.BezierTo(new Point(p1.X + weight, p1.Y),
                                         new Point(p2.X - weight, p2.Y),
                                         p2, true, true);
                        }
                    }
                    else
                    {
                        ctx.PolyLineTo(points.Skip(1).ToList(), true, true);
                    }

                    //  填充模式处理：闭合到 Plot Area 底部的基线
                    if (parameter?.ToString() == "Fill")
                    {
                        double baseY = offY + pH;
                        ctx.LineTo(new Point(points.Last().X, baseY), true, false);
                        ctx.LineTo(new Point(points.First().X, baseY), true, false);
                        ctx.Close();
                    }
                }

                geo.Freeze(); // 冻结对象提高 WPF 渲染性能
                return geo;
            }
            catch (Exception)
            {
                // 捕获可能的转换异常，防止设计器直接崩溃
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    public class PieCenterTransformConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 获取绑定的 W 和 H
            double w = values.Length > 0 ? System.Convert.ToDouble(values[0]) : 0;
            double h = values.Length > 1 ? System.Convert.ToDouble(values[1]) : 0;

            // 将坐标原点平移到容器中心点
            // 这样转换器计算出的 (0,0) 圆心点就正好在容器中心
            return new TranslateTransform(w / 2.0, h / 2.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class LegendPosToDockConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SglPieLegendPos pos)
            {
                return pos switch
                {
                    SglPieLegendPos.Left => Dock.Left,
                    SglPieLegendPos.Right => Dock.Right,
                    SglPieLegendPos.Top => Dock.Top,
                    SglPieLegendPos.Bottom => Dock.Bottom,
                    _ => Dock.Right
                };
            }
            return Dock.Right;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 将 0-255 的 Byte 值转换为 0.0-1.0 的 Double 值 (WPF Opacity)
    /// </summary>
    public class ByteToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is byte b)
            {
                return b / 255.0;
            }

            // 兼容可能被解析为 int 的情况
            try
            {
                double val = System.Convert.ToDouble(value);
                double opacity = val / 255.0;
                return Math.Max(0.0, Math.Min(1.0, opacity));
            }
            catch
            {
                return 1.0; // 默认不透明
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                double rawByte = d * 255.0;
                return (byte)Math.Max(0, Math.Min(255, rawByte));
            }
            return (byte)255;
        }
    }
    public class SglGridWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: W,  values[1]: LayoutRightMargin
            if (values.Length < 2) return 0.0;

            double w = System.Convert.ToDouble(values[0]);
            double rightMargin = System.Convert.ToDouble(values[1]);

            return Math.Max(0, w - rightMargin);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class BoolToDashConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDashed && isDashed)
                return new DoubleCollection(new double[] { 4, 4 }); // 4像素实线, 4像素空白
            return null; // 实线
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
    public class SglXLabelTopConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values: [0]ActualHeight, [1]LayoutBottomMargin
            if (values.Any(v => v == DependencyProperty.UnsetValue || v == null)) return 0.0;

            try
            {
                double canvasHeight = System.Convert.ToDouble(values[0]);
                double marginBottom = System.Convert.ToDouble(values[1]);

                // 轴线位置在 (H - BottomMargin)，文字再往下偏移 2~4 像素
                return canvasHeight - marginBottom + 4;
            }
            catch
            {
                return 0.0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class SglXPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values: [0]ActualWidth, [1]LayoutLeftMargin, [2]XLabelCount, [3]TextActualWidth
            if (values.Any(v => v == DependencyProperty.UnsetValue || v == null)) return 0.0;

            try
            {
                double canvasWidth = System.Convert.ToDouble(values[0]);
                double marginLeft = System.Convert.ToDouble(values[1]);
                int xLabelCount = Math.Max(1, System.Convert.ToInt32(values[2]));
                double textWidth = System.Convert.ToDouble(values[3]);
                int index = System.Convert.ToInt32(parameter);

                // 绘图区有效宽度（预留右侧4px间距对齐轴线）
                double plotW = canvasWidth - marginLeft - 4;

                // 每一个分类（Category）占据的宽度
                double catW = plotW / xLabelCount;

                // 计算中心点：左边距 + 之前的分类宽度 + 当前分类的一半 - 文字一半（实现居中）
                double left = marginLeft + (index * catW) + (catW / 2.0) - (textWidth / 2.0);

                return left;
            }
            catch
            {
                return 0.0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    /// <summary>
    /// 根据比例(0-1)计算在 Min-Max 范围内的实际值
    /// 用于预览模式下的数据模拟
    /// </summary>
    public class PercentToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 使用 System.Convert 确保无论 input 是 int 还是 double 都能转成功
            double max = System.Convert.ToDouble(value);
            if (double.TryParse(parameter.ToString(), out double percent))
            {
                return max * percent;
            }
            return 0.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class SglYPositionConverter : IMultiValueConverter
    {
        /// <summary>
        /// 严格模拟 sgl_barchart.c 的坐标计算映射
        /// values: [0]ActualHeight(Canvas), [1]LayoutBottomMargin, [2]YMax, [3]YMin, [4]LabelHeight(可选)
        /// parameter: 当前刻度的数值 (double)
        /// </summary>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 4 || values.Any(v => v == DependencyProperty.UnsetValue))
                return 0.0;

            double fullH = System.Convert.ToDouble(values[0]);
            double mb = System.Convert.ToDouble(values[1]);
            double yMax = System.Convert.ToDouble(values[2]);
            double yMin = System.Convert.ToDouble(values[3]);

            // 获取当前刻度值 (从参数传入)
            double v = System.Convert.ToDouble(parameter);

            // 计算 plot_rect.y1 (TopMargin) 和 plot_rect.y2 (BottomMargin 偏移)
            // 源码逻辑：plot_rect.y1 = full_rect.y1 + layout_top_margin (默认4)
            double mt = 4.0;
            double plotBottom = fullH - mb;
            double plotH = plotBottom - mt;

            // 2. 计算 y 坐标映射
            // 源码公式：y = plot_rect.y2 - (int32_t)(v - min) * plot_h / y_range
            double yRange = yMax - yMin;
            if (Math.Abs(yRange) < 1e-6) yRange = 1.0; // 防止除零

            // 线性映射：不使用 Clamp，超出 yMax/yMin 的值会直接返回 plot 区域外的坐标
            double y = plotBottom - (v - yMin) * plotH / yRange;

            //  如果提供了 Label 的 ActualHeight，则进行垂直居中偏移
            // 源码：y - (int16_t)y_font->font_height / 2
            if (values.Length >= 5 && values[4] is double labelHeight && labelHeight > 0)
            {
                return y - (labelHeight / 2.0);
            }

            return y;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class SglAxisYPosConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: H (控件总高度)
            // values[1]: LayoutBottomMargin (布局底部边距)

            if (values.Length < 2 || values[0] == null || values[1] == null)
                return 0.0;

            try
            {
                double h = System.Convert.ToDouble(values[0]);
                double bottomMargin = System.Convert.ToDouble(values[1]);

                // 计算横线所在的 Y 轴像素位置
                // 注意：在 WPF Canvas 中，Top = 0 是顶部，所以底部位置是 H - Margin
                double yPos = h - bottomMargin;

                return Math.Max(0, yPos);
            }
            catch
            {
                return 0.0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    public class SglBarRectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 安全检查：确保所有 11 个绑定项都已就位
            if (values == null || values.Length < 11 || values.Any(v => v == DependencyProperty.UnsetValue || v == null))
                return 0.0;

            try
            {
                string[] parts = parameter.ToString().Split('_');
                int si = int.Parse(parts[0]); // Series Index
                int pi = int.Parse(parts[1]); // Point Index
                string type = parts[2];       // Width, Height, Top, Left

                // 基础布局参数
                double actualW = System.Convert.ToDouble(values[0]);
                double actualH = System.Convert.ToDouble(values[1]);
                double marginLeft = System.Convert.ToDouble(values[2]);
                double marginBottom = System.Convert.ToDouble(values[3]);
                double catGap = System.Convert.ToDouble(values[4]);
                double barGap = System.Convert.ToDouble(values[5]);
                int seriesCount = Math.Max(1, System.Convert.ToInt32(values[6]));
                int pointCount = Math.Max(1, System.Convert.ToInt32(values[7]));

                // 数据值参数
                double val = System.Convert.ToDouble(values[8]);
                double yMax = System.Convert.ToDouble(values[9]);
                double yMin = System.Convert.ToDouble(values[10]);

                // --- 坐标系计算 ---
                double plotW = actualW - marginLeft - 5; // 预留右侧边缘
                double plotH = actualH - marginBottom - 5; // 预留顶部边缘

                // 计算每一组(Category)的宽度
                double catW = plotW / pointCount;
                // 计算去掉组间距后可用的宽度
                double usableW = Math.Max(1.0, catW - catGap);
                // 计算单个 Bar 的宽度
                double barW = (usableW - (seriesCount - 1) * barGap) / seriesCount;
                barW = Math.Max(1.0, barW);

                // 计算 Y 轴比例
                double yRange = (yMax - yMin == 0) ? 1.0 : (yMax - yMin);
                double plotBottom = actualH - marginBottom;

                // 计算 Bar 的顶部和底部坐标
                double valueY = plotBottom - (val - yMin) * plotH / yRange;
                double baselineY = plotBottom - (Math.Max(0, yMin) - yMin) * plotH / yRange;

                return type switch
                {
                    "Width" => barW,
                    "Height" => Math.Max(1.0, Math.Abs(baselineY - valueY)),
                    "Top" => Math.Min(valueY, baselineY),
                    "Left" => marginLeft + (pi * catW) + (catGap / 2.0) + si * (barW + barGap),
                    _ => 0.0
                };
            }
            catch { return 0.0; }
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    // 柱子宽度计算：严格对应源码 bar_w 计算公式
    public class SglBarWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // [0]:PlotWidth, [1]:PointCount, [2]:CatGap, [3]:BarGap, [4]:SeriesCount
            if (values.Any(v => v == DependencyProperty.UnsetValue)) return 10.0;
            double plotW = System.Convert.ToDouble(values[0]);
            int pointCount = Math.Max(1, System.Convert.ToInt32(values[1]));
            double catGap = System.Convert.ToDouble(values[2]);
            double barGap = System.Convert.ToDouble(values[3]);
            int seriesCount = Math.Max(1, System.Convert.ToInt32(values[4]));

            double categoryW = plotW / pointCount;
            double usableW = Math.Max(seriesCount, categoryW - catGap);
            double barW = (usableW - (seriesCount - 1) * barGap) / seriesCount;
            return Math.Max(1.0, barW);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;

    }
    // 动态生成轴线点集合
    public class BarchartAxisPointsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //values: [0]=W, [1]=H, [2]=MarginLeft, [3]=MarginBottom
            if (values.Length < 4 || values.Any(v => v == DependencyProperty.UnsetValue)) return null;
            double w = System.Convert.ToDouble(values[0]);
            double h = System.Convert.ToDouble(values[1]);
            double ml = System.Convert.ToDouble(values[2]);
            double mb = System.Convert.ToDouble(values[3]);

            // Y轴顶点 -> 原点 (L型转角) -> X轴右侧终点
            return new PointCollection {
            new Point(ml, 10),              // Y轴顶
            new Point(ml, h - mb),          // 原点
            new Point(w - 10, h - mb)       // X轴右
        };
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    // 2. 底部对齐位置生成器 (用于柱条和 Tick 文字)
    public class BarchartBarTopConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // [0]:H, [1]:MarginBottom, [2]:Height (自身的 ActualHeight)
            if (values.Length < 3 || values.Any(v => v == DependencyProperty.UnsetValue)) return 0.0;
            double h = System.Convert.ToDouble(values[0]);
            double mb = System.Convert.ToDouble(values[1]);
            double myH = System.Convert.ToDouble(values[2]);

            // Canvas坐标系中，Top = 总高度 - 底部留白 - 自身的ActualHeight
            // 对于 Tick 标签，还需要加一个简单的分段偏移 logic，这里暂略以简化
            return h - mb - myH;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }


    // 2. 柱子左右位置计算
    public class BarchartBarPosConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Any(v => v == DependencyProperty.UnsetValue)) return 0.0;
            int index = System.Convert.ToInt32(parameter);
            double ml = System.Convert.ToDouble(values[0]);
            double catGap = System.Convert.ToDouble(values[1]);
            double barGap = System.Convert.ToDouble(values[2]);
            return ml + catGap + (index * (16 + barGap)); // 16为预览固定柱宽
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }


    public class BoolToDashStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDashed && isDashed)
            {
                // 返回一个简单的 [2, 2] 虚线集合
                return new DoubleCollection { 2, 2 };
            }
            return null; // 实线
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class VerticesToPointCollectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 这里的 value 现在是 ObservableCollection<SglPoint>
            if (value is IEnumerable<SglPoint> sglPoints)
            {
                var points = new PointCollection();
                foreach (var p in sglPoints)
                {
                    // 手动转换自定义点到 WPF 的 Point
                    points.Add(new System.Windows.Point(p.X, p.Y));
                }
                return points;
            }
            return new PointCollection();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
    public class SliderTrackCenterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: H (总高), values[1]: Thickness (轨道厚度)
            if (values.Length < 2 || values[0] == DependencyProperty.UnsetValue) return 0.0;

            double h = System.Convert.ToDouble(values[0]);
            double thickness = System.Convert.ToDouble(values[1]);
            double knobRadius = Math.Max(0, Math.Floor(h / 2.0) - 1.0);
            thickness = Math.Max(0, Math.Min(thickness, knobRadius));

            // 计算偏移量，使轨道在高度 H 内垂直居中
            return (h - thickness) / 2.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class SliderTrackInsetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || value == DependencyProperty.UnsetValue) return 0.0;

            double h = System.Convert.ToDouble(value);
            return Math.Max(0, Math.Floor(h / 2.0) - 1.0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }
    public class SliderTrackThicknessConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return 0.0;

            double h = System.Convert.ToDouble(values[0]);
            double thickness = System.Convert.ToDouble(values[1]);
            double knobRadius = Math.Max(0, Math.Floor(h / 2.0) - 1.0);
            return Math.Max(0, Math.Min(thickness, knobRadius));
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class SliderTrackWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return 0.0;

            double w = System.Convert.ToDouble(values[0]);
            double h = System.Convert.ToDouble(values[1]);
            double inset = Math.Max(0, Math.Floor(h / 2.0) - 1.0);
            return Math.Max(0, w - 2.0 * inset);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class SliderFillWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue || values[2] == DependencyProperty.UnsetValue)
                return 0.0;

            double w = System.Convert.ToDouble(values[0]);
            double h = System.Convert.ToDouble(values[1]);
            double value = System.Convert.ToDouble(values[2]);
            double inset = Math.Max(0, Math.Floor(h / 2.0) - 1.0);
            double maxCenter = Math.Max(inset, w - 1.0 - inset);
            double center = ((w - 1.0) * value / 100.0) - 1.0;
            center = Math.Max(inset, Math.Min(center, maxCenter));
            return Math.Max(0, center - inset + 1.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class SliderFillPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 4 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue ||
                values[2] == DependencyProperty.UnsetValue || values[3] == DependencyProperty.UnsetValue)
                return 0.0;

            double w = System.Convert.ToDouble(values[0]);
            double h = System.Convert.ToDouble(values[1]);
            double value = System.Convert.ToDouble(values[2]);
            bool isVertical = System.Convert.ToBoolean(values[3]);
            double inset = Math.Max(0, Math.Floor(h / 2.0) - 1.0);
            double trackWidth = Math.Max(0, w - 2.0 * inset);
            double fillWidth = (double)new SliderFillWidthConverter().Convert(new object[] { w, h, value }, targetType, parameter, culture);

            if (isVertical)
            {
                // 旋转后的垂直模式需要从轨道“尾端”向前填充
                return inset + Math.Max(0, trackWidth - fillWidth);
            }

            // 水平模式：从轨道起点开始绘制
            return inset;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class SliderKnobPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 4) return 0.0;
            double w = System.Convert.ToDouble(values[0]);
            double h = System.Convert.ToDouble(values[1]);
            double value = System.Convert.ToDouble(values[2]);
            bool isVertical = System.Convert.ToBoolean(values[3]);
            double inset = Math.Max(0, Math.Floor(h / 2.0) - 1.0);
            double knobSize = Math.Max(0, h - 2.0);
            double displayValue = isVertical ? (100.0 - value) : value;
            double maxCenter = Math.Max(inset, w - 1.0 - inset);
            double center = ((w - 1.0) * displayValue / 100.0) - 1.0;
            center = Math.Max(inset, Math.Min(center, maxCenter));
            return center - (knobSize / 2.0);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;

    }
    public class SglLinearScaleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 增加第3个参数：IsVertical
            if (values.Length < 2 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return 0.0;

            double w = System.Convert.ToDouble(values[0]);
            double value = System.Convert.ToDouble(values[1]);

            // 默认水平逻辑
            double result = (value / 100.0) * w;
            return Math.Max(0, result);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = !string.IsNullOrEmpty(value as string);

            // 如果传入参数是 "Inverse"，则逻辑反转（用于 Path）
            if (parameter?.ToString() == "Inverse")
            {
                return hasValue ? Visibility.Collapsed : Visibility.Visible;
            }

            // 默认逻辑（用于 Image）：有值显示，没值隐藏
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return value;
        }
    }
    // 逻辑：True -> Visible, False -> Collapsed
    public class BoolToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    // 逻辑：True -> Collapsed, False -> Visible (用于编辑时隐藏原本的文字)
    public class InverseBoolToVisConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Collapsed;
        }
    }
    public class IconVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string iconName = value as string;
            bool hasImage = !string.IsNullOrEmpty(iconName) && iconName != "icon_default";

            // 如果是请求 Path 的显示状态
            if (parameter?.ToString() == "Path")
                return hasImage ? Visibility.Collapsed : Visibility.Visible;

            // 如果是请求 Rect (图片) 的显示状态
            return hasImage ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }
    public class Opacity255Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int opacity) return opacity / 255.0;
            return 1.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double opacity) return (int)(opacity * 255);
            return 255;
        }
    }
    public class ColorFormatIndexConverter : IValueConverter
    {
        // 将枚举转换为索引 (Enum -> int)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SglImageGenerator.ColorFormat format)
            {
                return (int)format;
            }
            return 0;
        }

        // 将索引转换为枚举 (int -> Enum)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return (SglImageGenerator.ColorFormat)index;
            }
            return SglImageGenerator.ColorFormat.RGB565;
        }
    }

    public class AlignToAlignmentConverter : IValueConverter
    {
        public class ColorFormatIndexConverter : IValueConverter
        {
            // 将枚举转换为索引 (Enum -> int)
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is SglImageGenerator.ColorFormat format)
                {
                    return (int)format;
                }
                return 0;
            }

            // 将索引转换为枚举 (int -> Enum)
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is int index)
                {
                    return (SglImageGenerator.ColorFormat)index;
                }
                return SglImageGenerator.ColorFormat.RGB565;
            }
        }
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string dir = parameter as string; // "H" 代表水平，"V" 代表垂直
            if (value is int alignIndex)
            {

                // 映射逻辑需对应的 AlignOptions 数组顺序
                switch (alignIndex)
                {
                    case 0: // CENTER
                        return dir == "H" ? HorizontalAlignment.Center : VerticalAlignment.Center;
                    case 1: // TOP_MID
                        return dir == "H" ? HorizontalAlignment.Center : VerticalAlignment.Top;
                    case 2: // TOP_LEFT
                        return dir == "H" ? HorizontalAlignment.Left : VerticalAlignment.Top;
                    case 3: // TOP_RIGHT
                        return dir == "H" ? HorizontalAlignment.Right : VerticalAlignment.Top;
                    case 4: // BOT_MID
                        return dir == "H" ? HorizontalAlignment.Center : VerticalAlignment.Bottom;
                    case 5: // BOT_LEFT
                        return dir == "H" ? HorizontalAlignment.Left : VerticalAlignment.Bottom;
                    case 6: // BOT_RIGHT
                        return dir == "H" ? HorizontalAlignment.Right : VerticalAlignment.Bottom;
                    case 7: // LEFT_MID
                        return dir == "H" ? HorizontalAlignment.Left : VerticalAlignment.Center;
                    case 8: // RIGHT_MID
                        return dir == "H" ? HorizontalAlignment.Right : VerticalAlignment.Center;

                    // 垂直/水平特殊对齐(根据 SGL 库行为定义，通常映射如下)
                    case 9:
                    case 10:
                    case 11: // VERT 系列
                        return dir == "H" ? HorizontalAlignment.Center : VerticalAlignment.Center;
                    case 12:
                    case 13:
                    case 14: // HORIZ 系列
                        return dir == "H" ? HorizontalAlignment.Center : VerticalAlignment.Center;

                    default:
                        return dir == "H" ? HorizontalAlignment.Center : VerticalAlignment.Center;
                }
            }
            return dir == "H" ? HorizontalAlignment.Center : VerticalAlignment.Center;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class ResourceTypeFilterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value 是 SglResManager.Resources 这个集合
            if (value is IEnumerable<SglResItem> resources)
            {
                string targetTypeStr = parameter as string; // 传入 "Image" 或 "Font"
                Console.WriteLine(targetTypeStr);
                return resources
                    .Where(r => r.ResType.ToString().Equals(targetTypeStr, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class NullToBoolConverter : IValueConverter
    {
        // 当数据源传给 UI 时：Object -> Boolean
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果 value 不为 null，返回 true (启用控件)；否则返回 false (禁用)
            return value != null;
        }

        // UI 传回数据源（通常属性面板不需要反向转换）
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
    public class FontPreviewTextConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return "";

            string rawText = values[0] as string;      // 原始 Text
            string fontName = values[1] as string;     // 当前选择的 FontName

            return SglFontValidator.GetValidPreviewText(rawText, fontName);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class FontNameToSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fontName = value?.ToString() ?? "";

            // 优先级 1：检查是否是用户生成的资源项
            // 因为用户生成的项存放在 Resources 集合里，且有明确的 FontSize 属性
            var res = SglResManager.Resources.FirstOrDefault(r => r.Name == fontName);
            if (res != null)
            {
                return (double)res.FontSize;
            }

            // 优先级 2：检查是否是内置的虚拟字体 (如 Consolas14)
            // 这种字体不在 Resources 集合里，需要从名字提取
            if (fontName.StartsWith("Consolas", StringComparison.OrdinalIgnoreCase))
            {
                var match = System.Text.RegularExpressions.Regex.Match(fontName, @"\d+$");
                if (match.Success)
                {
                    return double.Parse(match.Value);
                }
            }

            // 优先级 3：保底值
            return 14.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class FontNameToFamilyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string fontVarName = value as string;

            // 针对 consola14, consola23 等内置字号的特殊处理
            if (fontVarName.StartsWith("Consolas", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // 建议：直接使用最简路径尝试
                    // 如果字体文件就在 Resources/Fonts/consola.ttf
                    return new FontFamily(new Uri("pack://application:,,,/"), "./Resources/Fonts/#Consolas");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("内置字体加载失败: " + ex.Message);
                    return new FontFamily("Consolas"); // 如果本地装了该字体，这行会生效
                }
            }


            var res = SglResManager.Resources.FirstOrDefault(r => r.Name == fontVarName);
            if (res != null && File.Exists(res.FilePath))
            {
                // 调用之前写的 LoadFontFromFile
                return SglResManager.LoadFontFromFile(res.FilePath);
            }
            return new FontFamily("Segoe UI");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class ResTypeToDisplayTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {


            if (value is SglResType type)
            {
                if (type == SglResType.FontCSource) return "C";
                if (type == SglResType.FontSource) return "Ag";
                return "?"; // 如果显示问号，说明类型不是上面两个
            }
            return "!"; // 如果显示感叹号，说明 value 根本不是 SglResType 类型
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            => throw new NotImplementedException();
    }
    public class ResNameToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string resName = value as string;
            if (string.IsNullOrEmpty(resName)) return null;


            // 从定义的全局资源管理器中查找对应的资源项
            var res = SglResManager.Resources.FirstOrDefault(r => r.Name == resName);

            if (res != null && !res.FilePath.Contains(ProjectManager.ProjectDir))
            {
                if (res.Name == "Consolas" && res.ResType == SglResType.FontSource)
                {
                    Console.WriteLine($"break;;;;;;");
                }
                else
                {
                    res.FilePath = Path.Combine(ProjectManager.ProjectDir, res.FilePath);
                    Console.WriteLine($"{res.Name}->{res.FilePath}");
                }



            }

            if (res != null && !string.IsNullOrEmpty(res.FilePath) && File.Exists(res.FilePath))
            {
                try
                {
                    // 创建 BitmapImage 并解开文件锁定，防止删除资源时报错
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad; // 加载后立即释放文件句柄
                    bitmap.UriSource = new Uri(res.FilePath, UriKind.Absolute);
                    bitmap.EndInit();
                    return bitmap;
                }
                catch
                {
                    return null; // 图片损坏或格式不支持
                }
            }

            return null; // 未找到资源
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    // 根据资源类型显示/隐藏图片预览
    public class ImgVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SglResType type)
                return type == SglResType.Image ? Visibility.Visible : System.Windows.Visibility.Collapsed;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    // 根据资源类型显示/隐藏字体占位符(Ag)
    public class FontVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 绑定到 FilePath 最稳妥
            string path = value as string;
            if (string.IsNullOrEmpty(path)) return Visibility.Collapsed;

            string ext = Path.GetExtension(path).ToLower();
            // 如果是 .ttf, .otf (字体源) 或者 .c (生成的代码)，就显示 TextBlock
            if (ext == ".ttf" || ext == ".otf" || ext == ".c" || path.Contains("INTERNAL_RESOURCE"))
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value == null ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }


    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        // 从 Source (bool) 转换到 Target (Visibility)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isHidden)
            {
                // 如果 IsHidden 为 true，返回 Collapsed (折叠/隐藏)
                // 如果 IsHidden 为 false，返回 Visible (显示)
                return isHidden ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        // 从 Target (Visibility) 转换回 Source (bool) - 通常用于双向绑定
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                return vis == Visibility.Collapsed;
            }
            return false;
        }
    }
    // 圆角半径转换器
    public class SliderKnobRadiusConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double h = System.Convert.ToDouble(values[0]);
            double thick = System.Convert.ToDouble(values[1]);
            return new CornerRadius((h + thick) / 2.0);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }




    public class SliderSimpleHorizontalPosConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values: W, Value, H, Thickness
            double w = System.Convert.ToDouble(values[0]);
            double val = System.Convert.ToDouble(values[1]);
            double h = System.Convert.ToDouble(values[2]);
            double thick = System.Convert.ToDouble(values[3]);

            double knobSize = h + thick;
            double ratio = val / 127.0;
            if (ratio < 0) ratio = 0; if (ratio > 1) ratio = 1;

            // 计算公式：进度位置 - 半个滑块宽度
            return (w * ratio) - (knobSize / 2.0);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class IsVerticalToRotateTransformConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVertical = (bool)value;
            // 垂直时顺时针旋转 90 度
            // LayoutTransform 会自动处理旋转后的空间占位
            return isVertical ? new RotateTransform(90) : new RotateTransform(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }


    // 滑块直径计算: H + Thickness
    public class SliderKnobSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2) return 0.0;
            double h = System.Convert.ToDouble(values[0]);
            double thickness = System.Convert.ToDouble(values[1]);
            return h + thickness; // 直径随 H 同步变化，再加上额外厚度
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }



    public class SwitchKnobSizeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int h && values[1] is int margin)
            {
                // 滑块边长 = 容器高度 - 左右两边的间距
                double size = h - (margin * 2);
                return size > 0 ? size : 0;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class TreeViewLineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TreeViewItem item = value as TreeViewItem;
            if (item == null) return false;

            // 寻找父容器
            ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(item);

            // 2. 如果第一步失败（动态添加时常发生），尝试逻辑树向上找
            if (parent == null)
            {
                DependencyObject d = item;
                while (d != null && !(d is TreeViewItem) && !(d is TreeView))
                {
                    d = VisualTreeHelper.GetParent(d);
                }
                parent = d as ItemsControl;
            }

            if (parent != null && parent.Items.Count > 0)
            {
                //  检查是否是最后一个
                // 使用 Items.Count 而不是从 Generator 获取，因为数据通常比 UI 先到
                int index = parent.ItemContainerGenerator.IndexFromContainer(item);

                // 如果是新加的项 index 为 -1，通过数据对象反向查索引
                if (index == -1)
                {
                    index = parent.Items.IndexOf(item.DataContext);
                }

                return index == parent.Items.Count - 1;
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) => null;
    }


    public class EnumDescriptionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;
            var field = value.GetType().GetField(value.ToString());
            if (field == null) return value.ToString();
            var attributes = field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return attributes.Length > 0 ? ((DescriptionAttribute)attributes[0]).Description : value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }

    public class SglNumericConverter : IValueConverter, IMultiValueConverter
    {
        // 核心转换逻辑：支持链式算术解析，如 "-2.0/2.0"
        private object DoConvert(object value, object parameter)
        {
            if (value == null || value == DependencyProperty.UnsetValue) return 0.0;

            double result = System.Convert.ToDouble(value);
            string param = parameter as string;

            if (string.IsNullOrEmpty(param)) return result;

            try
            {
                // 使用正则表达式拆分操作符和操作数
                // 匹配 + - * / 以及紧跟的数字
                var matches = System.Text.RegularExpressions.Regex.Matches(param, @"([\+\-\*\/\%])\s*(-?\d+\.?\d*)");

                if (matches.Count > 0)
                {
                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        char op = match.Groups[1].Value[0];
                        double operand = double.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture);

                        switch (op)
                        {
                            case '+': result += operand; break;
                            case '-': result -= operand; break;
                            case '*': result *= operand; break;
                            case '/':
                                if (operand != 0) result /= operand;
                                break;
                        }
                    }
                }
                else
                {
                    // 如果没有操作符，尝试当作纯倍数处理（兼容旧逻辑）
                    if (double.TryParse(param, NumberStyles.Any, CultureInfo.InvariantCulture, out double multiplier))
                        return result * multiplier;
                }
            }
            catch
            {
                // 解析失败时返回原始值，避免设计器崩溃
                return result;
            }

            return result;
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DoConvert(value, parameter);
        }

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0) return 0.0;
            return DoConvert(values[0], parameter);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    public class SliderProgressConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 长度校验：必须包含 Value (0) 和 W (1)
            if (values == null || values.Length < 2) return 0.0;

            // 2. 有效性检查：防止初始化时的 UnsetValue 导致强转失败
            if (values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return 0.0;

            try
            {
                double val = System.Convert.ToDouble(values[0]);
                double totalW = System.Convert.ToDouble(values[1]);

                // 限制范围并计算百分比 (SGL Slider Value 范围 0-127)
                double ratio = Math.Max(0, Math.Min(127, val)) / 127.0;
                return ratio * totalW;

            }
            catch
            {
                return 0.0;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    public class BooleanToHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool status = (bool)value;
            return status ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class IntToThicknessSideConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double m = System.Convert.ToDouble(value);
            return new Thickness(m, 0, m, 0); // 左右留出间距
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class RadiusMinusGapConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 期望传入三个值：[0] Radius, [1] FillGap, [2] BorderWidth
            if (values.Length < 3 || values.Any(v => v == DependencyProperty.UnsetValue))
            {
                return new CornerRadius(0);
            }

            try
            {
                double r = System.Convert.ToDouble(values[0]); // 外圆角
                double g = System.Convert.ToDouble(values[1]); // 填充间隙
                double b = System.Convert.ToDouble(values[2]); // 边框宽度

                // 核心公式：内部圆角必须扣除边框占据的空间，才能实现物理对齐
                double finalR = Math.Max(0, r - g - b);

                return new CornerRadius(finalR);
            }
            catch
            {
                return new CornerRadius(0);
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class ProgressBarWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 3) return 0.0;

            double value = System.Convert.ToDouble(values[0]);   // 当前进度 (0-100)
            double totalW = System.Convert.ToDouble(values[1]);  // 控件总宽
            double gap = System.Convert.ToDouble(values[2]);     // 间隙

            // 实际可用宽度 = 总宽 - 左右间隙
            double availableWidth = totalW - (gap * 2);
            if (availableWidth < 0) availableWidth = 0;

            // 当前填充宽度
            return availableWidth * (value / 100.0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class IntToThicknessAllConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double g = System.Convert.ToDouble(value);
            return new Thickness(g); // Left, Top, Right, Bottom 全是 g
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
    public class ArcToPathDataConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 索引保护：确保所有 6 个绑定值都已就绪
            if (values.Length < 6 || values.Any(v => v == DependencyProperty.UnsetValue))
                return Geometry.Empty;

            // 获取参数
            double w = System.Convert.ToDouble(values[0]);
            double h = System.Convert.ToDouble(values[1]);
            double rIn = System.Convert.ToDouble(values[2]);
            double rOut = System.Convert.ToDouble(values[3]);
            double startAngle = System.Convert.ToDouble(values[4]);
            double endAngle = System.Convert.ToDouble(values[5]);

            // 2. 计算中心点 (通常居中)
            Point center = new Point(w / 2, h / 2);

            //  构建几何图形
            return CreateArcGeometry(center, rIn, rOut, startAngle, endAngle);
        }

        private Geometry CreateArcGeometry(Point center, double rIn, double rOut, double start, double end)
        {
            // 关键将起始和结束角度减去 90 度，并确保顺时针逻辑
            // 在屏幕坐标系中，0度通常指正右(3点钟方向)
            // 为了符合习惯（0度在正上/12点钟），减去 90
            double startRad = (start + 90) * Math.PI / 180.0;
            double endRad = (end + 90) * Math.PI / 180.0;

            // 计算四个关键点
            // 注意：WPF 的 Y 轴向下，这里的 Math.Sin 会自然地处理向下增长
            Point p1 = new Point(center.X + Math.Cos(startRad) * rOut, center.Y + Math.Sin(startRad) * rOut);
            Point p2 = new Point(center.X + Math.Cos(endRad) * rOut, center.Y + Math.Sin(endRad) * rOut);
            Point p3 = new Point(center.X + Math.Cos(endRad) * rIn, center.Y + Math.Sin(endRad) * rIn);
            Point p4 = new Point(center.X + Math.Cos(startRad) * rIn, center.Y + Math.Sin(startRad) * rIn);

            // 顺时针/逆时针判断
            bool isLargeArc = Math.Abs(end - start) > 180;

            // 如果 Start > End，通常需要反向逻辑，这里强制顺时针绘制
            SweepDirection sweep = SweepDirection.Clockwise;

            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext ctx = geometry.Open())
            {
                ctx.BeginFigure(p1, true, true);
                // 外弧
                ctx.ArcTo(p2, new Size(rOut, rOut), 0, isLargeArc, sweep, true, true);
                // 连线到内径
                ctx.LineTo(p3, true, true);
                // 内弧 (必须反向绘制回来才能封闭)
                ctx.ArcTo(p4, new Size(rIn, rIn), 0, isLargeArc, SweepDirection.Counterclockwise, true, true);
            }
            geometry.Freeze();
            return geometry;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class BooleanToGlowOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 如果 Status 为 true，发光透明度 0.8，否则为 0
            if (value is bool status)
            {
                return status ? 0.8 : 0.0;
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }

    public class LedColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 3 && values[0] is bool status && values[1] is Color on && values[2] is Color off)
            {
                return new SolidColorBrush(status ? on : off);
            }
            return Brushes.Gray;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    // 颜色转画刷：用于 Background 和 BorderBrush
    public class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Color color)
            {
                return new SolidColorBrush(color);
            }
            return Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                return brush.Color;
            }
            return Colors.Transparent;
        }
    }

    // 2. 整数转厚度：用于 BorderWidth -> BorderThickness
    public class IntToThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int thickness)
            {
                return new Thickness(thickness);
            }
            return new Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Thickness t)
            {
                return (int)t.Left;
            }
            return 0;
        }
    }
    public class MsgBoxButtonCornerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double r = System.Convert.ToDouble(value);
            string side = parameter as string;

            if (side == "Left")
                return new CornerRadius(0, 0, r, r); // 仅左下角
            if (side == "Right")
                return new CornerRadius(0, 0, r, r); // 仅右下角

            return new CornerRadius(r); // 默认全圆角
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class MinusOneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // 返回 count - 1，但最小不低于 -1 (代表未选中)
                return Math.Max(-1, count - 1);
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class MarginToLineHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 基础字号 + 间距偏移
            int margin = (value is int val) ? val : 0;
            return (double)(14 + margin);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
    }
    public class IntToDoubleConverter : IValueConverter, IMultiValueConverter
    {
        // 辅助方法：处理倍率逻辑
        private double GetDoubleValue(object value, object parameter)
        {
            double baseVal = 0;
            if (value is int i) baseVal = i;
            else if (value is sbyte s) baseVal = s;
            else if (value is byte b) baseVal = b;
            else if (value is double d) baseVal = d;

            // 检查是否有传入乘法参数 (例如 parameter="2.0")
            if (parameter != null && double.TryParse(parameter.ToString(), out double multiplier))
            {
                return baseVal * multiplier;
            }
            return baseVal;
        }

        // --- IValueConverter 实现 ---
        object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return GetDoubleValue(value, parameter);
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                // 如果有倍率，回传时要除掉
                if (parameter != null && double.TryParse(parameter.ToString(), out double multiplier) && multiplier != 0)
                    return (int)(d / multiplier);
                return (int)d;
            }
            return 0;
        }

        // --- IMultiValueConverter 实现 ---
        object IMultiValueConverter.Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values != null && values.Length > 0)
            {
                return GetDoubleValue(values[0], parameter);
            }
            return 0.0;
        }

        object[] IMultiValueConverter.ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return new object[] { ((IValueConverter)this).ConvertBack(value, null, parameter, culture) };
        }
    }
    //  整数转圆角：用于 Radius -> CornerRadius
    public class IntToCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int radius)
            {
                return new CornerRadius(radius);
            }
            return new CornerRadius(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CornerRadius cr)
            {
                return (int)cr.TopLeft;
            }
            return 0;
        }
    }

    /// <summary>
    /// 将 int 转为只有上圆角的 CornerRadius（给 Win 标题栏用）
    /// </summary>
    public class TopCornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int r = value is int radius ? radius : 0;
            return new CornerRadius(r, r, 0, 0);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is CornerRadius cr ? (int)cr.TopLeft : 0;
        }
    }

    public class OffsetToMarginConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length >= 2 && values[0] is sbyte x && values[1] is sbyte y)
                return new Thickness(x, y, 0, 0);
            return new Thickness(0);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    public class ProgressWidthWithGapConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is int val && values[1] is int totalW && values[2] is int gap)
            {
                // 内部可用总宽度 = 总宽 - 2 * 间隙
                double availableW = totalW - (gap * 2);
                if (availableW < 0) availableW = 0;
                return (val / 100.0) * availableW;
            }
            return 0.0;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class ProgressChunkStepConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is int width && values[1] is int gap)
            {
                // 定义一个矩形区域：宽 = 方块宽 + 间隙，高 = 100 (高不重要，只要宽对准)
                return new Rect(0, 0, width + gap, 100);
            }
            return new Rect(0, 0, 10, 10);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class ProgressHeightWithGapConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is int totalH && values[1] is int gap)
            {
                double h = totalH - (gap * 2);
                return h > 0 ? h : 0;
            }
            return 0.0;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
    public class OpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int alpha)
            {
                return alpha / 255.0; // 将 255 映射为 1.0
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double opacity)
            {
                return (int)(opacity * 255);
            }
            return 255;
        }


    }
    // 背景颜色切换
    public class SwitchBgColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool status = (bool)values[0];
            Color active = (Color)values[1];
            Color inactive = (Color)values[2];
            return new SolidColorBrush(status ? active : inactive);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    // 圆钮 X 坐标计算
    public class SwitchKnobPositionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool status = (bool)values[0];
            int w = (int)values[1];
            int radius = (int)values[2];
            int margin = (int)values[3];

            if (!status) return (double)margin; // 关闭：靠左
            return (double)(w - (radius * 2) - margin); // 开启：靠右
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }

    public class VerticalCenterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is int h && values[1] is int radius)
            {
                // (总高度 - 圆钮直径) / 2
                return (h - (radius * 2)) / 2.0;
            }
            return 0.0;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => null;
    }
}
