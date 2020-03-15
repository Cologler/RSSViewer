using Accessibility;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RSSViewer.Configuration;
using SQLitePCL;
using System.Collections.Generic;
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

        internal void MoveUp(IEnumerable<MatchStringConfViewModel> items)
        {
            var itemIndexes = items
                .Select(z => this.Matches.IndexOf(z))
                .ToHashSet();
            if (itemIndexes.Count == this.Matches.Count)
                return; // ignore move all

            var start = Enumerable.Range(0, this.Matches.Count)
                .Where(z => !itemIndexes.Contains(z))
                .First();

            // swap
            foreach (var i in itemIndexes.Where(z => z > start).OrderBy(z => z))
            {
                var ni = i - 1;
                (this.Matches[i], this.Matches[ni]) = (this.Matches[ni], this.Matches[i]);
            }
        }

        internal void MoveDown(IEnumerable<MatchStringConfViewModel> items)
        {
            var itemIndexes = items
                .Select(z => this.Matches.IndexOf(z))
                .ToHashSet();
            if (itemIndexes.Count == this.Matches.Count)
                return; // ignore move all

            var start = Enumerable.Range(0, this.Matches.Count)
                .Select(z => this.Matches.Count - z - 1)
                .Where(z => !itemIndexes.Contains(z))
                .First();

            // swap
            foreach (var i in itemIndexes.Where(z => z < start).OrderByDescending(z => z))
            {
                var ni = i + 1;
                (this.Matches[i], this.Matches[ni]) = (this.Matches[ni], this.Matches[i]);
            }
        }
    }
}
