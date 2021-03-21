
using System;
using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.ViewModels
{
    public class MatchRuleParentSelectorViewModel : MatchRuleCollectionViewModel
    {
        public override void ResetItems(IEnumerable<MatchRuleViewModel> viewModels)
        {
            if (viewModels is null)
                throw new ArgumentNullException(nameof(viewModels));

            base.ResetItems(viewModels.Prepend(MatchRuleViewModel.None));
        }
    }
}
