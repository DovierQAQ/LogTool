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
    /// FindText.xaml 的交互逻辑
    /// </summary>
    public partial class FindText : Window
    {
        public FindText(Action find_text)
        {
            InitializeComponent();

            find_text_callback = find_text;
        }
        public FindText(string s, Action find_text)
        {
            InitializeComponent();

            tb_find_text.Text = s;
            find_text_callback = find_text;
        }

        Action find_text_callback;
        static public FilterUtils.Filter filter;

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            filter = new FilterUtils.Filter();
            filter.Is_enable = true;
            filter.Text = tb_find_text.Text;
            filter.Is_case_sensitive = (bool)cb_case_sensitive.IsChecked;
            filter.Is_regex = (bool)cb_regex.IsChecked;
            find_text_callback.Invoke();
            Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void tb_find_text_Loaded(object sender, RoutedEventArgs e)
        {
            Keyboard.Focus(tb_find_text);
        }
    }
}
