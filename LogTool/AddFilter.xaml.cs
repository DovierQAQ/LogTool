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
        bool is_change = false;
        FilterUtils.Filter change_filter;

        public AddFilter()
        {
            InitializeComponent();

            cc_foreground.SelectedColor = Colors.Black;
            cc_background.SelectedColor = Colors.White;
        }

        public AddFilter(string s)
        {
            InitializeComponent();

            tb_filter_text.Text = s;
            cc_foreground.SelectedColor = Colors.Black;
            cc_background.SelectedColor = Colors.White;
        }

        public AddFilter(FilterUtils.Filter filter)
        {
            InitializeComponent();

            is_change = true;

            cb_enable.IsChecked = filter.Is_enable;
            cb_case_sensitive.IsChecked = filter.Is_case_sensitive;
            cb_regex.IsChecked = filter.Is_regex;
            tb_filter_text.Text = filter.Text;
            cc_foreground.SelectedColor = ((SolidColorBrush)filter.Foreground).Color;
            cc_background.SelectedColor = ((SolidColorBrush)filter.Background).Color;

            change_filter = filter;
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            if (tb_filter_text.Text.Equals(""))
            {
                MessageBox.Show("匹配文本不能为空");
                return;
            }

            if (is_change)
            {
                change_filter.Is_enable = (bool)cb_enable.IsChecked;
                change_filter.Is_case_sensitive = (bool)cb_case_sensitive.IsChecked;
                change_filter.Is_regex = (bool)cb_regex.IsChecked;
                change_filter.Text = tb_filter_text.Text;
                change_filter.Foreground = new SolidColorBrush((Color)cc_foreground.SelectedColor);
                change_filter.Background = new SolidColorBrush((Color)cc_background.SelectedColor);
            }
            else
            {
                MainWindow.filters.Add(new FilterUtils.Filter()
                {
                    Is_enable = (bool)cb_enable.IsChecked,
                    Is_case_sensitive = (bool)cb_case_sensitive.IsChecked,
                    Is_regex = (bool)cb_regex.IsChecked,
                    Text = tb_filter_text.Text,
                    Foreground = new SolidColorBrush((Color)cc_foreground.SelectedColor),
                    Background = new SolidColorBrush((Color)cc_background.SelectedColor)
                });
            }
            MainWindow.analys_log_data();
            Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void cc_foreground_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (tb_filter_text != null)
            {
                tb_filter_text.Foreground = new SolidColorBrush((Color)cc_foreground.SelectedColor);
            }
        }

        private void cc_background_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (tb_filter_text != null)
            {
                tb_filter_text.Background = new SolidColorBrush((Color)cc_background.SelectedColor);
            }
        }
    }
}
