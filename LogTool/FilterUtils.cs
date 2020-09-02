using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;

namespace LogTool
{
    public class FilterUtils
    {

        static public bool Filter_match(string target, Filter filter)
        {
            if (!filter.Is_enable)
            {
                return false;
            }
            string filter_text = filter.Text;
            if (!filter.Is_case_sensitive)
            {
                target = target.ToLower();
                filter_text = filter_text.ToLower();
            }
            if (target.Contains(filter_text))
            {
                return true;
            }
            return false;
        }

        static public void Filter_read(ref ObservableCollection<Filter> filters)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "打开过滤器文件";
            openFileDialog.Filter = "过滤器文件|*.tat|所有文件|*.*";
            openFileDialog.FileName = string.Empty;
            openFileDialog.FilterIndex = 1;
            // openFileDialog.Multiselect = true;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.DefaultExt = "tat";
            if (openFileDialog.ShowDialog() == false)
            {
                return;
            }

            string filter_file = openFileDialog.FileName;

            XmlDocument xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(filter_file);
            }
            catch (Exception)
            {
                MessageBox.Show("xml文件无效");
                return;
            }
            XmlNode xn;
            xn = xmlDoc.SelectSingleNode("TextAnalysisTool.NET");
            if (xn == null)
            {
                xn = xmlDoc.SelectSingleNode("GuoFanLogTool");
                if (xn == null)
                {
                    MessageBox.Show("文件格式错误");
                    return;
                }
            }
            try
            {
                xn = xn.SelectSingleNode("filters");
                XmlNodeList xnlNL = xn.SelectNodes("filter");
                foreach (XmlNode xnl in xnlNL)
                {
                    XmlElement xe = (XmlElement)xnl;
                    Filter item = new Filter();

                    item.Foreground = Brushes.Black;
                    item.Background = Brushes.White;

                    item.Is_enable = xe.GetAttribute("enabled").ToString().Equals("y");
                    item.Text = xe.GetAttribute("text").ToString();
                    if (xe.HasAttribute("foreColor"))
                    {
                        item.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#" + xe.GetAttribute("foreColor").ToString()));
                    }
                    if (xe.HasAttribute("backColor"))
                    {
                        item.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#" + xe.GetAttribute("backColor").ToString()));
                    }
                    item.Is_case_sensitive = xe.GetAttribute("case_sensitive").ToString().Equals("y");
                    item.Is_regex = xe.GetAttribute("regex").ToString().Equals("y");
                    filters.Add(item);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("过滤器文件损坏");
                return;
            }
        }

        public class Filter : INotifyPropertyChanged
        {
            private bool is_enable;
            public bool Is_enable 
            { 
                get 
                { 
                    return is_enable; 
                } 
                set 
                {
                    if (is_enable != value)
                    {
                        is_enable = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("Is_enable"));
                    }
                } 
            }
            private string text;
            public string Text 
            { 
                get
                {
                    return text;
                }
                set
                {
                    if (text != value)
                    {
                        text = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("Text"));
                    }
                }
            }
            private Brush foreground;
            public Brush Foreground 
            { 
                get
                {
                    return foreground;
                }
                set
                {
                    if (foreground != value)
                    {
                        foreground = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("Foreground"));
                    }
                }
            }
            private Brush background;
            public Brush Background 
            { 
                get
                {
                    return background;
                }
                set
                {
                    if (background != value)
                    {
                        background = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("Background"));
                    }
                }
            }
            private bool is_case_sensitive;
            public bool Is_case_sensitive 
            { 
                get
                {
                    return is_case_sensitive;
                }
                set
                {
                    if (is_case_sensitive != value)
                    {
                        is_case_sensitive = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("Is_case_sensitive"));
                    }
                }
            }
            private bool is_regex;
            public bool Is_regex 
            { 
                get
                {
                    return is_regex;
                }
                set
                {
                    if (is_regex != value)
                    {
                        is_regex = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("Is_regex"));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged = delegate { };
        }
    }
}
