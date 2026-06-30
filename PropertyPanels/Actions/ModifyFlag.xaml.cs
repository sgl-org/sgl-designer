using System;
using System.Windows.Controls;

namespace SglDesigner.PropertyPanels.Actions
{
    /// <summary>
    /// SetProperty.xaml 的交互逻辑
    /// </summary>
    public partial class ModifyFlag : UserControl
    {
        public Array SglFlagTypes => Enum.GetValues(typeof(SglFlagType));
        public ModifyFlag()
        {
            InitializeComponent();
        }
    }
}
