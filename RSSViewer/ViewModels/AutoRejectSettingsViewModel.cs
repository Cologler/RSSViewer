using Accessibility;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RSSViewer.Configuration;
using RSSViewer.RulesDb;
using RSSViewer.Services;

using SQLitePCL;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace RSSViewer.ViewModels
{
    public class AutoRejectSettingsViewModel
    {
        public ObservableCollection<MatchRuleViewModel> Matches { get; } = new ObservableCollection<MatchRuleViewModel>();

        public async Task Load(ConfigService configService)
        {
            var rules = await configService.ListMatchRulesAsync().ConfigureAwait(false);
            this.Matches.Clear();
            rules.Select(z => new MatchRuleViewModel(z))
                .ToList()
                .ForEach(this.Matches.Add);
        }

        internal void Add(MatchRule conf)
        {
            this.Matches.Add(new MatchRuleViewModel(conf));
        }

        internal async void Save(ConfigService configService)
        {
            var rules = this.Matches.Select(z => z.MatchRule).ToArray();
            for (var i = 0; i < rules.Length; i++)
            {
                rules[i].OrderCode = i + 1;
            }
            await configService.ReplaceMatchRulesAsync(rules);
        }

        internal void MoveUp(IEnumerable<MatchRuleViewModel> items)
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

        internal void MoveDown(IEnumerable<MatchRuleViewModel> items)
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
