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
using System.Windows.Threading;

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

        // game settings
        private Point GAMESIZE = new Point(4, 4);

        Game2048 game2048;
        private DispatcherTimer timer_2048 = null;
        private void Game_init()
        {
            this.Height += 360;
            cv_game.Background = Brushes.Black;

            game2048 = new Game2048(GAMESIZE);

            timer_2048 = new DispatcherTimer();
            timer_2048.Interval = TimeSpan.FromMilliseconds(10);
            timer_2048.Tick += new EventHandler(Timer_2048_callback);
            timer_2048.Start();
        }

        private void Timer_2048_callback(object sender, EventArgs e)
        {
            // todo
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (timer_2048 != null)
            {
                timer_2048.Stop();
            }
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

        private void cv_game_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Playground = new Point(cv_game.ActualWidth, cv_game.ActualHeight);
        }

        static private void KnuthDurstenfeldShuffle<T>(List<T> list)
        {
            int currentIndex;
            T tempValue;
            Random random = new Random();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                currentIndex = random.Next(0, i + 1);
                tempValue = list[currentIndex];
                list[currentIndex] = list[i];
                list[i] = tempValue;
            }
        }

        private Point Playground { get; set; }

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
            List<List<Card>> cards = new List<List<Card>>();
            Point size;

            public Game2048(Point playground, int card_amount = 2, int card_number = 2)
            {
                size = playground;

                for (int i = 0; i < size.X; i++)
                {
                    cards.Add(new List<Card>());
                    for (int j = 0; j < size.Y; j++)
                    {
                        cards[i].Add(new Card(new Point(i, j), 0));
                    }
                }

                List<int> card_index = new List<int>();
                for (int i = 0; i < size.X * size.Y; i++)
                {
                    card_index.Add(i);
                }
                KnuthDurstenfeldShuffle(card_index);
                for (int i = 0; i < card_amount; i++)
                {
                    int x = i % (int)size.X;
                    int y = (i - x) / (int)size.Y;
                    cards[x][y].Number = card_number;
                }
            }
        }

        private class Card
        {
            public Point Position { get; set; }
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
                    }
                    else
                    {
                        Level = 0;
                    }
                }
            }

            public int Level { get; private set; }

            public Card(Point pos, int num)
            {
                Position = pos;
                Number = num;
            }
        }
    }
}
