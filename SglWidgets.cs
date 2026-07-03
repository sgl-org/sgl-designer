using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Xceed.Wpf.Toolkit;
using MessageBox = System.Windows.MessageBox;
using Path = System.IO.Path;

namespace SglDesigner
{


    // 使用 partial 关键字，编译器会自动把这个文件和 MainWindow.xaml.cs 合并
    public partial class MainWindow
    {
        // 将其定义为公共属性
        public string[] AlignOptions { get; } = new string[] {
                "居中 (CENTER)",          // 0
                "上中 (TOP_MID)",        // 1
                "左上 (TOP_LEFT)",       // 2
                "右上 (TOP_RIGHT)",      // 3
                "下中 (BOT_MID)",        // 4
                "左下 (BOT_LEFT)",       // 5
                "右下 (BOT_RIGHT)",      // 6
                "左中 (LEFT_MID)",       // 7
                "右中 (RIGHT_MID)",      // 8
                "垂直左 (VERT_LEFT)",    // 9
                "垂直右 (VERT_RIGHT)",   // 10
                "垂直中 (VERT_MID)",     // 11
                "水平上 (HORIZ_TOP)",    // 12
                "水平下 (HORIZ_BOT)",    // 13
                "水平中 (HORIZ_MID)"     // 14
        };
        private void CreateWidget(SglMapping.SglType type, Color bgColor)
        {
            RecordBeforeChange();
            SglWidgetData data;

            // 根据类型实例化特定的 Data 对象
            switch (type)
            {
                case SglMapping.SglType.Button:
                    data = new SglButtonData();
                    break;
                case SglMapping.SglType.Label:
                    data = new SglLabelData();
                    break;
                case SglMapping.SglType.Progress:
                    data = new SglProgressData();
                    break;
                case SglMapping.SglType.Bar:
                    data = new SglBarData();
                    break;
                case SglMapping.SglType.Switch:
                    data = new SglSwitchData();
                    break;
                case SglMapping.SglType.CheckBox:
                    data = new SglCheckboxData();
                    break;
                case SglMapping.SglType.Led:
                    data = new SglLedData();
                    break;
                case SglMapping.SglType.Arc:
                    data = new SglArcData();
                    break;
                case SglMapping.SglType.Slider:
                    data = new SglSliderData();
                    break;
                case SglMapping.SglType.TextBox:
                    data = new SglTextBoxData();
                    break;
                case SglMapping.SglType.Line:
                    data = new SglLineData();
                    break;
                case SglMapping.SglType.Icon:
                    data = new SglIconData();
                    break;
                case SglMapping.SglType.Img_Ext:
                    data = new SglExtImageData();
                    break;
                case SglMapping.SglType.Rectangle:
                    data = new SglPanelData();
                    break;
                case SglMapping.SglType.Box:
                    data = new SglBoxData();
                    break;
                case SglMapping.SglType.Polygon:
                    data = SglPolygonData.CreateWithDefaults();//不能在类里初始化，否则重复添加
                    break;
                case SglMapping.SglType.Dropdown:
                    data = SglDropdownData.CreateDefaultOptions();
                    break;
                case SglMapping.SglType.Roller:
                    data = new SglRollerData();
                    break;
                case SglMapping.SglType.MsgBox:
                    data = new SglMsgBoxData();
                    break;
                case SglMapping.SglType.Circle:
                    data = new SglCircleData();
                    break;
                case SglMapping.SglType.Ring:
                    data = new SglRingData();
                    break;
                case SglMapping.SglType.Keyboard:
                    data = new SglKeyboardData();
                    break;
                case SglMapping.SglType.NumberKbd:
                    data = new SglNumberKbdData();
                    break;
                case SglMapping.SglType.Barchart:
                    data = new SglBarchartData();
                    break;
                case SglMapping.SglType.Piechart:
                    data = new SglPiechartData();
                    break;
                case SglMapping.SglType.Linechart:
                    data = new SglLinechartData();
                    break;
                case SglMapping.SglType.Qrcode:
                    data = new SglQrcodeData();
                    break;
                case SglMapping.SglType.Win:
                    data = new SglWinData();
                    break;
                case SglMapping.SglType.Statusbar:
                    data = new SglStatusbarData();
                    break;
                case SglMapping.SglType.Viewlist:
                    data = new SglViewlistData();
                    break;
                default:
                    data = new SglWidgetData();
                    break;
            }

            // --- 确定目标父容器 ---
            SglPageData selectedPage = SglScreen.Instance.SelectedScreen;
            if (selectedPage == null) return;

            // 默认目标是屏幕的根容器（Parent == null 的那个）
            SglWidgetData targetContainer = selectedPage.Widgets.FirstOrDefault(w => w.Parent == null);

            // 如果当前选中的是容器，则切换目标
            if (_selectedWidgets.Count == 1)
            {
                var selectedData = _selectedWidgets.First().DataContext as SglWidgetData;
                if (selectedData != null && selectedData.IsContainer)
                {
                    targetContainer = selectedData;
                }
                else if (_selectedWidgets.First().DataContext is SglWidgetData widget)
                {
                    // 如果选中的是普通控件，则添加到该控件所属的父容器
                    targetContainer = widget.Parent ?? targetContainer;
                }
            }

            if (targetContainer == null) return;

            // --- 设置属性并挂载 ---
            data.Id = GetNextAvailableId(type);
            data.Tag = data.Id;
            data.Type = type;
            data.Parent = targetContainer;


            // 如果是在容器内创建，可以设为 (0,0) 或居中
            data.X = targetContainer.W / 2 - data.W / 2;
            data.Y = targetContainer.H / 2 - data.H / 2;

            // 防止算出来的坐标是负数（比如控件比容器还大）
            if (data.X < 0) data.X = 0;
            if (data.Y < 0) data.Y = 0;


            targetContainer.Children.Add(data);

            RefreshCanvas(selectedPage);

            SglScreen.Instance.WidgetCount = SglScreen.GetAllWidgetsCount();
            //// 自动选中新创建的控件，方便用户立即编辑
            //// 延时一下确保 UI 已经生成
            //Dispatcher.BeginInvoke(new Action(() =>
            //{
            //    var newUI = FindUIByData(data); // 要一个通过 Data 找 Border 的方法
            //    if (newUI != null) SelectWidget(newUI);
            //}), System.Windows.Threading.DispatcherPriority.Loaded);
        }
        public FrameworkElement CreateVisualElement(SglWidgetData data, bool isBatchLoading = false)
        {
            //  创建 UI (Border)
            Border b = new Border
            {
                DataContext = data, // 绑定新的 Data 对象
                Tag = data,         // 保持兼容性
                Background = Brushes.Transparent,
                // 如果是容器类型，内部需要一个容器承载子元素
                Child = data.IsContainer ? new Canvas() : null,
                IsHitTestVisible = false
            };
            //画面对象绑定SglWidgetData,属性栏也要绑定  SglWidgetData

            //  执行绑定
            BaseBinder.BindBase(b, data);
            if (data is SglLabelData lbl) BaseBinder.BindLabel(b, lbl);
            if (data is SglButtonData btn) BaseBinder.BindButton(b, btn);
            if (data is SglBarData barData) BaseBinder.BindBar(b, barData);
            else if (data is SglProgressData pgb) BaseBinder.BindProgress(b, pgb);
            if (data is SglSwitchData sw) BaseBinder.BindSwitch(b, sw);
            if (data is SglCheckboxData cb) BaseBinder.BindCheckbox(b, cb);
            if (data is SglLedData led) BaseBinder.BindLed(b, led);
            if (data is SglArcData arc) BaseBinder.BindArc(b, arc);
            if (data is SglSliderData slider) BaseBinder.BindSlider(b, slider);
            if (data is SglTextBoxData tbx) BaseBinder.BindTextBox(b, tbx);
            if (data is SglLineData line) BaseBinder.BindLine(b, line);
            if (data is SglIconData Icon) BaseBinder.BindIcon(b, Icon);
            if (data is SglExtImageData img) BaseBinder.BindExtImage(b, img);
            if (data is SglQrcodeData qrcode) BaseBinder.BindQrcode(b, qrcode);
            if (data is SglPolygonData polygon) BaseBinder.BindPolygon(b, polygon);
            if (data is SglMsgBoxData msgbox) BaseBinder.BindMsgBox(b, msgbox);
            if (data is SglWinData win) BaseBinder.BindWin(b, win);
            if (data is SglStatusbarData sb) BaseBinder.BindStatusbar(b, sb);
            if (data is SglViewlistData vl) BaseBinder.BindViewlist(b, vl);
            if (data is SglDropdownData dropdown) BaseBinder.BindDropdown(b, dropdown);
            if (data is SglRollerData roller) BaseBinder.BindRoller(b, roller);
            if (data is SglCircleData circle) BaseBinder.BindCircle(b, circle);
            if (data is SglRingData ring) BaseBinder.BindRing(b, ring);
            if (data is SglKeyboardData kb) BaseBinder.BindKeyboard(b, kb);
            if (data is SglNumberKbdData kbd) BaseBinder.BindNumberKbd(b, kbd);
            if (data is SglBarchartData bar)
            {
                BaseBinder.BindBarchart(b, bar);
                bar.RefreshRequested += (d) =>
                {
                    // 必须在 UI 线程执行
                    Application.Current.Dispatcher.Invoke(() =>
                      {
                          // 重新绑定，这会触发 BindBarchart 内部的 canvas.Children.Clear() 逻辑
                          BaseBinder.BindBarchart(b, d);
                      });
                };
            }
            if (data is SglPiechartData pie) BaseBinder.BindPiechart(b, pie);
            if (data is SglLinechartData lchart)
            {
                BaseBinder.BindLinechart(b, lchart);
                lchart.RefreshRequested += (d) =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
            {
                BaseBinder.BindLinechart(b, d);
            });
                };
            }
            if (data is SglBoxData box) BaseBinder.BindBox(b, box);
            else if (data is SglPanelData panel) BaseBinder.BindPanel(b, panel);
            

            // 根容器不裁剪子元素，允许移出后仍可见并拖回。
            if (data.Parent == null && b.Child is Canvas rootCanvas)
            {
                b.ClipToBounds = false;
                rootCanvas.ClipToBounds = false;
            }


            b.MouseLeftButtonDown += Viewport_MouseDown;
            b.MouseLeftButtonUp += Viewport_MouseUp;
            b.MouseMove += Viewport_MouseMove;

            // --- 加载工程时不选中 ---
            if (!isBatchLoading)
            {
                SelectWidget(b, false);
            }



            return b;

        }
        private string GetNextAvailableId(SglMapping.SglType type)
        {
            string prefix = type.ToString().ToLower();

            // --- 使用递归函数获取全工程所有层级的 ID ---
            var allWidgets = SglScreen.Instance.ScreenList
        .SelectMany(s => GetAllWidgetsRecursive(s.Widgets)); // 递归展开

            var allUsedIndices = allWidgets
        .Where(w => w.Type == type && !string.IsNullOrEmpty(w.Id) && w.Id.StartsWith(prefix))
        .Select(w =>
        {
            string numPart = w.Id.Substring(prefix.Length);
            return int.TryParse(numPart, out int index) ? index : -1;
        })
        .Where(i => i >= 0)
        .Distinct()
        .OrderBy(i => i)
        .ToList();

            int nextIndex = 0;
            foreach (int index in allUsedIndices)
            {
                if (index == nextIndex) nextIndex++;
                else break;
            }

            return prefix + nextIndex;
        }

        // 辅助递归方法：将嵌套的树结构打平为单层列表
        private IEnumerable<SglWidgetData> GetAllWidgetsRecursive(IEnumerable<SglWidgetData> widgets)
        {
            foreach (var w in widgets)
            {
                yield return w; // 返回当前控件

                // 如果是容器，递归返回其子控件
                if (w.Children != null && w.Children.Any())
                {
                    foreach (var child in GetAllWidgetsRecursive(w.Children))
                    {
                        yield return child;
                    }
                }
            }
        }
        private void Align_Click(object sender, RoutedEventArgs e)
        {
            RecordBeforeChange();
            AdornerLayer.Children.Clear(); // 强制先清理一次
            LayoutHelper.Align(_selectedWidgets, (sender as FrameworkElement).Tag.ToString());
            //GlobalUpdate(); // 这里会重新调用 UpdateAdornerLayer 绘制新位置
            UpdateAdornerLayer();
        }
        // --- 点击事件路由 ---

        private void AddButton_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Button, Color.FromRgb(45, 45, 45));

        private void AddLabel_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Label, Colors.Transparent);

        private void AddCheckBox_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.CheckBox, Color.FromRgb(45, 45, 45));
        private void AddSwitch_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Switch, Color.FromRgb(45, 45, 45));
        private void AddLed_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Led, Color.FromRgb(45, 45, 45));
        private void AddArc_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Arc, Color.FromRgb(45, 45, 45));
        private void AddSlider_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Slider, Color.FromRgb(45, 45, 45));
        private void AddTextBox_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.TextBox, Color.FromRgb(45, 45, 45));
        private void AddLine_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Line, Color.FromRgb(45, 45, 45));
        private void AddIcon_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Icon, Color.FromRgb(45, 45, 45));
        private void AddImage_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Img_Ext, Color.FromRgb(45, 45, 45));
        private void AddPolygon_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Polygon, Color.FromRgb(45, 45, 45));
        private void AddMsgBox_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.MsgBox, Color.FromRgb(45, 45, 45));
        private void AddPanel_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Rectangle, Color.FromRgb(45, 45, 45));
        private void AddBox_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Box, Color.FromRgb(45, 45, 45));
        private void AddDropdown_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Dropdown, Color.FromRgb(45, 45, 45));
        private void AddRoller_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Roller, Color.FromRgb(45, 45, 48));
        private void AddCircle_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Circle, Color.FromRgb(45, 45, 45));
        private void AddRing_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Ring, Color.FromRgb(45, 45, 45));
        private void AddKeyboard_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Keyboard, Color.FromRgb(45, 45, 45));
        private void AddNumberKbd_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.NumberKbd, Color.FromRgb(45, 45, 45));
        private void AddProgress_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Progress, Color.FromRgb(30, 30, 30));
        private void AddBar_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Bar, Color.FromRgb(30, 30, 30));
        private void AddBarchart_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Barchart, Color.FromRgb(30, 30, 30));
        private void AddPiechart_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Piechart, Color.FromRgb(30, 30, 30));
        private void AddLinechart_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Linechart, Color.FromRgb(30, 30, 30));
        private void AddQrcode_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Qrcode, Colors.White);
        private void AddWin_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Win, Color.FromRgb(45, 45, 45));
        private void AddStatusbar_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Statusbar, Color.FromRgb(45, 45, 45));
        private void AddViewlist_Click(object sender, RoutedEventArgs e) => CreateWidget(SglMapping.SglType.Viewlist, Color.FromRgb(45, 45, 45));
        private void ExportFontCode_Click(object sender, RoutedEventArgs e) => ProcessFontInternal(true);
        private void PreviewAndAdd_Click(object sender, RoutedEventArgs e) => ProcessFontInternal(false);

        private void ProcessFontInternal(bool saveToFile)
        {
            try
            {
                var selectedRes = SourceFontCombo.SelectedItem as SglResItem;

                if (selectedRes == null || string.IsNullOrEmpty(selectedRes.FilePath))
                {
                    MessageBox.Show("请先在左侧选择一个有效的源字体资源！");
                    return;
                }

                // --- 处理内置字体路径问题 ---
                string sourcePath = Path.Combine(ProjectManager.ProjectDir, selectedRes.FilePath);

                // 如果是内置资源标记，则将其释放到临时目录，获取真实的物理路径
                if (sourcePath.Contains("INTERNAL_RESOURCE"))
                {
                    // 定义临时 TTF 存放路径
                    string tempTtfPath = Path.Combine(SglResManager.TempDir, "internal_consolas_temp.ttf");

                    // 确保目录存在
                    if (!Directory.Exists(SglResManager.TempDir)) Directory.CreateDirectory(SglResManager.TempDir);

                    // 从 WPF 资源中读取并写入临时文件
                    var uri = new Uri("pack://application:,,,/Resources/Fonts/Consolas.ttf");
                    var resourceStream = Application.GetResourceStream(uri);
                    if (resourceStream != null)
                    {
                        using (var fs = new FileStream(tempTtfPath, FileMode.Create, FileAccess.Write))
                        {
                            resourceStream.Stream.CopyTo(fs);
                        }
                        sourcePath = tempTtfPath; // 将路径重定向到物理临时文件
                    }
                    else
                    {
                        throw new Exception("无法加载内置字体资源流。");
                    }
                }
                // ------------------------------------

                int fontSize = int.Parse(FontSizeBox.Text);
                string fontName = FontNameBox.Text;
                string customChars = CustomCharsTextBox.Text;


                // 获取 UI 参数
                string targetCPath = Path.Combine(SglResManager.TempDir, fontName + ".c");

                // 解析 BPP
                int bpp = 4;
                if (BppCombo.SelectedItem is ComboBoxItem item)
                {
                    bpp = int.Parse(item.Content.ToString().Split(' ')[0]);
                }
                bool compressFont = CheckBoxFontCompress.IsChecked == true;

                // 2. 调用 SglFontGenerator
                var gen = new SglFontGenerator
                {
                    FontSize = fontSize,
                    Bpp = bpp,
                    Compress = compressFont,
                    FontName = fontName,

                };

                // 执行生成逻辑

                var result = gen.Process(sourcePath, customChars, (bool)CheckBoxAscii.IsChecked);
                double ratio = result.UncompressedBytes > 0
                  ? (1.0 - ((double)result.CompressedBytes / result.UncompressedBytes)) * 100.0
                  : 0.0;
                string ratioText = compressFont
                  ? $"{ratio:F1}%"
                  : "0.0% (未启用压缩)";
                OutputBox.Text =
                  $"字体: {fontName}\r\n" +
                  $"BPP: 请求 {bpp} / 实际输出 {result.EffectiveBpp}\r\n" +
                  $"位图大小: 原始 {result.UncompressedBytes} bytes -> 输出 {result.CompressedBytes} bytes\r\n" +
                  $"压缩率: {ratioText}";

                //  物理写入临时文件
                File.WriteAllText(targetCPath, result.SourceCode);

                //  更新或添加资源项
                var cRes = SglResManager.Resources.FirstOrDefault(r => r.Name == fontName && r.ResType == SglResType.FontCSource);
                if (cRes == null)
                {
                    cRes = new SglResItem { Name = fontName, ResType = SglResType.FontCSource };
                    SglResManager.Resources.Add(cRes);
                }

                cRes.FontSize = fontSize;
                cRes.FilePath = targetCPath;
                cRes.SourceTtfPath = selectedRes.FilePath; // 记录物理路径供预览
                cRes.AllowedChars = CustomCharsTextBox.Text;
                cRes.FontCompressed = compressFont;
                //cRes.GeneratedCode = result.SourceCode;
                //写入文件
                File.WriteAllText(targetCPath, result.SourceCode);

                FontViewModel.Instance.RefreshFontList();
                FontViewModel.Instance.RefreshFontTTFList();
                //刷新界面
                if (SglScreen.Instance.SelectedScreen != null)
                {
                    WeakReferenceMessenger.Default.Send(new ScreenChangedMessage(SglScreen.Instance.SelectedScreen));
                }

                MessageBox.Show(
                  $"字体 {fontName}.c 已生成并同步到资源列表。\n原始 {result.UncompressedBytes} bytes -> 输出 {result.CompressedBytes} bytes\n压缩率: {ratioText}",
                  "提示",
                  MessageBoxButton.OK,
                  MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("生成失败: " + ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 添加图片资源
        private void AddImageResource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "图片文件|*.png;*.jpg;*.bmp;*.bin",
                Title = "选择图片资源"
            };

            if (dialog.ShowDialog() == true)
            {
                // 调用之前定义的管理器
                foreach (var item in dialog.FileNames)
                {
                    SglResManager.AddResource(item, SglResType.Image);
                }
            }

            // 过滤出所有图片类型的资源名
            var imageOptions = SglResManager.Resources
        .Where(r => r.ResType == SglResType.Image)
        .Select(r => r.Name)
        .ToList();

            // 如果没有图片，加一个提示项
            if (imageOptions.Count == 0) imageOptions.Add("No Images Found");

            // 2. 绑定到下拉框 (假设的 XAML 中 ComboBox 名为 SourceImageCombo)
            SourceImageCombo.ItemsSource = imageOptions;

            if (imageOptions.Count > 0)
                SourceImageCombo.SelectedIndex = 0;

            ImageViewModel.Instance.RefreshImageList(); // 触发 UI 更新
        }

        // 添加字体资源 (简单模拟)
        private void AddFontResource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Multiselect = true,
                Filter = "字体文件|*.ttf;*.otf",
                Title = "选择嵌入式字体文件"
            };

            if (dialog.ShowDialog() == true)
            {
                foreach (var item in dialog.FileNames)
                {
                    string filePath = item;
                    // 自动生成 C 变量名（如 MyFont.ttf -> sgl_font_myfont）
                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    string fontVarName = fileName;

                    // 拷贝并添加到资源管理器
                    SglResManager.AddResource(filePath, SglResType.FontSource, fontVarName);
                }

            }

            FontViewModel.Instance.RefreshFontList();
            FontViewModel.Instance.RefreshFontTTFList();

        }

        // 删除选中的资源
        private void DeleteResource_Click(object sender, RoutedEventArgs e)
        {
            var selected = ResourceListBox.SelectedItem as SglResItem;
            if (selected == null) return;

            var result = MessageBox.Show($"确定要删除资源 '{selected.Name}' 吗？", "提示", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                SglResManager.DeleteResource(selected);
                FontViewModel.Instance.RefreshFontList();
                FontViewModel.Instance.RefreshFontTTFList();
                ImageViewModel.Instance.RefreshImageList();
            }

        }
        // 右键菜单：删除逻辑
        private void OnDeleteResource(object sender, RoutedEventArgs e)
        {
            // 不要直接调用 DeleteResource_Click(null, null)，因为那里面可能有坏代码
            // 直接在这里处理，或者重构 DeleteResource_Click 使其不依赖 sender
            if (sender is MenuItem menuItem && menuItem.DataContext is SglResItem res)
            {
                var result = MessageBox.Show($"确定要删除资源 {res.Name} 吗？", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    SglResManager.DeleteResource(res);
                    FontViewModel.Instance.RefreshFontList();
                    FontViewModel.Instance.RefreshFontTTFList();
                    ImageViewModel.Instance.RefreshImageList();
                }
            }
        }


        // 添加屏幕按钮
        private void BtnAddScreen_Click(object sender, RoutedEventArgs e)
        {
            RecordBeforeChange();
            SglScreen.Instance.AddScreen();
        }

        // 删除屏幕按钮
        private void BtnDelScreen_Click(object sender, RoutedEventArgs e)
        {
            var toDelete = SglScreen.Instance.SelectedScreen;
            if (toDelete == null) return;

            var result = MessageBox.Show($"确定要删除屏幕 \"{toDelete.Id}\" 吗？", "删除确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            RecordBeforeChange();
            SglScreen.Instance.RemoveScreen(toDelete);
        }
        private void DeleteSelected_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedWidgets.Count == 0) return;

            RecordBeforeChange();

            // 将选中的 UI 列表转为副本，避免在遍历时由于 UI 更改导致异常
            var targets = _selectedWidgets.ToList();

            foreach (var widgetUI in targets)
            {
                if (widgetUI is FrameworkElement element && element.DataContext is SglWidgetData data)
                {
                    // --- 从数据树中彻底移除 ---
                    RemoveDataFromTree(data);
                }

                // --- 从视觉画布中移除 ---
                // 无论它是直接在画布上，还是嵌套在 Border/Canvas 内部，都要从其物理父级中移除
                var parent = VisualTreeHelper.GetParent(widgetUI) as Panel;
                if (parent != null && parent.Children.Contains(widgetUI))
                {
                    parent.Children.Remove(widgetUI);
                }


            }

            //  清理状态
            ClearSelection();
            RefreshCanvas(SglScreen.Instance.SelectedScreen);
            SglScreen.Instance.WidgetCount = SglScreen.GetAllWidgetsCount();
        }

        // 右键菜单：修改逻辑 (回填取模面板)
        private void OnEditResource(object sender, RoutedEventArgs e)
        {
            //不要依赖 ResourceListBox.SelectedItem，因为右键点击时选中的不一定是它
            // ContextMenu 的 DataContext 会自动指向对应的 ListBoxItem 数据源
            if (sender is MenuItem menuItem && menuItem.DataContext is SglResItem res)
            {
                if (res.ResType == SglResType.FontCSource)
                {
                    // 切换 Tab 页面
                    SglScreen.Instance.RightPanelTabIndex = 1;

                    //  将数据回填
                    // 这里的 SourceFontCombo.SelectedItem 需要匹配 res.SourceTtfPath 
                    // 如果 SourceFontCombo 绑定的是文件名或对象，请确保类型匹配
                    int index = 0;
                    foreach (var item in SourceFontCombo.Items)
                    {
                        if (res.SourceTtfPath.Contains(item.ToString()))
                        {
                            SourceFontCombo.SelectedIndex = index;
                            break;
                        }
                        index++;
                    }


                    FontNameBox.Text = res.Name;

                    // 这里应该是设置输入框的值，而不是设置输入框本身的字体大小
                    // 如果是 TextBox:
                    FontSizeBox.Text = res.FontSize.ToString();
                    // 如果是 NumericUpDown:
                    CustomCharsTextBox.Text = res.AllowedChars;
                    CheckBoxFontCompress.IsChecked = res.FontCompressed;


                    //  提示 (可选)
                    // StatusText.Text = $"已载入字体配置: {res.Name}";
                }
            }
        }
        // 进入编辑模式
        private void OnEditScreenName(object sender, RoutedEventArgs e)
        {
            if (ScreenListBox.SelectedItem is SglPageData page)
            {
                page.IsEditing = true;
            }
        }
        private void OnDeleteScreen(object sender, RoutedEventArgs e)
        {
            BtnDelScreen_Click(null, null);
        }
        private void ScreenListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // 找到被双击的具体 Item
            if (ScreenListBox.SelectedItem is SglPageData page)
            {
                page.IsEditing = true;
            }
        }
        private void TextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // 当 TextBox 变为可见时 (即进入编辑模式)
            if (sender is TextBox textBox && (bool)e.NewValue == true)
            {
                // 必须使用异步或者优先权较低的调度，确保控件已经完全加载并渲染
                Dispatcher.BeginInvoke(new Action(() =>
        {
            textBox.Focus();      // 获取焦点，触发光标闪烁
            textBox.SelectAll();  // 全选文字，方便直接覆盖修改
        }), System.Windows.Threading.DispatcherPriority.Input);
            }
        }
        // 备份旧名称，用于校验失败时回滚
        private string _oldNameBackup;

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is SglPageData page)
            {
                _oldNameBackup = page.Id; // 进入编辑模式时先存下原名
                //textBox.SelectAll();
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is SglPageData page)
            {
                string newName = textBox.Text.Trim();
                SglPageData selectedPage = SglScreen.Instance.SelectedScreen;
                if (selectedPage == null) return;

                // 默认目标是屏幕的根容器（Parent == null 的那个）
                SglWidgetData rootContainer = selectedPage.Widgets.FirstOrDefault(w => w.Parent == null);
                if (rootContainer != null)
                {
                    if (!ValidateAndApplyName(page, newName))
                    {
                        // 如果校验失败，恢复备份的旧名
                        page.Id = _oldNameBackup;
                        rootContainer.Id = _oldNameBackup;
                    }
                    else
                    {
                        rootContainer.Id = newName;
                    }




                }
                page.IsEditing = false;
            }
        }

        private bool ValidateAndApplyName(SglPageData currentPage, string newName)
        {
            // 不能为空
            if (string.IsNullOrWhiteSpace(newName))
            {
                MessageBox.Show("页面 ID 不能为空。", "校验失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // 在 ScreenList 中寻找除了当前页面外，是否已有同名 ID
            bool isDuplicate = SglScreen.Instance.ScreenList
               .Any(s => s != currentPage && s.Id.Equals(newName, StringComparison.OrdinalIgnoreCase));

            if (isDuplicate)
            {
                MessageBox.Show($"ID '{newName}' 已存在，请使用唯一的名称。", "命名冲突", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            //  C 语言变量命名规则检查
            // 只能包含字母、数字、下划线，且不能以数字开头
            if (!System.Text.RegularExpressions.Regex.IsMatch(newName, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            {
                MessageBox.Show("ID 格式不合法！必须以字母或下划线开头，且仅包含字母、数字和下划线。", "警告");
                return false;
            }

            return true;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is SglPageData page)
            {
                if (e.Key == Key.Enter)
                {
                    // 显式更新绑定源，触发校验
                    BindingOperations.GetBindingExpression(textBox, TextBox.TextProperty)?.UpdateSource();
                    // 让焦点离开 TextBox，从而触发 LostFocus 中的 page.IsEditing = false
                    ScreenListBox.Focus();

                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    // 放弃修改，直接回滚
                    page.Id = _oldNameBackup;
                    page.IsEditing = false;
                    e.Handled = true;
                }
            }
        }
        // 辅助方法：递归修复反序列化丢失的 Parent
        private void RebuildParentReferences(SglWidgetData parent)
        {

            if (parent.Children == null) return;
            foreach (var child in parent.Children)
            {
                SglScreen.Instance.WidgetCount++;
                child.Parent = parent; // 手动接回断掉的引用
                RebuildParentReferences(child); // 递归处理孙子节点

            }
        }



        private Border _selectedWidget;
        private int _originalZIndex = 0;

        private FrameworkElement _currentPotentialParent = null;
        private bool _lastHighlightMode = false; // 记录上次高亮模式，防止红色线残留

        // 用于记录临时关闭了裁剪的容器，方便恢复
        private FrameworkElement _lastClippedParent = null;
        private Rectangle _highlightRect;

        private Point ClampLocalPositionToBounds(double x, double y, double width, double height, double boundWidth, double boundHeight)
        {
            double maxX = Math.Max(0, boundWidth - width);
            double maxY = Math.Max(0, boundHeight - height);
            return new Point(
              Math.Max(0, Math.Min(x, maxX)),
              Math.Max(0, Math.Min(y, maxY)));
        }

        private void RestoreClipChain()
        {
            if (_lastClippedParent == null)
                return;

            DependencyObject current = _lastClippedParent;
            while (current != null && current != WidgetContainer)
            {
                if (current is FrameworkElement fe)
                    fe.ClipToBounds = true;

                if (current is Border border && border.Child is Canvas canvas)
                    canvas.ClipToBounds = true;

                current = VisualTreeHelper.GetParent(current);
            }

            _lastClippedParent = null;
        }

        private void ClearParentHighlight()
        {
            if (_highlightRect != null && WidgetContainer.Children.Contains(_highlightRect))
            {
                WidgetContainer.Children.Remove(_highlightRect);
            }

            _highlightRect = null;
            _currentPotentialParent = null;
            _lastHighlightMode = false;
        }

        private Point GetContainerContentOriginInWidgetContainer(FrameworkElement containerUI)
        {
            // Win 等嵌套容器通过 Tag 暴露内容 Canvas
            if (containerUI.Tag is Canvas tagCanvas)
            {
                return tagCanvas.TranslatePoint(new Point(0, 0), WidgetContainer);
            }
            if (containerUI is Border border && border.Child is Canvas canvas)
            {
                return canvas.TranslatePoint(new Point(0, 0), WidgetContainer);
            }

            return containerUI.TranslatePoint(new Point(0, 0), WidgetContainer);
        }

        private Canvas GetContainerContentCanvas(FrameworkElement containerUI)
        {
            // Win 等嵌套容器通过 Tag 暴露内容 Canvas
            if (containerUI.Tag is Canvas tagCanvas)
            {
                return tagCanvas;
            }
            if (containerUI is Border border && border.Child is Canvas canvas)
            {
                return canvas;
            }

            return null;
        }

        private Point GetPositionInRootContainer(SglWidgetData data, SglWidgetData rootData)
        {
            double x = data?.X ?? 0;
            double y = data?.Y ?? 0;

            SglWidgetData current = data?.Parent;
            while (current != null && current != rootData)
            {
                x += current.X;
                y += current.Y;
                current = current.Parent;
            }

            return new Point(x, y);
        }

        private Size GetWidgetModelSize(SglWidgetData data)
        {
            if (data is SglBarData bar)
            {
                return new Size(bar.UIWidth, bar.UIHeight);
            }

            if (data is SglSliderData slider)
            {
                return new Size(slider.UIWidth, slider.UIHeight);
            }

            return new Size(data?.W ?? 0, data?.H ?? 0);
        }

        private Size GetContainerContentSize(SglWidgetData containerData, FrameworkElement containerUI)
        {
            // Win 等嵌套容器通过 Tag 暴露内容 Canvas
            if (containerUI.Tag is Canvas tagCanvas)
            {
                tagCanvas.UpdateLayout();
                double w = tagCanvas.ActualWidth > 0 ? tagCanvas.ActualWidth : containerData.W;
                double h = tagCanvas.ActualHeight > 0 ? tagCanvas.ActualHeight : containerData.H;
                return new Size(w, h);
            }
            if (containerUI is Border border && border.Child is Canvas canvas)
            {
                canvas.UpdateLayout();
                double width = canvas.ActualWidth > 0 ? canvas.ActualWidth : containerData.W;
                double height = canvas.ActualHeight > 0 ? canvas.ActualHeight : containerData.H;
                return new Size(width, height);
            }

            return new Size(containerData.W, containerData.H);
        }

        private bool ReparentDraggedWidget(
          Border widgetUI,
          SglWidgetData data,
          FrameworkElement targetParentUI,
          SglWidgetData targetParentData,
          Point mousePosInRoot,
          Point mousePosInDesigner)
        {
            if (widgetUI == null || data == null || targetParentUI == null || targetParentData == null)
                return false;

            if (data.Parent == targetParentData)
                return false;

            FrameworkElement targetContent = GetContainerContentCanvas(targetParentUI) ?? targetParentUI;
            if (targetContent is not Panel newVisualParent)
                return false;

            if (VisualTreeHelper.GetParent(widgetUI) is not Panel oldVisualParent)
                return false;

            Size widgetSize = GetWidgetModelSize(data);
            Size containerSize = GetContainerContentSize(targetParentData, targetParentUI);
            Point containerRootPos = GetContainerContentOriginInWidgetContainer(targetParentUI);
            Point localPos = ClampLocalPositionToBounds(
              mousePosInRoot.X - containerRootPos.X - _mouseRelativeOffset.X,
              mousePosInRoot.Y - containerRootPos.Y - _mouseRelativeOffset.Y,
              widgetSize.Width,
              widgetSize.Height,
              containerSize.Width,
              containerSize.Height);

            oldVisualParent.Children.Remove(widgetUI);
            data.Parent?.Children.Remove(data);

            data.Parent = targetParentData;
            data.X = (int)Math.Round(localPos.X);
            data.Y = (int)Math.Round(localPos.Y);
            targetParentData.Children.Add(data);

            newVisualParent.Children.Add(widgetUI);
            Canvas.SetLeft(widgetUI, data.X);
            Canvas.SetTop(widgetUI, data.Y);
            Panel.SetZIndex(widgetUI, 100000);
            widgetUI.CaptureMouse();

            _initialWidgetPositions[widgetUI] = new Point(data.X, data.Y);
            _dragStartMousePos = mousePosInDesigner;

            RestoreClipChain();
            ClearParentHighlight();
            UpdateAdornerLayer(false);
            return true;
        }

        private void CenterWidgetInCurrentParent(SglWidgetData widget)
        {
            if (widget?.Parent == null)
                return;

            FrameworkElement parentUI = FindVisualByData(widget.Parent);
            FrameworkElement widgetUI = FindVisualByData(widget) as FrameworkElement;

            Size containerSize = GetContainerContentSize(widget.Parent, parentUI);
            Size modelSize = GetWidgetModelSize(widget);
            double widgetWidth = widgetUI?.ActualWidth > 0 ? widgetUI.ActualWidth : modelSize.Width;
            double widgetHeight = widgetUI?.ActualHeight > 0 ? widgetUI.ActualHeight : modelSize.Height;

            Point centeredLocalPos = ClampLocalPositionToBounds(
              (containerSize.Width - widgetWidth) / 2.0,
              (containerSize.Height - widgetHeight) / 2.0,
              widgetWidth,
              widgetHeight,
              containerSize.Width,
              containerSize.Height);

            widget.X = (int)Math.Round(centeredLocalPos.X);
            widget.Y = (int)Math.Round(centeredLocalPos.Y);
        }

        private void DetectPotentialParent(FrameworkElement draggedElement)
        {
            var data = draggedElement.DataContext as SglWidgetData;
            if (data == null || WidgetContainer == null) return;

            Panel.SetZIndex(draggedElement, 100000);
            draggedElement.IsHitTestVisible = false;

            try
            {
                var transformToRoot = draggedElement.TransformToVisual(WidgetContainer);
                Point dragCenter = transformToRoot.Transform(new Point(draggedElement.ActualWidth / 2, draggedElement.ActualHeight / 2));

                FrameworkElement foundNewParent = null;

                // 1. 寻找新容器 (绿框逻辑)
                foreach (var kvp in _widgetVisualMap.Values.Reverse())
                {
                    if (kvp == draggedElement) continue;
                    var targetData = kvp.DataContext as SglWidgetData;

                    if (targetData == null || !targetData.IsContainer) continue;
                    if (targetData.Parent == null) continue; // 跳过最底层的根容器

                    Rect bounds = kvp.TransformToVisual(WidgetContainer).TransformBounds(new Rect(0, 0, kvp.ActualWidth, kvp.ActualHeight));
                    if (bounds.Contains(dragCenter))
                    {
                        if (targetData == data.Parent) break;
                        foundNewParent = kvp;
                        break;
                    }
                }

                // 2. 脱离原容器检测 (红框逻辑)
                bool isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
                bool isEscaping = isCtrl;

                if (data.Parent != null)
                {
                    // 【核心修正】：获取底层的主画布
                    var rootData = SglScreen.Instance.SelectedScreen?.Widgets?.FirstOrDefault(w => w.Parent == null);

                    // 只有当当前控件所在的容器【不是主画布】时，才允许触发红色脱离警告
                    if (data.Parent != rootData)
                    {
                        FrameworkElement currentParentUI = FindVisualByData(data.Parent);
                        if (currentParentUI != null)
                        {
                            Rect pBounds = currentParentUI.TransformToVisual(WidgetContainer).TransformBounds(new Rect(0, 0, currentParentUI.ActualWidth, currentParentUI.ActualHeight));

                            if (!pBounds.Contains(dragCenter))
                            {
                                isEscaping = true;
                                DependencyObject parent = currentParentUI;
                                while (parent != null && parent != WidgetContainer)
                                {
                                    if (parent is FrameworkElement fe) fe.ClipToBounds = false;
                                    if (parent is Border b && b.Child is Canvas c) c.ClipToBounds = false;
                                    parent = VisualTreeHelper.GetParent(parent);
                                }
                                _lastClippedParent = currentParentUI;
                            }
                            else
                            {
                                RestoreClipChain();
                            }
                        }
                    }
                }

                UpdateParentHighlight(foundNewParent, isEscapeMode: isEscaping);
            }
            finally
            {
                draggedElement.IsHitTestVisible = true;
            }
        }
        private void OnManualUIChanged(object sender, SpinEventArgs e)
        {

            
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdateAdornerLayer();
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
        }
        //private void IntegerUpDown_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        //{
        //    UpdateAdornerLayer();
        //}
        // 响应手动输入并按回车
        private void OnInputKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                UpdateAdornerLayer();

            }
        }

        private void UpdateParentHighlight(FrameworkElement foundParent, bool isEscapeMode = false)
        {
            // 1. 优先级修正：如果已经找到了新容器，哪怕同时也脱离了原容器，视觉上也应该优先显示“绿色（即将移入新容器）”
            bool isEnteringNew = foundParent != null;

            // 状态检查：如果目标和模式都没变，直接返回（防止拖拽时重复重绘导致闪烁）
            if (_currentPotentialParent == foundParent && _lastHighlightMode == isEscapeMode) return;

            // 强制清理旧的高亮框
            ClearParentHighlight();

            _currentPotentialParent = foundParent;
            _lastHighlightMode = isEscapeMode;

            // 如果既没有悬停在新目标上，也没有脱离原容器，直接退出
            if (!isEnteringNew && !isEscapeMode) return;

            FrameworkElement targetToHighlight = null;
            Brush highlightBrush = Brushes.Transparent;

            // --- 核心视觉逻辑 ---
            if (isEnteringNew)
            {
                // 情况 A：移入新容器 (绿色虚线)
                targetToHighlight = foundParent;
                highlightBrush = Brushes.LimeGreen; // 改为绿色
            }
            else if (isEscapeMode)
            {
                // 情况 B：移出当前容器 (红色虚线，高亮原容器，提示即将脱离)
                var activeUI = _selectedWidgets.FirstOrDefault();
                var data = activeUI?.DataContext as SglWidgetData;
                if (data?.Parent != null)
                {
                    targetToHighlight = FindVisualByData(data.Parent);
                    highlightBrush = Brushes.Red; // 改为纯红色 (之前是 Crimson)
                }
            }

            // --- 绘制虚线框 ---
            if (targetToHighlight != null)
            {
                targetToHighlight.UpdateLayout();

                _highlightRect = new Rectangle
                {
                    Width = targetToHighlight.ActualWidth + 4,
                    Height = targetToHighlight.ActualHeight + 4,
                    Stroke = highlightBrush,
                    StrokeDashArray = new DoubleCollection { 4, 4 }, 
                    StrokeThickness = 1,                           
                    IsHitTestVisible = false
                };

                WidgetContainer.Children.Add(_highlightRect);

                // 获取相对坐标并挂载
                Point pos = targetToHighlight.TransformToAncestor(WidgetContainer).Transform(new Point(0, 0));
                Canvas.SetLeft(_highlightRect, pos.X - 2);
                Canvas.SetTop(_highlightRect, pos.Y - 2);

                // ZIndex 设为 99999，刚好在你拖拽对象的 100000 之下，但在所有容器之上
                Panel.SetZIndex(_highlightRect, 99999);
            }
        }

        private void Viewport_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point pt = e.GetPosition(WidgetContainer);
            HitTestResult res = VisualTreeHelper.HitTest(WidgetContainer, pt);

            if (res?.VisualHit is FrameworkElement hit)
            {
                Console.WriteLine($"点击到了: {hit.GetType().Name}, DataContext: {hit.DataContext?.GetType().Name ?? "Null"}");
                var widgetUI = FindComponentBorder(hit);
                // 强制转换为 Border
                if (widgetUI is Border border)
                {
                    SelectWidget(border, false);
                    if ((widgetUI.DataContext is SglWidgetData data && data.Parent == null))
                    {
                        e.Handled = true;              // 拦截事件，防止再次触发
                        return;
                    }
                    // 强制获取一次菜单并打开
                    var menu = widgetUI.ContextMenu ?? WidgetContainer.ContextMenu;
                    if (menu != null)
                    {
                        menu.PlacementTarget = widgetUI;
                        menu.IsOpen = true;

                    }

                }
            }
        }
        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            Point m = e.GetPosition(DesignerCanvas);
            // --- 拖拽控件逻辑 ---
            if (_isDraggingWidget && _selectedWidgets.Any())
            {
                // 找到所有“可移动”的控件（即：有父节点的控件）
                var movableWidgets = _selectedWidgets
                  .Where(w => (w.DataContext as SglWidgetData)?.Parent != null)
                  .ToList();

                // 如果没有可移动的对象，则不执行逻辑
                if (!movableWidgets.Any()) return;


                AdornerLayer.Children.Clear();

                double dx = m.X - _dragStartMousePos.X;
                double dy = m.Y - _dragStartMousePos.Y;
                Border primary = _selectedWidgets.Last(); // 以最后一个选中的为基准

                if (!_initialWidgetPositions.ContainsKey(primary)) return;

                // 临时提升层级，确保拖拽时在最上方
                Panel.SetZIndex(primary, 100000);

                // 2. 计算目标位置
                double targetX = _initialWidgetPositions[primary].X + dx;
                double targetY = _initialWidgetPositions[primary].Y + dy;

                //  吸附逻辑
                if (SnapToggle.IsChecked == true)
                {
                    var data = primary.DataContext as SglWidgetData;
                    targetX = DoBoundarySnap(primary, targetX, true, data?.Parent);
                    targetY = DoBoundarySnap(primary, targetY, false, data?.Parent);
                }

                double finalDeltaX = targetX - _initialWidgetPositions[primary].X;
                double finalDeltaY = targetY - _initialWidgetPositions[primary].Y;

                //  更新所有选中控件的位置
                foreach (var b in _selectedWidgets)
                {
                    if (_initialWidgetPositions.TryGetValue(b, out Point sPos))
                    {
                        double newX = sPos.X + finalDeltaX;
                        double newY = sPos.Y + finalDeltaY;
                        Canvas.SetLeft(b, newX);
                        Canvas.SetTop(b, newY);

                        if (b.DataContext is SglWidgetData data)
                        {
                            data.X = (int)newX;
                            data.Y = (int)newY;
                        }

                    }
                }

                // --- 父容器检测逻辑 ---
                bool isCtrl = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);

                if (isCtrl)
                {
                    // 模式 A: 强制脱离模式 (Ctrl 按下)
                    // 此时不探测新容器，直接显示当前父容器为“即将脱离”状态（变红）
                    UpdateParentHighlight(null, isEscapeMode: true);
                    _currentPotentialParent = null;
                }
                else
                {
                    // 自动探测模式
                    // 该方法内部会判断：
                    // 鼠标是否在某个新容器内 (蓝色高亮)
                    // 鼠标是否移出了当前父容器范围 (红色高亮)
                    DetectPotentialParent(primary);
                }
                /*
                var primaryData = primary.DataContext as SglWidgetData;
                var rootData = SglScreen.Instance.SelectedScreen?.Widgets?.FirstOrDefault(w => w.Parent == null) as SglWidgetData;
                if (_selectedWidgets.Count == 1 && primaryData != null && rootData != null)
                {
                    Point mousePosInRoot = e.GetPosition(WidgetContainer);
                    bool didReparent = false;

                    if (isCtrl && primaryData.Parent != null && primaryData.Parent != rootData)
                    {
                        FrameworkElement rootUI = FindVisualByData(rootData);
                        didReparent = ReparentDraggedWidget(primary, primaryData, rootUI, rootData, mousePosInRoot, m);
                    }
                    else if (_currentPotentialParent != null)
                    {
                        var newParentData = _currentPotentialParent.DataContext as SglWidgetData;
                        if (newParentData != null && primaryData.Parent != newParentData)
                        {
                            didReparent = ReparentDraggedWidget(primary, primaryData, _currentPotentialParent, newParentData, mousePosInRoot, m);
                        }
                    }
                    else if (_lastHighlightMode && primaryData.Parent != null && primaryData.Parent != rootData)
                    {
                        FrameworkElement rootUI = FindVisualByData(rootData);
                        didReparent = ReparentDraggedWidget(primary, primaryData, rootUI, rootData, mousePosInRoot, m);
                    }

                    if (didReparent)
                    {
                        UpdateAdornerLayer(false);
                        return;
                    }
                }
                */
                UpdateAdornerLayer(false);
            }
            else if (_isResizing)
            {
                if (_resizingTarget == null || _activeHandleIndex < 0)
                {
                    ResetInteractionState(releaseMouseCapture: true);
                    UpdateAdornerLayer(false);
                    return;
                }

                AdornerLayer.Children.Clear();
                double rdx = m.X - _lastMousePos.X;
                double rdy = m.Y - _lastMousePos.Y;

                double l = Canvas.GetLeft(_resizingTarget);
                double t = Canvas.GetTop(_resizingTarget);
                double w = _resizingTarget.Width;
                double h = _resizingTarget.Height;

                // ---检测是否强制正方形 (针对 LED) ---
                bool forceSquare = _resizingTarget.DataContext is SglLedData ||
          _resizingTarget.DataContext is SglArcData ||
          _resizingTarget.DataContext is SglRingData ||
          _resizingTarget.DataContext is SglCircleData;

                // 如果强制正方形，将 rdx 和 rdy 统一为一个 delta
                if (forceSquare)
                {
                    // 根据控制点位置，决定哪个轴的移动是“主导”
                    // 对角线控制点取位移较大的轴，边缘控制点取对应轴
                    double delta = 0;
                    if (_activeHandleIndex == 1 || _activeHandleIndex == 6) delta = rdy;
                    else if (_activeHandleIndex == 3 || _activeHandleIndex == 4) delta = rdx;
                    else delta = (Math.Abs(rdx) > Math.Abs(rdy)) ? rdx : rdy;

                    // 根据控制点方向，决定 delta 的正负对尺寸的影响
                    // 例如：拉右下角(7)，+delta 是增加；拉左上角(0)，+delta 是减小
                    rdx = rdy = delta;
                }

                switch (_activeHandleIndex)
                {
                    case 0: // 左上
                        if (forceSquare) { l += rdx; t += rdx; w -= rdx; h -= rdx; }
                        else { l += rdx; t += rdy; w -= rdx; h -= rdy; }
                        break;
                    case 1: // 中上
                        t += rdy; h -= rdy;
                        if (forceSquare) { w = h; } // 高度驱动宽度
                        break;
                    case 2: // 右上
                        if (forceSquare) { t -= rdx; w += rdx; h += rdx; } // 宽度驱动高度
                        else { t += rdy; w += rdx; h -= rdy; }
                        break;
                    case 3: // 左中
                        l += rdx; w -= rdx;
                        if (forceSquare) { h = w; }
                        break;
                    case 4: // 右中
                        w += rdx;
                        if (forceSquare) { h = w; }
                        break;
                    case 5: // 左下
                        if (forceSquare) { l -= rdy; w += rdy; h += rdy; }
                        else { l += rdx; w -= rdx; h += rdy; }
                        break;
                    case 6: // 中下
                        h += rdy;
                        if (forceSquare) { w = h; }
                        break;
                    case 7: // 右下
                        if (forceSquare) { w += rdx; h = w; }
                        else { w += rdx; h += rdy; }
                        break;
                }

                // --- 属性同步优化 ---
                if (w > 5 && h > 5)
                {
                    ApplyResizeBounds(_resizingTarget, l, t, w, h);
                }

                DrawSizeTip(l, t, w, h);
                _lastMousePos = m;
                UpdateAdornerLayer(false);
            }
            else if (_isPanningCanvas)
            {
                Point c = e.GetPosition(this);
                CanvasTranslate.X += (c.X - _lastMousePos.X);
                CanvasTranslate.Y += (c.Y - _lastMousePos.Y);
                _lastMousePos = c;
            }
        }

        private void ApplyResizeBounds(Border target, double left, double top, double width, double height)
        {
            Canvas.SetLeft(target, left);
            Canvas.SetTop(target, top);

            if (target.DataContext is not SglWidgetData data)
            {
                target.Width = width;
                target.Height = height;
                return;
            }

            data.X = (int)left;
            data.Y = (int)top;

            switch (data)
            {
                case SglBarData bar:
                    bar.UIWidth = width;
                    bar.UIHeight = height;
                    break;
                case SglSliderData slider:
                    slider.UIWidth = width;
                    slider.UIHeight = height;
                    break;
                default:
                    target.Width = width;
                    target.Height = height;
                    data.W = (int)width;
                    data.H = (int)height;
                    break;
            }
        }

        private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        {
            //全局中键释放，停止画布拖拽
            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanningCanvas = false;
                ViewportContainer.ReleaseMouseCapture();
                ViewportContainer.Cursor = Cursors.Arrow; // 恢复默认鼠标形状
                e.Handled = true;
                return;
            }



            var activeWidgetUI = _selectedWidgets.FirstOrDefault();
            var data = activeWidgetUI?.DataContext as SglWidgetData;
            bool isEscapeMode = _lastHighlightMode;
            var pendingParent = _currentPotentialParent;
            bool wasResizing = _isResizing;

            // 立刻清除高亮红绿框
            ClearParentHighlight();

            if (wasResizing)
            {
                // resize 结束处理，位置尺寸已在 MouseMove 中同步
            }

            if (activeWidgetUI != null && data != null)
            {
                var rootData = SglScreen.Instance.SelectedScreen?.Widgets?.FirstOrDefault(w => w.Parent == null) as SglWidgetData;

                if (pendingParent != null)
                {
                    var newParentData = pendingParent.DataContext as SglWidgetData;
                    if (newParentData != null && data.Parent != newParentData)
                    {
                        // 0. 在断开旧父级之前，先算好相对于新容器的落点坐标
                        var newVisualParent = GetContainerContentCanvas(pendingParent) ?? WidgetContainer;
                        Size containerSize = GetContainerContentSize(newParentData, pendingParent);
                        Size widgetSize = GetWidgetModelSize(data);

                        Point localPos;
                        try
                        {
                            Point widgetPosInTarget = activeWidgetUI.TranslatePoint(new Point(0, 0), newVisualParent);
                            localPos = ClampLocalPositionToBounds(
                                widgetPosInTarget.X, widgetPosInTarget.Y,
                                widgetSize.Width, widgetSize.Height,
                                containerSize.Width, containerSize.Height);
                        }
                        catch
                        {
                            // TranslatePoint 失败时兜底居中
                            localPos = new Point(
                                Math.Max(0, (containerSize.Width - widgetSize.Width) / 2),
                                Math.Max(0, (containerSize.Height - widgetSize.Height) / 2));
                        }

                        // 1. 【断开物理关系】
                        if (activeWidgetUI.Parent is Panel oldVisualParent)
                        {
                            oldVisualParent.Children.Remove(activeWidgetUI);
                        }

                        // 2. 【清理旧数据链路】
                        if (data.Parent != null) data.Parent.Children.Remove(data);
                        else SglScreen.Instance.SelectedScreen.Widgets.Remove(data);

                        // 3. 【建立新数据链路】
                        data.Parent = newParentData;
                        newParentData.Children.Add(data);

                        // 4. 使用鼠标松开位置的局部坐标
                        data.X = (int)Math.Round(localPos.X);
                        data.Y = (int)Math.Round(localPos.Y);

                        // 5. 挂载到新容器的内容 Canvas
                        newVisualParent.Children.Add(activeWidgetUI);
                        Canvas.SetLeft(activeWidgetUI, data.X);
                        Canvas.SetTop(activeWidgetUI, data.Y);

                        // 6. 归一化层级并刷新选中状态（不走 Full Rebuild，避免坐标丢失）
                        RestoreWidgetUI(activeWidgetUI, data);
                        NormalizeZIndex(data.Parent);
                        UpdateAdornerLayer();
                        SglScreen.Instance.RefreshWidgetList();
                    }
                }

                //拖出当前容器，挂回根画布 (红色虚线松手)

                else if (isEscapeMode && data.Parent != null && rootData != null && data.Parent != rootData)
                {
                    // 0. 在断开旧父级之前，先算好相对于根画布的落点坐标
                    FrameworkElement rootUI = FindVisualByData(rootData);
                    Canvas rootCanvas = GetContainerContentCanvas(rootUI) ?? WidgetContainer;
                    Size rootSize = GetContainerContentSize(rootData, rootUI);
                    Size widgetSize = GetWidgetModelSize(data);

                    Point localPos;
                    try
                    {
                        Point widgetPosInRoot = activeWidgetUI.TranslatePoint(new Point(0, 0), rootCanvas);
                        localPos = ClampLocalPositionToBounds(
                            widgetPosInRoot.X, widgetPosInRoot.Y,
                            widgetSize.Width, widgetSize.Height,
                            rootSize.Width, rootSize.Height);
                    }
                    catch
                    {
                        // TranslatePoint 失败时兜底居中
                        localPos = new Point(
                            Math.Max(0, (rootSize.Width - widgetSize.Width) / 2),
                            Math.Max(0, (rootSize.Height - widgetSize.Height) / 2));
                    }

                    // 1. 断开旧容器物理关系
                    if (activeWidgetUI.Parent is Panel oldVisualParent)
                    {
                        oldVisualParent.Children.Remove(activeWidgetUI);
                    }

                    // 2. 清理旧数据链路
                    data.Parent.Children.Remove(data);

                    // 3. 建立新数据链路
                    data.Parent = rootData;
                    rootData.Children.Add(data);

                    // 4. 使用鼠标松开位置的局部坐标
                    data.X = (int)Math.Round(localPos.X);
                    data.Y = (int)Math.Round(localPos.Y);

                    // 5. 挂载到根画布
                    rootCanvas.Children.Add(activeWidgetUI);
                    Canvas.SetLeft(activeWidgetUI, data.X);
                    Canvas.SetTop(activeWidgetUI, data.Y);

                    // 6. 归一化层级并刷新选中状态
                    RestoreWidgetUI(activeWidgetUI, data);
                    NormalizeZIndex(data.Parent);
                    UpdateAdornerLayer();
                    SglScreen.Instance.RefreshWidgetList();
                }
                // ==========================================
                // 场景 3：普通位置移动 (未跨容器) 或操作取消
                // ==========================================
                else
                {
                    RestoreWidgetUI(activeWidgetUI, data);
                }
            }

            // 彻底重置所有拖拽相关的状态变量
            ResetInteractionState(releaseMouseCapture: true);
            UpdateAdornerLayer();
        }

        private void RestoreWidgetUI(Border ui, SglWidgetData data)
        {
            // 还原 ZIndex
            Panel.SetZIndex(ui, _originalZIndex);
        }

        private void ResetInteractionState(bool releaseMouseCapture = false)
        {
            _isDraggingWidget = false;
            _isResizing = false;
            _isPanningCanvas = false;
            _activeHandleIndex = -1;
            _resizingTarget = null;
            _activeSnapX = null;
            _activeSnapY = null;
            _initialWidgetPositions.Clear();
            ClearParentHighlight();

            if (releaseMouseCapture && Mouse.Captured != null)
            {
                Mouse.Captured.ReleaseMouseCapture();
            }
        }

        // 公用重绘逻辑 (代码同前，在 RefreshCanvas 后归一化层级)
        private void FinalizeLayoutChange(Border ui, SglWidgetData data)
        {
            UpdateParentHighlight(null);
            ui.ReleaseMouseCapture();

            // 彻底重绘 UI 树以刷新层级和容器嵌套关系
            RefreshCanvas(SglScreen.Instance.SelectedScreen);

            // 归一化受影响层级的 ZIndex 顺序
            NormalizeZIndex(data.Parent);

            // 重新关联选中状态
            var newUI = FindVisualByData(data);
            if (newUI is Border b)
            {
                _selectedWidgets.Clear();
                _selectedWidgets.Add(b);
                SyncWidgetTreeSelection(data);
                RefreshPropertyPanelSelectionState();
            }
            //GlobalUpdate();
        }



        private Point _mouseRelativeOffset;

        // 鼠标按下处理
        private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        {
   
            // 全局中键按下，开启画布拖拽

            if (e.ChangedButton == MouseButton.Middle)
            {
                _isPanningCanvas = true;
                _lastMousePos = e.GetPosition(this); // 记录相对主窗口的位置
                ViewportContainer.CaptureMouse();    // 捕获鼠标
                ViewportContainer.Cursor = Cursors.SizeAll; // 鼠标变成十字移动形状，提升体验
                e.Handled = true;
                return;
            }

            // 只有左键才执行选择、缩放、拖拽逻辑
            if (e.ChangedButton != MouseButton.Left) return;

            // 锚点检测 (逻辑不变)
            HitTestResult res = VisualTreeHelper.HitTest(AdornerLayer, e.GetPosition(AdornerLayer));
            if (res?.VisualHit is Rectangle rect && _handleRects.Contains(rect))
            {
                var resizeTarget = _selectedWidgets.FirstOrDefault();
                if (resizeTarget == null) return;

                ResetInteractionState(releaseMouseCapture: true);
                _activeHandleIndex = _handleRects.IndexOf(rect);
                _isResizing = true;
                _resizingTarget = resizeTarget;
                _lastMousePos = e.GetPosition(DesignerCanvas);
                ViewportContainer.CaptureMouse();
                e.Handled = true;
                return;
            }

            // 注意：获取点击位置必须相对于最外层设计容器，保证参考系稳定
            Point mousePosInCanvas = e.GetPosition(WidgetContainer);
            res = VisualTreeHelper.HitTest(WidgetContainer, mousePosInCanvas);

            if (res?.VisualHit != null)
            {
                Border w = FindComponentBorder(res.VisualHit);

                if (w != null)
                {
                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        ResetInteractionState(releaseMouseCapture: true);
                        SelectWidget(w, Keyboard.IsKeyDown(Key.LeftCtrl));
                        _isDraggingWidget = true;

                        // 【核心修复 1】：锁定鼠标到该 Border
                        w.CaptureMouse();

                        // 【核心修复 2】：计算偏移量时，必须明确相对于“被点击对象”本身
                        // 这样无论它嵌套在多深的地方，拖拽时的增量计算都是准确的
                        _mouseRelativeOffset = e.GetPosition(w);

                        _dragStartMousePos = e.GetPosition(DesignerCanvas);

                        // 记录初始位置（用于撤销或多选）
                        _initialWidgetPositions.Clear();
                        foreach (var b in _selectedWidgets)
                        {
                            _initialWidgetPositions[b] = new Point(Canvas.GetLeft(b), Canvas.GetTop(b));
                        }

                        UpdateAdornerLayer();
                        if (w.DataContext is SglWidgetData widgetData && widgetData.Parent != null)
                        {
                            RecordBeforeChange();
                        }

                    }
                    e.Handled = true;
                    return;
                }
            }

            //  点击空白（清理选中）
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ResetInteractionState(releaseMouseCapture: true);
                // 如果当前有 TextBox 正在输入，强制移除焦点
                if (Keyboard.FocusedElement is TextBox IdTextBox)
                {
                    // 方案 A：让画布拿走焦点
                    ViewportContainer.Focus();
                }
                ClearSelection();
                _isPanningCanvas = true;
                _lastMousePos = e.GetPosition(this);
                ViewportContainer.CaptureMouse();
                UpdateAdornerLayer();
            }
        }
        // 定义一个结构来记录吸附状态
        private double? _activeSnapX = null;
        private double? _activeSnapY = null;

        private double DoBoundarySnap(Border current, double targetPos, bool isX, SglWidgetData parentContext)
        {
            // --- 获取网格大小作为吸附阈值 ---
            double gridSize = GridSizeInput.Value ?? 8;
            double threshold = gridSize / 2; // 吸附灵敏度设为网格的一半比较自然

            double bestPos = targetPos;
            double curSize = isX ? current.ActualWidth : current.ActualHeight;

            if (isX) _activeSnapX = null; else _activeSnapY = null;

            // --- 优先进行：参考线吸附（对齐兄弟控件） ---
            foreach (var kvp in _widgetVisualMap)
            {
                SglWidgetData otherData = kvp.Key;
                FrameworkElement otherUI = kvp.Value as FrameworkElement;

                if (otherUI == null || _selectedWidgets.Contains(otherUI)) continue;
                if (otherData.Parent != parentContext) continue;

                double oPos = isX ? otherData.X : otherData.Y;
                double oSize = isX ? otherData.W : otherData.H;

                double[] oLines = { oPos, oPos + oSize / 2, oPos + oSize };
                double[] myOffsets = { 0, curSize / 2, curSize };

                foreach (var line in oLines)
                {
                    foreach (var offset in myOffsets)
                    {
                        if (Math.Abs(targetPos - (line - offset)) < threshold)
                        {
                            bestPos = line - offset;
                            DrawSnapLine(line, isX, otherUI);
                            if (isX) _activeSnapX = line; else _activeSnapY = line;
                            return bestPos; // 匹配到参考线，优先返回
                        }
                    }
                }
            }

            // --- 3. 次优选择：网格吸附（如果没有匹配到任何参考线） ---
            // 如果没有触发参考线吸附，则强制对齐到网格
            bestPos = Math.Round(targetPos / gridSize) * gridSize;

            return bestPos;
        }

        private void DrawSnapLine(double pos, bool isX, FrameworkElement referenceUI)
        {
            // 寻找参考对象所在的容器（即 Parent 的 UI）
            // 如果是在根画布，则直接相对于 WidgetContainer
            Visual parentVisual = VisualTreeHelper.GetParent(referenceUI) as Visual ?? WidgetContainer;

            // 将相对坐标转换为相对于 WidgetContainer 的绝对坐标
            Point refPoint = isX ? new Point(pos, 0) : new Point(0, pos);
            Point globalPoint = parentVisual.TransformToAncestor(WidgetContainer).Transform(refPoint);

            //  获取 ScreenArea 的位置作为线的延伸边界
            Point screenPos = WidgetContainer.TranslatePoint(new Point(0, 0), WidgetContainer);

            Line snp = new Line
            {
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 2 },
                SnapsToDevicePixels = true
            };

            if (isX)
            {
                snp.X1 = snp.X2 = globalPoint.X;
                snp.Y1 = screenPos.Y;
                snp.Y2 = screenPos.Y + WidgetContainer.ActualHeight;
            }
            else
            {
                snp.Y1 = snp.Y2 = globalPoint.Y;
                snp.X1 = screenPos.X;
                snp.X2 = screenPos.X + WidgetContainer.ActualWidth;
            }

            Panel.SetZIndex(snp, 1000);
            AdornerLayer.Children.Add(snp);
        }
        private void DrawSizeTip(double x, double y, double w, double h)
        {
            // 定义在 AdornerLayer 上的最终物理坐标
            double finalScreenX;
            double finalScreenY;

            // 优先直接用目标控件自身做坐标转换，避免父级 Visual 已被重建时抛异常
            if (_resizingTarget != null)
            {
                try
                {
                    Point screenPoint = _resizingTarget.TranslatePoint(new Point(w / 2, 0), AdornerLayer);
                    finalScreenX = screenPoint.X;
                    finalScreenY = screenPoint.Y;
                }
                catch (InvalidOperationException)
                {
                    finalScreenX = x + (w / 2);
                    finalScreenY = y;
                }
            }
            else
            {
                // 如果没有父容器（理论上不可能），则退回到基础计算
                finalScreenX = x + (w / 2);
                finalScreenY = y;
            }

            // 2. 创建 UI 元素
            var txt = new TextBlock
            {
                Text = $"{(int)w}x{(int)h}px",
                FontSize = 10,
                Foreground = Brushes.Black,
                FontWeight = FontWeights.Bold
            };

            Border b = new Border
            {
                Background = _nxpYellow,
                Padding = new Thickness(4, 2, 4, 2),
                CornerRadius = new CornerRadius(2),
                Child = txt,
                IsHitTestVisible = false
            };

            //  设置位置
            // 减去 25 是为了让提示框水平居中，减去 25 是为了在控件上方留出间距
            Canvas.SetLeft(b, finalScreenX - 25);
            Canvas.SetTop(b, finalScreenY - 25);

            Panel.SetZIndex(b, 999999);
            AdornerLayer.Children.Add(b);
        }

        private void UpdateAdornerLayer(bool clear = true)
        {
            if (AdornerLayer == null || WidgetContainer == null) return;

            if (clear)
            {
                AdornerLayer.Children.Clear();
            }

            if (!_selectedWidgets.Any())
            {
                _handleRects.ForEach(r => r.Visibility = Visibility.Collapsed);
                return;
            }

            // 强制 UI 刷新，确保 Transform 能够拿到最新的视觉树引用
            WidgetContainer.UpdateLayout();

            foreach (var widget in _selectedWidgets)
            {
                // 如果不是上级，说明该 Visual 已经“失效”或者是孤立的
                if (!widget.IsDescendantOf(WidgetContainer)) continue;

                try
                {
                    GeneralTransform transform = widget.TransformToAncestor(WidgetContainer);
                    Rect globalRect = transform.TransformBounds(new Rect(0, 0, widget.ActualWidth, widget.ActualHeight));

                    // 绘制虚线框
                    var selectionFrame = new Rectangle
                    {
                        Width = globalRect.Width,
                        Height = globalRect.Height,
                        Stroke = _nxpBlue,
                        StrokeThickness = 1,
                        StrokeDashArray = new DoubleCollection { 2, 2 },
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(selectionFrame, globalRect.Left);
                    Canvas.SetTop(selectionFrame, globalRect.Top);
                    AdornerLayer.Children.Add(selectionFrame);

                    // 只有单选才画锚点
                    if (_selectedWidgets.Count == 1)
                    {                                              //|| widget.DataContext is SglExtImageData || widget.DataContext is SglIconData
                        if ((widget.DataContext is SglWidgetData data && data.Parent == null) || widget.DataContext is SglQrcodeData)
                        {
                            continue;
                        }
                        UpdateHandlesPosition(globalRect);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("转换坐标失败: " + ex.Message);
                }
            }
        }

        private void UpdateHandlesPosition(Rect rect)
        {
            double l = rect.Left;
            double t = rect.Top;
            double w = rect.Width;
            double h = rect.Height;

            Point[] pts = {
            new Point(l, t),     new Point(l + w/2, t),  new Point(l + w, t),
            new Point(l, t + h/2),                new Point(l + w, t + h/2),
            new Point(l, t + h),   new Point(l + w/2, t + h), new Point(l + w, t + h)
          };

            for (int i = 0; i < 8; i++)
            {
                var handle = _handleRects[i];
                handle.Visibility = Visibility.Visible;
                Canvas.SetLeft(handle, pts[i].X - 3.5);
                Canvas.SetTop(handle, pts[i].Y - 3.5);

                if (VisualTreeHelper.GetParent(handle) is Panel p) p.Children.Remove(handle);
                AdornerLayer.Children.Add(handle);
            }
        }

        private void UpdateCodePreview()
        {
            StringBuilder sbMain = new StringBuilder();     // 存放全局定义和 init 函数
            StringBuilder sbHandlers = new StringBuilder(); // 存放 事件的分发函数体
            HashSet<string> processedHandlers = new HashSet<string>();

            sbMain.AppendLine("/* SGL Designer Auto Generated - 2026 */");
            sbMain.AppendLine("#include \"sgl.h\"\n");

            // 获取当前屏幕的所有根控件
            var rootWidgets = SglScreen.Instance.SelectedScreen.Widgets;


            // 定义一个局部递归函数来遍历树
            void GenerateAllHandlers(IEnumerable<SglWidgetData> widgets)
            {
                foreach (var widget in widgets)
                {
                    // 如果该控件有回调名，且还没生成过这个函数名
                    if (!string.IsNullOrEmpty(widget.EventCallbackName) && !processedHandlers.Contains(widget.EventCallbackName))
                    {
                     
                        // 注意：这里传入 null 的 HashSet 是因为外层已经控制了去重，或者让内部只管生成自己
                        sbHandlers.AppendLine(SglMapping.GenerateCEventHandlers(widget, new HashSet<string>()));
                        processedHandlers.Add(widget.EventCallbackName);
                    }
                    // 递归子节点
                    if (widget.Children.Count > 0) GenerateAllHandlers(widget.Children);
                }
            }

            GenerateAllHandlers(rootWidgets);

            // 将生成的函数体附加到文件开头部分（C 语言要求先定义后使用）
            sbMain.Append(sbHandlers.ToString());

            // --- 生成 UI 初始化函数 (创建对象与绑定) ---
            sbMain.AppendLine("void sgl_ui_init(void) {");
            sbMain.AppendLine("    sgl_obj_t* screen = sgl_screen_act(); // 获取当前活跃屏幕\n");

            foreach (var widgetData in rootWidgets)
            {
                // GenerateSglCode 内部应包含：创建、设置属性、递归子控件、以及 sgl_obj_set_event_cb
                sbMain.Append(SglMapping.GenerateSglCode(widgetData, "screen"));
            }

            sbMain.AppendLine("}");


        }



        private void TestImageGen_Click(object sender, RoutedEventArgs e)
        {
            string selectedName = SourceImageCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(selectedName)) return;

            var res = SglResManager.Resources.FirstOrDefault(r => r.Name == selectedName);
            if (res == null) return;

            // 加载位图
            BitmapImage bmp = new BitmapImage(new Uri(res.FilePath));

            // 获取配置 (从工程设置或 UI 选项)
            string fmt = (ImgColorFormatCombo.SelectedItem as ComboBoxItem)?.Content.ToString(); // RGB565/RGB888
            bool isLE = IsLittleEndian.IsChecked ?? true;

            // 生成模板代码
            //string code = SglImageConverter.ConvertToSglPixmapCode(res, res.Name, fmt, isLE);

            //CodePreview.Text = code;


        }
    }
}
