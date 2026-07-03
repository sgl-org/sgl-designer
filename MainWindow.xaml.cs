using AutoUpdaterDotNET;

using CommunityToolkit.Mvvm.Messaging;
using GongSolutions.Wpf.DragDrop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;
namespace SglDesigner
{
    public partial class MainWindow : Window, IDropTarget
    {
        // --- 核心字段 ---
        private List<Border> _selectedWidgets = new List<Border>();
        private Dictionary<Border, Point> _initialWidgetPositions = new Dictionary<Border, Point>();
        private List<Rectangle> _handleRects = new List<Rectangle>();

        private bool _isDraggingWidget, _isPanningCanvas, _isResizing, _isInternalSelectionChange;
        private Point _lastMousePos, _dragStartMousePos;
        private Border _resizingTarget;
        private int _activeHandleIndex;
        private SglWidgetData _propertySyncSource;
        private bool _isSynchronizingMultiSelectionProperties;

        private readonly SolidColorBrush _nxpYellow = new SolidColorBrush(Color.FromRgb(255, 215, 0));
        private readonly SolidColorBrush _nxpBlue = new SolidColorBrush(Color.FromRgb(0, 161, 241));
        private bool _isInternalChange = false;
        private static readonly HashSet<string> MultiSelectExcludedProperties = new HashSet<string>
        {
            nameof(SglWidgetData.Id),
            nameof(SglWidgetData.Tag),
            nameof(SglWidgetData.Type),
            nameof(SglWidgetData.Parent),
            nameof(SglWidgetData.Children),
            nameof(SglWidgetData.Events),
            nameof(SglWidgetData.EventRoutes),
            nameof(SglWidgetData.EventCallbackName),
            nameof(SglWidgetData.IsSelected),
            nameof(SglWidgetData.IsContainer)
        };


        // 定义映射表：Key 是数据模型，Value 是对应的 WPF UI 控件
        // 这样给一个数据对象，就能秒找它在 Canvas 上的 Border 或 Path
        private readonly Dictionary<SglWidgetData, FrameworkElement> _widgetVisualMap = new();

        public List<LineDirection> LineDirectionOptions { get; } =
   Enum.GetValues(typeof(LineDirection)).Cast<LineDirection>().ToList();
        public MainWindow()
        {
            InitializeComponent();
            InitAdorners();

            // 确保在 PreviewKeyDown 捕捉，防止被子控件拦截
            this.PreviewKeyDown += MainWindow_KeyDown;
            this.Loaded += (s, e) => { ZoomToFit_Click(null, null); UpdateRulers(); };
            CompositionTarget.Rendering += (s, e) => UpdateRulers();

            this.DataContext = SglScreen.Instance;


            WeakReferenceMessenger.Default.Register<ScreenChangedMessage>(this, (r, m) =>
            {
                RefreshCanvas(m.Value);
                //SglScreen.Instance.WidgetCount = SglScreen.GetAllWidgetsCount();
                UpdateAdornerLayer();
            });

            // 订阅数据层发出的刷新请求
            SglWidgetData.RequestAdornerUpdate += () =>
            {
                // 回到 UI 线程执行刷新
                this.Dispatcher.Invoke(() => UpdateAdornerLayer());
            };



            // 初始化事件下拉框
            //InitEventSelector();
            //InitEventAction();
            // --- 立即补回内置字体 ---
            SglResManager.EnsureInternalResources();

            FontViewModel.Instance.RefreshFontList();
            FontViewModel.Instance.RefreshFontTTFList();

            ConfigureAutoUpdater();

            String version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //version = version.Substring(0, version.Length - 2);  //去掉最后一位版本号
            //String BuildDateTime = System.IO.File.GetLastWriteTime(this.GetType().Assembly.Location).ToString();
            this.Title = "Sgl Designer V" + version; //+ "    Build：" + BuildDateTime;

        }


        // MainWindow.xaml.cs 
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 确保窗口完全加载后，手动刷新一次当前选中的屏幕
            if (SglScreen.Instance.SelectedScreen != null)
            {
                RefreshCanvas(SglScreen.Instance.SelectedScreen);
            }
        }
        //private void InitEventSelector()
        //{
        //    // 获取枚举中除 Null 以外的所有值
        //    var allEvents = Enum.GetValues(typeof(SglEventType))
        //                        .Cast<SglEventType>()
        //                        .Where(e => e != SglEventType.Null);

        //    EventSelector.ItemsSource = allEvents;

        //    // 默认选中第一个有效事件（通常是 Normal 或 Pressed）
        //    if (EventSelector.Items.Count > 0)
        //        EventSelector.SelectedIndex = 0;
        //}
        //private void InitEventAction()
        //{
        //    // 获取枚举中除 Null 以外的所有值
        //    var allEvents = Enum.GetValues(typeof(SglActionType))
        //                        .Cast<SglActionType>()
        //                        .Where(e => e != SglActionType.Null);

        //    ActionSelector.ItemsSource = allEvents;

        //    // 默认选中第一个有效事件（通常是 Normal 或 Pressed）
        //    if (ActionSelector.Items.Count > 0)
        //        ActionSelector.SelectedIndex = 0;
        //}
        private void RefreshCanvas(SglPageData newPage)
        {
            WidgetContainer.Children.Clear();
            _widgetVisualMap.Clear();

            if (newPage == null) return;

            // 从顶级控件开始递归构建
            foreach (var widget in newPage.Widgets)
            {
                BuildVisualTreeRecursive(widget, WidgetContainer);
            }

        }

        private void BuildVisualTreeRecursive(SglWidgetData data, Panel currentParentUI)
        {
            // 创建当前控件的 UI (例如 Border)
            FrameworkElement uiElement = CreateVisualElement(data, true);
            _widgetVisualMap[data] = uiElement;
            //AttachDragEvents(uiElement);

            // 将 UI 加入当前的父级容器
            currentParentUI.Children.Add(uiElement);

            //  设置相对坐标 (注意：这里直接设置相对父级的 X, Y)
            Canvas.SetLeft(uiElement, data.X);
            Canvas.SetTop(uiElement, data.Y);

            //  如果是容器，处理它的子对象
            if (data.IsContainer)
            {

                // 或者直接在 CreateVisualByData 里为 Panel 返回了一个包含 Canvas 的 Border
                var innerContainer = FindInternalCanvas(uiElement);

                if (innerContainer != null)
                {
                    foreach (var child in data.Children)
                    {
                        BuildVisualTreeRecursive(child, innerContainer);
                    }
                }
            }
        }
        private Panel FindInternalCanvas(FrameworkElement parentVisual)
        {
            // Win 等嵌套容器通过 Tag 暴露内容 Canvas（与 GetContainerContentCanvas 保持一致）
            if (parentVisual.Tag is Canvas tagCanvas)
            {
                return tagCanvas;
            }

            //如果在 CreateVisualByData 里创建 Panel 时直接用的是 Canvas
            if (parentVisual is Canvas canvas)
            {
                return canvas;
            }

            // 如果为了好看，外层套了 Border (推荐)
            // Border -> Child (Canvas)
            if (parentVisual is Border border && border.Child is Panel p)
            {
                return p;
            }

            // 如果用了复杂的模板，可以使用 VisualTreeHelper 查找
            // 这里简单处理，默认返回 null 表示该控件无法作为容器
            return null;
        }
        private Border FindVisualByData(SglWidgetData targetData)
        {
            // 从画布根容器开始查找
            return FindVisualRecursive(WidgetContainer, targetData);
        }
        private Border FindVisualRecursive(DependencyObject parent, SglWidgetData targetData)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // 如果是 Border 且 DataContext 匹配
                if (child is Border b && b.DataContext == targetData)
                {
                    return b;
                }

                // 继续向深层递归（比如 Grid 或 Canvas 内部）
                var result = FindVisualRecursive(child, targetData);
                if (result != null) return result;
            }
            return null;
        }
        private void WidgetTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is SglWidgetData selectedData)
            {
                // 通过数据查找到对应的物理 Border 控件
                Border visualBorder = FindVisualByData(selectedData) as Border;

                if (visualBorder != null)
                {
                    //直接调用现有的核心选中函数
                    // 这里 multi 传 false，表示 TreeView 单选逻辑
                    SelectWidget(visualBorder, false);

                    //  让画布里的对象自动滚动到视野中
                    visualBorder.BringIntoView();
                }
            }
        }
        // --- 核心更新入口 ---
        //private void GlobalUpdate()
        //{

        //    UpdateAdornerLayer();


        //}

        private void InitAdorners()
        {
            Cursor[] cursors = { Cursors.SizeNWSE, Cursors.SizeNS, Cursors.SizeNESW, Cursors.SizeWE, Cursors.SizeWE, Cursors.SizeNESW, Cursors.SizeNS, Cursors.SizeNWSE };
            for (int i = 0; i < 8; i++)
            {
                Rectangle r = new Rectangle { Width = 7, Height = 7, Fill = Brushes.White, Stroke = _nxpBlue, StrokeThickness = 1, Tag = i, Visibility = Visibility.Collapsed, Cursor = cursors[i] };
                r.MouseDown += (s, e) =>
                {
                    _activeHandleIndex = (int)(s as Rectangle).Tag;
                    _resizingTarget = _selectedWidgets.Last();
                    _isResizing = true;
                    _lastMousePos = e.GetPosition(DesignerCanvas);
                    (s as Rectangle).CaptureMouse();
                    e.Handled = true;
                };
                HandleLayer.Children.Add(r); _handleRects.Add(r);
            }
        }

        // --- 尺标逻辑 ---
        private void UpdateRulers()
        {
            TopRuler.Children.Clear();
            LeftRuler.Children.Clear();

            double sc = CanvasScale.ScaleX;
            double tx = CanvasTranslate.X;
            double ty = CanvasTranslate.Y;
            double sw = WidgetContainer.Width;
            double sh = WidgetContainer.Height;

            if (double.IsNaN(sw) || double.IsNaN(sh) || sw <= 0 || sh <= 0) return;

            // --- 核心改进：动态计算步进 ---
            // 希望主刻度在屏幕上的物理距离大约为 80 像素
            double targetPhysicalGap = 80;
            // 计算原始坐标系下需要的步进值
            double rawStep = targetPhysicalGap / sc;

            // 将步进值规格化（取 10, 20, 50, 100, 200, 500... 这种好看的数字）
            int step = CalculateDynamicStep(rawStep);
            int subStep = step / 5; // 细分刻度通常为主刻度的 1/5
                                    // ----------------------------

            double startX = -tx / sc, startY = -ty / sc;
            double endX = (TopRuler.ActualWidth - tx) / sc, endY = (LeftRuler.ActualHeight - ty) / sc;

            // 绘制逻辑（保持不变，但使用动态生成的 step 和 subStep）
            for (double i = Math.Floor(startX / subStep) * subStep; i <= endX; i += subStep)
            {
                double px = i * sc + tx;
                // 使用精度容差判断是否为主刻度
                bool isMajor = Math.Abs(i % step) < (subStep / 2.0);

                TopRuler.Children.Add(new Line
                {
                    X1 = px,
                    X2 = px,
                    Y1 = isMajor ? 10 : 18,
                    Y2 = 25,
                    Stroke = Brushes.DimGray,
                    StrokeThickness = 1
                });

                if (isMajor)
                {
                    var t = new TextBlock { Text = ((int)Math.Round(i)).ToString(), FontSize = 9, Foreground = Brushes.Gray };

                    // 强制测量文字，以获取其实际宽度
                    t.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    double textWidth = t.DesiredSize.Width;

                    // 居中对齐：刻度位置 px 减去文字宽度的一半
                    Canvas.SetLeft(t, px - (textWidth / 2));
                    Canvas.SetTop(t, 0);
                    TopRuler.Children.Add(t);
                }
            }

            //  纵向标尺循环处理
            for (double i = Math.Floor(startY / subStep) * subStep; i <= endY; i += subStep)
            {
                // 计算在 Canvas 上的物理位置
                double py = i * sc + ty;

                // 精度容错判断是否为主刻度
                bool isMajor = Math.Abs(i % step) < (subStep / 2.0);

                // 绘制刻度线
                LeftRuler.Children.Add(new Line
                {
                    Y1 = py,
                    Y2 = py,
                    X1 = isMajor ? 10 : 18,
                    X2 = 25, // 主刻度长，细分刻度短
                    Stroke = Brushes.DimGray,
                    StrokeThickness = 1
                });

                if (isMajor)
                {
                    var t = new TextBlock
                    {
                        Text = ((int)Math.Round(i)).ToString(),
                        FontSize = 9,
                        Foreground = Brushes.Gray,
                        RenderTransform = new RotateTransform(-90)
                    };

                    t.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    // 旋转 90 度后，文字原本的宽度变成了垂直方向的偏移参考
                    double textWidth = t.DesiredSize.Width;

                    Canvas.SetLeft(t, 0);
                    // 居中对齐：py 加上文字宽度的一半（因为旋转中心默认在左上角，旋转后向上偏移）
                    Canvas.SetTop(t, py + (textWidth / 2));

                    LeftRuler.Children.Add(t);
                }
            }

            DrawScreenGuide(TopRuler, tx, (sw * sc) + tx, sw + "px", true);
            DrawScreenGuide(LeftRuler, ty, (sh * sc) + ty, sh + "px", false);
        }

        /// <summary>
        /// 计算最适合人类阅读的步进值 (1, 2, 5 序列)
        /// </summary>
        private int CalculateDynamicStep(double rawStep)
        {
            // 获取 10 的幂
            double log = Math.Log10(rawStep);
            double pow10 = Math.Pow(10, Math.Floor(log));
            double normalized = rawStep / pow10;

            if (normalized < 1.5) return (int)(1 * pow10);
            if (normalized < 3.5) return (int)(2 * pow10);
            if (normalized < 7.5) return (int)(5 * pow10);
            return (int)(10 * pow10);
        }

        private void DrawScreenGuide(Canvas c, double start, double end, string label, bool isH)
        {
            Line l = new Line { Stroke = _nxpYellow, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 4, 2 } };
            if (isH) { l.X1 = start; l.X2 = end; l.Y1 = l.Y2 = 24; } else { l.Y1 = start; l.Y2 = end; l.X1 = l.X2 = 24; }
            c.Children.Add(l);
            var t = new TextBlock { Text = label, FontSize = 10, Foreground = _nxpYellow, FontWeight = FontWeights.Bold };
            if (isH) { Canvas.SetLeft(t, (start + end) / 2 - 15); Canvas.SetTop(t, 9); }
            else { Canvas.SetTop(t, (start + end) / 2 + 15); Canvas.SetLeft(t, 10); t.RenderTransform = new RotateTransform(-90); }
            c.Children.Add(t);
        }


        private Border FindComponentBorder(DependencyObject child)
        {
            DependencyObject parent = child;

            // 沿着视觉树向上爬
            while (parent != null)
            {
                // 判定条件：它是一个 Border，且它的 DataContext 是的 SglWidgetData
                if (parent is Border border && border.DataContext is SglWidgetData)
                {
                    return border;
                }

                // 如果已经爬到了容器根部，就停下来
                if (parent == WidgetContainer) break;

                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
        // --- 选中逻辑处理 ---

        private void SyncWidgetTreeSelection(SglWidgetData data)
        {
            // 先把当前屏幕所有对象的选中状态清空
            if (SglScreen.Instance.SelectedScreen != null)
            {
                foreach (var w in SglScreen.Instance.SelectedScreen.Widgets)
                {
                    w.IsSelected = false;
                }
            }

            //选中当前点击的数据对象
            // 由于 XAML 里的 Style 已经做了双向绑定，
            // 设置 data.IsSelected = true 会自动让 TreeView 里的对应节点变蓝。
            data.IsSelected = true;
            // 选中后强制让 TreeView 获得焦点，这样选中的高亮才明显
            WidgetTree.Focus();

            WidgetTree.Dispatcher.BeginInvoke(new Action(() =>
            {
                // 尝试通过容器寻找对应的 TreeViewItem 并带入视野
                var tvi = FindTreeViewItem(WidgetTree, data);
                tvi?.BringIntoView();
            }));
        }
        // 辅助方法：通过数据查找 TreeViewItem
        private TreeViewItem FindTreeViewItem(ItemsControl container, object item)
        {
            if (container.DataContext == item) return container as TreeViewItem;
            if (container is TreeViewItem tvi && !tvi.IsExpanded) tvi.IsExpanded = true;

            container.ApplyTemplate();
            var presenter = (ItemsPresenter)container.Template.FindName("ItemsHost", container);
            if (presenter != null) presenter.ApplyTemplate();
            else return null;

            foreach (var child in container.Items)
            {
                var childContainer = (TreeViewItem)container.ItemContainerGenerator.ContainerFromItem(child);
                if (childContainer == null) continue;
                var result = FindTreeViewItem(childContainer, item);
                if (result != null) return result;
            }
            return null;
        }

        private void SelectWidget(Border w, bool multi)
        {
            // 如果内部状态正在改变（如程序自动触发），则跳过
            if (_isInternalSelectionChange) return;




            // 如果不是多选模式（未按 Ctrl），且当前点击的控件不在选中列表中
            // 则先清空当前所有选中，改为单选这个控件
            if (!multi)
            {



                if (!_selectedWidgets.Contains(w))
                {
                    _selectedWidgets.Clear();
                    _selectedWidgets.Add(w);
                }
                else
                {
                    // 如果已在选中列表中，且没按 Ctrl，通常维持现状或将它设为“主选对象”
                    _selectedWidgets.Remove(w);
                    _selectedWidgets.Add(w); // 移动到最后，成为主选
                }
            }
            else
            {

                // 多选模式：如果在列表里就移除（反选），不在就加入
                if (_selectedWidgets.Contains(w))
                    _selectedWidgets.Remove(w);
                else
                    _selectedWidgets.Add(w);
            }


            // 激活属性面板（假设右侧有属性编辑器）
            PropertyPanel.IsEnabled = _selectedWidgets.Any();

            _selectedWidget = w;
            if (_selectedWidget == null)
            {

                PropertyPanel.IsEnabled = false;
                return;
            }

            SglScreen.Instance.SelectedScreen.SelectedWidgetData = _selectedWidget.DataContext as SglWidgetData;
            PropertyPanel.IsEnabled = true;

            //选中属性面板
            SglScreen.Instance.RightPanelTabIndex = 0;


            //开启保护锁：防止在同步属性面板时触发 ValueChanged 事件反向覆盖数据
            _isInternalSelectionChange = true;

            RefreshPropertyPanelSelectionState();


            if (w == null) return;

            // WPF 会根据 Data 的类型去 Resources 里找匹配的 DataTemplate
            PropertyPanelHost.Content = PropertyPanel.DataContext;

            _isInternalSelectionChange = false;

            UpdateAdornerLayer();
        }

        private void RefreshPropertyPanelSelectionState()
        {
            if (_selectedWidgets.Count == 1 || CanBatchEditSelectedWidgets())
            {
                if (IdTextBox != null)
                {
                    IdTextBox.ClearValue(TextBox.BorderBrushProperty);
                    IdTextBox.ClearValue(TextBox.BorderThicknessProperty);
                }

                var primaryData = _selectedWidgets.LastOrDefault()?.DataContext as SglWidgetData;
                SetPropertySyncSource(primaryData);
                PropertyPanel.DataContext = primaryData;
            }
            else
            {
                SetPropertySyncSource(null);
                PropertyPanel.DataContext = null;
            }

            PropertyPanelHost.Content = PropertyPanel.DataContext;
            PropertyPanel.IsEnabled = _selectedWidgets.Any();
        }

        private bool CanBatchEditSelectedWidgets()
        {
            if (_selectedWidgets.Count <= 1)
                return false;

            var selectedDatas = _selectedWidgets
                .Select(x => x.DataContext as SglWidgetData)
                .Where(x => x != null)
                .ToList();

            if (selectedDatas.Count != _selectedWidgets.Count)
                return false;

            var firstType = selectedDatas[0].GetType();
            return selectedDatas.All(x => x.GetType() == firstType);
        }

        private void SetPropertySyncSource(SglWidgetData data)
        {
            if (ReferenceEquals(_propertySyncSource, data))
                return;

            if (_propertySyncSource != null)
                _propertySyncSource.PropertyChanged -= PropertySyncSource_PropertyChanged;

            _propertySyncSource = data;

            if (_propertySyncSource != null)
                _propertySyncSource.PropertyChanged += PropertySyncSource_PropertyChanged;
        }

        private void PropertySyncSource_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isInternalSelectionChange || _isSynchronizingMultiSelectionProperties)
                return;

            if (!CanBatchEditSelectedWidgets())
                return;

            if (string.IsNullOrWhiteSpace(e.PropertyName) || MultiSelectExcludedProperties.Contains(e.PropertyName))
                return;

            if (sender is not SglWidgetData source)
                return;

            var propertyInfo = source.GetType().GetProperty(e.PropertyName);
            if (propertyInfo == null || !propertyInfo.CanRead || !propertyInfo.CanWrite)
                return;

            var value = propertyInfo.GetValue(source);
            var targets = _selectedWidgets
                .Select(x => x.DataContext as SglWidgetData)
                .Where(x => x != null && !ReferenceEquals(x, source) && x.GetType() == source.GetType())
                .ToList();

            if (targets.Count == 0)
                return;

            _isSynchronizingMultiSelectionProperties = true;
            try
            {
                foreach (var target in targets)
                {
                    propertyInfo.SetValue(target, value);
                }
            }
            finally
            {
                _isSynchronizingMultiSelectionProperties = false;
            }
        }


        //// 点击 "+" 按钮
        //private void AddEvent_Click(object sender, RoutedEventArgs e)
        //{
        //    // 获取触发点击的按钮
        //    var btn = sender as FrameworkElement;
        //    if (btn == null) return;

        //    // 向上寻找正确的 DataContext
        //    // 如果按钮在 StackPanel 里，通常 btn.DataContext 就是要的对象
        //    if (btn.DataContext is SglWidgetData data)
        //    {
        //        if (EventSelector.SelectedItem != null)
        //        {
        //            var selectedType = (SglEventType)EventSelector.SelectedItem;
        //            data.AddEventRoute(selectedType);
        //        }
        //    }
        //    else
        //    {
        //        // 调试用：看看现在的 DataContext 到底是个啥
        //        string actualType = btn.DataContext?.GetType().Name ?? "null";
        //        System.Diagnostics.Debug.WriteLine($"DataContext 实际类型是: {actualType}");
        //    }
        //}

        //private void RemoveEvent_Click(object sender, RoutedEventArgs e)
        //{
        //    if (sender is Button btn && btn.Tag is SglEventType type)
        //    {
        //        // 关键点：不要直接用 this.DataContext
        //        // 也不要直接用 btn.DataContext (它是 KeyValuePair)
        //        // 要找的是 ItemsControl 或者是外层面板的 DataContext

        //        DependencyObject parent = btn;
        //        while (parent != null && !(parent is ItemsControl))
        //        {
        //            parent = VisualTreeHelper.GetParent(parent);
        //        }

        //        if (parent is ItemsControl itemsControl && itemsControl.DataContext is SglWidgetData data)
        //        {
        //            data.RemoveEventRoute(type);
        //        }
        //        else if (this.DataContext is SglWidgetData rootData)
        //        {
        //            // 如果 ItemsControl 没找到，尝试直接用当前 Page/UserControl 的 DataContext
        //            rootData.RemoveEventRoute(type);
        //        }
        //    }
        //}



        // --- 清空选中 ---
        private void ClearSelection()
        {
            if (_selectedWidgets.Count == 0) return;

            _selectedWidgets.Clear();
            SetPropertySyncSource(null);
            PropertyPanel.DataContext = null;
            PropertyPanelHost.Content = null;
            PropertyPanel.IsEnabled = false;

            // 清除可能残留的吸附状态
            _activeSnapX = null;
            _activeSnapY = null;

            UpdateAdornerLayer();
        }



        private void ZoomToFit_Click(object sender, RoutedEventArgs e)
        {
            var screenData = SglScreen.Instance.SelectedScreen;
            var parent = ViewportContainer;

            // 安全检查
            if (screenData == null || parent == null) return;

            //恢复缩放
            CanvasScale.ScaleX = 1.0;
            CanvasScale.ScaleY = 1.0;

            //  核心计算：使用工程设置中的宽 (Width) 和 高 (Height)
            // 假设 SglPageData 中有 Width 和 Height 属性
            double screenW = ProjectManager.Instance.Width;
            double screenH = ProjectManager.Instance.Height;

            // 如果 ActualWidth 还没准备好 (NaN)，设为 0
            double parentW = double.IsNaN(parent.ActualWidth) ? 0 : parent.ActualWidth;
            double parentH = double.IsNaN(parent.ActualHeight) ? 0 : parent.ActualHeight;

            // 计算居中偏移 (防止出现 NaN 错误)
            double offsetX = (parentW - screenW) / 2;
            double offsetY = (parentH - screenH) / 2;

            //  应用位移 (拦截非法值)
            CanvasTranslate.X = double.IsNaN(offsetX) ? 50 : offsetX;
            CanvasTranslate.Y = double.IsNaN(offsetY) ? 50 : offsetY;

            //  同步画布显示尺寸与背景
            WidgetContainer.Width = screenW;
            WidgetContainer.Height = screenH;


        }
        private void ResetView_Click(object sender, RoutedEventArgs e) { WidgetContainer.Children.Clear(); ClearSelection(); }

        /// <summary>
        /// 递归/分层查找并从数据源中移除
        /// </summary>
        private void RemoveDataFromTree(SglWidgetData data)
        {
            if (data == null) return;

            if (data.Parent == null)
            {
                // 如果没有父级，说明在屏幕顶层列表中
                var topLevelList = SglScreen.Instance.SelectedScreen?.Widgets;
                if (topLevelList != null && topLevelList.Contains(data))
                {
                    topLevelList.Remove(data);
                }
            }
            else
            {
                // 如果有父级，从父级的 Children 集合中移除
                if (data.Parent.Children.Contains(data))
                {
                    data.Parent.Children.Remove(data);
                }
            }
        }
        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e) { double z = e.Delta > 0 ? 1.1 : 0.9; CanvasScale.ScaleX *= z; CanvasScale.ScaleY *= z; }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // ==========================================
            // 焦点保护：如果是正在输入文本/数字，绝不拦截快捷键！
            // 否则用户在文本框里按左右键、或者撤销时会触发画布逻辑。
            // ==========================================
            if (e.OriginalSource is TextBox ||
                e.OriginalSource is System.Windows.Controls.Primitives.TextBoxBase ||
                e.OriginalSource is Xceed.Wpf.Toolkit.IntegerUpDown)
            {
                return; // 让输入框自己处理按键，直接跳出
            }

            bool ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;

            // ==========================================
            // 原有的全局快捷键
            // ==========================================
            if (ctrl && e.Key == Key.S)
            {
                e.Handled = true;
                SaveProject_Click(null, null);
            }
            else if (ctrl && e.Key == Key.O)
            {
                e.Handled = true;
                LoadProject_Click(null, null);
            }
            else if (ctrl && e.Key == Key.D)
            {
                e.Handled = true;
                CloneSelected_Click(null, null);
            }
            else if (ctrl && e.Key == Key.Z)
            {
                PerformUndo();
                e.Handled = true;
            }
            else if (ctrl && e.Key == Key.Y)
            {
                PerformRedo();
                e.Handled = true;
            }
            // ==========================================
            // 方向键微调控件位置
            // ==========================================
            else if (e.Key == Key.Up || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right)
            {
                if (_selectedWidgets.Any())
                {
                    // 贴心小功能：按住 Shift 键时，每次移动 10 个像素，否则 1 个像素
                    int step = shift ? 10 : 1;

                    int deltaX = 0;
                    int deltaY = 0;

                    // 注意：WPF 屏幕坐标系中，越往下 Y 越大。
                    // (如果你要求严格的“上Y+ 下Y-”，请把 Up 和 Down 的符号互换)
                    if (e.Key == Key.Up) deltaY = -step; // 视觉向上：Y-
                    if (e.Key == Key.Down) deltaY = step;  // 视觉向下：Y+
                    if (e.Key == Key.Left) deltaX = -step; // 视觉向左：X-
                    if (e.Key == Key.Right) deltaX = step;  // 视觉向右：X+

                    RecordBeforeChange();

                    foreach (var w in _selectedWidgets)
                    {
                        if (w.DataContext is SglWidgetData data)
                        {
                            data.X += deltaX;
                            data.Y += deltaY;

                            // 同步更新画布上的视觉位置
                            Canvas.SetLeft(w, data.X);
                            Canvas.SetTop(w, data.Y);
                        }
                    }

                    // 更新高亮虚线框的位置
                    UpdateAdornerLayer(false);

                    // 【核心】斩断事件，防止 WPF 把焦点切走或者引起 ScrollViewer 滚动
                    e.Handled = true;
                }
            }
        }


        private void Exit_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
        private void Undo_Click(object sender, RoutedEventArgs e) { PerformUndo(); }
        private void Redo_Click(object sender, RoutedEventArgs e) { PerformRedo(); }
        private void CloneSelected_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedWidgets.Any()) return;
            RecordBeforeChange();

            var targetDatas = _selectedWidgets
                .Select(w => w.DataContext as SglWidgetData)
                .Where(d => d != null)
                .ToList();

            // 创建一个临时列表，记录这一批克隆中新产生的 ID
            List<string> newlyCreatedIds = new List<string>();

            _selectedWidgets.Clear();
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            };

            foreach (var oldData in targetDatas)
            {
                // 深拷贝
                string json = JsonConvert.SerializeObject(oldData, settings);
                var newData = JsonConvert.DeserializeObject(json, oldData.GetType(), settings) as SglWidgetData;

                if (newData == null) continue;

                //偏移位置
                newData.X += 20;
                newData.Y += 20;

                // 递归修复时，把这个列表传进去
                RepairCloneRecursive(newData, oldData.Parent, newlyCreatedIds);

                //  挂载数据树
                if (oldData.Parent != null)
                {
                    oldData.Parent.Children.Add(newData);
                }
                else
                {
                    SglScreen.Instance.SelectedScreen?.Widgets.Add(newData);
                }

            }

            RefreshPropertyPanelSelectionState();

            RefreshCanvas(SglScreen.Instance.SelectedScreen);
            SglScreen.Instance.WidgetCount = SglScreen.GetAllWidgetsCount();
            UpdateAdornerLayer();
        }

        private void RepairCloneRecursive(SglWidgetData data, SglWidgetData parent, List<string> reservedIds)
        {
            // 分配新 ID 并占位
            data.Id = GetNextAvailableIdWithReserved(data.Type, reservedIds);
            data.Tag = data.Id; // 强制同步 Tag
            data.Parent = parent;
            reservedIds.Add(data.Id);

            //关键补全：重新生成事件处理函数名
            // 确保生成的函数名如 rectangle9_clicked_handler 而不是旧的 rectangle7_...
            data.EventCallbackName = $"{data.Id.ToLower()}_event_handler";

            // 获取所有 Key 的列表
            var keys = data.EventRoutes.Keys.ToList();

            foreach (var key in keys)
            {
                //获取包装类对象
                var routeValue = data.EventRoutes[key];

                //  检查对象不为空（如果用了包装类，通常检查 HandlerName 是否有效）
                if (routeValue != null)
                {
                    // 转换事件枚举为蛇形命名，例如 Clicked -> clicked
                    string eventSuffix = key.ToString().ToSnakeCase().ToLower();

                    //  关键修改包装类内部的属性，而不是覆盖对象本身
                    routeValue.HandlerName = $"{data.Id.ToLower()}_{eventSuffix}_handler";
                }
            }

            //  递归处理子控件
            if (data.Children != null)
            {
                foreach (var child in data.Children)
                {
                    RepairCloneRecursive(child, data, reservedIds);
                }
            }
            SglScreen.Instance.WidgetCount = SglScreen.GetAllWidgetsCount();
        }

        private string GetNextAvailableIdWithReserved(SglMapping.SglType type, List<string> reservedIds)
        {
            string prefix = type.ToString().ToLower();

            // 获取库里已有的 ID
            var existingIds = SglScreen.Instance.ScreenList
                .SelectMany(s => GetAllWidgetsRecursive(s.Widgets))
                .Select(w => w.Id);

            //合并【已有的】和【这一批刚生成的】
            var allIds = existingIds.Concat(reservedIds);

            var allUsedIndices = allIds
                .Where(id => id.StartsWith(prefix))
                .Select(id =>
                {
                    string numPart = id.Substring(prefix.Length);
                    return int.TryParse(numPart, out int index) ? index : -1;
                })
                .Where(i => i >= 0)
                .Distinct()
                .ToList();

            //  找空位 (逻辑保持不变)
            int nextIndex = 0;
            allUsedIndices.Sort();
            foreach (int index in allUsedIndices)
            {
                if (index == nextIndex) nextIndex++;
                else if (index > nextIndex) break;
            }

            return prefix + nextIndex;
        }
        private void About_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.Owner = this;
            about.ShowDialog();
        }

        private void GenerateCode_Click(object sender, RoutedEventArgs e)
        {
            UpdateCodePreview();
        }
        private void IdTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CommitIdChange(sender as TextBox);
                // 清除焦点，视觉上告知用户编辑完成
                Keyboard.ClearFocus();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // 撤销修改，恢复原 ID
                var tb = sender as TextBox;
                var currentWidget = (tb.DataContext ?? tb.Tag) as SglWidgetData;
                if (currentWidget != null) tb.Text = currentWidget.Id;

                Keyboard.ClearFocus();
                e.Handled = true;
            }
        }
        private void CommitIdChange(TextBox textBox)
        {
            if (textBox == null) return;
            var currentWidget = (textBox.DataContext ?? textBox.Tag) as SglWidgetData;
            if (currentWidget == null) return;

            string newId = textBox.Text.Trim();

            // 如果没改，恢复正常边框颜色并退出
            if (newId == currentWidget.Id)
            {
                textBox.ClearValue(TextBox.BorderBrushProperty); // 恢复系统默认边框
                return;
            }

            //基础校验：不能为空、不能有空格、必须符合C变量命名(不以数字开头等)
            bool isInvalidFormat = string.IsNullOrWhiteSpace(newId) ||
                                   newId.Contains(" ") ||
                                   char.IsDigit(newId[0]);

            //  全局唯一性校验（递归扫描）
            bool isDuplicate = SglScreen.Instance.ScreenList
                .SelectMany(s => GetAllWidgetsRecursive(s.Widgets))
                .Any(w => w != currentWidget && w.Id == newId);

            if (isInvalidFormat || isDuplicate)
            {
                // --- 静默错误处理 ---
                // 变红提示用户 ID 有问题
                textBox.BorderBrush = Brushes.Red;
                textBox.BorderThickness = new Thickness(1.5);

                // 恢复为旧 ID，不打断用户操作
                textBox.Text = currentWidget.Id;

                // 可选：在状态栏显示简短提示，而不是弹窗
                // StatusText.Text = isDuplicate ? "ID 已存在" : "ID 格式非法";
            }
            else
            {
                // --- 校验通过 ---
                textBox.ClearValue(TextBox.BorderBrushProperty); // 恢复边框
                currentWidget.Id = newId;
                _isDirty = true;




                // 如果有树状图，记得刷新树状图的节点名称
                // RefreshWidgetTree(); 
            }
        }
        private void IdTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            var tb = sender as TextBox;
            if (tb == null) return;

            tb.Tag = tb.DataContext;
            // 开始编辑时，如果有红色边框，先清掉
            tb.ClearValue(TextBox.BorderBrushProperty);

            // 全选文本，方便用户直接覆盖输入
            tb.SelectAll();
        }

        private void IdTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitIdChange(sender as TextBox);
        }

        private void PropertyPanel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is DependencyObject source && FindPropertyPanelEditor(source) != null)
            {
                BeginPropertyPanelEditSession();
            }
        }

        private void PropertyPanel_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus is DependencyObject source && FindPropertyPanelEditor(source) != null)
            {
                BeginPropertyPanelEditSession();
            }
        }

        private void PropertyPanel_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.NewFocus is DependencyObject newFocus && IsDescendantOfPropertyPanel(newFocus))
                return;

            EndPropertyPanelEditSession();
        }

        private static DependencyObject FindPropertyPanelEditor(DependencyObject source)
        {
            while (source != null)
            {
                if (source is TextBox || source is ComboBox || source is CheckBox || source is Xceed.Wpf.Toolkit.IntegerUpDown || source is Xceed.Wpf.Toolkit.ColorPicker)
                    return source;

                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }

            return null;
        }

        private bool IsDescendantOfPropertyPanel(DependencyObject source)
        {
            while (source != null)
            {
                if (source == PropertyPanel)
                    return true;

                source = System.Windows.Media.VisualTreeHelper.GetParent(source);
            }

            return false;
        }
        // --- 新增：层级控制 ---

        // --- 修改后的置顶逻辑 ---
        private void BringToFront_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedWidgets.Any()) return;
            RecordBeforeChange();

            // 按当前 ZIndex 升序排列，保持选中项之间的相对层级
            var sorted = _selectedWidgets.OrderBy(x => Canvas.GetZIndex(x)).ToList();

            foreach (var w in sorted)
            {
                if (w.DataContext is SglWidgetData data)
                {
                    var parentList = data.Parent != null ? data.Parent.Children : SglScreen.Instance.SelectedScreen.Widgets;

                    // 移动数据位置：先删再加，自动触发 TreeView 刷新
                    parentList.Remove(data);
                    parentList.Add(data);
                }
            }

            // 执行归一化：根据新的数据顺序重排所有组件的 ZIndex
            var firstData = _selectedWidgets.First().DataContext as SglWidgetData;
            NormalizeZIndex(firstData?.Parent);

            UpdateAdornerLayer();
        }

        // --- 修改后的置底逻辑 ---
        private void SendToBack_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedWidgets.Any()) return;
            RecordBeforeChange();

            // 按当前 ZIndex 降序排列，保持选中项之间的相对层级
            var sorted = _selectedWidgets.OrderByDescending(x => Canvas.GetZIndex(x)).ToList();

            foreach (var w in sorted)
            {
                if (w.DataContext is SglWidgetData data)
                {
                    var parentList = data.Parent != null ? data.Parent.Children : SglScreen.Instance.SelectedScreen.Widgets;

                    // 移动数据到索引 0
                    parentList.Remove(data);
                    parentList.Insert(0, data);
                }
            }

            // 执行归一化
            var firstData = _selectedWidgets.First().DataContext as SglWidgetData;
            NormalizeZIndex(firstData?.Parent);


            UpdateAdornerLayer();
        }




        // --- IDropTarget 接口实现 ---
        // --- 实现 IDropTarget 接口缺少的方法 ---

        void IDropTarget.DragEnter(IDropInfo dropInfo)
        {
            // 接口要求实现，通常逻辑在 DragOver 处理，这里留空即可
        }

        void IDropTarget.DragLeave(IDropInfo dropInfo)
        {
            // 接口要求实现，留空即可
        }

        void IDropTarget.DropHint(IDropHintInfo dropHintInfo)
        {
            // 接口要求实现，用于显示拖拽提示，留空即可
        }


        void IDropTarget.DragOver(IDropInfo dropInfo)
        {
            var draggedData = dropInfo.Data as SglWidgetData;
            var targetData = dropInfo.TargetItem as SglWidgetData;

            if (draggedData != null)
            {
                // 逻辑：如果目标是容器，允许 DropTargetAdorners.Highlight (进入)
                // 如果目标不是容器，只允许在它上下移动位置
                if (targetData != null && targetData.IsContainer)
                {
                    // 允许进入容器
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                    dropInfo.Effects = DragDropEffects.Move;
                }
                else
                {
                    // 目标不是容器，或者是根画布，只允许插入排序
                    dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
                    dropInfo.Effects = DragDropEffects.Move;
                }
            }
        }



        void IDropTarget.Drop(IDropInfo dropInfo)
        {
            var draggedData = dropInfo.Data as SglWidgetData;
            if (draggedData == null) return;

            // 记录移动前的父级，用于判断是否发生了跨容器移动
            var oldParent = draggedData.Parent;

            // 执行默认移动逻辑
            GongSolutions.Wpf.DragDrop.DragDrop.DefaultDropHandler.Drop(dropInfo);

            //更新父子引用
            UpdateDataParentAfterDrop(dropInfo, draggedData);

            //  坐标逻辑：如果发生了跨容器移动，则居中
            if (draggedData.Parent != oldParent && draggedData.Parent != null)
            {
                // 目标是容器，计算中心点
                // 这里直接取容器数据的 Width/Height 算出中心
                // 减去被拖拽组件自身尺寸的一半，实现视觉居中
                draggedData.X = (draggedData.Parent.W / 2) - (draggedData.W / 2);
                draggedData.Y = (draggedData.Parent.H / 2) - (draggedData.H / 2);

                // 边界检查：防止坐标为负数
                if (draggedData.X < 0) draggedData.X = 0;
                if (draggedData.Y < 0) draggedData.Y = 0;
            }
            else if (draggedData.Parent == null && oldParent != null)
            {
                // 如果是从容器拖回根画布，可以考虑放到画布中心或者保持位置
                // 这里演示放到画布中心 (假设 ScreenArea 有固定宽高)
                draggedData.X = 10; // 或者根据需求设置默认位置
                draggedData.Y = 10;
            }

            //  同步 ZIndex 和刷新 UI
            NormalizeZIndex(draggedData.Parent);
            RefreshCanvas(SglScreen.Instance.SelectedScreen);

            //  恢复选中
            var newUI = FindVisualByData(draggedData);
            if (newUI is Border b)
            {
                _selectedWidgets.Clear();
                _selectedWidgets.Add(b);
                SyncWidgetTreeSelection(draggedData);
                RefreshPropertyPanelSelectionState();
            }


            UpdateAdornerLayer();
        }

        /// <summary>
        /// 辅助方法：判断拖拽后的目标父级
        /// </summary>
        private void UpdateDataParentAfterDrop(IDropInfo dropInfo, SglWidgetData data)
        {
            // 获取目标集合（即放下后的父级集合）
            var targetCollection = dropInfo.TargetCollection as ObservableCollection<SglWidgetData>;

            if (targetCollection == SglScreen.Instance.SelectedScreen.Widgets)
            {
                data.Parent = null; // 拖到了根层级
            }
            else
            {
                // 通过 TargetCollection 寻找拥有这个集合的父对象
                // 遍历所有组件找到哪个容器的 Children 正是这个 targetCollection
                data.Parent = FindContainerByCollection(SglScreen.Instance.SelectedScreen.Widgets, targetCollection);
            }
        }

        private SglWidgetData FindContainerByCollection(ObservableCollection<SglWidgetData> currentLevel, ObservableCollection<SglWidgetData> target)
        {
            foreach (var item in currentLevel)
            {
                if (item.Children == target) return item;
                var found = FindContainerByCollection(item.Children, target);
                if (found != null) return found;
            }
            return null;
        }

        /// <summary>
        /// 归一化层级：使 ZIndex 与 TreeView 索引严格对应
        /// </summary>
        private void NormalizeZIndex(SglWidgetData parentData = null)
        {
            var collection = parentData != null ? parentData.Children : SglScreen.Instance.SelectedScreen.Widgets;

            for (int i = 0; i < collection.Count; i++)
            {
                var data = collection[i];
                var ui = FindVisualByData(data);
                if (ui != null)
                {
                    // 索引越大，ZIndex 越高，显示在越前面
                    Canvas.SetZIndex(ui, i);
                }
            }
        }

        private void Process_OutputDataReceived(object sender, System.Diagnostics.DataReceivedEventArgs e)
        {
            // 处理命令行的输出结果
            if (!string.IsNullOrEmpty(e.Data))
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    LogInfo.AppendText(e.Data + Environment.NewLine);
                });
            }
        }

        private void AppendAnsiLog(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            SglScreen.Instance.BottomPanelTabIndex = 1;
            this.Dispatcher.Invoke(() =>
            {
                // 获取或创建文档的最后一个段落
                Paragraph para = LogInfo.Document.Blocks.LastBlock as Paragraph;
                if (para == null)
                {
                    para = new Paragraph();
                    LogInfo.Document.Blocks.Add(para);
                }

                // 正则表达式匹配 ANSI 转义序列： \x1B[ (参数) m
                // 注意：\033 在 C# 字符串中表示为 \x1B
                string[] parts = Regex.Split(text, @"(\x1B\[[0-9;]*m)");

                Brush currentForeground = Brushes.White; // 默认颜色

                foreach (var part in parts)
                {
                    if (string.IsNullOrEmpty(part)) continue;

                    if (part.StartsWith("\x1B["))
                    {
                        // 根据定义的宏进行颜色映射
                        switch (part)
                        {
                            case "\x1B[0m": currentForeground = Brushes.White; break;       // NONE
                            case "\x1B[31m": currentForeground = Brushes.Red; break;         // RED
                            case "\x1B[32m": currentForeground = Brushes.LimeGreen; break;   // GREEN
                            case "\x1B[33m": currentForeground = Brushes.Yellow; break;      // YELLOW
                            case "\x1B[34m": currentForeground = Brushes.DeepSkyBlue; break; // BLUE
                            case "\x1B[35m": currentForeground = Brushes.MediumPurple; break;// PURPLE
                            case "\x1B[36m": currentForeground = Brushes.Cyan; break;         // CYAN
                            case "\x1B[37m": currentForeground = Brushes.White; break;       // WHITE
                            case "\x1B[1;34m": currentForeground = Brushes.LightSkyBlue; break; // LIGHT_BLUE
                            default:
                                // 处理其他未定义的代码（可选）
                                if (part == "\x1B[1m") { /* 加粗逻辑 */ }
                                break;
                        }
                    }
                    else
                    {
                        // 普通文本部分，创建带有颜色的 Run
                        para.Inlines.Add(new Run(part) { Foreground = currentForeground });
                    }
                }

                // 如果文本包含换行符，开启新段落以便下次输入
                //if (text.Contains(Environment.NewLine) || text.Contains("\n"))
                //{
                //    LogInfo.Document.Blocks.Add(new Paragraph());
                //}

                // 自动滚动到底部
                LogInfo.ScrollToEnd();
            });
        }

        private void LogInfo_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var rtb = sender as RichTextBox;
            if (rtb == null) return;

            // 寻找内部 ScrollViewer
            var sv = GetScrollViewer(rtb);
            if (sv != null)
            {
                // 这里的 3 是滚动倍率，可以根据手感调整
                double offset = sv.VerticalOffset - (e.Delta / 3.0);
                sv.ScrollToVerticalOffset(offset);
                e.Handled = true;
            }
        }

        // 辅助函数：获取内部 ScrollViewer
        private ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer) return (ScrollViewer)depObj;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private void ExportGitPull_Click(object sender, RoutedEventArgs e)
        {

            var result = MessageBox.Show("拉取新代码未经测试可能会有未知异常,是否继续？",
                                            "确认",
                                            MessageBoxButton.YesNo,
                                            MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            // 切换 Tab 页面
            SglScreen.Instance.BottomPanelTabIndex = 1;
            LogInfo.Document.Blocks.Clear();
            try
            {


                //获取程序路径
                string path = AppDomain.CurrentDomain.BaseDirectory;


                //清空目录内容  这样删除不干净，.git目录下报异常
                //Directory.Delete(Path.Combine(path, "sgl"), true);
                using (Process rdProcess = new Process())
                {
                    rdProcess.StartInfo.FileName = "cmd.exe";
                    rdProcess.StartInfo.Arguments = $"/C rd /s /q \"{Path.Combine(path, "sgl")}\"";
                    rdProcess.StartInfo.UseShellExecute = false;
                    rdProcess.StartInfo.CreateNoWindow = true;
                    rdProcess.Start();
                    rdProcess.WaitForExit();
                }

                //var cmd = Cli.Wrap("git.exe").WithArguments(new[] { "clone", "--progress", "https://www.gitee.com/sgl-org/sgl" }).WithWorkingDirectory(path);

                //await foreach (var cmdEvent in cmd.ListenAsync())
                //{
                //    switch (cmdEvent)
                //    {
                //        case StartedCommandEvent started:
                //            AppendAnsiLog($"Process started; ID: {started.ProcessId}");
                //            break;
                //        case StandardOutputCommandEvent stdOut:
                //            AppendAnsiLog($"Out> {stdOut.Text}");
                //            break;
                //        case StandardErrorCommandEvent stdErr:
                //            AppendAnsiLog($"Err> {stdErr.Text}");
                //            break;
                //        case ExitedCommandEvent exited:
                //            AppendAnsiLog($"Process exited; Code: {exited.ExitCode}");
                //            break;
                //    }
                //}

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = @"git";
                startInfo.Arguments = $"clone --progress https://www.gitee.com/sgl-org/sgl"; // Replace "dir" with your command
                startInfo.WorkingDirectory = path;
                // Set the process options
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;

                // Create the process and start it
                Process process = new Process();
                process.StartInfo = startInfo;
                process.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                process.ErrorDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                AppendAnsiLog($"\x1B[31m{ex.Message}");
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            //DispatcherTimer tm = new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
            //tm.Tick += delegate
            //{
            manual_check_update = true;
            AutoUpdater.Start(OTA_URL);
            AppendAnsiLog($"\x1B[37m检查更新中，请稍候...\n");
            //AutoUpdater.Start("https://xfdr0805.github.io/software/sgl/update.xml");
            //AutoUpdater.Start("http://192.168.2.244:4000/software/sgl/update.xml");
            //};
            //tm.Start();
        }

        private void AddToEnvButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;

                // 定义需要添加的两个核心路径
                string mingwPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(basePath, @"tools\mingw64\bin"));
                string toolsPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(basePath, @"tools")); // make 所在目录

                // 安全检查一：确保文件夹在本地真实存在
                if (!System.IO.Directory.Exists(mingwPath) || !System.IO.Directory.Exists(toolsPath))
                {
                    MessageBox.Show("工具链路径不完整，拒绝添加环境变量！\n请检查 tools 目录及其内容是否完整。",
                                    "安全警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                EnvironmentVariableTarget targetLevel = EnvironmentVariableTarget.User;
                string currentPath = Environment.GetEnvironmentVariable("PATH", targetLevel) ?? "";

                // 将 PATH 按照分号切分成数组
                string[] pathParts = currentPath.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                //检查是否“同时存在”
                bool mingwExists = pathParts.Any(p => string.Equals(p.TrimEnd('\\', '/'), mingwPath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase));
                bool toolsExists = pathParts.Any(p => string.Equals(p.TrimEnd('\\', '/'), toolsPath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase));

                // 只有同时存在时才不添加
                if (mingwExists && toolsExists)
                {
                    MessageBox.Show("MinGW 和 make 的路径均已完整存在于环境变量中，无需重新添加！",
                                    "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                //  否则重新添加并覆盖原来的（清理旧残留，确保成对出现）
                // 过滤掉原本 PATH 中可能存在的残留项（只剔除的工具路径，保留用户的其他变量）
                var cleanedPaths = pathParts.Where(p =>
                    !string.Equals(p.TrimEnd('\\', '/'), mingwPath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(p.TrimEnd('\\', '/'), toolsPath.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase)
                ).ToList();

                // 将这两个工具链路径重新作为一组追加到末尾
                cleanedPaths.Add(mingwPath);
                cleanedPaths.Add(toolsPath);

                //  安全拼接并写入
                string newPath = string.Join(";", cleanedPaths);
                Environment.SetEnvironmentVariable("PATH", newPath, targetLevel);

                MessageBox.Show("已成功修复并覆盖工具链环境变量！\n\n注意：\n1. SglDesigner 可能需要重启才能在全局读取到新变量。\n2. 已打开的 cmd/PowerShell 窗口需要重新打开才能生效。",
                                "操作成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (System.Security.SecurityException)
            {
                MessageBox.Show("权限不足！操作被拒绝。",
                                "权限错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"修改环境变量时发生未知错误：\n{ex.Message}",
                                "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ExportCodeToBuild()
        {
            var screenList = SglScreen.Instance.ScreenList;
            if (screenList == null || !screenList.Any())
            {
                MessageBox.Show("没有可导出的屏幕内容");
                return;
            }

            // 选择导出文件夹
            // 注意：OpenFolderDialog 需要 .NET 8 或更高版本。
            // 如果是旧版本 Microsoft.Win32.SaveFileDialog

            //获取程序路径
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string exportPath = Path.Combine(path, "simulator/ui_export"); ;

            try
            {
                if (!Directory.Exists(exportPath))
                {
                    Directory.CreateDirectory(exportPath);
                }
                else
                {
                    //清空目录内容
                    Directory.Delete(exportPath, true);
                }




            }
            catch (Exception ex)
            {

                AppendAnsiLog($"\x1B[31m{ex.Message}");
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);

                return;
            }



            // 创建子目录结构
            string fontDir = Path.Combine(exportPath, "fonts");
            string imgDir = Path.Combine(exportPath, "images");
            string screenDir = Path.Combine(exportPath, "screens");

            Directory.CreateDirectory(fontDir);
            Directory.CreateDirectory(imgDir);
            Directory.CreateDirectory(screenDir);





            // 递归获取所有控件（防止嵌套在 Panel 里的图片被漏掉）
            var list = new List<SglWidgetData>();

            List<SglWidgetData> GetFlattenWidgets(IEnumerable<SglWidgetData> ws)
            {

                foreach (var w in ws)
                {
                    list.Add(w);
                    if (w.Children.Count > 0) list.AddRange(GetFlattenWidgets(w.Children));
                }
                return list;
            }
            //导出所有图片 (images/)
            IEnumerable<SglWidgetData> imageWidgets = null;

            foreach (var screen in SglScreen.Instance.ScreenList)
            {
                var allWidgets = screen.Widgets;
                var flatWidgets = GetFlattenWidgets(allWidgets);
                imageWidgets = flatWidgets.Where(w => w is SglIconData || w is SglExtImageData || w is SglCircleData || w is SglPanelData || w is SglQrcodeData).ToList();
            }

            HashSet<string> exportedFiles = new HashSet<string>();

            if (imageWidgets != null)
            {
                foreach (var widget in imageWidgets)
                {
                    string resName = "";
                    string prefix = "";

                    // 根据类型确定前缀和资源名
                    if (widget is SglIconData icon)
                    {
                        resName = icon.IconName;
                        prefix = "ico_" + widget.Id; // 图标前缀
                    }
                    else if (widget is SglExtImageData img)
                    {
                        resName = img.PixmapVarName;
                        prefix = "img_" + widget.Id; ; // 图片前缀
                    }
                    else if (widget is SglPanelData bg)
                    {
                        resName = bg.PixmapVarName;
                        prefix = "img_" + widget.Id; ; // 图片前缀
                    }
                    else if (widget is SglCircleData ciccle)
                    {
                        resName = ciccle.PixmapVarName;
                        prefix = "img_" + widget.Id; ; // 图片前缀
                    }
                    else if (widget is SglQrcodeData qrcode)
                    {
                        resName = qrcode.PixmapVarName;
                        prefix = "img_" + widget.Id; ; // 图片前缀
                    }

                    if (string.IsNullOrEmpty(resName)) continue;

                    // 资源查找逻辑 (保持不变)
                    var resItem = SglResManager.Resources.FirstOrDefault(r =>
                        r.Name.Equals(resName, StringComparison.OrdinalIgnoreCase));

                    if (resItem == null)
                    {
                        string pureName = Path.GetFileNameWithoutExtension(resName);
                        resItem = SglResManager.Resources.FirstOrDefault(r =>
                            r.Name.Equals(pureName, StringComparison.OrdinalIgnoreCase));
                    }

                    if (resItem == null) continue;

                    // --- 核心修改：生成带前缀的新名称 ---
                    string finalVarName = $"{prefix}_{resItem.Name}";

                    if (exportedFiles.Contains(finalVarName)) continue;

                    //int targetW = (int)widget.W;
                    //int targetH = (int)widget.H;
                    string format = ProjectManager.Instance.ColorFormat.ToString();

                    string code = $"// Error: File not found {resItem.FilePath}";

                    //if (!File.Exists(res.FilePath)) return $"// Error: File not found {res.FilePath}";

                    // 获取当前工程下的 assets 字体路径
                    // 假设 res.FilePath 存储的是相对于当前工程的路径
                    BitmapImage sourceBmp = SglImageGenerator.LoadBitmapNoLock(resItem.FilePath);

                    // 强制转为 Bgra32
                    var bgraBmp = new FormatConvertedBitmap(sourceBmp, PixelFormats.Bgra32, null, 0);

                    // 缩放
                    ScaleTransform scale = new ScaleTransform((double)widget.W / bgraBmp.PixelWidth, (double)widget.H / bgraBmp.PixelHeight);
                    Console.WriteLine($"{bgraBmp.PixelWidth} {bgraBmp.PixelHeight} {scale.Value}");
                    TransformedBitmap scaledBmp = new TransformedBitmap(bgraBmp, scale);
                    //高质量采样 导出
                    RenderOptions.SetBitmapScalingMode(scaledBmp, BitmapScalingMode.HighQuality);
                    if (widget is not SglIconData)
                    {

                        code = SglImageGenerator.ConvertToSglPixmapCode(scaledBmp, finalVarName.Substring(4), format);
                    }
                    else
                    {
                        code = SglImageGenerator.ConvertToSgl4BitIconCode(scaledBmp, finalVarName.Substring(4));
                    }


                    string outPath = Path.Combine(imgDir, $"{finalVarName.Substring(4)}.c");
                    File.WriteAllText(outPath, code);
                    exportedFiles.Add(finalVarName);
                }
            }

            //  导出字体 (fonts/)
            //var fontRes = SglResManager.Resources.Where(r => r.ResType == SglResType.FontCSource);
            //foreach (var res in fontRes)
            //{
            //    File.WriteAllText(Path.Combine(fontDir, $"{res.Name}.c"), res.GeneratedCode);
            //}
            //  导出字体 (直接从 assets 复制)
            var fontRes = SglResManager.Resources.Where(r => r.ResType == SglResType.FontCSource);
            foreach (var res in fontRes)
            {
                // 获取当前工程下的 assets 字体路径
                // 假设 res.FilePath 存储的是相对于当前工程的路径
                string sourceFilePath = path = Path.Combine(ProjectManager.ProjectDir, res.FilePath); ;

                if (string.IsNullOrEmpty(sourceFilePath) || !File.Exists(sourceFilePath))
                {
                    Console.WriteLine("警告: 找不到字体取模文件, 跳过复制。");
                    continue;
                }

                // 目标路径：导出目录/fonts/文件名
                string fileName = Path.GetFileName(sourceFilePath);
                string destFilePath = Path.Combine(fontDir, fileName);

                try
                {
                    // 执行复制（true 表示覆盖同名文件）
                    File.Copy(sourceFilePath, destFilePath, true);
                    Console.WriteLine($"已导出字体: {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"导出字体 {res.Name} 失败: {ex.Message}");
                }
            }

            //  导出屏幕 (screens/)
            List<string> screenNames = new List<string>();

            try
            {

                //foreach (var screen in SglScreen.Instance.ScreenList)
                {

                    //遍历并导出每个屏幕的 .c 文件
                    foreach (var page in screenList)
                    {
                        string fileName = $"{page.Id}.c";
                        string fullPath = System.IO.Path.Combine(screenDir, fileName);

                        // 调用的代码生成逻辑
                        string cCode = SglMapping.ConvertPageToCode(page);
                        System.IO.File.WriteAllText(fullPath, cCode);

                        screenNames.Add(page.Id);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //导出事件
            StringBuilder sbEvent = new StringBuilder();
            sbEvent.AppendLine("/* SGL Auto Generated Events */");
            sbEvent.AppendLine("#include \"ui.h\"\n");

            foreach (var screen in SglScreen.Instance.ScreenList)
            {
                // 遍历每个屏幕的控件树生成函数体
                foreach (var widget in screen.Widgets)
                {
                    sbEvent.Append(SglMapping.GenerateCEventHandlers(widget, new HashSet<string>()));
                }
            }

            File.WriteAllText(Path.Combine(exportPath, "event.c"), sbEvent.ToString());

            string headerContent = SglMapping.GenerateHeaderFile(screenNames, SglResManager.Resources, exportedFiles);
            File.WriteAllText(Path.Combine(exportPath, "ui.h"), headerContent);
            string defineContent = SglMapping.GenerateDefineFile(screenNames, SglResManager.Resources, exportedFiles);
            File.WriteAllText(Path.Combine(exportPath, "ui.c"), defineContent);
            string helperContent = SglMapping.GenerateHelperFile(screenNames, SglResManager.Resources, exportedFiles);
            File.WriteAllText(Path.Combine(exportPath, "helper.c"), helperContent);

            //MessageBox.Show($"导出完成！共生成 {screenList.Count} 个屏幕文件和 1 个头文件。",
            //                       "导出成功", MessageBoxButton.OK, MessageBoxImage.Information);

        }

       

        private void ExportRun_Click(object sender, RoutedEventArgs e)
        {
            // 切换 Tab 页面
            SglScreen.Instance.BottomPanelTabIndex = 1;
            LogInfo.Document.Blocks.Clear();
            try
            {
                //获取程序路径
                string path = AppDomain.CurrentDomain.BaseDirectory;

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = System.IO.Path.Combine(path, @"tools/make");
                string ccPrefixPath = System.IO.Path.Combine(path, @"tools/mingw64/bin/").Replace("\\", "/");
                startInfo.Arguments = $"-j16 run CC_PREFIX={ccPrefixPath}";
                startInfo.WorkingDirectory = System.IO.Path.Combine(path, @"simulator");
                // Set the process options
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;

                // Create the process and start it
                Process process = new Process();
                process.StartInfo = startInfo;
                process.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                process.ErrorDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message, "警告！", MessageBoxButton.OK, MessageBoxImage.Error);
            }
           

        }
        private void SglConfig_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new SglConfigWindow
            {
                Owner = this
            };
            configWindow.ShowDialog();
        }

        private void ExportBuild_Click(object sender, RoutedEventArgs e)
        {
            // 切换 Tab 页面
            SglScreen.Instance.BottomPanelTabIndex = 1;
            LogInfo.Document.Blocks.Clear();

            try
            {
                //编译前导出
                ExportCodeToBuild();

                ProcessStartInfo startInfo = new ProcessStartInfo();
                //获取程序路径                                      
                string path = AppDomain.CurrentDomain.BaseDirectory;
                startInfo.FileName = System.IO.Path.Combine(path, @"tools/make");
                //startInfo.Arguments = $"-j16 CC_PREFIX={System.IO.Path.Combine(path, @"tools\mingw64\bin\")}"; 
                string ccPrefixPath = System.IO.Path.Combine(path, @"tools/mingw64/bin/").Replace("\\", "/");
                startInfo.Arguments = $"-j16 CC_PREFIX={ccPrefixPath}";

                startInfo.WorkingDirectory = System.IO.Path.Combine(path, @"simulator");
                //GC 不支持中文路径,否则找不到编译工具


                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = true;

                Process process = new Process();
                process.StartInfo = startInfo;
                process.OutputDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                process.ErrorDataReceived += new DataReceivedEventHandler(OutputDataReceived);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message,"警告！",MessageBoxButton.OK,MessageBoxImage.Error);
            }
            

        }

        private void OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
      
            Console.WriteLine(e.Data);
            this.Dispatcher.Invoke(() =>
            {
                AppendAnsiLog(e.Data + Environment.NewLine);  //Environment.NewLine
            });
        }
    }
}
