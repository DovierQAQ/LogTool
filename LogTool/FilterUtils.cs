using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

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

        public class Filter
        {
            public bool Is_enable { get; set; }
            public string Text { get; set; }
            public Brush Foreground { get; set; }
            public Brush Background { get; set; }
            public bool Is_case_sensitive { get; set; }
            public bool Is_regex { get; set; }
        }
    }
}
