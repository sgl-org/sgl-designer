using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SglDesigner
{
    public static class SglImageGenerator
    {
        public enum ColorFormat { RGB565, RGB888 }

        public static string ConvertToSglPixmapCode(BitmapSource bitmap, string varName, string mode)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            // 统一转为 BGRA32，方便提取 Alpha 和 RGB
            var cb = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            cb.CopyPixels(pixels, stride, 0);

            // 检测是否有透明像素
            bool hasAlpha = CheckHasAlpha(pixels);
            string finalFormat = "";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#include <stdint.h>");
            sb.AppendLine("#include <sgl_core.h>");
            sb.AppendLine("");
            sb.AppendLine($"// {varName} - {width}x{height}");
            sb.AppendLine($"static const uint8_t {varName}_data[] = {{");
            sb.Append("    ");

            int lineCounter = 0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                byte a = pixels[i + 3];

                if (mode == ColorFormat.RGB565.ToString()) //"16-bit"
                {
                    if (hasAlpha)
                    {
                        // --- ARGB4444 ---
                        // SGL 源码解析: pix_value = read_ptr[1] | (read_ptr[2] << 8)
                        // pix_alpha = sgl_opa4_table[pix_value >> 12]
                        finalFormat = "SGL_PIXMAP_FMT_ARGB4444";
                        ushort argb4444 = (ushort)(((a >> 4) << 12) | ((r >> 4) << 8) | ((g >> 4) << 4) | (b >> 4));
                        sb.Append($"0x{(byte)(argb4444 & 0xFF):X2}, 0x{(byte)(argb4444 >> 8):X2}, ");
                        lineCounter += 2;
                    }
                    else
                    {
                        // --- RGB565 ---
                        finalFormat = "SGL_PIXMAP_FMT_RGB565";
                        ushort rgb565 = (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
                        sb.Append($"0x{(byte)(rgb565 & 0xFF):X2}, 0x{(byte)(rgb565 >> 8):X2}, ");
                        lineCounter += 2;
                    }
                }
                else // 32-bit 模式
                {
                    if (hasAlpha)
                    {
                        // --- ARGB8888 ---
                        // SGL 源码解析: pix_alpha = read_ptr[4], index += 4
                        // 注意: 的 SGL 源码 ARGB8888 似乎是 4 字节，这里按标准 ARGB8888 输出
                        finalFormat = "SGL_PIXMAP_FMT_ARGB8888";
                        sb.Append($"0x{r:X2}, 0x{g:X2}, 0x{b:X2}, 0x{a:X2}, ");
                        lineCounter += 4;
                    }
                    else
                    {
                        // --- RGB888 ---
                        finalFormat = "SGL_PIXMAP_FMT_RGB888";
                        sb.Append($"0x{r:X2}, 0x{g:X2}, 0x{b:X2}, ");
                        lineCounter += 3;
                    }
                }

                if (lineCounter >= 12)
                {
                    sb.Append("\n    ");
                    lineCounter = 0;
                }
            }

            sb.AppendLine("\n};");
            sb.AppendLine("");

            // 2. 生成 Pixmap 结构体
            sb.AppendLine($"const sgl_pixmap_t {varName}_pixmap = {{");
            sb.AppendLine($"    .width = {width},");
            sb.AppendLine($"    .height = {height},");
            sb.AppendLine($"    .bitmap.array = {varName}_data,");
            sb.AppendLine($"    .format = {finalFormat},");
            sb.AppendLine("};");

            return sb.ToString();
        }

        /// <summary>
        /// 扫描像素数组，判断是否存在非全不透明的像素
        /// </summary>
        private static bool CheckHasAlpha(byte[] pixels)
        {
            for (int i = 3; i < pixels.Length; i += 4)
            {
                if (pixels[i] < 255) return true; // 只要有一个像素有透明度，就认为有 Alpha
            }
            return false;
        }
        public static string ConvertToSgl4BitIconCode(BitmapSource bitmap, string varName)
        {
            int width = bitmap.PixelWidth;
            int height = bitmap.PixelHeight;

            // 转为 Bgra32
            var cb = new FormatConvertedBitmap(bitmap, PixelFormats.Bgra32, null, 0);
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];
            cb.CopyPixels(pixels, stride, 0);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#include <stdint.h>");
            sb.AppendLine("#include <sgl_core.h>");
            sb.AppendLine("");
            sb.AppendLine($"// Icon 4-bit Alpha Data: {width}x{height}");
            sb.AppendLine($"static const uint8_t {varName}_data[] = {{");

            for (int y = 0; y < height; y++)
            {
                sb.Append("    ");
                for (int x = 0; x < width; x += 2)
                {
                    // 处理第一个像素 (高 4 位)
                    int px1Idx = (y * width + x) * 4;
                    byte a1 = pixels[px1Idx + 3];
                    byte high4 = (byte)(a1 >> 4); // 取 8 位 Alpha 的高 4 位

                    // 处理第二个像素 (低 4 位)
                    byte low4 = 0;
                    if (x + 1 < width) // 防止宽度为奇数越界
                    {
                        int px2Idx = (y * width + (x + 1)) * 4;
                        byte a2 = pixels[px2Idx + 3];
                        low4 = (byte)(a2 >> 4);
                    }

                    // 合成一个字节 (注意：根据提供的范例，高位在前)
                    byte combined = (byte)((high4 << 4) | low4);
                    sb.Append($"0x{combined:X2},");
                }
                sb.AppendLine($" // Line {y}");
            }

            sb.AppendLine("};");
            sb.AppendLine("");

            sb.AppendLine($"const sgl_icon_pixmap_t {varName}_pixmap = {{");
            sb.AppendLine($"    .width = {width},");
            sb.AppendLine($"    .height = {height},");
            sb.AppendLine($"    .bitmap = {varName}_data,");
            sb.AppendLine("};");

            return sb.ToString();
        }
        /// <summary>
        /// 依据目标尺寸生成缩放后的 C 代码
        /// </summary>
        /// <param name="res">原始资源项</param>
        /// <param name="targetWidth">控件设置的宽度</param>
        /// <param name="targetHeight">控件设置的高度</param>
        public static string ConvertToSglCode(SglResItem res, double targetWidth, double targetHeight, string format, string customVarName = null)
        {
            if (!File.Exists(res.FilePath)) return $"// Error: File not found {res.FilePath}";

            // 如果没有提供自定义名称，则默认使用资源名
            string varName = customVarName ?? res.Name;

            BitmapImage sourceBmp = LoadBitmapNoLock(res.FilePath);

            // 强制转为 Bgra32
            var bgraBmp = new FormatConvertedBitmap(sourceBmp, PixelFormats.Bgra32, null, 0);

            // 缩放
            ScaleTransform scale = new ScaleTransform(targetWidth / bgraBmp.PixelWidth, targetHeight / bgraBmp.PixelHeight);
            TransformedBitmap scaledBmp = new TransformedBitmap(bgraBmp, scale);

            // 使用 varName 生成代码
            return ConvertToSglPixmapCode(scaledBmp, varName, format);
        }



        public static BitmapImage LoadBitmapNoLock(string path)
        {
            // 如果是相对路径，尝试基于当前工程目录转为绝对路径
            if (!Path.IsPathRooted(path))
            {

                path = Path.Combine(ProjectManager.ProjectDir, path);

                // 或者使用 ProjectManager 记录的当前工程根目录
            }

            if (!File.Exists(path)) return null;

            BitmapImage bitmap = new BitmapImage();
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                //bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            return bitmap;
        }
    }
}

