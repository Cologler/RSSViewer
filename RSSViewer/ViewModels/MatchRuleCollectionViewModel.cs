
using Jasily.ViewModel;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Services;
using RSSViewer.ViewModels.Bases;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

            this.ResetItems(sorted);
        }
    }
}
