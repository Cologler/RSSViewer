using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
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

        public MatchRuleListManagerViewModel()
        {
            this.SetTagRulesViewModel.ItemsView.SortDescriptions.Add(
                new(nameof(MatchRuleViewModel.DisplayValue), ListSortDirection.Ascending));
        }

        public ActionRuleListManagerViewModel ActionRulesViewModel { get; } = new();

        public ItemsViewerViewModel<MatchRuleViewModel> SetTagRulesViewModel { get; } = new();

        /// <summary>
        /// map by id.
        /// </summary>
        public Dictionary<string, TagViewModel> TagsViewModel { get; } = new();

        public void Load()
        {
            using var scope = this.ServiceProvider.CreateScope();
            using var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

            var tags = ctx.Tags.AsQueryable()
                .AsNoTracking()
                .ToDictionary(z => z.Id);
            this.TagsViewModel.Clear();
            foreach (var kvp in tags)
            {
                this.TagsViewModel.Add(kvp.Key, new TagViewModel(kvp.Value));
            }

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
            var tags = this.TagsViewModel.Select(z => z.Value).ToList();
            if (tags.Where(z => z.IsChanged || z.IsAdded).Any())
            {
                using (var scope = this.ServiceProvider.CreateScope())
                {
                    using (var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>())
                    {
                        ctx.Tags.AddRange(tags.Where(z => z.IsAdded).Select(z => z.Tag));
                        ctx.Tags.UpdateRange(tags.Where(z => !z.IsAdded && z.IsChanged).Select(z => z.Tag));
                        ctx.SaveChanges();
                    }              
                }
            }

            var updated = Enumerable.Empty<MatchRuleViewModel>()
                .Concat(this.ActionRulesViewModel.Items)
                .Concat(this.SetTagRulesViewModel.Items)
                .Where(z => !z.IsAdded && z.IsChanged).Select(z => z.MatchRule).ToArray();

            var added = Enumerable.Empty<MatchRuleViewModel>()
                .Concat(this.ActionRulesViewModel.Items)
                .Concat(this.SetTagRulesViewModel.Items)
                .Where(z => z.IsAdded).Select(z => z.MatchRule).ToArray();

            var removed = Enumerable.Empty<MatchRuleViewModel>()
                .Concat(this._removedRules)
                .Concat(this.ActionRulesViewModel.RemovedRules)
                .Select(z => z.MatchRule).ToArray();

            await this.ServiceProvider.GetRequiredService<ConfigService>().UpdateMatchRulesAsync(updated, added, removed);
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
