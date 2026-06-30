using System.Windows;
using System.Windows.Controls;

namespace SglDesigner.PropertyPanels
{
    public partial class PropRow : UserControl
    {
        public PropRow()
        {
            InitializeComponent();
        }

        // 定义 Header 依赖属性，这样就可以在 XAML 中写 Header="xxx"
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(
                nameof(Header),
                typeof(string),
                typeof(PropRow),
                new PropertyMetadata(string.Empty));

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }
    }
}