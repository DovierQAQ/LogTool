using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace LogTool
{
    class MainApp
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                try
                {
                    MainWindow.read_log_data(args[0]);
                }
                catch (Exception)
                {

                }
            }
            App app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
