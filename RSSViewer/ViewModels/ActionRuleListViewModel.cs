
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.RssItemHandlers;
using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.ViewModels.Bases;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace RSSViewer.ViewModels
{
    public class ActionRuleListViewModel : ItemsViewerViewModel<MatchRuleViewModel>
    {
        /// <summary>
        /// a helper method.
        /// </summary>
        /// <returns></returns>
        public async Task LoadActionRulesFromDbAsync()
        {
            var rules = await this.ServiceProvider.GetRequiredService<ConfigService>().GetActionRulesAsync();

            var tree = rules.BuildTree(out var noParentItems);

            var viewModels = tree
                .Where(z => !z.IsRoot)
                .Select(z => new MatchRuleViewModel(z.Item) { TreeLevel = z.Level-1 })
                .ToList();

            if (noParentItems.Count > 0)
            {
                viewModels.InsertRange(0, noParentItems
                    .Select(z => new MatchRuleViewModel(z) { TreeLevel = 1 })
                    .Prepend(MatchRuleViewModel.NoParent));
            }

            foreach (var item in viewModels)
            {
                item.RefreshDisplayPrefix();
            }

            // setup handlers
            var handlers = this.ServiceProvider.GetRequiredService<RssItemHandlersService>()
                .GetRuleTargetHandlers()
                .ToDictionary(z => z.Id);
            foreach (var item in viewModels)
            {
                item.Handler = handlers.GetValueOrDefault(item.MatchRule.HandlerId ?? KnownHandlerIds.DefaultHandlerId);
            }

            this.ResetItems(viewModels);
        }

        /// <summary>
        /// Get range for the item and childs of it.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        protected Range? GetRange(MatchRuleViewModel item)
        {
            var index = this.Items.IndexOf(item);
            if (index < 0)
                return default;
            var level = item.TreeLevel;
            var childs = this.Items.Skip(index + 1).TakeWhile(z => z.TreeLevel > level).ToList();
            return index..(index + childs.Count + 1);
        }

        public override void UpdateItem(MatchRuleViewModel item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            var handlerId = item.MatchRule.HandlerId ?? KnownHandlerIds.DefaultHandlerId;
            item.Handler = this.ServiceProvider.GetRequiredService<RssItemHandlersService>()
                .GetRuleTargetHandlers()
                .Where(z => z.Id == handlerId)
                .FirstOrDefault();

            var currentLevel = item.TreeLevel;

            var currentIndex = this.Items.IndexOf(item);
            if (currentIndex < 0)
                throw new NotImplementedException();

            MatchRule GetCurrentParent()
            {
                if (currentLevel == 0)
                    return null;
                var currentParentLevel = item.TreeLevel - 1;
                var currentParent = this.Items.Take(currentIndex).Where(z => z.TreeLevel == currentParentLevel).Last();
                return currentParent.MatchRule;
            }

            if (GetCurrentParent() != item.MatchRule.Parent)
            {
                int levelChanged;
                int insertPos;

                var childs = this.Items.Skip(currentIndex + 1).TakeWhile(z => z.TreeLevel > currentLevel).ToList();
                var itemsToMove = childs.Prepend(item).ToList();
                // pop
                for (var i = 0; i <= childs.Count; i++)
                {
                    this.Items.RemoveAt(currentIndex);
                }

                if (item.MatchRule.Parent is null)
                {
                    levelChanged = -currentLevel;
                    insertPos = this.Items.Count;
                }
                else
                {
                    var newParentIndex = this.Items
                        .Select((z, i) => z.MatchRule == item.MatchRule.Parent ? i : -1)
                        .Where(z => z >= 0)
                        .First();
                    var newParent = this.Items[newParentIndex];

                    levelChanged = newParent.TreeLevel + 1 - currentLevel;
                    insertPos = newParentIndex + 1;
                }

                foreach (var x in itemsToMove.AsEnumerable().Reverse())
                {
                    this.Items.Insert(insertPos, x);
                }

                foreach (var c in itemsToMove)
                {
                    c.TreeLevel += levelChanged;
                    Debug.Assert(c.TreeLevel >= 0);
                    c.RefreshDisplayPrefix();
                }
            }
        }

        public HashSet<MatchRuleViewModel> Find(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new HashSet<MatchRuleViewModel>(this.Items);
            }
            else
            {
                var results = new HashSet<MatchRuleViewModel>();
                var dictById = this.Items.Where(z => !z.IsAdded).ToDictionary(z => z.MatchRule);
                var direct = this.Items
                    .Where(z => z.MatchRule.Argument.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var item in direct)
                {
                    var x = item;
                    while (x is not null && !results.Contains(x))
                    {
                        results.Add(x);
                        x = x.MatchRule.Parent is not null ? dictById.GetValueOrDefault(x.MatchRule.Parent) : null;
                    }
                }
                return results;
            }
        }

        protected override Predicate<MatchRuleViewModel> GetFilter(string searchText)
        {
            var value = searchText;
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            else
            {
                var r = this.Find(value.Trim());
                return v => r.Contains(v);
            }
        }
    }
}
