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
    /// AddFilter.xaml 的交互逻辑
    /// </summary>
    public partial class AddFilter : Window
    {
        public AddFilter()
        {
            InitializeComponent();
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            if (tb_filter_text.Text.Equals(""))
            {
                MessageBox.Show("匹配文本不能为空");
                return;
            }

            MainWindow.filters.Add(new FilterUtils.Filter()
            {
                Is_enable = true,
                Is_case_sensitive = (bool)cb_case_sensitive.IsChecked,
                Is_regex = (bool)cb_regex.IsChecked,
                Text = tb_filter_text.Text,
                Foreground = new SolidColorBrush((Color)cp_forground.SelectedColor),
                Background = new SolidColorBrush((Color)cp_background.SelectedColor)
            });
            Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
