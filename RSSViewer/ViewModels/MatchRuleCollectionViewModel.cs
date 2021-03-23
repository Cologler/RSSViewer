
using Jasily.ViewModel;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.RssItemHandlers;
using RSSViewer.RulesDb;
using RSSViewer.Services;
using RSSViewer.ViewModels.Bases;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace RSSViewer.ViewModels
{
    public class MatchRuleCollectionViewModel : SelectableListViewModel<MatchRuleViewModel>
    {
        protected override async ValueTask LoadItemsAsync()
        {
            var serviceProvider = App.RSSViewerHost.ServiceProvider;
            this.ResetItemsFromTree(await serviceProvider.GetRequiredService<ConfigService>().ListMatchRulesAsync());
        }

        protected void ResetItemsFromTree(IList<MatchRule> rules)
        {
            if (rules is null)
                throw new ArgumentNullException(nameof(rules));

            var (tree, noParentItems) = rules.ToTree();

            var viewModels = tree
                .Where(z => !z.IsRoot)
                .Select(z => new MatchRuleViewModel(z.Item) { TreeLevel = z.Level-1 })
                .ToList();

            if (noParentItems.Count > 0)
            {
                viewModels.InsertRange(0, noParentItems.Select(z => new MatchRuleViewModel(z) { TreeLevel = 1 }).Prepend(MatchRuleViewModel.NoParent));
            }

            foreach (var item in viewModels)
            {
                item.RefreshDisplayPrefix();
            }

            // setup handlers
            var serviceProvider = App.RSSViewerHost.ServiceProvider;
            var handlers = serviceProvider.GetRequiredService<RssItemHandlersService>()
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

        /// <summary>
        /// call after a item was updated
        /// </summary>
        public virtual void OnUpdateItem(MatchRuleViewModel item)
        {
            if (item is null)
                throw new ArgumentNullException(nameof(item));

            var currentLevel = item.TreeLevel;

            var currentIndex = this.Items.IndexOf(item);
            if (currentIndex < 0)
                throw new NotImplementedException();

            int? GetCurrentParentId()
            {
                if (currentLevel == 0)
                    return null;
                var currentParentLevel = item.TreeLevel - 1;
                var currentParent = this.Items.Take(currentIndex).Where(z => z.TreeLevel == currentParentLevel).Last();
                return currentParent.MatchRule.Id;
            }

            if (GetCurrentParentId() != item.MatchRule.ParentId)
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

                if (item.MatchRule.ParentId is null)
                {
                    levelChanged = -currentLevel;
                    insertPos = this.Items.Count;
                }
                else
                {
                    var newParentIndex = this.Items
                        .Select((z, i) => z.MatchRule.Id == item.MatchRule.ParentId.Value ? i : -1)
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

        public HashSet<MatchRuleViewModel> Search(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new HashSet<MatchRuleViewModel>(this.Items);
            }
            else
            {
                var results = new HashSet<MatchRuleViewModel>();
                var dictById = this.Items.Where(z => !z.IsAdded).ToDictionary(z => z.MatchRule.Id);
                var direct = this.Items
                    .Where(z => z.MatchRule.Argument.Contains(text, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                foreach (var item in direct)
                {
                    var x = item;
                    while (x is not null && !results.Contains(x))
                    {
                        results.Add(x);
                        x = x.MatchRule.ParentId.HasValue ? dictById.GetValueOrDefault(x.MatchRule.ParentId.Value) : null;
                    }
                }
                return results;
            }
        }
    }
}
