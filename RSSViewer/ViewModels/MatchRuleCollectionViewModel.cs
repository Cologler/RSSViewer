
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
                vm.DisplayPrefix = new string(' ', level * 2);
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

            this.UpdateDisplayPrefix(sorted);
            this.ResetItems(sorted);
        }

        /// <summary>
        /// call after a item was updated
        /// </summary>
        public void OnUpdateItem(MatchRuleViewModel item)
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
                item.DisplayPrefix = new string(' ', item.TreeLevel * 3);
                item.RefreshProperties();
            }
        }
    }
}
