using System;
using System.Windows.Controls;

namespace SglDesigner.PropertyPanels.Actions
{
    /// <summary>
    /// SetProperty.xaml 的交互逻辑
    /// </summary>
    public partial class SetProperty : UserControl
    {
        public Array SglPropertyTypes => Enum.GetValues(typeof(SglPropertyType));
        public SetProperty()
        {
            InitializeComponent();
        }
    }
}
