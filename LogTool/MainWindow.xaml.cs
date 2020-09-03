using GFBytesUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
        }

        string window_title = "GuoFan Log Tool";

        static GFSerial serial = null;

        static ObservableCollection<LogItem> log_data = new ObservableCollection<LogItem>();
        static ObservableCollection<LogItem> log_data_filtered = new ObservableCollection<LogItem>();
        static Mutex log_data_mutex = new Mutex();
        Mutex buffer_mutex = new Mutex();
        string log_buffer = "";
        string log_file_name = "";

        static string file_data = "";

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

            if (!Directory.Exists("log"))
            {
                Directory.CreateDirectory("log");
            }
        }

        private void serial_close_callback()
        {
            open_state();
        }

        private void open_state()
        {
            btn_com_open.Content = "打开串口";
            cb_com.IsEnabled = true;
            cb_baud.IsEnabled = true;
        }

        private void close_state()
        {
            btn_com_open.Content = "关闭串口";
            cb_com.IsEnabled = false;
            cb_baud.IsEnabled = false;
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
                    MessageBox.Show("无法关闭串口");
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
                    log_file_name = "log/" + cb_com.Text + "_" + TimeUtils.GetFileTimeString() + ".log";
                }
                catch (Exception)
                {
                    MessageBox.Show("无法打开串口");
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

            buffer_mutex.WaitOne();
            log_buffer += Encoding.UTF8.GetString(recv_data).Replace("\r", "");
            buffer_mutex.ReleaseMutex();
        }

        private void log_show_callback(object sender, EventArgs e)
        {
            buffer_mutex.WaitOne();
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

            dg_log.ScrollIntoView(dg_log.Items[dg_log.Items.Count - 1]);
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
            log_add_item(new LogItem(s, Brushes.DarkRed, Brushes.White));
        }

        private void log_add_info(string s)
        {
            log_add_item(new LogItem(s, Brushes.DarkCyan, Brushes.White));
        }

        private void log_add_normal(string s)
        {
            log_add_item(new LogItem(s, Brushes.Black, Brushes.White));
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
            }
            else
            {
                is_show_filtered = true;
                dg_log.DataContext = log_data_filtered;
                btn_show_filtered.Content = "全部";
            }

            if (serial.is_open)
            {
                dg_log.ScrollIntoView(dg_log.Items[dg_log.Items.Count - 1]);
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
        }

        private void dg_filter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
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
                MessageBox.Show("编辑过滤器：" + ex.Message);
            }
        }

        private void btn_save_filters_Click(object sender, RoutedEventArgs e)
        {
            FilterUtils.Filter_save(ref filters);
        }

        static public void analys_log_data()
        {
            try
            {
                if (!serial.is_open && !file_data.Equals(""))
                {
                    string[] lines = file_data.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                    log_clear();
                    foreach (var line in lines)
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
            using (StreamReader streamReader = new StreamReader(file_name, Encoding.Default))
            {
                file_data = streamReader.ReadToEnd();
            }
            log_data_file_name = file_name;
        }

        private void dg_log_Drop(object sender, DragEventArgs e)
        {
            if (!serial.is_open)
            {
                string fileName = ((Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();

                read_log_data(fileName);

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

        private void btn_filter_delete_Click(object sender, RoutedEventArgs e)
        {
            if (dg_filter.SelectedCells.Count > 0)
            {
                FilterUtils.Filter filter = (FilterUtils.Filter)dg_filter.SelectedCells[0].Item;
                filters.Remove(filter);
                analys_log_data();
            }
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
            Background = Brushes.White;
        }

        public LogItem(string text, Brush fg, Brush bg)
        {
            Text = text;
            Foreground = fg;
            Background = bg;
        }
    }
}
