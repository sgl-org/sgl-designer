using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using static SglDesigner.ProjectManager;
namespace SglDesigner
{
    public static partial class ProjectManager
    {
        //还原实际目录使用
        public static string ProjectDir = "";

        public static SglProjectData Instance { get; set; } = new SglProjectData();
        public partial class SglProjectData : ObservableObject
        {
            [ObservableProperty] private string _version = "1.0.0.0";
            [ObservableProperty] private string _dateTime = "";
            [ObservableProperty] private string _saveBy = "SGL UI Designer";
            [ObservableProperty] private SglImageGenerator.ColorFormat _colorFormat = SglImageGenerator.ColorFormat.RGB565;
            [ObservableProperty] private int _width = 320;
            [ObservableProperty] private int _height = 240;

            [ObservableProperty] private string _exportFolder = "";
            [JsonProperty(Order = 7)]
            public List<SglPageData> ScreenList { get; set; } = new List<SglPageData>();
            [JsonProperty(Order = 8)]
            public List<SglResItem> ResourceList { get; set; } = new List<SglResItem>();
        }


        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            // 确保派生类（Slider, Progress等）的特有属性不丢失
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,

        };
        private static string SerializeProject(IEnumerable<SglPageData> screens, IEnumerable<SglResItem> resources)
        {


            ProjectManager.Instance.ScreenList = screens.ToList();
            ProjectManager.Instance.ResourceList = resources.ToList(); // 确保资源被存入 JSON
            return JsonConvert.SerializeObject(ProjectManager.Instance, Settings);
        }

        public static string SaveAs(IEnumerable<SglPageData> screens, IEnumerable<SglResItem> resources)
        {
            SaveFileDialog dlg = new SaveFileDialog { Filter = "SGL Project (*.json)|*.json" };
            if (dlg.ShowDialog() == true)
            {
                string json = SerializeProject(screens, resources);
                File.WriteAllText(dlg.FileName, json);
                return dlg.FileName;
            }
            return null;
        }
        public static string GetRelativePath(string relativeTo, string path)
        {
            // 如果路径是特殊的内置标识，直接返回原样
            if (string.IsNullOrEmpty(path) || path.StartsWith("Internal") || !Path.IsPathRooted(path))
            {
                return path;
            }

            try
            {
                string absoluteRelativeTo = Path.GetFullPath(relativeTo);
                string absolutePath = Path.GetFullPath(path);

                if (!absoluteRelativeTo.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    absoluteRelativeTo += Path.DirectorySeparatorChar;

                Uri baseUri = new Uri(absoluteRelativeTo, UriKind.Absolute);
                Uri targetUri = new Uri(absolutePath, UriKind.Absolute);

                Uri relativeUri = baseUri.MakeRelativeUri(targetUri);
                return Uri.UnescapeDataString(relativeUri.ToString()).Replace('/', Path.DirectorySeparatorChar);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"路径转换失败: {path}, 错误: {ex.Message}");
                return path; // 转换失败则保留原样，防止程序崩溃
            }
        }
        public static void DirectSave(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            string projectDir = Path.GetDirectoryName(filePath);

            foreach (var item in SglResManager.Resources)
            {
                if (item.Name == "Consolas") continue;

                // 将绝对路径转为相对路径存储，例如 C:\Project\assets\icon.png -> assets/icon.png
                if (!string.IsNullOrEmpty(item.FilePath))
                {
                    // 使用相对路径存入 JSON
                    item.FilePath = GetRelativePath(projectDir, item.FilePath);
                }

                if (!string.IsNullOrEmpty(item.SourceTtfPath))
                {
                    item.SourceTtfPath = GetRelativePath(projectDir, item.SourceTtfPath);
                }
            }
            ProjectManager.Instance.DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            ProjectManager.Instance.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            File.WriteAllText(filePath, SerializeProject(SglScreen.Instance.ScreenList, SglResManager.Resources));
        }

        // ---  加载逻辑 ---
        public static SglProjectData LoadProject(out string filePath)
        {
            filePath = null;
            OpenFileDialog dlg = new OpenFileDialog { Filter = "SGL Project (*.json)|*.json" };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    filePath = dlg.FileName;
                    ProjectDir = Path.GetDirectoryName(filePath);
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<SglProjectData>(json, Settings);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("工程加载失败: " + ex.Message);
                }
            }
            return null;
        }

    }

    public partial class MainWindow
    {
        
        public string _currentProjectPath = string.Empty; // 用于记录当前打开的文件路径

        private void UpdateTitleWithFileName()
        {
            if (!string.IsNullOrEmpty(_currentProjectPath))
            {
                string fileName = System.IO.Path.GetFileName(_currentProjectPath);
                this.Title = $"SglDesigner - {fileName} (已保存于 {DateTime.Now:HH:mm:ss})";
            }
        }

        private bool _isDirty = false; 

        private void SetProject_Click(object sender, RoutedEventArgs e)
        {
            var currentScreen = SglScreen.Instance.SelectedScreen;
            if (currentScreen == null) return;

            var settingsWin = new ProjectSettingsWindow(currentScreen);
            settingsWin.DataContext = currentScreen;
            settingsWin.Owner = this;

            if (settingsWin.ShowDialog() == true)
            {





                // 重新计算并绘制标尺与参考线
                UpdateRulers();

                // 居中显示
                ZoomToFit_Click(null, null);


                UpdateSglConfig(ProjectManager.Instance.Width, ProjectManager.Instance.Height);


                _isDirty = true;

                // 同时修改内部 Canvas
                WidgetContainer.Width = ProjectManager.Instance.Width;
                WidgetContainer.Height = ProjectManager.Instance.Height;

                SglScreen.Instance.SelectedScreen.Widgets[0].W = ProjectManager.Instance.Width;
                SglScreen.Instance.SelectedScreen.Widgets[0].H = ProjectManager.Instance.Height;

            }
        }
        public void UpdateSglConfig(int newWidth, int newHeight)
        {
            //获取程序路径
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(path, "simulator/sgl_port_sdl2.c"); ;

            try
            {
                // 读取文件的所有行
                string[] lines = File.ReadAllLines(filePath);

                for (int i = 0; i < lines.Length; i++)
                {
                    // 去掉首尾空格后判断是否以目标宏定义开头
                    string trimmedLine = lines[i].Trim();

                    if (trimmedLine.StartsWith("#define  CONFIG_SGL_PANEL_WIDTH"))
                    {
                        // 直接重写整行内容
                        lines[i] = $"#define  CONFIG_SGL_PANEL_WIDTH          {newWidth}";
                    }
                    else if (trimmedLine.StartsWith("#define  CONFIG_SGL_PANEL_HEIGHT"))
                    {
                        lines[i] = $"#define  CONFIG_SGL_PANEL_HEIGHT         {newHeight}";
                    }
                }

                // 2. 将修改后的行数组重新写回文件
                File.WriteAllLines(filePath, lines);

                Console.WriteLine("文件已直接按行更新成功！");
            }
            catch (Exception ex)
            {
                Console.WriteLine("发生错误: " + ex.Message);
            }
        }
        private void NewProject_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否需要提醒（如果有未保存的更改）
            if (_isDirty || SglScreen.Instance.ScreenList.Any(s => s.Widgets.Any()))
            {
                var result = MessageBox.Show("当前工程尚未保存，新建工程将丢失所有更改。是否继续？",
                                             "确认新建",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes) return;
            }

            // 2. 执行重置
            ResetProject();
        }

        private void ResetProject()
        {
            // --- 重置数据单例 ---
            var sglScreen = SglScreen.Instance;

            // 清空屏幕列表
            sglScreen.ScreenList.Clear();

            // 调用单例内部的 AddScreen() 重新初始化默认第一个屏幕
            sglScreen.AddScreen();

            // 选中第一个屏幕（这会自动触发 Messenger 发送 ScreenChangedMessage）
            sglScreen.SelectedScreen = sglScreen.ScreenList[0];

            // --- 重置全局状态 ---
            _currentProjectPath = string.Empty;
            _isDirty = false;

            // 更新 UI 标题
            this.Title = "SglDesigner - 新建工程";

            // 清空属性面板
            PropertyPanel.DataContext = null;


            // 强制清理画布
            WidgetContainer.Children.Clear();

            SglResManager.Resources.Clear();

            SglResManager.EnsureInternalResources();

            FontViewModel.Instance.RefreshFontList();
            FontViewModel.Instance.RefreshFontTTFList();

        }


        private void SaveProject_Click(object sender, RoutedEventArgs e)
        {
            // 确定保存路径
            if (string.IsNullOrEmpty(_currentProjectPath))
            {
                SaveFileDialog dlg = new SaveFileDialog { Filter = "SGL Project (*.json)|*.json" };
                if (dlg.ShowDialog() != true) return;
                _currentProjectPath = dlg.FileName;
            }

            string projectDir = Path.GetDirectoryName(_currentProjectPath);
            string assetsDir = Path.Combine(projectDir, "assets");
            if (!Directory.Exists(assetsDir)) Directory.CreateDirectory(assetsDir);

            // 2. 物理资源搬运
            foreach (var item in SglResManager.Resources)
            {
                // --- 核心保护：如果是内置资源，跳过所有路径搬运和转换逻辑 ---
                if (item.Name == "Consolas") continue;
                if (string.IsNullOrEmpty(item.FilePath) || !File.Exists(Path.Combine(projectDir, item.FilePath))) continue;

                // 获取文件名
                string fileName = Path.GetFileName(item.FilePath);
                string destPath = Path.Combine(assetsDir, fileName);
                string sourcePath = Path.Combine(projectDir, item.FilePath);

                // 如果当前文件不在 assets 目录下，则搬运
                if (Path.GetFullPath(sourcePath) != Path.GetFullPath(destPath))
                {
                    File.Copy(item.FilePath, destPath, true);
                    // 更新内存中的路径为最新的绝对路径，确保导出代码时能找到文件
                    item.FilePath = destPath;
                }

                //// 字体 TTF 同理
                //if (item.ResType == SglResType.FontCSource && !string.IsNullOrEmpty(item.SourceTtfPath))
                //{
                //    string ttfName = Path.GetFileName(item.SourceTtfPath);
                //    string ttfDest = Path.Combine(assetsDir, ttfName);
                //    if (Path.GetFullPath(item.SourceTtfPath) != Path.GetFullPath(ttfDest))
                //    {
                //        File.Copy(item.SourceTtfPath, ttfDest, true);
                //        item.SourceTtfPath = ttfDest;
                //    }
                //}
            }

            //  执行序列化保存
            // var screens = SglScreen.Instance.ScreenList;
            //var resources = SglResManager.Resources; // 拿到此时已经更新过路径的资源列表


            ProjectManager.DirectSave(_currentProjectPath);

            UpdateTitleWithFileName();
            MessageBox.Show("工程保存成功!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        }



        private void LoadProject_Click(object sender, RoutedEventArgs e)
        {


            OpenFileDialog dlg = new OpenFileDialog { Filter = "SGL Project (*.json)|*.json" };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string filePath = dlg.FileName;
                    ProjectDir = Path.GetDirectoryName(filePath);
                    string json = File.ReadAllText(filePath);
                    ProjectManager.Instance = JsonConvert.DeserializeObject<SglProjectData>(json, ProjectManager.Settings);

                    if (ProjectManager.Instance != null)
                    {
                        SglScreen.Instance.WidgetCount = 0;


                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        Console.WriteLine($"\n--- 开始加载工程 ---");
                        // 测量 JSON 解析时间
                        //ProjectManager.Instance = ProjectManager.LoadProject(out path);


                        Console.WriteLine($"JSON反序列化耗时: {sw.ElapsedMilliseconds} ms");

                        long lastMark = sw.ElapsedMilliseconds;
                        _currentProjectPath = filePath;
                        //string projectDir = Path.GetDirectoryName(filePath);

                        // 2. 资源池清理与内置资源恢复
                        SglResManager.Resources.Clear();
                        SglResManager.EnsureInternalResources();
                        Console.WriteLine($"资源池重置耗时: {sw.ElapsedMilliseconds - lastMark} ms");
                        lastMark = sw.ElapsedMilliseconds;



                        Console.WriteLine($"屏蔽属性面板耗时: {sw.ElapsedMilliseconds - lastMark} ms");
                        lastMark = sw.ElapsedMilliseconds;

                        // 清空当前所有资源，防止上一个工程的残留或重复累加
                        SglResManager.Resources.Clear();

                        // 2. 重新注入内置默认资源（确保 Consolas 等基础资源始终在第一位）
                        SglResManager.EnsureInternalResources();


                        //  资源路径还原并添加工程私有资源
                        foreach (var res in ProjectManager.Instance.ResourceList)
                        {
                            // 如果这个资源的名字叫 Consolas 且是 FontSource，说明它是工程保存进去的内置项
                            // 已经在 Step 2 注入过了，这里直接跳过，防止重复
                            if (res.Name == "Consolas" && res.ResType == SglResType.FontSource)
                                continue;

                            //// 路径还原逻辑
                            //if (!string.IsNullOrEmpty(res.FilePath))
                            //    res.FilePath = Path.GetFullPath(Path.Combine(projectDir, res.FilePath));

                            //if (!string.IsNullOrEmpty(res.SourceTtfPath))
                            //    res.SourceTtfPath = Path.GetFullPath(Path.Combine(projectDir, res.SourceTtfPath));

                            //// 二次查重保护（万一工程文件里有重复条目）
                            //if (SglResManager.Resources.Any(r => r.Name == res.Name))
                            //    continue;

                            SglResManager.Resources.Add(res);
                            Console.WriteLine($"{res.Name}->{res.FilePath}");
                        }

                        FontViewModel.Instance.RefreshFontList();
                        FontViewModel.Instance.RefreshFontTTFList();
                        ImageViewModel.Instance.RefreshImageList();

                        Console.WriteLine($"资源路径还原与加载耗时: {sw.ElapsedMilliseconds - lastMark} ms (共 {ProjectManager.Instance.ResourceList.Count} 个资源)");
                        lastMark = sw.ElapsedMilliseconds;

                        //  屏幕数据清理
                        var sglScreen = SglScreen.Instance;
                        sglScreen.ScreenList.Clear();
                        sglScreen.SelectedScreen = null;
                        Console.WriteLine($"清理旧屏幕数据耗时: {sw.ElapsedMilliseconds - lastMark} ms");
                        lastMark = sw.ElapsedMilliseconds;


                        //  核心逻辑：递归重建父级引用与树挂载
                        int widgetCount = 0;
                        foreach (var page in ProjectManager.Instance.ScreenList)
                        {
                            foreach (var rootWidget in page.Widgets)
                            {
                                RebuildParentReferences(rootWidget);

                            }
                            widgetCount++;
                            sglScreen.ScreenList.Add(page);
                        }
                        Console.WriteLine($"重建父子引用与集合挂载耗时: {sw.ElapsedMilliseconds - lastMark} ms (处理了 {widgetCount} 个根控件)");
                        lastMark = sw.ElapsedMilliseconds;

                        //  重新建立选中关系（可能触发 Canvas 的初始重绘）
                        if (sglScreen.ScreenList.Count > 0)
                        {
                            sglScreen.SelectedScreen = sglScreen.ScreenList[0];
                        }
                        // 同时修改内部 Canvas
                        WidgetContainer.Width = ProjectManager.Instance.Width;
                        WidgetContainer.Height = ProjectManager.Instance.Height;
                        //更新屏幕大小
                        SglScreen.Instance.SelectedScreen.Widgets[0].W = ProjectManager.Instance.Width;
                        SglScreen.Instance.SelectedScreen.Widgets[0].H = ProjectManager.Instance.Height;

                        Console.WriteLine($"建立选中关系耗时: {sw.ElapsedMilliseconds - lastMark} ms");
                        lastMark = sw.ElapsedMilliseconds;

                        //  全局 UI 更新触发
                        //GlobalUpdate();
                        Console.WriteLine($"GlobalUpdate (UI刷新) 耗时: {sw.ElapsedMilliseconds - lastMark} ms");
                        sw.Stop();
                        Console.WriteLine($"--- 加载结束 最终总耗时: {sw.ElapsedMilliseconds} ms ---\n");


                        MessageBox.Show($"项目加载完成！\n耗时：{sw.ElapsedMilliseconds} ms\n屏幕数：{widgetCount}\n控件数：{SglScreen.Instance.WidgetCount}个", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("工程加载失败: " + ex.Message);
                }
            }

        }
    }
}