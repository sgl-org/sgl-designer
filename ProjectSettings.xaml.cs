using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;
using System.Windows.Controls;

namespace SglDesigner
{
    public partial class ProjectSettingsWindow : Window
    {
        public ProjectSettingsWindow(SglPageData data)
        {
            InitializeComponent();
            this.DataContext = data; // 直接绑定当前页面数据
        }

        private void ComboPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataContext is SglPageData data)
            {
                switch (ComboPresets.SelectedIndex)
                {
                    case 1: ProjectManager.Instance.Width = 240; ProjectManager.Instance.Height = 240; break;
                    case 2: ProjectManager.Instance.Width = 320; ProjectManager.Instance.Height = 240; break;
                    case 3: ProjectManager.Instance.Width = 480; ProjectManager.Instance.Height = 272; break;
                    case 4: ProjectManager.Instance.Width = 800; ProjectManager.Instance.Height = 480; break;
                }
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; // 设置结果并关闭
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Browser_Click(object sender, RoutedEventArgs e)
        {

            var owner = this;
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true; // 设置为文件夹选择模式
            //dialog.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

            dialog.Title = "选择代码导出目录";
            //ShowDialog(this) 要保存父窗口 否则点取消 窗口会不见
            if (dialog.ShowDialog(owner) == CommonFileDialogResult.Ok)
            {
                this.DialogResult = true;

                var page_data = this.DataContext as SglPageData;
                ProjectManager.Instance.ExportFolder = dialog.FileName;
            }
            else
            {
                this.DialogResult = false;
                //阻止窗口关闭
                e.Handled = true;
            }
        }
    }
}
