using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LogTool
{
    class GFSerial
    {
        static public PortsToDisplay ports = new PortsToDisplay();
        static private DispatcherTimer timer_refresh = null;

        //public bool is_open = false;
        public Mutex recv_data_mutex = new Mutex();
        public List<byte> serial_recv_data = new List<byte>();
        public bool is_open
        {
            get
            {
                return serialPort.IsOpen;
            }
        }

        SerialPort serialPort = new SerialPort();
        Action recv_callback = null;

        public GFSerial(Action serial_recv_callback)
        {
            recv_callback = serial_recv_callback;

            serialPort.ReadTimeout = 8000;
            serialPort.WriteTimeout = 8000;
            serialPort.ReadBufferSize = 1024;
            serialPort.WriteBufferSize = 1024;
            serialPort.Parity = Parity.None;
            serialPort.DataBits = 8;
            serialPort.StopBits = StopBits.One;
            serialPort.Handshake = Handshake.None;
            // serialPort.ReceivedBytesThreshold = 1; // 每1字节触发处理函数
            serialPort.DataReceived += new SerialDataReceivedEventHandler(Serial_DataReceived);

            if (timer_refresh == null) // todo multiple serial objects
            {
                timer_refresh = new DispatcherTimer();
                timer_refresh.Interval = TimeSpan.FromMilliseconds(2000);
                timer_refresh.Tick += new EventHandler(refresh_ports_callback);
                timer_refresh.Start();
            }

            refresh_ports();
        }

        private void refresh_ports()
        {
            ports.Ports = SerialPort.GetPortNames().ToList();
        }

        private void refresh_ports_callback(object sender, EventArgs e)
        {
            refresh_ports();
        }

        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            byte[] reDatas = new byte[serialPort.BytesToRead];

            serialPort.Read(reDatas, 0, reDatas.Length);

            recv_data_mutex.WaitOne();
            serial_recv_data.AddRange(reDatas);
            recv_data_mutex.ReleaseMutex();
            
            if (recv_callback != null)
            {
                recv_callback.Invoke();
            }
        }

        public bool open_serial(string com, int baud)
        {
            if (!ports.Ports.Any(e => e.StartsWith(com)))
            {
                return false;
            }

            serialPort.PortName = com;
            serialPort.BaudRate = baud;

            serialPort.Open();
            //is_open = true;

            return true;
        }

        public bool close_serial()
        {
            serialPort.DiscardInBuffer();
            serialPort.DiscardOutBuffer();

            serialPort.Close();
            //is_open = false;

            return true;
        }

        public bool send(byte[] data)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Write(data, 0, data.Length);
                return true;
            }
            return false;
        }

        public class PortsToDisplay : INotifyPropertyChanged
        {
            private List<string> ports;
            public List<string> Ports
            {
                get { return ports; }
                set
                {
                    if (ports != value)
                    {
                        ports = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("Ports"));
                    }
                }
            }
            public event PropertyChangedEventHandler PropertyChanged = delegate { };
        }
    }
}
