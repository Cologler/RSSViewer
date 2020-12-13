﻿using Accessibility;
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
    public class AutoRulesViewModel
    {
        private readonly List<MatchRuleViewModel> _removedRules = new List<MatchRuleViewModel>();

        public ObservableCollection<MatchRuleViewModel> Rules { get; } = new ObservableCollection<MatchRuleViewModel>();

        public async Task Load(ConfigService configService)
        {
            var rules = await configService.ListMatchRulesAsync();
            this.Rules.Clear();
            rules.Select((z, i) => new MatchRuleViewModel(z, i))
                .ToList()
                .ForEach(this.Rules.Add);
        }

        internal void AddRule(MatchRule conf)
        {
            this.Rules.Add(new MatchRuleViewModel(conf, this.Rules.Count, true));
        }

        internal void RemoveRule(MatchRuleViewModel ruleViewModel)
        {
            if (ruleViewModel is null)
                throw new System.ArgumentNullException(nameof(ruleViewModel));
            this._removedRules.Add(ruleViewModel);
            this.Rules.Remove(ruleViewModel);
        }

        internal async void Save(ConfigService configService)
        {
            var ruleViewModels = this.Rules.ToArray();
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
                this.Rules.Where(z => !z.IsAdded && z.IsChanged).Select(z => z.MatchRule).ToArray(),
                this.Rules.Where(z => z.IsAdded).Select(z => z.MatchRule).ToArray(),
                this._removedRules.Select(z => z.MatchRule).ToArray());
        }

        internal void MoveUp(IEnumerable<MatchRuleViewModel> items)
        {
            var itemIndexes = items
                .Select(z => this.Rules.IndexOf(z))
                .ToHashSet();
            if (itemIndexes.Count == this.Rules.Count)
                return; // ignore move all

            var start = Enumerable.Range(0, this.Rules.Count)
                .Where(z => !itemIndexes.Contains(z))
                .First();

            // swap
            foreach (var i in itemIndexes.Where(z => z > start).OrderBy(z => z))
            {
                var ni = i - 1;
                (this.Rules[i].Index, this.Rules[ni].Index) = (this.Rules[ni].Index, this.Rules[i].Index);
                (this.Rules[i], this.Rules[ni]) = (this.Rules[ni], this.Rules[i]);
            }
        }

        internal void MoveDown(IEnumerable<MatchRuleViewModel> items)
        {
            var itemIndexes = items
                .Select(z => this.Rules.IndexOf(z))
                .ToHashSet();
            if (itemIndexes.Count == this.Rules.Count)
                return; // ignore move all

            var start = Enumerable.Range(0, this.Rules.Count)
                .Select(z => this.Rules.Count - z - 1)
                .Where(z => !itemIndexes.Contains(z))
                .First();

            // swap
            foreach (var i in itemIndexes.Where(z => z < start).OrderByDescending(z => z))
            {
                var ni = i + 1;
                (this.Rules[i].Index, this.Rules[ni].Index) = (this.Rules[ni].Index, this.Rules[i].Index);
                (this.Rules[i], this.Rules[ni]) = (this.Rules[ni], this.Rules[i]);
            }
        }
    }
}
