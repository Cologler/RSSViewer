using Accessibility;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

using RSSViewer.Configuration;
using RSSViewer.Helpers;
using RSSViewer.RssItemHandlers;
using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.Utils;

using SQLitePCL;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;

namespace RSSViewer.ViewModels
{
    public class MatchRuleCollectionManagerViewModel : MatchRuleCollectionViewModel
    {
        private readonly List<MatchRuleViewModel> _removedRules = new List<MatchRuleViewModel>();
        private string _searchText;

        public MatchRuleCollectionManagerViewModel()
        {
            this.RulesView = new ListCollectionView(this.Items);
        }

        public string SearchText
        {
            get => this._searchText;
            set
            {
                if (this.ChangeModelProperty(ref this._searchText, value))
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        this.RulesView.Filter = null;
                    }
                    else
                    {
                        var t = value.Trim();
                        this.RulesView.Filter = (v) => ((MatchRuleViewModel)v).MatchRule.Argument.Contains(t, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }

        public ListCollectionView RulesView { get; }

        internal void AddRule(MatchRule conf)
        {
            this.Items.Add(new MatchRuleViewModel(conf, true));
        }

        internal void RemoveRule(MatchRuleViewModel ruleViewModel)
        {
            if (ruleViewModel is null)
                throw new ArgumentNullException(nameof(ruleViewModel));
            this._removedRules.Add(ruleViewModel);
            this.Items.Remove(ruleViewModel);
        }

        internal async void Save(ConfigService configService)
        {
            await configService.UpdateMatchRulesAsync(
                this.Items.Where(z => !z.IsAdded && z.IsChanged).Select(z => z.MatchRule).ToArray(),
                this.Items.Where(z => z.IsAdded).Select(z => z.MatchRule).ToArray(),
                this._removedRules.Select(z => z.MatchRule).ToArray());
        }

        public void Combine(IList<MatchRuleViewModel> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));
            if (items.Count < 2)
                return;

            // check
            if (items.Select(z => z.MatchRule.ParentId).ToHashSet().Count > 1)
            {
                MessageBox.Show("Unable combine: some item did not has the same parent.");
                return;
            }
            if (items.Select(z => z.MatchRule.HandlerId ?? KnownHandlerIds.DefaultHandlerId).ToHashSet().Count > 1)
            {
                MessageBox.Show("Unable combine: some item did not has the same handler.");
                return;
            }
            if (items.Select(z => z.MatchRule.OnFeedId).ToHashSet().Count > 1)
            {
                MessageBox.Show("Unable combine: some item did not has the same target feed.");
                return;
            }


            var newValue = string.Join("|", 
                items.Select(z => RegexHelper.ConvertToRegexPattern(z.MatchRule.Mode, z.MatchRule.Argument)));
            if (!RegexUtils.IsValidPattern(newValue))
            {
                MessageBox.Show("Unable combine.");
                return;
            }

            DateTime theMaxTime = default;
            var totalMatched = 0;
            foreach (var item in items)
            {
                if (theMaxTime < item.MatchRule.LastMatched)
                    theMaxTime = item.MatchRule.LastMatched;
                totalMatched += item.MatchRule.TotalMatchedCount;
            }

            var target = items[0];
            target.MatchRule.Mode = MatchMode.Regex;
            target.MatchRule.Argument = newValue;
            target.MatchRule.LastMatched = theMaxTime;
            target.MatchRule.TotalMatchedCount = totalMatched;
            target.MarkChanged();
            this.OnUpdateItem(target);
            target.RefreshProperties();

            foreach (var item in items.Where(z => z != target))
            {
                this.RemoveRule(item);
            }
        }
    }
}
