using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LogTool
{
    /// <summary>
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Info_write_line("作者：郭帆");
            Info_write_line("邮箱：guofan2@midea.com");
            Info_write_line("用途：采集串口日志、分析日志");
            Info_write_line("第一版时间：");
        }

        private void Info_write(string content)
        {
            tb_info.Dispatcher.Invoke(new Action(delegate
            {
                tb_info.Text += content;
            }));
        }

        private void Info_write_line(string content)
        {
            Info_write(content + "\n");
        }
    }
}
