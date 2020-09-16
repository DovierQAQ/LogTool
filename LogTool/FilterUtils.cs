using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            if (filter.Is_regex)
            {
                Match m = Regex.Match(target, filter_text);
                if (m.Success)
                {
                    return true;
                }
            }
            else if (!filter.Is_case_sensitive)
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
                    item.Background = null;

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

                    bool is_match = false;
                    foreach (var filter in filters)
                    {
                        if (filter.Is_case_sensitive == item.Is_case_sensitive && filter.Is_regex == item.Is_regex && filter.Text.Equals(item.Text))
                        {
                            is_match = true;
                            break;
                        }
                    }
                    if (!is_match)
                    {
                        filters.Add(item);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("过滤器文件损坏");
                return;
            }
        }

        static public void Filter_save(ref ObservableCollection<Filter> filters)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "选择过滤器文件位置";
            saveFileDialog.Filter = "过滤器文件|*.tat|所有文件|*.*";
            saveFileDialog.FileName = "filter";
            saveFileDialog.FilterIndex = 1;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.DefaultExt = "tat";
            if (saveFileDialog.ShowDialog() == false)
            {
                return;
            }

            string file_path = saveFileDialog.FileName;

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.AppendChild(xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement root = xmlDoc.CreateElement("GuoFanLogTool");
            root.SetAttribute("version", "1.0");
            xmlDoc.AppendChild(root);
            XmlElement filter_list = xmlDoc.CreateElement("filters");
            root.AppendChild(filter_list);

            foreach (var filter in filters)
            {
                XmlElement filter_xml = xmlDoc.CreateElement("filter");
                filter_xml.SetAttribute("enabled", filter.Is_enable ? "y" : "n");
                filter_xml.SetAttribute("case_sensitive", filter.Is_case_sensitive ? "y" : "n");
                filter_xml.SetAttribute("regex", filter.Is_regex ? "y" : "n");
                filter_xml.SetAttribute("foreColor", filter.Foreground.ToString().Replace("#", ""));
                filter_xml.SetAttribute("backColor", filter.Background.ToString().Replace("#", ""));
                filter_xml.SetAttribute("text", filter.Text);
                filter_list.AppendChild(filter_xml);
            }

            xmlDoc.Save(file_path);
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
            private string state;
            public string State
            {
                get
                {
                    return state;
                }
                set
                {
                    if (state != value)
                    {
                        state = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("State"));
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
                        refresh_state();
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
                        refresh_state();
                        PropertyChanged(this, new PropertyChangedEventArgs("Is_regex"));
                    }
                }
            }
            private int match_count;
            public int Match_count 
            { 
                get
                {
                    return match_count;
                }
                set
                {
                    if (match_count != value)
                    {
                        match_count = value;
                        PropertyChanged(this, new PropertyChangedEventArgs("Match_count"));
                    }
                }
            }

            private void refresh_state()
            {
                State = "";
                if (is_case_sensitive)
                {
                    State += "[Aa]";
                }
                if (is_regex)
                {
                    State += "[r]";
                }
            }

            public event PropertyChangedEventHandler PropertyChanged = delegate { };
        }
    }
}
