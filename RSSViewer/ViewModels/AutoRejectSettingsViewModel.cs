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
        private readonly List<MatchRuleViewModel> _removedRules = new List<MatchRuleViewModel>();

        public ObservableCollection<MatchRuleViewModel> Matches { get; } = new ObservableCollection<MatchRuleViewModel>();

        public async Task Load(ConfigService configService)
        {
            var rules = await configService.ListMatchRulesAsync();
            this.Matches.Clear();
            rules.Select(z => new MatchRuleViewModel(z))
                .ToList()
                .ForEach(this.Matches.Add);
        }

        internal void AddRule(MatchRule conf)
        {
            this.Matches.Add(new MatchRuleViewModel(conf, true));
        }

        internal void RemoveRule(MatchRuleViewModel ruleViewModel)
        {
            if (ruleViewModel is null)
                throw new System.ArgumentNullException(nameof(ruleViewModel));
            this._removedRules.Add(ruleViewModel);
            this.Matches.Remove(ruleViewModel);
        }

        internal async void Save(ConfigService configService)
        {
            var ruleViewModels = this.Matches.ToArray();
            for (var i = 0; i < ruleViewModels.Length; i++)
            {
                var orderCode = i + 1;
                var viewModel = ruleViewModels[i];
                if (viewModel.MatchRule.OrderCode != orderCode)
                {
                    viewModel.MatchRule.OrderCode = i + 1;
                    viewModel.MarkChanged();
                }                
            }

            await configService.UpdateMatchRulesAsync(
                this.Matches.Where(z => !z.IsAdded && z.IsChanged).Select(z => z.MatchRule).ToArray(),
                this.Matches.Where(z => z.IsAdded).Select(z => z.MatchRule).ToArray(),
                this._removedRules.Select(z => z.MatchRule).ToArray());
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
