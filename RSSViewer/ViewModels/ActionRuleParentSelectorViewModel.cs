
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RSSViewer.ViewModels
{
    public class ActionRuleParentSelectorViewModel : ActionRuleListViewModel
    {
        private readonly TaskCompletionSource _readyEvent = new();

        public Task Ready => this._readyEvent.Task;

        public override void ResetItems(IEnumerable<MatchRuleViewModel> viewModels)
        {
            if (viewModels is null)
                throw new ArgumentNullException(nameof(viewModels));

            base.ResetItems(viewModels.Prepend(MatchRuleViewModel.None));
            this._readyEvent.TrySetResult();
        }
    }
}
