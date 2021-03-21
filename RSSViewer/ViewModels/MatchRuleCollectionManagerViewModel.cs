using Accessibility;

using Microsoft.EntityFrameworkCore.Metadata.Internal;

using RSSViewer.Configuration;
using RSSViewer.RulesDb;
using RSSViewer.Services;

using SQLitePCL;

using System;
using System.Collections.Generic;
using System.Linq;
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
            var ruleViewModels = this.Items.ToArray();
            for (var i = 0; i < ruleViewModels.Length; i++)
            {
                var orderCode = i + 1;
                var viewModel = ruleViewModels[i];
                if (viewModel.MatchRule.OrderCode != orderCode)
                {
                    viewModel.MatchRule.OrderCode = orderCode;
                    viewModel.MarkChanged();
                }
            }

            await configService.UpdateMatchRulesAsync(
                this.Items.Where(z => !z.IsAdded && z.IsChanged).Select(z => z.MatchRule).ToArray(),
                this.Items.Where(z => z.IsAdded).Select(z => z.MatchRule).ToArray(),
                this._removedRules.Select(z => z.MatchRule).ToArray());
        }
    }
}
