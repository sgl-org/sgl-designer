using SharpFont;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SglDesigner
{
    public class SglFontGenerator
    {
        // 核心配置属性
        public int Bpp { get; set; } = 4; // 1, 2, 4, 8
        public bool Compress { get; set; } = false;
        public int FontSize { get; set; } = 24;
        public string FontName { get; set; } = "sgl_font";

        public struct Result
        {
            public string SourceCode;
            public byte[] BinaryData;
            public int EffectiveBpp;
            public int UncompressedBytes;
            public int CompressedBytes;
        }

        public Result Process(string ttfPath, string inputChars, bool isHasAscii)
        {
            int effectiveBpp = NormalizeOutputBpp(Bpp);

            using (var lib = new Library())
            using (var face = new Face(lib, ttfPath))
            {
                face.SetPixelSizes(0, (uint)FontSize);

                if (isHasAscii)
                {
                    string ascii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

                    inputChars += ascii;
                }

                // 预处理字符：去重并排序，确保生成的 Unicode 段是连续的
                var sortedChars = inputChars.Distinct().OrderBy(c => c).ToList();

                List<SglGlyph> glyphs = new List<SglGlyph>();
                List<byte> bitmapData = new List<byte>();
                uint currentOffset = 0;

                // 根据 BPP 选择渲染模式：1bpp 使用原生二值渲染+网格适配，>=2bpp 使用抗锯齿+自动微调
                bool useMono = effectiveBpp == 1;
                LoadTarget loadTarget = useMono ? LoadTarget.Mono : LoadTarget.Normal;
                RenderMode renderMode = useMono ? RenderMode.Mono : RenderMode.Normal;
                LoadFlags loadFlags = LoadFlags.ForceAutohint;

                foreach (char c in sortedChars)
                {
                    uint glyphIndex = face.GetCharIndex(c);
                    if (glyphIndex == 0) continue; // 字体中不存在此字符

                    face.LoadGlyph(glyphIndex, loadFlags, loadTarget);
                    face.Glyph.RenderGlyph(renderMode);

                    FTBitmap ftbmp = face.Glyph.Bitmap;

                    // 关键点：如果宽度还是0，说明是空白占位符（如空格）
                    // 但要确保图标不是因为渲染失败变成0
                    byte[] compressed = new byte[0];
                    byte[] packed = new byte[0];
                    if (ftbmp.Width > 0 && ftbmp.Rows > 0)
                    {
                        ProcessPixels(ftbmp, effectiveBpp, Compress, useMono, out packed, out compressed);
                    }
                    glyphs.Add(new SglGlyph
                    {
                        Char = c,
                        BitmapIndex = currentOffset,
                        AdvW = (int)(face.Glyph.Metrics.HorizontalAdvance.ToDouble() * 16),
                        BoxW = ftbmp.Width,
                        BoxH = ftbmp.Rows,
                        OfsX = face.Glyph.BitmapLeft,
                        OfsY = face.Glyph.BitmapTop - ftbmp.Rows,
                        Data = compressed,
                        RawData = packed
                    });

                    bitmapData.AddRange(compressed);
                    currentOffset += (uint)compressed.Length;
                }

                return new Result
                {
                    SourceCode = GenerateCSource(glyphs, bitmapData, effectiveBpp),
                    BinaryData = bitmapData.ToArray(), // 这里仅导出位图，如需完整bin可序列化结构体
                    EffectiveBpp = effectiveBpp,
                    UncompressedBytes = glyphs.Sum(g => g.RawData?.Length ?? 0),
                    CompressedBytes = glyphs.Sum(g => g.Data?.Length ?? 0)
                };
            }
        }

        // PackBits 定义
        // 将线性 8-bit 灰度像素压缩为指定 BPP 的位流
        public static byte[] PackBits(byte[] src, int bpp)
        {
            if (src == null || src.Length == 0) return new byte[0];
            if (bpp != 1 && bpp != 2 && bpp != 4 && bpp != 8)
                throw new ArgumentOutOfRangeException(nameof(bpp), "Bpp only supports 1, 2, 4, 8.");

            if (bpp == 8)
                return (byte[])src.Clone();

            int pixelsPerByte = 8 / bpp;
            int len = (src.Length + pixelsPerByte - 1) / pixelsPerByte;
            byte[] dest = new byte[len];
            int maxValue = (1 << bpp) - 1;

            for (int i = 0; i < src.Length; i++)
            {
                byte val = QuantizePixel(src[i], maxValue);
                int byteIdx = i / pixelsPerByte;
                int shift = (pixelsPerByte - 1 - (i % pixelsPerByte)) * bpp;
                dest[byteIdx] |= (byte)(val << shift);
            }
            return dest;
        }

        // 获取图标库列表
        public List<char> GetFontIcons(string ttfPath)
        {
            var icons = new List<char>();
            using (var lib = new Library())
            using (var face = new Face(lib, ttfPath))
            {
                uint gindex;
                uint charcode = face.GetFirstChar(out gindex);
                while (gindex != 0)
                {
                    if (charcode > 32) icons.Add((char)charcode);
                    charcode = face.GetNextChar(charcode, out gindex);
                }
            }
            return icons;
        }


        private void ProcessPixels(FTBitmap ftbmp, int effectiveBpp, bool compress, bool isMono, out byte[] packed, out byte[] output)
        {
            int width = ftbmp.Width;
            int rows = ftbmp.Rows;

            if (width == 0 || rows == 0 || ftbmp.Buffer == IntPtr.Zero)
            {
                packed = new byte[0];
                output = new byte[0];
                return;
            }

            byte[] raw = ftbmp.BufferData;
            int pitch = Math.Abs(ftbmp.Pitch);
            byte[] grayscalePixels = new byte[width * rows];

            if (isMono)
            {
                // 二值位图：每个 bit 是一个像素，8 个像素打包为 1 字节。
                // 解包为 0 或 255 的灰度数组，复用后续量化管线。
                for (int y = 0; y < rows; y++)
                {
                    int srcRow = y * pitch;
                    int dstRow = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        int byteIdx = srcRow + (x >> 3);
                        int bitIdx = 7 - (x & 7);
                        int val = (byteIdx < raw.Length && ((raw[byteIdx] >> bitIdx) & 1) != 0) ? 255 : 0;
                        grayscalePixels[dstRow + x] = (byte)val;
                    }
                }
            }
            else
            {
                // 抗锯齿灰度：1 字节/像素，按 pitch 逐行拷贝
                for (int y = 0; y < rows; y++)
                {
                    int srcRow = y * pitch;
                    int dstRow = y * width;
                    int copyCount = Math.Min(width, raw.Length - srcRow);
                    if (copyCount <= 0)
                        break;
                    Array.Copy(raw, srcRow, grayscalePixels, dstRow, copyCount);
                }
            }

            byte[] quantizedPixels = QuantizePixels(grayscalePixels, effectiveBpp);
            packed = PackQuantizedValues(quantizedPixels, effectiveBpp);
            output = compress ? EncodeRle(quantizedPixels, effectiveBpp) : packed;
        }

        private static byte QuantizePixel(byte value, int maxValue)
        {
            if (maxValue <= 1)
                return value >= 128 ? (byte)1 : (byte)0;

            // Gamma 校正量化：用 gamma=0.7 提亮中间调，
            // 让抗锯齿边缘的浅色像素 (value 8~32) 不至于被完全丢弃，
            // 从而保留细笔画和字形细节。
            float normalized = value / 255.0f;
            float gammaCorrected = (float)Math.Pow(normalized, 0.7);
            int result = (int)(gammaCorrected * maxValue + 0.5f);
            return (byte)Math.Max(0, Math.Min(maxValue, result));
        }

        private static byte[] QuantizePixels(byte[] grayscalePixels, int bpp)
        {
            if (grayscalePixels == null || grayscalePixels.Length == 0)
                return new byte[0];

            if (bpp == 8)
                return (byte[])grayscalePixels.Clone();

            int maxValue = (1 << bpp) - 1;
            byte[] quantized = new byte[grayscalePixels.Length];
            for (int i = 0; i < grayscalePixels.Length; i++)
                quantized[i] = QuantizePixel(grayscalePixels[i], maxValue);

            return quantized;
        }

        private static byte[] PackQuantizedValues(byte[] src, int bpp)
        {
            if (src == null || src.Length == 0) return new byte[0];
            if (bpp != 1 && bpp != 2 && bpp != 4 && bpp != 8)
                throw new ArgumentOutOfRangeException(nameof(bpp), "Bpp only supports 1, 2, 4, 8.");

            if (bpp == 8)
                return (byte[])src.Clone();

            int pixelsPerByte = 8 / bpp;
            int len = (src.Length + pixelsPerByte - 1) / pixelsPerByte;
            byte[] dest = new byte[len];

            for (int i = 0; i < src.Length; i++)
            {
                int byteIdx = i / pixelsPerByte;
                int shift = (pixelsPerByte - 1 - (i % pixelsPerByte)) * bpp;
                dest[byteIdx] |= (byte)(src[i] << shift);
            }

            return dest;
        }

        private static byte[] EncodeRle(byte[] quantizedPixels, int bpp)
        {
            if (quantizedPixels == null || quantizedPixels.Length == 0)
                return new byte[0];

            var writer = new BitWriter();
            writer.WriteBits(quantizedPixels[0], bpp);

            int i = 1;
            while (i < quantizedPixels.Length)
            {
                byte current = quantizedPixels[i];
                writer.WriteBits(current, bpp);

                if (current != quantizedPixels[i - 1])
                {
                    i++;
                    continue;
                }

                int runEnd = i + 1;
                while (runEnd < quantizedPixels.Length && quantizedPixels[runEnd] == current)
                    runEnd++;

                int extraRepeatCount = runEnd - (i + 1);
                while (extraRepeatCount > 73)
                {
                    int continuationPixels = Math.Min(75, extraRepeatCount);
                    EncodeRepeatFinish(writer, continuationPixels - 2, bpp, true, current);
                    writer.WriteBits(current, bpp);
                    extraRepeatCount -= continuationPixels;
                }

                bool hasNext = runEnd < quantizedPixels.Length;
                EncodeRepeatFinish(writer, extraRepeatCount, bpp, hasNext, hasNext ? quantizedPixels[runEnd] : (byte)0);
                i = hasNext ? runEnd + 1 : runEnd;
            }

            return writer.ToArray();
        }

        private static void EncodeRepeatFinish(BitWriter writer, int extraRepeatCount, int bpp, bool hasNext, byte nextValue)
        {
            if (extraRepeatCount < 0)
                throw new ArgumentOutOfRangeException(nameof(extraRepeatCount));

            if (extraRepeatCount <= 10)
            {
                for (int i = 0; i < extraRepeatCount; i++)
                    writer.WriteBit(true);

                if (hasNext)
                {
                    writer.WriteBit(false);
                    writer.WriteBits(nextValue, bpp);
                }
                return;
            }

            for (int i = 0; i < 11; i++)
                writer.WriteBit(true);

            writer.WriteBits(extraRepeatCount - 10, 6);

            if (hasNext)
                writer.WriteBits(nextValue, bpp);
        }

        private static int NormalizeOutputBpp(int requestedBpp)
        {
            switch (requestedBpp)
            {
                case 1:
                case 2:
                case 4:
                    return requestedBpp;
                case 8:
                    // Current SGL font renderer has no 8bpp decode path, fallback to 4bpp.
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(requestedBpp), "Bpp only supports 1, 2, 4, 8.");
            }
        }

        private string GenerateCSource(List<SglGlyph> glyphs, List<byte> bitmap, int effectiveBpp)
        {
            StringBuilder sb = new StringBuilder();
            string nameUpper = FontName.ToUpper();

            sb.AppendLine($"#include <sgl_core.h>\n#include <sgl_font.h>\n");
            sb.AppendLine($"#ifndef CONFIG_SGL_FONT_{nameUpper}\n#define CONFIG_SGL_FONT_{nameUpper} 1\n#endif\n");
            sb.AppendLine($"#if (CONFIG_SGL_FONT_{nameUpper})");

            if (Bpp != effectiveBpp)
                sb.AppendLine($"/* Requested {Bpp}bpp, generated as {effectiveBpp}bpp because current SGL font renderer does not support {Bpp}bpp fonts. */");
            if (Compress)
                sb.AppendLine("/* Font RLE compression enabled. Requires CONFIG_SGL_FONT_COMPRESSED in SGL. */");

            // Bitmap
            sb.AppendLine($"static const uint8_t font_bitmap[] = {{");
            for (int i = 0; i < bitmap.Count; i++)
            {
                if (i % 12 == 0) sb.Append("    ");
                sb.Append($"0x{bitmap[i]:x2}, ");
                if (i % 12 == 11) sb.AppendLine();
            }
            sb.AppendLine("\n};");

            // 2. Table
            sb.AppendLine("\nstatic const sgl_font_table_t font_table[] = {");
            sb.AppendLine("    {.bitmap_index = 0, .adv_w = 0, .box_w = 0, .box_h = 0, .ofs_x = 0, .ofs_y = 0},");
            foreach (var g in glyphs)
                sb.AppendLine($"    {{.bitmap_index = {g.BitmapIndex}, .adv_w = {g.AdvW}, .box_w = {g.BoxW}, .box_h = {g.BoxH}, .ofs_x = {g.OfsX}, .ofs_y = {g.OfsY}}}, /* U+{(int)g.Char:X4} */");
            sb.AppendLine("};");

            //  自动计算 Unicode 范围 (处理不连续)
            var ranges = GetRanges(glyphs);
            sb.AppendLine("\nstatic const sgl_font_unicode_t font_unicode[] = {");
            foreach (var r in ranges)
                sb.AppendLine($"    {{ .offset = 0x{r.Start:X4}, .len = {r.Len}, .list = NULL, .tab_offset = {r.TabOffset} }},");
            sb.AppendLine("};");

            //  Object
            sb.AppendLine($"\nconst sgl_font_t {FontName} = {{");
            sb.AppendLine($"    .bitmap = font_bitmap,");
            sb.AppendLine($"    .table = font_table,");
            sb.AppendLine($"    .font_table_size = SGL_ARRAY_SIZE(font_table),");
            sb.AppendLine($"    .font_height = {FontSize},");
            sb.AppendLine($"    .base_line = {FontSize / 4},");
            sb.AppendLine($"    .bpp = {effectiveBpp},");
            sb.AppendLine($"    .compress = {(Compress ? 1 : 0)},");
            sb.AppendLine($"    .unicode = font_unicode, .unicode_num = SGL_ARRAY_SIZE(font_unicode),");
            sb.AppendLine("};");
            sb.AppendLine("#endif");

            return sb.ToString();
        }

        private List<RangeSegment> GetRanges(List<SglGlyph> glyphs)
        {
            var res = new List<RangeSegment>();
            if (glyphs.Count == 0) return res;
            int start = 0;
            for (int i = 1; i <= glyphs.Count; i++)
            {
                if (i == glyphs.Count || glyphs[i].Char != glyphs[i - 1].Char + 1)
                {
                    res.Add(new RangeSegment { Start = glyphs[start].Char, Len = i - start, TabOffset = start + 1 });
                    start = i;
                }
            }
            return res;
        }

        private class RangeSegment { public int Start, Len, TabOffset; }
        private class SglGlyph { public char Char; public uint BitmapIndex; public int AdvW, BoxW, BoxH, OfsX, OfsY; public byte[] Data; public byte[] RawData; }

        private sealed class BitWriter
        {
            private readonly List<byte> _bytes = new List<byte>();
            private int _bitPos;

            public void WriteBit(bool bit)
            {
                WriteBits((uint)(bit ? 1 : 0), 1);
            }

            public void WriteBits(int value, int bitCount)
            {
                WriteBits((uint)value, bitCount);
            }

            public void WriteBits(uint value, int bitCount)
            {
                for (int i = bitCount - 1; i >= 0; i--)
                {
                    if (_bitPos == 0)
                        _bytes.Add(0);

                    if (((value >> i) & 0x1) != 0)
                    {
                        int last = _bytes.Count - 1;
                        _bytes[last] |= (byte)(1 << (7 - _bitPos));
                    }

                    _bitPos = (_bitPos + 1) & 0x7;
                }
            }

            public byte[] ToArray()
            {
                return _bytes.ToArray();
            }
        }
    }
}
