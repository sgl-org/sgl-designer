using System.IO;
using System.Windows;
using System.Windows.Input;

namespace SglDesigner
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        // App.xaml.cs 内部
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                // 这里的 TempDir 必须能被访问到，如果是 private，
                // 建议在 SglResManager 中加一个 public static string GetTempDir()
                string path = SglResManager.TempDir;

                if (Directory.Exists(path))
                {
                    // 递归删除临时文件夹及其下的所有图片、字体文件
                    Directory.Delete(path, true);
                }
            }
            catch
            {
                // 忽略正在被 WPF 渲染引擎占用的文件错误
            }
            base.OnExit(e);
        }

        // 必须放在 App 类里
        private void GlobalUpDown_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is Xceed.Wpf.Toolkit.IntegerUpDown upDown)
            {
                // 斩断事件冒泡，不让外层 ScrollViewer 拿到滚轮事件
                e.Handled = true;

                if (!upDown.IsEnabled || upDown.IsReadOnly) return;

                int step = upDown.Increment ?? 1;
                int currentValue = upDown.Value ?? 0;

                if (e.Delta > 0)
                {
                    upDown.Value = currentValue + step;
                }
                else if (e.Delta < 0)
                {
                    upDown.Value = currentValue - step;
                }
            }
        }
    }
}
