using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;

namespace SglDesigner
{
    public enum SglEventType
    {
        Null = 0,
        //Normal = 1,
        Pressed = 2,
        Released = 3,
        Clicked = 4,
        LongClicked = 5,
        Motion = 6,
        MoveUp = 7,
        MoveDown = 8,
        MoveLeft = 9,
        MoveRight = 10,
        LongPressed = 11,
        //OptionWalk = 12,
        //OptionTap = 13,
        //DrawInit = 14,
        //DrawMain = 15,
        Focused = 16,
        Unfocused = 17,
        Destroyed = 18
    }
    public enum SglActionType
    {
        ChangeScreen,
        CallFunction,
        //DeleteScreen,
        ModifyFlag,
        SetProperty,
    }
    public enum SglPropertyType
    {
        Basic_X,
        Basic_Y,
        Basic_Width,
        Basic_Height,
        Border_Width,
        Border_Radius,
    }
    public enum SglFlagType
    {
        HIDDEN,
        VISABLE,
        CLICKABLE,
        MOVABLE,
        UNMOVABLE,
        FLEXIBLE,
        UNFLEXIBLE,
        MOVE_TOP,
        MOVE_UP,
        MOVE_BOTTOM,
        MOVE_DOWN
    }

    public partial class SglPoint : ObservableObject
    {
        [ObservableProperty] private double _x;
        [ObservableProperty] private double _y;

        public SglPoint(double x, double y) { X = x; Y = y; }
    }

    public partial class EventRouteValue : ObservableObject
    {
        [ObservableProperty] private string _handlerName = "";
    }


    // Action 基类
    public abstract partial class SglActionBase : ObservableObject
    {
        [ObservableProperty] private SglActionType _actionType;
    }
    /// <summary>
    /// 当 SglPageData 的 ID 发生变化时发送的消息
    /// </summary>
    public class PageIdChangedMessage
    {
        // 旧 ID（用于查找谁引用了它）
        public string OldId { get; }

        // 新 ID（用于更新为最新的值）
        public string NewId { get; }

        public PageIdChangedMessage(string oldId, string newId)
        {
            OldId = oldId;
            NewId = newId;
        }
    }

    public class PageDeletedMessage
    {
        public string DeletedPageId { get; }
        public PageDeletedMessage(string deletedPageId) => DeletedPageId = deletedPageId;
    }
    // 专门用于切换页面的 Action
    public partial class SglChangeScreenAction : SglActionBase, IRecipient<PageIdChangedMessage>, IRecipient<PageDeletedMessage>
    {
        //public SglChangeScreenAction() => ActionType = SglActionType.ChangeScreen;

        // 这里的类型明确为 Page
        [ObservableProperty] private string _targetPage;

        public SglChangeScreenAction()
        {
            ActionType = SglActionType.ChangeScreen;
            // 必须注册！
            WeakReferenceMessenger.Default.Register<PageIdChangedMessage>(this);
            WeakReferenceMessenger.Default.Register<PageDeletedMessage>(this);
        }

        public void Receive(PageIdChangedMessage message)
        {
            // 如果我当前引用的页面 ID 就是被改掉的那个
            if (TargetPage == message.OldId)
            {
                TargetPage = message.NewId;
                // 如果使用了对象绑定，对象引用本身没变，所以甚至不需要额外操作
                // 但如果存的是 ID 字符串，这一步是必须的
            }
        }

        public void Receive(PageDeletedMessage message)
        {
            // 如果当前 Action 指向的页面就是被删除的那个，则清空引用
            if (TargetPage == message.DeletedPageId)
            {
                TargetPage = null;
            }
        }

    }

    // 2. 专门用于修改控件属性的 Action
    public partial class SglSetPropertyAction : SglActionBase, IRecipient<WidgetIdChangedMessage>, IRecipient<WidgetDeletedMessage>
    {
        //public SglSetPropertyAction() => ActionType = SglActionType.SetProperty;

        // 这里的类型明确为 Widget
        [ObservableProperty] private string _target;
        [ObservableProperty] private SglPropertyType _propertyType;
        [ObservableProperty] private string _value;

        public SglSetPropertyAction()
        {
            // 注册监听
            WeakReferenceMessenger.Default.Register<WidgetIdChangedMessage>(this);
            WeakReferenceMessenger.Default.Register<WidgetDeletedMessage>(this);
        }

        public void Receive(WidgetIdChangedMessage message)
        {
            // 如果当前 Action 指向的控件 ID 正是被修改的那个
            if (Target == message.OldId)
            {
                Target = message.NewId;
                // 确保 UI 同步更新
                OnPropertyChanged(nameof(Target));
            }
        }

        public void Receive(WidgetDeletedMessage message)
        {
            // 如果当前 Action 指向的控件就是被删除的那个
            if (Target == message.DeletedWidgetId)
            {
                Target = null;
                OnPropertyChanged(nameof(Target));
            }
        }
    }


    //  调用函数的 Action
    public partial class SglCallFunctionAction : SglActionBase
    {
        public SglCallFunctionAction() => ActionType = SglActionType.CallFunction;

        [ObservableProperty] private string _funcName = "YourFunc";
    }

    // 事件项：触发器 + 动作列表
    public partial class SglEventItem : ObservableObject
    {
        [ObservableProperty] private SglEventType _trigger = SglEventType.Released;

        // 核心修改：使用基类泛型，支持存放任何继承自 SglActionBase 的子类
        public ObservableCollection<SglActionBase> Actions { get; } = new();

        // 用于给 ComboBox 选择，决定下一次添加什么类型的动作
        [ObservableProperty] private SglActionType _actionType = SglActionType.ChangeScreen;
    }
    public partial class SglModifyFlagAction : SglActionBase, IRecipient<WidgetIdChangedMessage>, IRecipient<WidgetDeletedMessage>
    {
        //public SglModifyFlagAction() => ActionType = SglActionType.ModifyFlag;

        // 这里的类型明确为 Widget
        [ObservableProperty] private string _target;
        [ObservableProperty] private SglFlagType _flagType;

        public SglModifyFlagAction()
        {
            // 注册监听
            WeakReferenceMessenger.Default.Register<WidgetIdChangedMessage>(this);
            WeakReferenceMessenger.Default.Register<WidgetDeletedMessage>(this);
        }

        public void Receive(WidgetIdChangedMessage message)
        {
            // 如果当前 Action 指向的控件 ID 正是被修改的那个
            if (Target == message.OldId)
            {
                Target = message.NewId;
                // 确保 UI 同步更新
                OnPropertyChanged(nameof(Target));
            }
        }

        public void Receive(WidgetDeletedMessage message)
        {
            // 如果当前 Action 指向的控件就是被删除的那个
            if (Target == message.DeletedWidgetId)
            {
                Target = null;
                OnPropertyChanged(nameof(Target));
            }
        }

    }

    // 当 SglWidgetData 的 ID 发生变化时发送的消息
    public class WidgetIdChangedMessage
    {
        public string OldId { get; }
        public string NewId { get; }

        public WidgetIdChangedMessage(string oldId, string newId)
        {
            OldId = oldId;
            NewId = newId;
        }
    }

    // 当控件被删除时发送的消息（例如删除屏幕时清理关联的 Action）
    public class WidgetDeletedMessage
    {
        public string DeletedWidgetId { get; }
        public WidgetDeletedMessage(string deletedWidgetId) => DeletedWidgetId = deletedWidgetId;
    }
    // 基类：所有控件共有的属性
    public partial class SglWidgetData : ObservableObject
    {
        private static readonly HashSet<string> IgnoredUndoProperties = new HashSet<string>
        {
            nameof(IsSelected),
            nameof(Parent)
        };

        // --- 核心身份 ---
        [ObservableProperty] private string _id;
        [ObservableProperty] private SglMapping.SglType _type;
        [ObservableProperty] private string _tag;

        // --- 几何属性 ---
        [ObservableProperty] private int _x;
        [ObservableProperty] private int _y;
        [ObservableProperty] private int _w;
        [ObservableProperty] private int _h;
        [ObservableProperty] private bool _isHidden = false;

        // --- SGL 通用事件 (对应 sgl_obj_set_event_cb) ---
        [ObservableProperty] private string _eventCallbackName = ""; // 总入口函数名


        // 是否允许交互 (对应 sgl_obj_set_click)
        [ObservableProperty] private bool _isClickable = false;

        // 将 string 改为 EventRouteValue
        public ObservableDictionary<SglEventType, EventRouteValue> EventRoutes { get; } = new();

        //  Id 变更时广播消息重要
        partial void OnIdChanged(string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(oldValue) || oldValue == newValue) return;

            // 广播 Widget ID 变更
            WeakReferenceMessenger.Default.Send(new WidgetIdChangedMessage(oldValue, newValue));

            // 2. 通知 UI 刷新 ID 属性（对 ComboBox 匹配至关重要）
            OnPropertyChanged(nameof(Id));

            //  顺便通知 SglScreen 刷新扁平化列表（可选，视具体 UI 需求而定）
            SglScreen.Instance.RefreshWidgetList();
        }
        // --- 树形结构 (逻辑层) ---
        public ObservableCollection<SglWidgetData> Children { get; } = new();

        private SglWidgetData _parent;
        [JsonIgnore]
        public SglWidgetData Parent
        {
            get => _parent;
            set => SetProperty(ref _parent, value);
        }

        [ObservableProperty] private bool _isSelected;

        // 辅助计算
        public bool IsContainer => Type == SglMapping.SglType.Rectangle || Type == SglMapping.SglType.Box || Type == SglMapping.SglType.Win || Type == SglMapping.SglType.Viewlist;

        // 定义一个静态事件，方便 MainWindow 订阅
        public static event Action RequestAdornerUpdate;

        public SglWidgetData()
        {
            this.PropertyChanging += OnWidgetPropertyChanging;

            // 当 ID 变化时，自动初始化默认回调名
            this.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Id) && !string.IsNullOrEmpty(Id))
                {
                    if (string.IsNullOrEmpty(EventCallbackName))
                        EventCallbackName = $"{Id.ToLower()}_event_handler";
                }
                // 2. 触发事件
                RequestAdornerUpdate?.Invoke();
            };

        }

        private void OnWidgetPropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            if (string.IsNullOrEmpty(e.PropertyName))
                return;

            if (IgnoredUndoProperties.Contains(e.PropertyName))
                return;

            MainWindow.NotifyModelPropertyChanging(e.PropertyName);
        }

        // 更换掉原本的 Dictionary，改为更灵活的事件列表
        public ObservableCollection<SglEventItem> Events { get; } = new();

        // 快捷方法：添加新事件
        public void AddNewEvent(SglEventType type)
        {
            var item = new SglEventItem { Trigger = type };

            // 因为不再使用 SglAction，这里直接 new 具体的子类对象
            // 这会在界面上生成第一个默认的动作面板
            //var defaultAction = new SglChangeScreenAction();

            //// 2. 确保 Type 属性赋值，以触发 XAML 中的 DataTrigger 切换模板
            //defaultAction.ActionType = SglActionType.ChangeScreen;

            //item.Actions.Add(defaultAction);

            Events.Add(item);
        }


        // 添加事件的方法
        public void AddEventRoute(SglEventType type)
        {
            if (!EventRoutes.ContainsKey(type))
            {
                EventRoutes.Add(type, new EventRouteValue { HandlerName = $"{Id}_{type}" });
            }
        }

        // 删除事件的方法
        public void RemoveEventRoute(SglEventType type)
        {
            if (EventRoutes.ContainsKey(type))
            {
                EventRoutes.Remove(type);
            }
        }

    }










}
