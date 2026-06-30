using System;
using System.Windows;

namespace SglDesigner
{
    /// <summary>
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
            String BuildDateTime = System.IO.File.GetLastWriteTime(this.GetType().Assembly.Location).ToString();
            BuildDate.Content = "Suzhou    BuildDate: " + BuildDateTime;

            desc.AppendText("V1.0.0.6:\r\n\r\n");
            desc.AppendText("   1. 修复页面删除后，引用ID未清理问题.\r\n");
            desc.AppendText("   2. 添加Win控件，ViewList控件，StatusBar控件，Roller控件.\r\n");
            desc.AppendText("   3. 完善对象移入移出容器，并添加虚线提示.\r\n");
            desc.AppendText("   4. 更新Makefile添加SGL源码方式.\r\n");
            desc.AppendText("   5. 屏幕列表可以自由拖拽调整顺序.\r\n");
            desc.AppendText("   6. 添加颜色吸管功能.\r\n");
            desc.AppendText("   7. 添加资源文件名称过滤以符合C规范,非法字符替换为下划线.\r\n");
            desc.AppendText("   8. 适配SGL最新API.\r\n");

            desc.AppendText("\r\nV1.0.0.5:\r\n\r\n");
            desc.AppendText("   1. 增加宏参数设置窗口.\r\n");
            desc.AppendText("   2. 增加RLE压缩字体支持.\r\n");
            desc.AppendText("   3. 优化撤消(CTRL + Z),重做(CTRL + Y)功能.\r\n");
            desc.AppendText("   4. 增加批量修改属性功能（同一类型的控件）.\r\n");
            desc.AppendText("   5. 适配新版本代码，修复已知问题.\r\n");

            desc.AppendText("\r\nV1.0.0.4:\r\n\r\n");
            desc.AppendText("   1. 增加Bar,Box,LineChart，qrcode控件 .\r\n");
            desc.AppendText("   2. 修复工具链问题，强烈建议加入用户环境变量中，方便任意位置编译 .\r\n");
            desc.AppendText("   3. 修复已知问题\r\n");

            desc.AppendText("\r\nV1.0.0.3:\r\n\r\n");
            desc.AppendText("   1. 修复已知问题 .\r\n");
            desc.AppendText("   2. 导出变量定义为NULL .\r\n");
            desc.AppendText("   3. 完善Led控件,Polygon控件属性设置 .\r\n");
            desc.AppendText("   4. 多边形边框支持透明 .\r\n");
            desc.AppendText("   5. 重构事件处理，可支持页面切换，属性设置等.\r\n");
            desc.AppendText("   6. 增加自动更新功能.\r\n");

            desc.AppendText("\r\nV1.0.0.2:\r\n");
            desc.AppendText("   1. 增加更新SGL代码按钮 \r\n");
            desc.AppendText("   2. 初步支持BarChart,LineChart控件支持 .\r\n");
            desc.AppendText("   3. 增加自动更新功能 .\r\n");
            desc.AppendText("   4. 修复ID未同步问题 .\r\n");
            desc.AppendText("   5. 修复调整画布大小未同步模拟器问题.\r\n");
            desc.AppendText("   6. 其它已知问题 .\r\n");

            desc.AppendText("\r\nV1.0.0.1:\r\n");
            desc.AppendText("   1. 集成编译环境,可直接编译运行看到结果 .\r\n");
            desc.AppendText("   2. 增加ui.c文件，改为全局变量 .\r\n");
            desc.AppendText("   3. 图片和字体支持多选 .\r\n");
            desc.AppendText("   4. 修复事件添加问题 .\r\n");
            desc.AppendText("   5. 其它已知修复 .\r\n");

            desc.AppendText("\r\nV1.0.0.0:\r\n");
            desc.AppendText("   1. 初始版本发布.\r\n");

        }
    }
}
