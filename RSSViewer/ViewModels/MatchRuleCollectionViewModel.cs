
using Jasily.ViewModel;

using Microsoft.Extensions.DependencyInjection;

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
            var configService = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>();
            var rules = await configService.ListMatchRulesAsync();
            var viewModels = rules.Select(z => new MatchRuleViewModel(z)).ToList();

            var lookup = viewModels.ToLookup(z => z.MatchRule.ParentId);

            var sorted = new List<MatchRuleViewModel>();
            void Walk(MatchRuleViewModel vm, int level)
            {
                vm.TreeLevel = level;
                sorted.Add(vm);
                foreach (var c in lookup[vm.MatchRule.Id])
                {
                    Walk(c, level+1);
                }
            }
            foreach (var item in viewModels)
            {
                if (item.MatchRule.IsRootRule())
                {
                    Walk(item, 0);
                }
            }

            var noParentItems = viewModels.Except(sorted).ToList();
            if (noParentItems.Count > 0)
            {
                foreach (var item in noParentItems)
                {
                    item.TreeLevel = 1;
                }
                sorted.InsertRange(0, noParentItems.Prepend(MatchRuleViewModel.NoParent));
            }

            this.UpdateDisplayPrefix(viewModels);
            this.ResetItems(sorted);
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
                }

                this.UpdateDisplayPrefix(itemsToMove);
            }
        }

        private void UpdateDisplayPrefix(IList<MatchRuleViewModel> viewModels)
        {
            foreach (var item in viewModels)
            {
                if (item.TreeLevel > 0)
                {
                    // char copy from https://en.wikipedia.org/wiki/Box-drawing_character
                    item.DisplayPrefix = new string(' ', item.TreeLevel) + "├ ";
                    item.RefreshProperties();
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
