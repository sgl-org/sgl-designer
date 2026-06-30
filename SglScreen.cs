using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SglDesigner
{

    // 继承自 ValueChangedMessage，专门用来传递选中的 SglPageData 对象
    public class ScreenChangedMessage : ValueChangedMessage<SglPageData>
    {
        public ScreenChangedMessage(SglPageData value) : base(value)
        {
        }
    }

    public partial class SglPageData : ObservableObject
    {
        [ObservableProperty] private string _id;
        [ObservableProperty] private string _name;
        //[ObservableProperty] private int _width = 320;
        //[ObservableProperty] private int _height = 240;
        //[ObservableProperty] private SglImageConverter.ColorFormat _colorFormat;
        //[ObservableProperty] private string _exportFolder;
        [ObservableProperty] private bool _isEditing;

        [ObservableProperty] private SglWidgetData _selectedWidgetData;

        // 该屏幕拥有的所有控件（TreeView 绑定此项）
        public ObservableCollection<SglWidgetData> Widgets { get; } = new();

        // 当 ID 属性改变时，这个方法会被自动调用
        partial void OnIdChanged(string oldValue, string newValue)
        {
            // 如果新旧值一样，或者初始加载，不处理
            if (string.IsNullOrEmpty(oldValue) || oldValue == newValue) return;

            // 2. 发送全局消息，让所有的 SglChangeScreenAction 自动更新自己的 TargetPageId
            WeakReferenceMessenger.Default.Send(new PageIdChangedMessage(oldValue, newValue));

            //  通知 UI：Id 属性本身变了
            // 这会强制 ComboBox 重新对 SelectedValuePath="Id" 进行比对
            OnPropertyChanged(nameof(Id));
        }

        partial void OnNameChanged(string value)
        {
            var root = Widgets.FirstOrDefault(w => w.Parent == null);
            if (root != null)
            {
                root.Id = value;
                root.Tag = value;
            }
        }
        public SglPageData(string id, string name)
        {
            Id = id;
            Name = name;



        }

    }


    public partial class SglScreen : ObservableObject
    {
        private static SglScreen _instance;
        public static SglScreen Instance => _instance ??= new SglScreen();
        //控件总数量 
        [JsonIgnore]
        [ObservableProperty] private int _widgetCount = 1;

        // ListBox 绑定的数据源
        [ObservableProperty]
        private ObservableCollection<SglPageData> _screenList = new();

        // ListBox 当前选中的屏幕
        [ObservableProperty]
        private SglPageData _selectedScreen;

        //右侧面板属性tab索引 
        [ObservableProperty]
        private int _rightPanelTabIndex = 0;

        //底部面板属性tab索引 
        [ObservableProperty]
        private int _bottomPanelTabIndex = 0;
        private SglScreen()
        {
            // 默认初始化第一个屏幕
            AddScreen();

        }

        // 获取所有 Widget（包括子控件）的扁平化列表
        public IEnumerable<SglWidgetData> AllWidgetsFlat
        {
            get
            {
                if (SelectedScreen?.Widgets == null)
                    return Enumerable.Empty<SglWidgetData>();

                var result = new List<SglWidgetData>();
                FlattenWidgets(SelectedScreen.Widgets, result);
                return result;
            }
        }

        // 递归扁平化 Widgets
        private void FlattenWidgets(IEnumerable<SglWidgetData> widgets, List<SglWidgetData> result)
        {
            if (widgets == null) return;

            foreach (var widget in widgets)
            {
                result.Add(widget);
                if (widget.Children != null && widget.Children.Any())
                {
                    FlattenWidgets(widget.Children, result);
                }
            }
        }

        // 刷新方法
        public void RefreshWidgetList()
        {
            OnPropertyChanged(nameof(AllWidgetsFlat));
        }

        // --- 功能：添加屏幕 ---
        public void AddScreen()
        {
            // --- 1. 生成唯一的 ID ---
            int index = ScreenList.Count;
            string newId;

            // 循环查重：如果生成的 ID 已经存在，则序号自增
            do
            {
                newId = $"screen{index}";
                index++;
            } while (ScreenList.Any(s => s.Id.Equals(newId, StringComparison.OrdinalIgnoreCase)));

            // --- 2. 创建新页面 ---
            // 建议 Id 和 Name 保持同步，避免导出代码时混淆
            var newPage = new SglPageData(newId, newId);

            // --- 3. 初始化根容器 ---
            var root = new SglPanelData
            {
                Id = newPage.Id,
                //Name = newPage.Id, // 确保 Name 属性也被赋值，用于 C 代码变量名
                Type = SglMapping.SglType.Rectangle,
                X = 0,
                Y = 0,
                W = ProjectManager.Instance.Width,
                H = ProjectManager.Instance.Height,
                BgColor = Color.FromRgb(0x2F, 0x2F, 0x2F),
                Radius = 0,
                Parent = null
            };

            newPage.Widgets.Add(root);

            // --- 4. 添加到列表并切换 ---
            ScreenList.Add(newPage);
            SelectedScreen = newPage;

            if (SelectedScreen != null)
            {
                WeakReferenceMessenger.Default.Send(new ScreenChangedMessage(SelectedScreen));
            }
        }

        public static int GetWidgetsCount(SglWidgetData root)
        {
            if (root == null) return 0;

            int count = 1; // 计入当前控件自身

            // 如果该控件有子控件（如 Panel, Page 等容器）
            if (root.Children != null)
            {
                foreach (var child in root.Children)
                {
                    count += GetWidgetsCount(child);
                }
            }

            return count;
        }
        public static int GetAllWidgetsCount()
        {
            int count = 0;
            foreach (var root in SglScreen.Instance.ScreenList)
            {
                foreach (var widget in root.Widgets)
                {
                    count = GetWidgetsCount(widget);
                }
            }
            return count;
        }

        // --- 功能：删除屏幕 ---
        public void RemoveScreen(SglPageData page)
        {
            if (page == null) return;

            int index = ScreenList.IndexOf(page);

            // 收集待删除屏幕中所有控件的 ID（用于清理跨屏幕引用）
            var deletedWidgetIds = new HashSet<string>();
            CollectWidgetIds(page.Widgets, deletedWidgetIds);

            // 2. 执行删除
            ScreenList.Remove(page);

            //  清理残留引用：通知所有 SglChangeScreenAction 目标屏幕已删除
            WeakReferenceMessenger.Default.Send(new PageDeletedMessage(page.Id));

            //  清理残留引用：通知所有 SglSetPropertyAction / SglModifyFlagAction 目标控件已删除
            foreach (var widgetId in deletedWidgetIds)
            {
                WeakReferenceMessenger.Default.Send(new WidgetDeletedMessage(widgetId));
            }

            //  焦点切换逻辑
            if (SelectedScreen == page)
            {
                if (ScreenList.Count > 0)
                {
                    // 优先选择被删除项的前一项，如果删的是第0项，则选新的第0项
                    int nextIndex = Math.Max(0, index - 1);
                    SelectedScreen = ScreenList[nextIndex];
                }
                else
                {
                    SelectedScreen = null;
                }
            }

            //  通知 UI 层：当前没有选中的屏幕了，应该关闭属性面板和画布
            if (SelectedScreen == null)
            {
                WeakReferenceMessenger.Default.Send(new ScreenChangedMessage(null));
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new ScreenChangedMessage(SelectedScreen));
            }
        }

        // 递归收集控件树中所有控件的 ID
        private static void CollectWidgetIds(IEnumerable<SglWidgetData> widgets, HashSet<string> ids)
        {
            if (widgets == null) return;
            foreach (var w in widgets)
            {
                if (!string.IsNullOrEmpty(w.Id))
                    ids.Add(w.Id);
                if (w.Children?.Count > 0)
                    CollectWidgetIds(w.Children, ids);
            }
        }

        //当选中的屏幕改变时，通知 UI 层重绘画布
        partial void OnSelectedScreenChanged(SglPageData value)
        {
            // 这里可以触发一个事件，让 MainWindow 捕获并清空/重绘画布
            WeakReferenceMessenger.Default.Send(new ScreenChangedMessage(value));
        }
    }



}
