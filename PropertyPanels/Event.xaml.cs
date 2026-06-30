using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SglDesigner.PropertyPanels
{
    /// <summary>
    /// Event.xaml 的交互逻辑
    /// </summary>
    public partial class EventPropertyPanel : UserControl
    {
        public Array SglEventTypes => Enum.GetValues(typeof(SglEventType));
        public Array SglActionTypes => Enum.GetValues(typeof(SglActionType));

        public EventPropertyPanel()
        {
            InitializeComponent();
        }

        private void AddNewEvent_Click(object sender, RoutedEventArgs e)
        {
            // 这里的 DataContext 应该是选中的 SglWidgetData
            if (this.DataContext is SglWidgetData widget)
            {
                widget.AddNewEvent(SglEventType.Released);
                Console.WriteLine(widget.Id);
            }
        }

        private void RemoveEvent_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var eventItem = btn.DataContext as SglEventItem;
            if (this.DataContext is SglWidgetData model && eventItem != null)
            {
                model.Events.Remove(eventItem);
            }
        }


        private void AddAction_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            // 获取当前按钮所在的事件项 (SglEventItem)
            var eventItem = btn?.DataContext as SglEventItem;
            if (eventItem == null) return;

            // 2. 获取 ComboBox 选中的类型
            // 注意：根据的 XAML，ActionType 应该绑定在 SglEventItem 上，
            // 或者通过其他方式获取。这里假设它就在 eventItem 的属性里。
            SglActionType selectedType = eventItem.ActionType;

            //  根据选中的类型，创建具体的强类型对象
            SglActionBase newAction = selectedType switch
            {
                SglActionType.ChangeScreen => new SglChangeScreenAction(),
                SglActionType.SetProperty => new SglSetPropertyAction(),
                SglActionType.CallFunction => new SglCallFunctionAction(),
                SglActionType.ModifyFlag => new SglModifyFlagAction(),
                _ => new SglCallFunctionAction()
            };

            //  显式设置类型（触发 DataTrigger 或 DataType 匹配）
            newAction.ActionType = selectedType;

            //  添加到集合，界面会立即根据新对象的类型弹出对应的面板
            eventItem.Actions.Add(newAction);
        }



        private void RemoveAction_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;

            // 修改这里：将 SglAction 替换为的动作基类（例如 SglActionBase）
            // 如果不确定基类名，也可以直接用 object，但建议用基类
            var action = btn?.DataContext as SglActionBase;

            if (action != null)
            {
                // 2. 找到该按钮所属的 SglEventItem
                var eventItem = FindParentEvent(btn);

                if (eventItem != null)
                {
                    //  从 Actions 集合中移除
                    // 注意：Actions 集合的泛型类型也应该是 SglActionBase
                    eventItem.Actions.Remove(action);
                }
            }
        }

        // 辅助方法：查找父级事件
        private SglEventItem FindParentEvent(DependencyObject child)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null)
            {
                if (parent is FrameworkElement frameworkElement &&
                    frameworkElement.DataContext is SglEventItem eventItem)
                {
                    return eventItem;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
    }
}
