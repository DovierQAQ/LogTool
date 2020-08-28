using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        Mutex buffer_mutex = new Mutex();
        string log_buffer = "";

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            serial = new GFSerial(delegate { recv_data_callback(); });

            cb_com.DataContext = GFSerial.ports;

            cb_baud.SelectedIndex = 8;

            log_data.Add(new LogItem() { Text = "Log Tool file begin" });
            dg_log.DataContext = log_data;

            DispatcherTimer timer_show_log = new DispatcherTimer();
            timer_show_log.Interval = TimeSpan.FromMilliseconds(20);
            timer_show_log.Tick += new EventHandler(log_show_callback);
            timer_show_log.Start();
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
            string[] recv_items = log_buffer.Split('\n');
            log_buffer = "";
            buffer_mutex.ReleaseMutex();

            log_data.Last().Text += recv_items[0];
            for (int i = 1; i < recv_items.Length; i++)
            {
                log_data.Add(new LogItem() { Text = recv_items[i] });
            }

            dg_log.ScrollIntoView(dg_log.Items[dg_log.Items.Count - 1]);
        }

        class LogItem
        {
            public string Text { get; set; }
        }
    }
}
