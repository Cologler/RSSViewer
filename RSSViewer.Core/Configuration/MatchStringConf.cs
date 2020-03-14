using System.Text.RegularExpressions;

namespace RSSViewer.Configuration
{
    public class MatchStringConf
    {
        public MatchStringMode MatchMode { get; set; }

        public string MatchValue { get; set; }

        public int MatchOptions { get; set; }
    }
}
