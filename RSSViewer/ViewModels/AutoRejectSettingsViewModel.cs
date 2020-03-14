using RSSViewer.Configuration;
using System.Collections.ObjectModel;
using System.Linq;

namespace RSSViewer.ViewModels
{
    public class AutoRejectSettingsViewModel
    {
        public ObservableCollection<MatchStringConfViewModel> Matches { get; } = new ObservableCollection<MatchStringConfViewModel>();

        public void Load(AppConf conf)
        {
            this.Matches.Clear();
            conf.AutoReject.Matches
                .Select(z => new MatchStringConfViewModel(z))
                .ToList()
                .ForEach(this.Matches.Add);
        }

        internal void Add(MatchStringConf conf)
        {
            this.Matches.Add(new MatchStringConfViewModel(conf));
        }

        internal void Save(AppConf conf)
        {
            conf.AutoReject.Matches = this.Matches.Select(z => z.Conf).ToList();
        }
    }
}
