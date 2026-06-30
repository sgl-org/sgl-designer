using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SglDesigner
{
    // 资源类型枚举
    public enum SglResType { Image, FontSource, FontCSource } // 增加 FontCSource

    // 资源项模型
    public class SglResItem : ObservableObject
    {
        public string Name { get; set; }        // 变量名：sgl_font16
        public int FontSize { get; set; } // 记录该字库取模时的字号
        public string FilePath { get; set; }    // 当前显示的路径：取模后应为 .c 路径
        public string SourceTtfPath { get; set; } // 隐藏属性：记录原始 .ttf 路径，用于预览渲染
        public SglResType ResType { get; set; }
        public string AllowedChars { get; set; } // 字符集
        public bool FontCompressed { get; set; } // 字体是否启用 RLE 压缩
        //public string GeneratedCode { get; set; } // 暂存生成的 C 代码字符串

        // 用于 UI 显示的预览属性
        public ImageSource ImagePreview
        {
            get
            {
                if (ResType != SglResType.Image || string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
                    return null;

                try
                {
                    // 使用内存流加载，避免锁定文件
                    BitmapImage bitmap = new BitmapImage();
                    using (FileStream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad; // 加载后立即释放流
                        bitmap.StreamSource = stream;
                        bitmap.EndInit();
                    }
                    bitmap.Freeze(); // 跨线程可用且性能更好
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }
        }
        // UI 显示优化：如果是 .c 资源，显示一个特殊的图标
        public string DisplayName => ResType == SglResType.FontCSource ? $"{Name}.c" : Name;


        // 重写 ToString，这样 ComboBox 默认就会显示这个字符串
        public override string ToString()
        {
            return ResType == SglResType.FontCSource ? $"{Name}.c" : Name;
        }

    }
    public class FontViewModel : ObservableObject
    {
        // 单例实例
        public static FontViewModel Instance { get; } = new FontViewModel();

        // 提供给 UI 绑定的属性：合并了内置和用户生成的字体
        public IEnumerable<string> FontOptions
        {
            get
            {
                // 获取内置字体名字列表
                var list = new List<string>(SglResManager.BuiltInFonts);

                // 2. 获取用户生成的 C 代码字体资源名
                var userFonts = SglResManager.Resources
                                .Where(r => r.ResType == SglResType.FontCSource)
                                .Select(r => r.Name);

                //  合并并去重
                return list.Concat(userFonts).Distinct();
            }
        }

        // 当用户生成了新字体（ProcessFontInternal 执行完）时调用
        public void RefreshFontList()
        {
            // 发出通知，UI 会重新执行 FontOptions 的 get 块
            OnPropertyChanged(nameof(FontOptions));
        }

        //取模时用的字体
        // 获取所有需要参与代码生成的字体资源对象
        public IEnumerable<SglResItem> FontTTF
        {
            get
            {
                // 过滤出类型为 FontCSource 的资源
                // 如果内置字体也有对应的 ResItem，一并包含进去
                return SglResManager.Resources
                       .Where(r => r.ResType == SglResType.FontSource);
            }
        }
        public void RefreshFontTTFList()
        {
            // 发出通知，UI 会重新执行 FontOptions 的 get 块
            OnPropertyChanged(nameof(FontTTF));
        }
    }
    public class ImageViewModel : ObservableObject
    {
        // 单例实例，方便 XAML 直接访问
        public static ImageViewModel Instance { get; } = new ImageViewModel();

        // 提供给 UI 绑定的图片资源列表
        public IEnumerable<SglResItem> ImageOptions
        {
            get
            {
                // 创建一个包含 null 选项的列表
                var options = new List<SglResItem> { new SglResItem() { Name = "" } };

                // 添加所有图片资源
                options.AddRange(SglResManager.Resources
                    .Where(r => r.ResType == SglResType.Image)
                    .ToList());

                return options;
            }
        }

        // 当用户导入新图片、删除图片或重命名资源时调用此方法
        public void RefreshImageList()
        {
            OnPropertyChanged(nameof(ImageOptions));
        }
    }
    // 全局资源管理器
    public static class SglResManager
    {
        public static ObservableCollection<SglResItem> Resources { get; } = new();

        // 临时缓存路径：AppData/Local/Temp/SglDesigner/Resources
        public static string TempDir = Path.Combine(Path.GetTempPath(), "SglDesigner", "Resources");

        // 定义 MCU 固件内置的字体名称
        public static readonly List<string> BuiltInFonts = new()
        {
            "consolas14",
            "consolas23",
            "consolas24",
            "consolas32"
        };

        // 2. 默认选中的字体
        public const string DefaultFont = "consolas14";




        // 定义内置字体的唯一标识
        public const string InternalFontName = "CONSOLAS";
        static SglResManager()
        {
            if (!Directory.Exists(TempDir)) Directory.CreateDirectory(TempDir);



        }


        public static void EnsureInternalResources()
        {
            // 注入一个通用的 Consolas 源字体项 ---
            if (!Resources.Any(r => r.Name == "Consolas" && r.ResType == SglResType.FontSource))
            {
                Resources.Add(new SglResItem
                {
                    Name = "Consolas",
                    ResType = SglResType.FontSource, // 设为源类型，供下拉框过滤显示
                    FilePath = "INTERNAL_RESOURCE(内置默认字体源)",
                    FontSize = 0 
                });
            }



        }



    public static void AddResource(string originalPath, SglResType type, string customName = null)
    {
        string fileName = Path.GetFileName(originalPath);
        // 1. 获取初始名称
        string rawVarName = customName ?? Path.GetFileNameWithoutExtension(originalPath).ToLower();

        // 2. 过滤为符合C语言规范的变量名
        string varName = SanitizeForCIdentifier(rawVarName);

        string targetPath = Path.Combine(TempDir, fileName);

        File.Copy(originalPath, targetPath, true);

        if (!Resources.Any(r => r.Name == varName))
        {
            Resources.Add(new SglResItem
            {
                Name = varName,
                FilePath = targetPath,
                ResType = type
            });
        }
    }

    /// <summary>
    /// 将字符串转换为符合C语言规范的标识符
    /// </summary>
    private static string SanitizeForCIdentifier(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "_";

        // 使用正则表达式，将所有 非字母、非数字、非下划线 的字符替换为 "_"
        string sanitized = Regex.Replace(input, @"[^a-zA-Z0-9_]", "_");

        // C语言变量名不能以数字开头，如果是数字开头，则在前面追加一个下划线
        if (char.IsDigit(sanitized[0]))
        {
            sanitized = "_" + sanitized;
        }



        return sanitized;
    }
    public static void DeleteResource(SglResItem item)
        {
            if (item == null) return;
            Resources.Remove(item);
            if (File.Exists(item.FilePath)) File.Delete(item.FilePath);
        }
        public static FontFamily LoadFontFromFile(string filePath)
        {
            // 获取字体文件中的第一个字体族名称
            var fonts = Fonts.GetFontFamilies(new Uri("file:///" + Path.GetDirectoryName(filePath) + "/"), "./");
            foreach (var family in fonts)
            {
                return family; // 通常一个 ttf 只有一个族
            }
            return new FontFamily("Consolas");
        }



    }


    public static class SglFontValidator
    {
        // 默认包含所有可见 ASCII 字符 (32-126)
        private static string DefaultAscii = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";

        public static string GetValidPreviewText(string rawText, string fontVarName)
        {
            if (string.IsNullOrEmpty(rawText)) return "";
            //if (fontVarName == "sgl_font_default") return rawText; // 系统默认字体不限制

            // 从资源管理器找到该字体对应的“生成配置”
            // 注意：需要给 SglResItem 增加一个 AllowedChars 属性来记录生成时的 CustomCharsTextBox 内容
            var res = SglResManager.Resources.FirstOrDefault(r => r.Name == fontVarName);
            if (res == null || string.IsNullOrEmpty(res.AllowedChars))
                return FilterText(rawText, DefaultAscii);

            string allowed = DefaultAscii + res.AllowedChars;
            return FilterText(rawText, allowed);
        }

        private static string FilterText(string input, string allowed)
        {
            // 如果不在允许范围内，替换为 □ (u25FB) 或者直接移除
            char[] result = input.Select(c => allowed.Contains(c) ? c : '□').ToArray();
            return new string(result);
        }
    }
}
