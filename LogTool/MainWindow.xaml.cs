﻿using GFBytesUtils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LogTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            string saved_width = ConfigurationManager.AppSettings["window_width"];
            string saved_height = ConfigurationManager.AppSettings["window_height"];
            try
            {
                Width = int.Parse(saved_width);
                Height = int.Parse(saved_height);
            }
            catch (Exception ex)
            {
                Console.WriteLine("w: " + saved_width + ", h: " + saved_height);
                Console.WriteLine("window size: " + ex.Message);
            }
        }

        static string window_title = "GuoFan Log Tool V1.1";
        static string release_date = " 2020-11-17";

        static GFSerial serial = null;

        static ObservableCollection<LogItem> log_data = new ObservableCollection<LogItem>();
        static ObservableCollection<LogItem> log_data_filtered = new ObservableCollection<LogItem>();
        static Mutex log_data_mutex = new Mutex();
        Mutex buffer_mutex = new Mutex();
        string log_buffer = "";
        string log_file_name = "";
        static public string log_path = "log";

        static string[] log_data_lines;

        static public ObservableCollection<FilterUtils.Filter> filters = new ObservableCollection<FilterUtils.Filter>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            serial = new GFSerial(delegate { recv_data_callback(); }, delegate { serial_close_callback(); });

            cb_com.DataContext = GFSerial.ports;
            if (cb_com.Text.Equals("") && GFSerial.ports.Ports.Count > 0)
            {
                cb_com.SelectedIndex = 0;
            }

            cb_baud.SelectedIndex = 8;

            dg_log.DataContext = log_data;

            dg_filter.DataContext = filters;

            DispatcherTimer timer_show_log = new DispatcherTimer();
            timer_show_log.Interval = TimeSpan.FromMilliseconds(10);
            timer_show_log.Tick += new EventHandler(log_show_callback);
            timer_show_log.Start();

            analys_log_data();

            Title = window_title;

            open_state();

            string saved_com = ConfigurationManager.AppSettings["selected_com"];
            string saved_baud = ConfigurationManager.AppSettings["selected_baud"];
            if (!saved_com.Equals(""))
            {
                cb_com.Text = saved_com;
            }
            if (!saved_baud.Equals(""))
            {
                cb_baud.Text = saved_baud;
            }

            log_path = ConfigurationManager.AppSettings["log_path"];
        }

        static public void Create_log_dir(string path)
        {
            if (Regex.Match(path, @"^\w:").Success)
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            else
            {
                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + path))
                {
                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + path);
                }
            }
        }

        private string Get_log_path()
        {
            if (Regex.Match(log_path, @"^\w:").Success)
            {
                return log_path;
            }
            else
            {
                return AppDomain.CurrentDomain.BaseDirectory + log_path;
            }    
        }

        private void serial_close_callback()
        {
            open_state();
        }

        private void refresh_title()
        {
            if (log_data_file_name.Equals(""))
            {
                Title = window_title + release_date;
            }
            else
            {
                Title = window_title + " - " + log_data_file_name;
            }
        }

        private void open_state()
        {
            btn_com_open.Content = "打开串口";
            cb_com.IsEnabled = true;
            cb_baud.IsEnabled = true;
            mn_edit.IsEnabled = true;
            refresh_title();
        }

        private void close_state()
        {
            btn_com_open.Content = "关闭串口";
            cb_com.IsEnabled = false;
            cb_baud.IsEnabled = false;
            mn_edit.IsEnabled = false;
            Title = window_title;
        }

        private void btn_com_open_Click(object sender, RoutedEventArgs e)
        {
            if (serial.is_open)
            {
                try
                {
                    serial.close_serial();
                }
                catch (Exception)
                {
                    MessageBox.Show("无法关闭串口", "错误");
                    return;
                }

                open_state();
            }
            else
            {
                try
                {
                    serial.open_serial(cb_com.Text, int.Parse(cb_baud.Text));
                    log_clear();
                    Create_log_dir(log_path);
                    log_file_name = Get_log_path() + "/" + cb_com.Text + "_" + TimeUtils.GetFileTimeString() + ".log";
                }
                catch (Exception)
                {
                    MessageBox.Show("无法打开串口", "错误");
                    return;
                }

                close_state();
            }
        }

        private void recv_data_callback()
        {
            serial.recv_data_mutex.WaitOne();
            byte[] recv_data = serial.serial_recv_data.ToArray();
            serial.serial_recv_data.Clear();
            serial.recv_data_mutex.ReleaseMutex();

            string recv_string = Encoding.UTF8.GetString(recv_data).Replace("\r", "");
            buffer_mutex.WaitOne();
            log_buffer += recv_string;
            buffer_mutex.ReleaseMutex();
            tb_recv_data.Dispatcher.Invoke(new Action(delegate { tb_recv_data.Text += recv_string; }));
        }

        private void log_show_callback(object sender, EventArgs e)
        {
            if (!buffer_mutex.WaitOne(10))
            {
                return;
            }
            if (log_buffer.Equals(""))
            {
                buffer_mutex.ReleaseMutex();
                return;
            }
            string[] recv_items = log_buffer.Split('\n');
            if (recv_items.Length <= 1)
            {
                buffer_mutex.ReleaseMutex();
                return;
            }
            log_buffer = recv_items.Last();
            tb_recv_data.Dispatcher.Invoke(new Action(delegate { tb_recv_data.Text = log_buffer; }));
            using (StreamWriter streamWriter = new StreamWriter(log_file_name, true, Encoding.UTF8))
            {
                for (int i = 0; i < recv_items.Length - 1; i++)
                {
                    streamWriter.WriteLine("[" + TimeUtils.GetTimeString() + "]  " + recv_items[i]);
                }
            }
            buffer_mutex.ReleaseMutex();

            for (int i = 0; i < recv_items.Length - 1; i++)
            {
                log_add_by_filters(recv_items[i]);
            }

            if (dg_log.Items.Count > 0)
            {
                dg_log.ScrollIntoView(dg_log.Items[dg_log.Items.Count - 1]);
            }
        }

        private void serial_send(byte[] data)
        {
            try
            {
                if (!serial.send(data))
                {
                    log_add_error("发送失败");
                }
            }
            catch (Exception)
            {

            }
        }

        static private void log_clear()
        {
            log_data_mutex.WaitOne();
            log_data.Clear();
            log_data_filtered.Clear();
            log_data_mutex.ReleaseMutex();
            foreach (var filter in filters)
            {
                filter.Match_count = 0;
            }
        }

        static private void log_add_by_filters(string s)
        {
            bool is_match = false;
            foreach (FilterUtils.Filter filter in filters)
            {
                if (!is_match && FilterUtils.Filter_match(s, filter))
                {
                    is_match = true;
                    LogItem item = new LogItem(s, filter.Foreground, filter.Background);
                    log_add_item(item);
                    log_filtered_add_item(item);
                    filter.Match_count++;
                }
            }
            if (!is_match)
            {
                log_add_item(new LogItem(s));
            }
        }

        static private void log_filtered_add_item(LogItem item)
        {
            log_data_mutex.WaitOne();
            log_data_filtered.Add(item);
            if (log_data_filtered.Count > 10000 && serial.is_open)
            {
                for (int i = 0; i < 5000; i++)
                {
                    log_data_filtered.RemoveAt(0);
                }
            }
            log_data_mutex.ReleaseMutex();
        }

        static private void log_add_item(LogItem item)
        {
            log_data_mutex.WaitOne();
            log_data.Add(item);
            if (log_data.Count > 10000 && serial.is_open)
            {
                for (int i = 0; i < 5000; i++)
                {
                    log_data.RemoveAt(0);
                }
            }
            log_data_mutex.ReleaseMutex();
        }

        private void log_add_error(string s)
        {
            log_add_item(new LogItem(s, Brushes.DarkRed, null));
        }

        private void log_add_info(string s)
        {
            log_add_item(new LogItem(s, Brushes.DarkCyan, null));
        }

        private void log_add_normal(string s)
        {
            log_add_item(new LogItem(s, Brushes.Black, null));
        }

        private void serial_send_data(string data)
        {
            List<byte> byte_data;
            if (cb_is_hex.IsChecked == true)
            {
                try
                {
                    byte_data = BytesUtils.String_to_byte(data).ToList();
                }
                catch (Exception)
                {
                    log_add_error("转换十六进制失败，发送失败");
                    return;
                }
            }
            else
            {
                byte_data = Encoding.UTF8.GetBytes(data).ToList();
            }
            if (cb_is_enter.IsChecked == true)
            {
                byte_data.Add((byte)'\r');
                byte_data.Add((byte)'\n');
            }
            if (cb_is_clear.IsChecked == true)
            {
                log_clear();
            }
            if (cb_is_not_print.IsChecked == false)
            {
                log_add_normal(data);
            }

            serial_send(byte_data.ToArray());
        }

        private void btn_send_Click(object sender, RoutedEventArgs e)
        {
            serial_send_data(tb_send_data.Text);
        }

        private void tb_send_data_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                serial_send_data(tb_send_data.Text);
            }
        }

        private void btn_add_filter_Click(object sender, RoutedEventArgs e)
        {
            add_filter();
        }

        private void mn_add_filter_Click(object sender, RoutedEventArgs e)
        {
            add_filter();
        }

        private void add_filter()
        {
            AddFilter addFilter;
            if (selected_item != null)
            {
                addFilter = new AddFilter(selected_item.Text);
            }
            else
            {
                addFilter = new AddFilter();
            }

            addFilter.ShowDialog();
        }

        private void btn_clear_log_Click(object sender, RoutedEventArgs e)
        {
            log_clear();
        }

        bool is_show_filtered = false;
        LogItem selected_item = null;
        private void btn_show_filtered_Click(object sender, RoutedEventArgs e)
        {
            switch_filter_show_state();
        }

        private void switch_filter_show_state()
        {
            if (is_show_filtered)
            {
                is_show_filtered = false;
                dg_log.DataContext = log_data;
                btn_show_filtered.Content = "过滤";
                mn_swith_filter.IsChecked = false;
            }
            else
            {
                is_show_filtered = true;
                dg_log.DataContext = log_data_filtered;
                btn_show_filtered.Content = "全部";
                mn_swith_filter.IsChecked = true;
            }

            find_index = 0;

            if (serial.is_open)
            {
                if (dg_log.Items.Count > 0)
                {
                    dg_log.ScrollIntoView(dg_log.Items[dg_log.Items.Count - 1]);
                }
            }
            else
            {
                if (selected_item != null)
                {
                    dg_log.ScrollIntoView(selected_item);
                }
            }
        }

        private void dg_log_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selected_item = (LogItem)dg_log.SelectedItem;
        }

        private void btn_read_filters_Click(object sender, RoutedEventArgs e)
        {
            FilterUtils.Filter_read(ref filters);

            analys_log_data();
        }

        private void edit_filter()
        {
            try
            {
                AddFilter addFilter;
                if (dg_filter.SelectedCells.Count > 0)
                {
                    FilterUtils.Filter filter = (FilterUtils.Filter)dg_filter.SelectedCells[0].Item;
                    addFilter = new AddFilter(filter);
                }
                else
                {
                    addFilter = new AddFilter();
                }
                addFilter.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("编辑过滤器：" + ex.Message, "错误");
            }
        }

        private void dg_filter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            edit_filter();
        }

        private void mn_edit_filter_Click(object sender, RoutedEventArgs e)
        {
            if (dg_filter.SelectedCells.Count > 0)
            {
                edit_filter();
            }
            else
            {
                MessageBox.Show("未选定任何过滤器", "提示");
            }
        }

        private void btn_save_filters_Click(object sender, RoutedEventArgs e)
        {
            FilterUtils.Filter_save(ref filters);
        }

        private void mn_read_filters_Click(object sender, RoutedEventArgs e)
        {
            FilterUtils.Filter_read(ref filters);

            analys_log_data();
        }

        private void mn_save_filters_Click(object sender, RoutedEventArgs e)
        {
            FilterUtils.Filter_save(ref filters);
        }

        static public void analys_log_data()
        {
            try
            {
                if (!serial.is_open && log_data_lines != null && log_data_lines.Length > 0)
                {
                    log_clear();
                    foreach (var line in log_data_lines)
                    {
                        log_add_by_filters(line);
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        static string log_data_file_name = "";
        static public void read_log_data(string file_name)
        {
            if (!File.Exists(file_name))
            {
                return;
            }
            string file_data = "";
            using (StreamReader streamReader = new StreamReader(file_name, Encoding.UTF8))
            {
                file_data = streamReader.ReadToEnd();
            }
            log_data_lines = file_data.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            log_data_file_name = file_name;
        }

        private void dg_log_Drop(object sender, DragEventArgs e)
        {
            if (!serial.is_open)
            {
                string fileName = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();

                read_log_data(fileName);
                refresh_title();

                analys_log_data();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.H && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                switch_filter_show_state();
            }
            else if (e.Key == Key.N && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                add_filter();
            }
            else if (e.Key == Key.O && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                if (!serial.is_open)
                {
                    string log_file = open_log_file();
                    if (log_file.Equals(""))
                    {
                        return;
                    }

                    read_log_data(log_file);
                    refresh_title();

                    analys_log_data();
                }
                else
                {
                    MessageBox.Show("请先关闭串口再进行日志分析", "提示");
                }
            }
            else if (e.Key == Key.F && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
            {
                begin_find();
            }
            else if (e.Key == Key.F2)
            {
                find_text(true);
            }
            else if (e.Key == Key.F3)
            {
                find_text();
            }
            else if (e.Key == Key.F5)
            {
                if (!serial.is_open)
                {
                    read_log_data(log_data_file_name);

                    analys_log_data();
                }
            }
        }

        private void btn_filter_clear_Click(object sender, RoutedEventArgs e)
        {
            filters.Clear();
            analys_log_data();
        }

        private void mn_clear_filters_Click(object sender, RoutedEventArgs e)
        {
            filters.Clear();
            analys_log_data();
        }

        private void delete_filter()
        {
            if (dg_filter.SelectedCells.Count > 0)
            {
                FilterUtils.Filter filter = (FilterUtils.Filter)dg_filter.SelectedCells[0].Item;
                filters.Remove(filter);
                analys_log_data();
            }
            else
            {
                MessageBox.Show("请先选中需删除的过滤器", "提示");
            }
        }

        private void btn_filter_delete_Click(object sender, RoutedEventArgs e)
        {
            delete_filter();
        }

        private void mn_delete_filter_Click(object sender, RoutedEventArgs e)
        {
            delete_filter();
        }

        private void dg_filter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (dg_filter.SelectedCells.Count > 0)
            {
                if (dg_filter.SelectedCells[0].Column.DisplayIndex == 0)
                {
                    FilterUtils.Filter filter = (FilterUtils.Filter)dg_filter.SelectedCells[0].Item;
                    filter.Is_enable = !filter.Is_enable;
                    analys_log_data();
                }
            }
        }

        private void dg_log_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            add_filter();
        }

        private string open_log_file()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "打开日志文件";
            openFileDialog.Filter = "日志文件|*.log;*.txt|所有文件|*.*";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            // openFileDialog.Multiselect = true;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "log";
            if (openFileDialog.ShowDialog() == false)
            {
                return "";
            }

            return openFileDialog.FileName;
        }

        private void mn_open_Click(object sender, RoutedEventArgs e)
        {
            if (!serial.is_open)
            {
                string log_file = open_log_file();
                if (log_file.Equals(""))
                {
                    return;
                }

                read_log_data(log_file);
                refresh_title();

                analys_log_data();
            }
            else
            {
                MessageBox.Show("请先关闭串口再进行日志分析");
            }
        }

        private void mn_refresh_log_Click(object sender, RoutedEventArgs e)
        {
            if (!serial.is_open)
            {
                read_log_data(log_data_file_name);

                analys_log_data();
            }
        }

        private void mn_quit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void mn_swith_filter_Click(object sender, RoutedEventArgs e)
        {
            switch_filter_show_state();
        }

        private void mn_zoom_in_Click(object sender, RoutedEventArgs e)
        {
            log_add_error("guofan - todo");
        }

        private void mn_zoom_out_Click(object sender, RoutedEventArgs e)
        {
            log_add_error("guofan - todo");
        }

        private void mn_reset_zoom_Click(object sender, RoutedEventArgs e)
        {
            log_add_error("guofan - todo");
        }

        private void mn_enable_all_filters_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < filters.Count; i++)
            {
                filters[i].Is_enable = true;
            }
        }

        private void mn_disable_all_filters_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < filters.Count; i++)
            {
                filters[i].Is_enable = false;
            }
        }

        int find_index = 0;
        private void find_text(bool rev = false)
        {
            if (serial.is_open)
            {
                MessageBox.Show("日志采集过程，编辑功能禁用", "抱歉");
                return;
            }

            if (FindText.filter != null && FindText.filter.Text.Length > 0)
            {
                if (is_show_filtered)
                {
                    find_in_log_data(ref log_data_filtered, rev);
                }
                else
                {
                    find_in_log_data(ref log_data, rev);
                }
            }
        }

        private void find_in_log_data(ref ObservableCollection<LogItem> logItems, bool rev)
        {
            if (rev)
            {
                if (find_index == 0)
                {
                    find_index = logItems.Count - 1;
                }

                for (int i = find_index; i >= 0; i--)
                {
                    if (FilterUtils.Filter_match(logItems[i].Text, FindText.filter))
                    {
                        find_index = i - 1;
                        dg_log.SelectedItem = logItems[i];
                        dg_log.ScrollIntoView(logItems[i]);
                        return;
                    }
                }

                MessageBox.Show("已查找至文件开头", "提示");
                find_index = logItems.Count-1;
            }
            else
            {
                if (find_index == logItems.Count - 1)
                {
                    find_index = 0;
                }

                for (int i = find_index; i < logItems.Count; i++)
                {
                    if (FilterUtils.Filter_match(logItems[i].Text, FindText.filter))
                    {
                        find_index = i + 1;
                        dg_log.SelectedItem = logItems[i];
                        dg_log.ScrollIntoView(logItems[i]);
                        return;
                    }
                }

                MessageBox.Show("已查找至文件末尾", "提示");
                find_index = 0;
            }
        }

        private void begin_find()
        {
            if (serial.is_open)
            {
                MessageBox.Show("日志采集过程，编辑功能禁用", "抱歉");
                return;
            }

            FindText find;
            if (selected_item != null)
            {
                find = new FindText(selected_item.Text, delegate { find_text(); });
            }
            else
            {
                find = new FindText(delegate { find_text(); });
            }
            find.ShowDialog();
        }

        private void mn_find_Click(object sender, RoutedEventArgs e)
        {
            begin_find();
        }

        private void mn_pre_Click(object sender, RoutedEventArgs e)
        {
            find_text(true);
        }

        private void mn_next_Click(object sender, RoutedEventArgs e)
        {
            find_text();
        }

        private void mn_goto_Click(object sender, RoutedEventArgs e)
        {
            log_add_error("guofan - todo");
        }

        private void mn_settings_Click(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings(log_path);
            settings.ShowDialog();
        }

        private void mn_help_Click(object sender, RoutedEventArgs e)
        {
            log_add_error("guofan - todo");
        }

        private void mn_about_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.Show();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            configuration.AppSettings.Settings["window_width"].Value = ActualWidth.ToString();
            configuration.AppSettings.Settings["window_height"].Value = ActualHeight.ToString();
            configuration.AppSettings.Settings["selected_com"].Value = cb_com.Text;
            configuration.AppSettings.Settings["selected_baud"].Value = cb_baud.Text;
            configuration.AppSettings.Settings["log_path"].Value = log_path;

            configuration.Save();

            Environment.Exit(0);
        }
    }

    class LogItem
    {
        public string Text { get; set; }
        public Brush Foreground { get; set; }
        public Brush Background { get; set; }

        public LogItem(string text)
        {
            Text = text;
            Foreground = Brushes.Black;
            Background = null;
        }

        public LogItem(string text, Brush fg, Brush bg)
        {
            Text = text;
            Foreground = fg;
            Background = bg;
        }
    }
}
