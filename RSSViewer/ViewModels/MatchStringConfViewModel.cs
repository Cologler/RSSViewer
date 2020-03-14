using RSSViewer.Configuration;

namespace RSSViewer.ViewModels
{
    public class MatchStringConfViewModel
    {
        public MatchStringConf Conf { get; }

        public MatchStringConfViewModel(MatchStringConf matchStringConf)
        {
            this.Conf = matchStringConf;
        }

        public string DisplayValue
        {
            get
            {
                return $"({this.Conf.MatchMode.ToString()}) {this.Conf.MatchValue}";
            }
        }
    }
}
