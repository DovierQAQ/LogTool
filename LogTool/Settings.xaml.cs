using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Settings.xaml 的交互逻辑
    /// </summary>
    public partial class Settings : Window
    {
        public Settings(string log_path)
        {
            InitializeComponent();

            tb_log_path.Text = log_path;
        }

        private void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.log_path = tb_log_path.Text;
            Close();
        }

        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btn_log_path_Click(object sender, RoutedEventArgs e)
        {
            //SaveFileDialog saveFileDialog = new SaveFileDialog();
            //saveFileDialog.Title = "选择日志文件位置";
            //saveFileDialog.Filter = "日志文件|*.log;*.txt|所有文件|*.*";
            //saveFileDialog.FileName = "log";
            //saveFileDialog.FilterIndex = 1;
            //saveFileDialog.RestoreDirectory = true;
            //saveFileDialog.DefaultExt = "log";
            //if (saveFileDialog.ShowDialog() == false)
            //{
            //    return;
            //}

            //string file_path = saveFileDialog.FileName + '\n';
            //file_path = Regex.Replace(file_path, @"\\\w+?\n", ""); // todo

            //tb_log_path.Text = file_path;
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            
            if (folderBrowserDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }

            string folder = folderBrowserDialog.SelectedPath;

            tb_log_path.Text = folder;
        }
    }
}
