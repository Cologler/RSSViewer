using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.ViewModels.Bases;

namespace RSSViewer.ViewModels
{
    public class MatchRuleListManagerViewModel : BaseViewModel
    {
        private readonly List<MatchRuleViewModel> _removedRules = new();

        public ActionRuleListManagerViewModel ActionRulesViewModel { get; } = new();

        public ListViewModel<MatchRuleViewModel> SetTagRulesViewModel { get; } = new();

        /// <summary>
        /// the new tags map by tag name.
        /// </summary>
        public Dictionary<string, Tag> NewTags = new();

        public void Load()
        {
            using var scope = this.ServiceProvider.CreateScope();
            using var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

            var tags = ctx.Tags.AsQueryable()
                .AsNoTracking()
                .ToDictionary(z => z.Id);

            var matchRules = ctx.MatchRules.AsQueryable()
                .AsNoTracking()
                .ToList();

            var setTagRules = matchRules.Where(z => z.HandlerType == HandlerType.SetTag)
                .Select(z => new MatchRuleViewModel(z, tags.GetValueOrDefault(z.HandlerId)))
                .ToList();

            this.SetTagRulesViewModel.ResetItems(setTagRules);
        }

        public async void Save()
        {
            if (this.NewTags.Count > 0)
            {
                using (var scope = this.ServiceProvider.CreateScope())
                {
                    using (var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>())
                    {
                        ctx.Tags.AddRange(this.NewTags.Values);
                        ctx.SaveChanges();
                    }              
                }
            }

            var updated = this.SetTagRulesViewModel.Items
                .Where(z => !z.IsAdded && z.IsChanged).Select(z => z.MatchRule).ToArray();

            var added = this.SetTagRulesViewModel.Items
                .Where(z => z.IsAdded).Select(z => z.MatchRule).ToArray();

            var removed = this._removedRules.Select(z => z.MatchRule).ToArray();

            await this.ServiceProvider.GetRequiredService<ConfigService>().UpdateMatchRulesAsync(updated, added, removed);

            this.ActionRulesViewModel.Save();
        }

        public void OnUpdateItem(MatchRuleViewModel viewModel)
        {
            if (viewModel.MatchRule.HandlerType == HandlerType.Action)
            {
                this.ActionRulesViewModel.OnUpdateItem(viewModel);
            }
        }

        public void AddRule(MatchRuleViewModel viewModel)
        {
            Debug.Assert(viewModel.IsAdded);

            if (viewModel.MatchRule.HandlerType == HandlerType.Action)
            {
                this.ActionRulesViewModel.AddRule(viewModel);
            }
            else
            {
                this.SetTagRulesViewModel.Items.Add(viewModel);
            }
        }

        public void RemoveRule(MatchRuleViewModel viewModel)
        {
            if (viewModel.MatchRule.HandlerType == HandlerType.Action)
            {
                this.ActionRulesViewModel.RemoveRule(viewModel);
            }
            else
            {
                this._removedRules.Add(viewModel);
                this.SetTagRulesViewModel.Items.Remove(viewModel);
            } 
        }
    }
}
