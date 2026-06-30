using System.Windows;
using System.Windows.Controls;

namespace SglDesigner.PropertyPanels
{
    /// <summary>
    /// LabelPropertyPanel.xaml 的交互逻辑
    /// </summary>
    public partial class DropdownPropertyPanel : UserControl
    {
        public DropdownPropertyPanel()
        {
            InitializeComponent();
        }
        private void AddOption_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SglDropdownData data && !string.IsNullOrWhiteSpace(NewOptionText.Text))
            {
                data.Options.Add(NewOptionText.Text);
                NewOptionText.Clear();
            }
        }

        private void DeleteOption_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SglDropdownData data && OptionsList.SelectedItem is string selected)
            {
                data.Options.Remove(selected);
            }
        }
    }
}
