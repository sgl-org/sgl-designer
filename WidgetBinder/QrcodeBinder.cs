using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SglDesigner
{
    public partial class SglQrcodeData : SglWidgetData
    {
        // ---- 外观 (匹配 sgl_qrcode_t 结构体) ----
        [ObservableProperty] private Color _bgColor = Colors.White;        // sgl_qrcode_t.bg_color
        [ObservableProperty] private Color _cellColor = Colors.Black;      // sgl_qrcode_t.cell_color
        [ObservableProperty] private int _cellRadius = 0;                  // sgl_qrcode_t.cell_radius  (clamped to min(scale/2, cell_radius))
        [ObservableProperty] private int _opacity = 255;                   // sgl_qrcode_t.alpha
        [ObservableProperty] private int _logoRadius = 0;                  // sgl_qrcode_t.obj.radius → logo 圆角

        // ---- 编码参数 ----
        [ObservableProperty] private string _url = "https://example.com";  // sgl_qrcode_set_text
        [ObservableProperty] private int _qrVersion = 5;                   // sgl_qrcode_set_version  (1-40, 决定模块数 size=4*version+17)
        [ObservableProperty] private int _qrEcc = 0;                       // sgl_qrcode_set_ecc       (0-3: L/M/Q/H)
        [ObservableProperty] private int _qrScale = 4;                     // sgl_qrcode_set_scale     (1-15, 每个模块像素数)
        [ObservableProperty] private int _qrQuietZone = 1;                 // sgl_qrcode_set_zone      (0-15, 静区宽度)

        // ---- Logo 图片 ----
        [ObservableProperty] private string _pixmapVarName = "";           // sgl_qrcode_set_logo
        public bool HasPixmap => !string.IsNullOrEmpty(PixmapVarName);

        public SglQrcodeData()
        {
            Type = SglMapping.SglType.Qrcode;
            // 默认: version=5, scale=4, zone=1 → (37+2)*4 = 156
            SyncSizeFromQrParams();
        }

        /// <summary>计算 QR 码自然尺寸: (size + 2*zone) * scale</summary>
        public int QrNaturalSize
        {
            get
            {
                int size = 4 * QrVersion + 17;
                return Math.Max((size + 2 * QrQuietZone) * QrScale, 20);
            }
        }

        private void SyncSizeFromQrParams()
        {
            int s = QrNaturalSize;
            W = s;
            H = s;
        }

        partial void OnQrVersionChanged(int value) => SyncSizeFromQrParams();
        partial void OnQrScaleChanged(int value) => SyncSizeFromQrParams();
        partial void OnQrQuietZoneChanged(int value) => SyncSizeFromQrParams();
    }

    public partial class BaseBinder
    {
        public static void BindQrcode(Border b, SglQrcodeData data)
        {
            QrcodePreviewElement preview = new QrcodePreviewElement
            {
                IsHitTestVisible = false,
                SnapsToDevicePixels = true
            };

            // OneWay: 参数变化时同步尺寸，不需要从元素回写 data
            preview.SetBinding(FrameworkElement.WidthProperty, new Binding("W") { Source = data, Mode = BindingMode.OneWay });
            preview.SetBinding(FrameworkElement.HeightProperty, new Binding("H") { Source = data, Mode = BindingMode.OneWay });
            Bind(preview, QrcodePreviewElement.UrlProperty, "Url", data);
            Bind(preview, QrcodePreviewElement.QrVersionProperty, "QrVersion", data);
            Bind(preview, QrcodePreviewElement.QrEccProperty, "QrEcc", data);
            Bind(preview, QrcodePreviewElement.QrScaleProperty, "QrScale", data);
            Bind(preview, QrcodePreviewElement.QrQuietZoneProperty, "QrQuietZone", data);
            Bind(preview, QrcodePreviewElement.BgColorProperty, "BgColor", data);
            Bind(preview, QrcodePreviewElement.CellColorProperty, "CellColor", data);
            Bind(preview, QrcodePreviewElement.CellRadiusProperty, "CellRadius", data);
            Bind(preview, QrcodePreviewElement.AlphaProperty, "Opacity", data);
            Bind(preview, QrcodePreviewElement.LogoRadiusProperty, "LogoRadius", data);
            Bind(preview, QrcodePreviewElement.PixmapVarNameProperty, "PixmapVarName", data);

            b.Child = preview;
        }
    }

    internal sealed class QrcodePreviewElement : FrameworkElement
    {
        // ---- 依赖属性 ----
        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register(nameof(Url), typeof(string), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata("https://example.com", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty QrVersionProperty =
            DependencyProperty.Register(nameof(QrVersion), typeof(int), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata(5, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty QrEccProperty =
            DependencyProperty.Register(nameof(QrEcc), typeof(int), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty QrScaleProperty =
            DependencyProperty.Register(nameof(QrScale), typeof(int), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata(4, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty QrQuietZoneProperty =
            DependencyProperty.Register(nameof(QrQuietZone), typeof(int), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BgColorProperty =
            DependencyProperty.Register(nameof(BgColor), typeof(Color), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CellColorProperty =
            DependencyProperty.Register(nameof(CellColor), typeof(Color), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CellRadiusProperty =
            DependencyProperty.Register(nameof(CellRadius), typeof(int), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty AlphaProperty =
            DependencyProperty.Register(nameof(Alpha), typeof(int), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata(255, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LogoRadiusProperty =
            DependencyProperty.Register(nameof(LogoRadius), typeof(int), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PixmapVarNameProperty =
            DependencyProperty.Register(nameof(PixmapVarName), typeof(string), typeof(QrcodePreviewElement),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

        // ---- CLR 属性 ----
        public string Url           { get => (string)GetValue(UrlProperty); set => SetValue(UrlProperty, value); }
        public int QrVersion        { get => (int)GetValue(QrVersionProperty); set => SetValue(QrVersionProperty, value); }
        public int QrEcc            { get => (int)GetValue(QrEccProperty); set => SetValue(QrEccProperty, value); }
        public int QrScale          { get => (int)GetValue(QrScaleProperty); set => SetValue(QrScaleProperty, value); }
        public int QrQuietZone      { get => (int)GetValue(QrQuietZoneProperty); set => SetValue(QrQuietZoneProperty, value); }
        public Color BgColor        { get => (Color)GetValue(BgColorProperty); set => SetValue(BgColorProperty, value); }
        public Color CellColor      { get => (Color)GetValue(CellColorProperty); set => SetValue(CellColorProperty, value); }
        public int CellRadius       { get => (int)GetValue(CellRadiusProperty); set => SetValue(CellRadiusProperty, value); }
        public int Alpha            { get => (int)GetValue(AlphaProperty); set => SetValue(AlphaProperty, value); }
        public int LogoRadius       { get => (int)GetValue(LogoRadiusProperty); set => SetValue(LogoRadiusProperty, value); }
        public string PixmapVarName { get => (string)GetValue(PixmapVarNameProperty); set => SetValue(PixmapVarNameProperty, value); }

        protected override Size MeasureOverride(Size availableSize)
        {
            // 根据当前参数计算应有尺寸，确保首帧布局就正确
            int v = Clamp(QrVersion, 1, 40);
            int s = Clamp(QrScale, 1, 15);
            int z = Clamp(QrQuietZone, 0, 15);
            int modSize = 4 * v + 17;
            double total = (modSize + 2 * z) * s;
            return new Size(total, total);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            int version = Clamp(QrVersion, 1, 40);
            int scale   = Clamp(QrScale, 1, 15);
            int zone    = Clamp(QrQuietZone, 0, 15);
            int size    = 4 * version + 17;                     // QR 码模块数
            double alpha = Clamp(Alpha, 0, 255) / 255.0;

            // 完全根据 QR 参数计算模块区/静区
            double qrArea = size * scale;                       // 模块区像素
            double quietPx = zone * scale;                      // 单边静区像素
            double totalArea = qrArea + 2 * quietPx;            // QR 码完整尺寸

            // 用 Width（DP 值，绑定后立即生效）而非 ActualWidth（需等布局 pass，可能滞后）
            // 避免版本变化导致元素缩放后 ActualWidth 仍是旧尺寸
            double renderW = !double.IsNaN(Width) && Width > 0 ? Width : totalArea;
            double renderH = !double.IsNaN(Height) && Height > 0 ? Height : totalArea;

            // QR 码整体在渲染区内居中
            double qrOffsetX = (renderW - totalArea) / 2.0;
            double qrOffsetY = (renderH - totalArea) / 2.0;
            double originX = qrOffsetX + quietPx;
            double originY = qrOffsetY + quietPx;

            // 裁剪区域
            Rect clip = new Rect(0, 0, renderW, renderH);
            dc.PushClip(new RectangleGeometry(clip));

            // 背景 (匹配 SGL: sgl_draw_rect 填充整个 obj->coords)
            Color bg = BgColor;
            SolidColorBrush bgBrush = new SolidColorBrush(
                Color.FromArgb((byte)(bg.A * alpha), bg.R, bg.G, bg.B));
            bgBrush.Freeze();
            dc.DrawRectangle(bgBrush, null, clip);

            // 生成 QR 矩阵
            bool[,] modules = BuildPreviewMatrix(size, Url ?? string.Empty, version);

            // 单元格圆角 (对应 sgl_qrcode_t.cell_radius, clamped to min(scale/2, cell_radius))
            int cellR = Math.Min(scale / 2, CellRadius);
            double cellCorner = cellR > 0 ? cellR : 0;

            Color cell = CellColor;
            SolidColorBrush cellBrush = new SolidColorBrush(
                Color.FromArgb((byte)(cell.A * alpha), cell.R, cell.G, cell.B));
            cellBrush.Freeze();

            // 绘制模块
            Pen cellPen = null;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (!modules[x, y]) continue;

                    double rx = originX + x * scale;
                    double ry = originY + y * scale;
                    Rect cellRect = new Rect(rx, ry, scale, scale);

                    if (cellCorner > 0)
                        dc.DrawRoundedRectangle(cellBrush, cellPen, cellRect, cellCorner, cellCorner);
                    else
                        dc.DrawRectangle(cellBrush, cellPen, cellRect);
                }
            }

            // 绘制 logo 区域
            DrawLogoArea(dc, originX, originY, qrArea, size, scale, alpha);

            dc.Pop();
        }

        /// <summary>绘制 logo 安全区（居中，尺寸按 ecc 自动计算）</summary>
        private void DrawLogoArea(DrawingContext dc, double originX, double originY,
                                   double qrPixels, int size, int scale, double alpha)
        {
            int logoRadius = LogoRadius;
            string pixName = PixmapVarName;

            // 没有 logo 图片 → 不绘制
            if (string.IsNullOrEmpty(pixName))
                return;

            // 计算安全区 (对应 qrcode_get_pixmap_size)
            uint total = Pow2((uint)size);
            uint allow = 0;
            switch (Clamp(QrEcc, 0, 3))
            {
                case 0: allow = total * 7  / 100; break;
                case 1: allow = total * 15 / 100; break;
                case 2: allow = total * 25 / 100; break;
                case 3: allow = total * 30 / 100; break;
            }
            allow = allow * 80 / 100;                               // safe zone, not fill the ecc all area
            uint modSize = (uint)Math.Sqrt(allow);
            // 安全区在模块区域内居中 (origin 已是模块区域左上角)
            int logoOffset = (int)((size - modSize) / 2);

            double logoX = originX + logoOffset * scale;
            double logoY = originY + logoOffset * scale;
            double logoW = modSize * scale;
            double logoH = modSize * scale;

            // 白色背景
            SolidColorBrush logoBg = new SolidColorBrush(Color.FromArgb((byte)(255 * alpha), 255, 255, 255));
            logoBg.Freeze();

            if (logoRadius > 0)
                dc.DrawRoundedRectangle(logoBg, null,
                    new Rect(logoX, logoY, logoW, logoH), logoRadius, logoRadius);
            else
                dc.DrawRectangle(logoBg, null, new Rect(logoX, logoY, logoW, logoH));

            // 尝试加载 logo 图片（通过 SglResManager 查找实际文件路径）
            try
            {
                var res = SglResManager.Resources.FirstOrDefault(r => r.Name == pixName);
                if (res == null || string.IsNullOrEmpty(res.FilePath))
                    return;

                string imgPath = res.FilePath;
                // 如果路径不是绝对路径且不包含项目目录，则拼接项目目录
                if (!Path.IsPathRooted(imgPath) && !imgPath.Contains(ProjectManager.ProjectDir))
                    imgPath = Path.Combine(ProjectManager.ProjectDir, imgPath);

                if (File.Exists(imgPath))
                {
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.UriSource = new Uri(imgPath);
                    bmp.EndInit();
                    bmp.Freeze();

                    double margin = 2;
                    dc.DrawImage(bmp, new Rect(logoX + margin, logoY + margin,
                                               logoW - margin * 2, logoH - margin * 2));
                }
            }
            catch
            {
                // logo 加载失败时不显示，不影响 QR 码主体
            }
        }

        /// <summary>生成预览用 QR 矩阵（含定位图案、时序图案、伪随机数据区）</summary>
        private static bool[,] BuildPreviewMatrix(int size, string seed, int version)
        {
            bool[,] modules = new bool[size, size];
            bool[,] reserved = new bool[size, size];

            // 三个定位图案 (Finder patterns)
            AddFinder(modules, reserved, 0, 0, size);
            AddFinder(modules, reserved, size - 7, 0, size);
            AddFinder(modules, reserved, 0, size - 7, size);

            // 对齐图案 (Alignment patterns) - version >= 2 时出现
            if (version >= 2)
            {
                int[] alignCenters = GetAlignmentCenters(version);
                foreach (int ay in alignCenters)
                {
                    foreach (int ax in alignCenters)
                    {
                        // 跳过与 finder 重叠的位置
                        if ((ax < 7 && ay < 7) ||
                            (ax > size - 8 && ay < 7) ||
                            (ax < 7 && ay > size - 8))
                            continue;
                        AddAlignment(modules, reserved, ax, ay, size);
                    }
                }
            }

            // 时序图案 (Timing patterns)
            AddTiming(modules, reserved, size);

            // 格式信息保留区域
            AddFormatReserved(reserved, size);

            // 版本信息保留区域 (version >= 7)
            if (version >= 7)
                AddVersionReserved(reserved, size);

            // 数据区：用种子生成伪随机填充
            int hash = 0;
            foreach (char c in seed) hash = (hash * 31 + c) & 0x7FFFFFFF;
            Random rng = new Random(hash + version * 7919);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (reserved[x, y]) continue;
                    modules[x, y] = rng.Next(2) == 1;
                }
            }

            return modules;
        }

        #region QR 结构绘制

        private static void AddFinder(bool[,] m, bool[,] r, int sx, int sy, int size)
        {
            for (int y = -1; y <= 7; y++)
            {
                for (int x = -1; x <= 7; x++)
                {
                    int px = sx + x, py = sy + y;
                    if (px < 0 || py < 0 || px >= size || py >= size) continue;
                    r[px, py] = true;
                    bool inCore = x >= 0 && x <= 6 && y >= 0 && y <= 6;
                    if (!inCore) { m[px, py] = false; continue; }
                    bool border = x == 0 || x == 6 || y == 0 || y == 6;
                    bool center = x >= 2 && x <= 4 && y >= 2 && y <= 4;
                    m[px, py] = border || center;
                }
            }
        }

        private static void AddAlignment(bool[,] m, bool[,] r, int cx, int cy, int size)
        {
            for (int y = -2; y <= 2; y++)
            {
                for (int x = -2; x <= 2; x++)
                {
                    int px = cx + x, py = cy + y;
                    if (px < 0 || py < 0 || px >= size || py >= size) continue;
                    r[px, py] = true;
                    bool border = Math.Abs(x) == 2 || Math.Abs(y) == 2;
                    bool center = x == 0 && y == 0;
                    m[px, py] = border || center;
                }
            }
        }

        private static int[] GetAlignmentCenters(int version)
        {
            // QR Code 标准对齐图案位置
            if (version == 2) return new[] { 6, 18 };
            if (version == 3) return new[] { 6, 22 };
            if (version == 4) return new[] { 6, 26 };
            if (version == 5) return new[] { 6, 30 };
            if (version == 6) return new[] { 6, 34 };
            if (version == 7) return new[] { 6, 22, 38 };
            if (version == 8) return new[] { 6, 24, 42 };
            if (version == 9) return new[] { 6, 26, 46 };
            if (version == 10) return new[] { 6, 28, 54 };
            // 更高版本简化处理
            int step = (version <= 20) ? 4 : (version <= 30 ? 6 : 8);
            System.Collections.Generic.List<int> centers = new System.Collections.Generic.List<int> { 6 };
            int last = 6;
            while (last + step < 4 * version + 17 - 7)
            {
                last += step;
                centers.Add(last);
            }
            return centers.ToArray();
        }

        private static void AddTiming(bool[,] m, bool[,] r, int size)
        {
            for (int i = 8; i < size - 8; i++)
            {
                r[i, 6] = true;
                r[6, i] = true;
                m[i, 6] = i % 2 == 0;
                m[6, i] = i % 2 == 0;
            }
        }

        private static void AddFormatReserved(bool[,] r, int size)
        {
            for (int i = 0; i < 9; i++)
            {
                if (i != 6) { r[8, i] = true; r[i, 8] = true; }
            }
            for (int i = 0; i < 8; i++)
            {
                r[size - 1 - i, 8] = true;
                r[8, size - 1 - i] = true;
            }
        }

        private static void AddVersionReserved(bool[,] r, int size)
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    r[size - 11 + j, i] = true;
                    r[i, size - 11 + j] = true;
                }
            }
        }

        #endregion

        private static uint Pow2(uint v) { return v * v; }

        private static int Clamp(int val, int min, int max) => Math.Max(min, Math.Min(max, val));
    }
}
