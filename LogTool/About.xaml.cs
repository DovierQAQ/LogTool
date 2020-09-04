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
    /// About.xaml 的交互逻辑
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Info_write_line("作者：郭帆");
            Info_write_line("邮箱：guofan2@midea.com");
            Info_write_line("用途：采集串口日志、分析日志");
            Info_write_line("第一版时间：");
        }

        private void Info_write(string content)
        {
            tb_info.Dispatcher.Invoke(new Action(delegate
            {
                tb_info.Text += content;
            }));
        }

        private void Info_write_line(string content)
        {
            Info_write(content + "\n");
        }

        private int icon_click_cnt = 0;
        private bool is_easter_egg = false;
        private void img_icon_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            icon_click_cnt++;

            if (icon_click_cnt > 2)
            {
                if (!is_easter_egg)
                {
                    is_easter_egg = true;
                    Game_init();
                }
            }
            else
            {
                Info_write_line("(ノ๑`ȏ´๑)ノ︵⌨");
            }
        }
        private void Game_init()
        {
            this.Height += 360;
            cv_game.Background = Brushes.Black;

            // todo
        }

        private void Draw_clear()
        {
            cv_game.Dispatcher.Invoke(new Action(delegate { cv_game.Children.Clear(); }));
        }

        private void Draw_rect(Point center, int width, int height, Brush fill_color, Brush border_color)
        {
            Point begin = new Point(center.X - width / 2, center.Y - height / 2);
            Point end = new Point(center.X + width / 2, center.Y + height / 2);
            Draw_rect(begin, end, fill_color, border_color);
        }

        private void Draw_rect(Point startPt, Point endPt, Brush fill_color, Brush border_color)
        {
            RectangleGeometry myRectangleGeometry = new RectangleGeometry();
            myRectangleGeometry.Rect = new Rect(startPt, endPt);

            Path myPath = new Path();
            myPath.Fill = fill_color;
            myPath.Stroke = border_color;
            myPath.StrokeThickness = 1;
            myPath.Data = myRectangleGeometry;

            cv_game.Dispatcher.Invoke(new Action(delegate { cv_game.Children.Add(myPath); }));
        }

        private void Draw_line(Point startPt, Point endPt, Brush color, int width = 1)
        {
            LineGeometry myLineGeometry = new LineGeometry();
            myLineGeometry.StartPoint = startPt;
            myLineGeometry.EndPoint = endPt;

            Path myPath = new Path();
            myPath.Stroke = color;
            myPath.StrokeThickness = width;
            myPath.Data = myLineGeometry;

            cv_game.Dispatcher.Invoke(new Action(delegate { cv_game.Children.Add(myPath); }));
        }

        static private Brush COLOR_CELL = new SolidColorBrush(Color.FromRgb(200, 200, 200));
        static private int GAPSIZE = 5;
        static private int BOARDRIGHT = 4;
        static private int BOARDBOTTOM = 4;
        static private Point BOARDSIZE = new Point(BOARDRIGHT, BOARDBOTTOM);
        static private int STARTCARDAMOUNT = 2;
        static private int STARTCARDNUMBER = 2;
        static private int AWARDNUMBER = 2;

        private enum Direction
        {
            UP = 0,
            DOWN,
            LEFT,
            RIGHT,
            STAY
        };

        private class Game2048
        {

        }

        private class Card
        {
            Rect card;
            Point card_center;
            Point position;
            int number;
            public int Number
            {
                get
                {
                    return number;
                }
                set
                {
                    number = value;
                    if (value != 0)
                    {
                        Level = (int)Math.Log(value);
                        Card_color = new SolidColorBrush(Color.FromRgb((byte)(Level * 15), (byte)(200 - Level * 15), (byte)(255 - Level * 15)));
                    }
                    else
                    {
                        Level = 0;
                        Card_color = COLOR_CELL;
                    }
                    // Number_size = RECTSIZE - Level * 5;
                }
            }
            public int Level { get; set; }
            public int Number_size { get; set; }
            public Brush Card_color { get; set; }
        }
    }
}
