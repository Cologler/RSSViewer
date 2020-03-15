using Jasily.ViewModel;
using RSSViewer.Configuration;

namespace RSSViewer.ViewModels
{
    public class MatchStringConfViewModel : BaseViewModel
    {
        public MatchStringConf Conf { get; }

        public MatchStringConfViewModel(MatchStringConf matchStringConf)
        {
            this.Conf = matchStringConf;
        }

        [ModelProperty]
        public string DisplayValue
        {
            get
            {
                return $"({this.Conf.MatchMode.ToString()}) {this.Conf.MatchValue}";
            }
        }
    }
}
