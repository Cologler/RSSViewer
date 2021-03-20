using Accessibility;

using Jasily.ViewModel;

using Microsoft.EntityFrameworkCore.Metadata.Internal;
using RSSViewer.Configuration;
using RSSViewer.RulesDb;
using RSSViewer.Services;

using SQLitePCL;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RSSViewer.ViewModels
{
    public class AutoRulesViewModel : BaseViewModel
    {
        private readonly List<MatchRuleViewModel> _removedRules = new List<MatchRuleViewModel>();
        private string _searchText;

        public AutoRulesViewModel()
        {
            this.RulesView = new ListCollectionView(this.Rules);
        }

        public ObservableCollection<MatchRuleViewModel> Rules { get; } = new ObservableCollection<MatchRuleViewModel>();

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
                throw new ArgumentNullException(nameof(ruleViewModel));
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
                    viewModel.MatchRule.OrderCode = orderCode;
                    viewModel.MarkChanged();
                }
            }

            await configService.UpdateMatchRulesAsync(
                this.Rules.Where(z => !z.IsAdded && z.IsChanged).Select(z => z.MatchRule).ToArray(),
                this.Rules.Where(z => z.IsAdded).Select(z => z.MatchRule).ToArray(),
                this._removedRules.Select(z => z.MatchRule).ToArray());
        }
    }
}
