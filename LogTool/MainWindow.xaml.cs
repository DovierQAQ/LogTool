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

        GFSerial serial = null;

        ObservableCollection<LogItem> log_data = new ObservableCollection<LogItem>();
        ObservableCollection<LogItem> log_data_filtered = new ObservableCollection<LogItem>();
        Mutex log_data_mutex = new Mutex();
        Mutex buffer_mutex = new Mutex();
        string log_buffer = "";
        string log_file_name = "";

        static public ObservableCollection<FilterUtils.Filter> filters = new ObservableCollection<FilterUtils.Filter>();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            serial = new GFSerial(delegate { recv_data_callback(); }, delegate { serial_close_callback(); });

            cb_com.DataContext = GFSerial.ports;
            Console.WriteLine(cb_com.Items.Count.ToString());
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

            log_data_mutex.WaitOne();
            for (int i = 0; i < recv_items.Length - 1; i++)
            {
                bool is_match = false;
                foreach (FilterUtils.Filter filter in filters)
                {
                    if (!is_match && FilterUtils.Filter_match(recv_items[i], filter))
                    {
                        is_match = true;
                        LogItem item = new LogItem(recv_items[i], filter.Foreground, filter.Background);
                        log_data.Add(item);
                        log_data_filtered.Add(item);
                        if (log_data_filtered.Count > 10000)
                        {
                            for (int j = 0; j < 5000; j++)
                            {
                                log_data_filtered.RemoveAt(0);
                            }
                        }
                    }
                }
                if (!is_match)
                {
                    log_data.Add(new LogItem(recv_items[i]));
                }
            }
            if (log_data.Count > 10000)
            {
                for (int i = 0; i < 5000; i++)
                {
                    log_data.RemoveAt(0);
                }
            }

            dg_log.ScrollIntoView(dg_log.Items[dg_log.Items.Count - 1]);
            log_data_mutex.ReleaseMutex();
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

        private void log_clear()
        {
            log_data_mutex.WaitOne();
            log_data.Clear();
            log_data_filtered.Clear();
            log_data_mutex.ReleaseMutex();
        }

        private void log_add_item(LogItem item)
        {
            log_data_mutex.WaitOne();
            log_data.Add(item);
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

        static public string selected_string = "";
        private void btn_add_filter_Click(object sender, RoutedEventArgs e)
        {
            AddFilter addFilter = new AddFilter();

            addFilter.ShowDialog();
        }

        private void btn_clear_log_Click(object sender, RoutedEventArgs e)
        {
            log_clear();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            filters.Clear();
        }

        bool is_show_filtered = false;
        private void btn_show_filtered_Click(object sender, RoutedEventArgs e)
        {
            if (is_show_filtered)
            {
                is_show_filtered = false;
                dg_log.DataContext = log_data;
                btn_show_filtered.Content = "显示过滤";
            }
            else
            {
                is_show_filtered = true;
                dg_log.DataContext = log_data_filtered;
                btn_show_filtered.Content = "显示全部";
            }

            dg_log.ScrollIntoView(dg_log.Items[dg_log.Items.Count - 1]);
        }

        private void btn_read_filters_Click(object sender, RoutedEventArgs e)
        {
            FilterUtils.Filter_read(ref filters);
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
