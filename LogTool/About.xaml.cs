using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Info_write_line("第一版时间：2020-09");
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

            if (icon_click_cnt > 9)
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
        GameAI gameAI;
        private void Game_init()
        {
            this.Height += 360;
            cv_game.Background = Brushes.Black;

            game2048 = new Game2048(GAMESIZE);

            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
        }

        Int64 time_last = 0;
        Int64 time_to_print = 0;
        Int64 time_for_ai = 0;
        const int FRAME_TIME = 7;
        const int GAP_WIDTH = 3;
        const int MOVE_SPEED = 70;
        bool is_closed = false;

        static Brush COLOR_CELL = Brushes.AliceBlue;

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (is_closed)
            {
                return;
            }

            Int64 time_now = TimeUtils.GetTimestamp();
            if (time_last == 0)
            {
                time_last = time_now;
            }
            int time_spend = (int)(time_now - time_last);

            if (time_spend >= FRAME_TIME)
            {
                Draw_game();

                time_last = time_now;
            }

            if (time_now - time_for_ai >= 500)
            {
                if (gameAI != null)
                {
                    gameAI.Go();
                }

                time_for_ai = time_now;
            }

            if (time_now - time_to_print > 1000)
            {
                time_to_print = time_now;
                Console.WriteLine("===========================>print debug info: " + TimeUtils.GetTimestamp().ToString());
                // todo
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            is_closed = true;
        }

        private void Draw_game()
        {

            int w_unit = (int)(Playground.X / (game2048.size.X * 2));
            int h_unit = (int)(Playground.Y / (game2048.size.Y * 2));
            int card_width = (int)(Playground.X / game2048.size.X) - GAP_WIDTH;
            int card_height = (int)(Playground.Y / game2048.size.Y) - GAP_WIDTH;

            if (game2048.card_moves.Count > 0)
            {
                game2048.card_move_mutex.WaitOne();

                CardMovement card_move = game2048.card_moves[0];
                if (!card_move.Is_stop)
                {
                    Point card_center = Calc_card_center(card_move.Origin_card, w_unit, h_unit);

                    card_move.Step(card_center, card_width, card_height, MOVE_SPEED);
                    if (!card_move.Is_in)
                    {
                        Card card_cover = new Card(card_move.Origin_card.Position, 0);
                        CardVisual cardVisual = new CardVisual(card_cover, card_center, card_width, card_height);
                        cv_game.Children.Add(cardVisual.button);
                        cv_game.Children.Add(card_move.Current_card.button);
                        card_move.Is_in = true;
                    }
                }
                if (card_move.Is_stop)
                {
                    Point card_center = Calc_card_center(card_move.Dest_card, w_unit, h_unit);

                    Card card_cover = new Card(card_move.Dest_card.Position, card_move.Dest_card.Number);
                    CardVisual cardVisual = new CardVisual(card_cover, card_center, card_width, card_height);
                    cv_game.Children.Add(cardVisual.button);

                    cv_game.Children.Remove(card_move.Current_card.button);
                    game2048.card_moves.Remove(card_move);
                }

                game2048.card_move_mutex.ReleaseMutex();
            }
            else
            {
                Draw_clear();

                foreach (List<Card> card_line in game2048.cards)
                {
                    foreach (Card card in card_line)
                    {
                        Point card_center = Calc_card_center(card, w_unit, h_unit);
                        CardVisual cardVisual = new CardVisual(card, card_center, card_width, card_height);
                        cv_game.Children.Add(cardVisual.button);
                    }
                }
            }
        }

        private Point Calc_card_center(Card card, int w_unit, int h_unit)
        {
            return new Point((1 + 2 * card.Position.X) * w_unit, (1 + 2 * card.Position.Y) * h_unit);
        }

        private void Draw_clear()
        {
            cv_game.Dispatcher.Invoke(new Action(delegate { cv_game.Children.Clear(); }));
        }

        private void Draw_text(Point pos, string text, Brush color, int text_size = 0, bool is_bold = false, int width = 0, int height = 0)
        {
            Label label = new Label();

            label.Content = text;
            label.Foreground = color;
            if (text_size > 0)
            {
                label.FontSize = text_size;
            }
            label.HorizontalContentAlignment = HorizontalAlignment.Center;
            label.VerticalContentAlignment = VerticalAlignment.Center;
            label.Width = width;
            label.Height = height;
            if (is_bold)
            {
                label.FontWeight = FontWeights.Bold;
            }

            Point begin = new Point(pos.X - width / 2, pos.Y - height / 2);
            Canvas.SetLeft(label, begin.X);
            Canvas.SetTop(label, begin.Y);

            cv_game.Children.Add(label);
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

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (game2048 != null && gameAI == null)
            {
                switch (e.Key)
                {
                    case Key.W:
                    case Key.Up: game2048.Move(Direction.UP); break;
                    case Key.S:
                    case Key.Down: game2048.Move(Direction.DOWN); break;
                    case Key.A:
                    case Key.Left: game2048.Move(Direction.LEFT); break;
                    case Key.D:
                    case Key.Right: game2048.Move(Direction.RIGHT); break;
                    case Key.Enter:
                        gameAI = new GameAI(game2048);
                        break;
                }
            }
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
            UP,
            DOWN,
            LEFT,
            RIGHT,
            STAY
        };

        static private Point Direction_to_offset(Direction direction)
        {
            switch (direction)
            {
                case Direction.UP: return new Point(0, -1);
                case Direction.DOWN: return new Point(0, 1);
                case Direction.LEFT: return new Point(-1, 0);
                case Direction.RIGHT: return new Point(1, 0);
                default: return new Point(0, 0);
            }
        }

        static private Point Move_direction(Point point, Direction direction)
        {
            Point offset = Direction_to_offset(direction);

            return new Point(point.X + offset.X, point.Y + offset.Y);
        }

        private class Game2048
        {
            public List<List<Card>> cards = new List<List<Card>>();
            public Point size;
            public List<CardMovement> card_moves = new List<CardMovement>();
            public Mutex card_move_mutex = new Mutex();

            Random random = new Random();

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

                for (int i = 0; i < card_amount; i++)
                {
                    Card_generate(card_number);
                }
            }

            public Game2048(Point playground, List<List<Card>> from_cards)
            {
                size = playground;
                
                for (int i = 0; i < size.X; i++)
                {
                    cards.Add(new List<Card>());
                    for (int j = 0; j < size.Y; j++)
                    {
                        cards[i].Add(new Card(new Point(i, j), from_cards[i][j].Number));
                    }
                }
            }

            public bool Move(Direction direction, bool is_generate = true)
            {
                if (direction == Direction.STAY)
                {
                    return false;
                }

                bool is_move = false;
                for (int i = 0; i < size.X; i++)
                {
                    for (int j = 0; j < size.Y; j++)
                    {
                        int x = 0;
                        int y = 0;
                        switch (direction)
                        {
                            case Direction.UP: x = i; y = j; break;
                            case Direction.DOWN: x = i; y = (int)size.Y-j-1; break;
                            case Direction.LEFT: x = i; y = j; break;
                            case Direction.RIGHT: x = (int)size.X-i-1; y = j; break;
                        }
                        if (Card_move(new Point(x, y), direction))
                        {
                            is_move = true;
                        }
                    }
                }

                if (is_move && is_generate)
                {
                    Card_generate(2);
                }

                return is_move;
            }

            private bool Card_move(Point card_pos, Direction direction)
            {
                if (card_pos.X < 0 || card_pos.X >= size.X || card_pos.Y < 0 || card_pos.Y >= size.Y)
                {
                    return false;
                }

                switch (direction)
                {
                    case Direction.UP: if (card_pos.Y <= 0) return false; break;
                    case Direction.DOWN: if (card_pos.Y >= size.Y - 1) return false; break;
                    case Direction.LEFT: if (card_pos.X <= 0) return false; break;
                    case Direction.RIGHT: if (card_pos.X >= size.X - 1) return false; break;
                    case Direction.STAY: return false;
                }

                Point dest_pos = Move_direction(card_pos, direction);
                Card card_current = cards[(int)card_pos.X][(int)card_pos.Y];
                Card card_dest = cards[(int)dest_pos.X][(int)dest_pos.Y];
                if (card_current.Number != 0)
                {
                    if (card_current.Number == card_dest.Number)
                    {
                        CardMovement card_move = new CardMovement(new Card(card_current.Position, card_current.Number), direction);

                        card_dest.Number *= 2;
                        card_current.Number = 0;

                        card_move.Dest_card = new Card(card_dest.Position, card_dest.Number);

                        card_move_mutex.WaitOne();
                        card_moves.Add(card_move);
                        card_move_mutex.ReleaseMutex();

                        Card_move(dest_pos, direction);
                        return true;
                    }
                    else if (card_dest.Number == 0)
                    {
                        CardMovement card_move = new CardMovement(new Card(card_current.Position, card_current.Number), direction);

                        card_dest.Number = card_current.Number;
                        card_current.Number = 0;

                        card_move.Dest_card = new Card(card_dest.Position, card_dest.Number);

                        card_move_mutex.WaitOne();
                        card_moves.Add(card_move);
                        card_move_mutex.ReleaseMutex();

                        Card_move(dest_pos, direction);
                        return true;
                    }
                }

                return false;
            }

            private bool Card_generate(int card_number, int other_number = 0, double other_number_rate = 0)
            {
                List<int> card_index = new List<int>();
                for (int i = 0; i < size.X * size.Y; i++)
                {
                    card_index.Add(i);
                }
                KnuthDurstenfeldShuffle(card_index);

                int number = card_number;
                if (other_number > 0)
                {
                    if (random.Next(0, 99) <= other_number_rate * 100)
                    {
                        number = other_number;
                    }
                }

                foreach (var index in card_index)
                {
                    int x = index % (int)size.X;
                    int y = (index - x) / (int)size.Y;

                    Card card_now = cards[x][y];
                    if (card_now.Number == 0)
                    {
                        card_now.Number = number;
                        return true;
                    }
                }

                return false;
            }
        }

        private class GameAI
        {
            Game2048 game;
            int max_depth;

            private bool is_running = false;

            public GameAI(Game2048 game2048, int depth = 5)
            {
                game = game2048;
                max_depth = depth;
            }

            public void Go()
            {
                Thread thread = new Thread(() =>
                {
                    if (is_running)
                    {
                        return;
                    }
                    is_running = true;
                    var result = Alpha(game, Double.NegativeInfinity, Double.PositiveInfinity);
                    Console.WriteLine("result: " + result.Item2.ToString());
                    game.Move(result.Item2);
                    is_running = false;
                });
                thread.Start();
            }

            private Tuple<double, Direction> Alpha(Game2048 game2048, double alpha_val, double beta_val, int depth = 0)
            {
                if (depth >= max_depth)
                {
                    return new Tuple<double, Direction>(Evaluate(game2048), Direction.STAY);
                }

                double max_val = alpha_val;
                Direction max_dir = Direction.STAY;
                foreach (Direction dir in Enum.GetValues(typeof(Direction)))
                {
                    Game2048 attempt_game = new Game2048(game2048.size, game2048.cards);

                    if (attempt_game.Move(dir, false))
                    {
                        double beta_now = Beta(attempt_game, max_val, beta_val, depth + 1);
                        if (beta_now > max_val)
                        {
                            max_val = beta_now;
                            max_dir = dir;
                            if (max_val > beta_val)
                            {
                                return new Tuple<double, Direction>(max_val, max_dir);
                            }
                        }
                    }
                }

                return new Tuple<double, Direction>(max_val, max_dir);
            }

            private double Beta(Game2048 game2048, double alpha_val, double beta_val, int depth)
            {
                if (depth >= max_depth)
                {
                    return Evaluate(game2048);
                }

                double min_val = beta_val;
                for (int i = 0; i < game2048.size.X; i++)
                {
                    for (int j = 0; j < game2048.size.Y; j++)
                    {
                        if (game2048.cards[i][j].Number == 0)
                        {
                            Game2048 attempt_game = new Game2048(game2048.size, game2048.cards);

                            attempt_game.cards[i][j].Number = 2;
                            var alpha_now = Alpha(attempt_game, alpha_val, min_val, depth + 1);

                            if (alpha_now.Item1 < min_val)
                            {
                                min_val = alpha_now.Item1;
                            }
                            if (alpha_val >= min_val)
                            {
                                return min_val;
                            }

                            // todo awardnumber
                        }
                    }
                }

                return min_val;
            }

            double weight_em = 1;
            double weight_ml = 1;
            double weight_mo = 0;
            double weight_av = 1;

            private double Evaluate(Game2048 game2048)
            {
                int empty_count = 0;
                int max_level = 0;
                int level_sum = 0;
                int level_num = 0;
                foreach (List<Card> line in game2048.cards)
                {
                    foreach (Card card in line)
                    {
                        if (card.Number == 0)
                        {
                            empty_count++;
                        }
                        else
                        {
                            level_sum += card.Level;
                            level_num++;
                        }
                        if (card.Level > max_level)
                        {
                            max_level = card.Level;
                        }
                    }
                }

                double em = empty_count * weight_em;
                double ml = max_level * weight_ml;
                double av = level_sum / level_num * weight_av;

                int mono = 0;
                for (int j = 0; j < game2048.size.Y; j++)
                {
                    List<int> index_x = new List<int>();
                    for (int i = 0; i < game2048.size.X; i++)
                    {
                        index_x.Add(i);
                    }
                    if (j % 2 != 0)
                    {
                        index_x.Reverse();
                    }

                    for (int i = 0; i < game2048.size.X; i++)
                    {
                        int y = j;
                        int x = index_x[i];
                        if (y > 0 && i == 0)
                        {
                            mono += game2048.cards[x][y].Level - game2048.cards[x][y - 1].Level;
                        }
                        if (i > 0)
                        {
                            int x_pre = index_x[i - 1];
                            mono += game2048.cards[x][y].Level - game2048.cards[x_pre][y].Level;
                        }
                    }
                }

                double mo = mono * weight_mo;

                return em + ml + av + mo;
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
                        Level = (int)Math.Log(value, 2);
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

        private class CardMovement
        {
            public Card Origin_card;
            public CardVisual Current_card;
            public Card Dest_card;
            public Direction direction;
            public bool Is_in;
            public bool Is_stop;

            private int move_distance;

            public CardMovement(Card origin, Direction dir)
            {
                Origin_card = origin;
                direction = dir;
                Is_in = false;
                Is_stop = false;

                move_distance = 0;
            }

            public void Step(Point center, int width, int height, int pix = 1)
            {
                if (Current_card == null)
                {
                    Current_card = new CardVisual(Origin_card, center, width, height);
                }
                else
                {
                    Current_card.Resize(width, height);
                }

                Current_card.Step(direction, pix);
                move_distance += pix;
                switch (direction)
                {
                    case Direction.UP:
                    case Direction.DOWN:
                        if (move_distance >= Current_card.button_height)
                        {
                            Is_stop = true;
                        }
                        break;
                    case Direction.LEFT:
                    case Direction.RIGHT:
                        if (move_distance >= Current_card.button_width)
                        {
                            Is_stop = true;
                        }
                        break;
                }
            }
        }

        private class CardVisual
        {
            public Button button;
            public Point button_center;
            public int button_width;
            public int button_height;

            public CardVisual(Card card, Point center, int width, int height)
            {
                button = new Button();
                button_center = center;
                button_width = width;
                button_height = height;

                Brush card_color;
                if (card.Number == 0)
                {
                    card_color = COLOR_CELL;
                }
                else
                {
                    card_color = Calc_card_color(card.Level);
                    button.Content = card.Number.ToString();
                }

                int font_size = Math.Min(width, height) - card.Number.ToString().Length * 13; // todo
                button.FontSize = font_size >= 10 ? font_size : 10;
                button.Background = card_color;
                button.Width = width;
                button.Height = height;
                button.FontWeight = FontWeights.Bold;

                Point begin = new Point(center.X - width / 2, center.Y - height / 2);
                Canvas.SetLeft(button, begin.X);
                Canvas.SetTop(button, begin.Y);
            }

            private Brush Calc_card_color(int level)
            {
                return new SolidColorBrush(Color.FromRgb((byte)(level * 15), (byte)(200 - level * 15), (byte)(255 - level * 15)));
            }

            public void Resize(int width, int height)
            {
                button_width = width;
                button_height = height;
            }

            public void Step(Direction direction, int pix)
            {
                Point offset = Direction_to_offset(direction);
                offset.X *= pix;
                offset.Y *= pix;
                button_center.X += offset.X;
                button_center.Y += offset.Y;

                Point begin = new Point(button_center.X - button_width / 2, button_center.Y - button_height / 2);
                Canvas.SetLeft(button, begin.X);
                Canvas.SetTop(button, begin.Y);
            }
        }
    }
}
