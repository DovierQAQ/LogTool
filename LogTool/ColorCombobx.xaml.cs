using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
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

namespace LogTool
{
    /// <summary>
    /// ColorCombobx.xaml 的交互逻辑
    /// </summary>
    public partial class ColorCombobx : UserControl
    {
        public ColorCombobx()
        {
            InitializeComponent();

            cb_color.ItemsSource = typeof(Colors).GetProperties();
        }

        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(nameof(SelectedColor), 
            typeof(Color?), typeof(ColorCombobx), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));
        public Color? SelectedColor
        {
            get
            {
                return (Color?)GetValue(SelectedColorProperty);
            }
            set
            {
                SetValue(SelectedColorProperty, value);
                if (cb_color.Items.Count > 0)
                {
                    foreach (PropertyInfo color_property in cb_color.Items)
                    {
                        if (((Color)ColorConverter.ConvertFromString(color_property.Name)).Equals(value))
                        {
                            cb_color.SelectedItem = color_property;
                        }
                    }
                }
            }
        }

        private static void OnSelectedColorChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ColorCombobx cc = o as ColorCombobx;
            if (cc != null)
            {
                cc.OnSelectedColorChanged((Color?)e.OldValue, (Color?)e.NewValue);
            }
        }

        protected virtual void OnSelectedColorChanged(Color? oldValue, Color? newValue)
        {
            var args = new RoutedPropertyChangedEventArgs<Color?>(oldValue, newValue);
            args.RoutedEvent = SelectedColorChangedEvent;
            RaiseEvent(args);
        }

        public static readonly RoutedEvent SelectedColorChangedEvent = EventManager.RegisterRoutedEvent(nameof(SelectedColorChanged),
            RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<Color?>), typeof(ColorCombobx));
        public event RoutedPropertyChangedEventHandler<Color?> SelectedColorChanged
        {
            add
            {
                AddHandler(SelectedColorChangedEvent, value);
            }
            remove
            {
                RemoveHandler(SelectedColorChangedEvent, value);
            }
        }

        private void UpdateSelectedColor(Color? color)
        {
            SelectedColor = ((color != null) && color.HasValue)
                            ? (Color?)Color.FromArgb(color.Value.A, color.Value.R, color.Value.G, color.Value.B)
                            : null;
        }

        private void cb_color_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var color_property = (PropertyInfo)cb_color.SelectedItem;
            UpdateSelectedColor((Color)ColorConverter.ConvertFromString(color_property.Name));
        }
    }
}
