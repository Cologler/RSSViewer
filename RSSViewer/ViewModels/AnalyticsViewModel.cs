using Jasily.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.ViewModels
{
    public class AnalyticsViewModel : BaseViewModel
    {
        private RssViewViewModel _rssViewViewModel;

        public AnalyticsViewModel(RssViewViewModel rssViewViewModel) => this._rssViewViewModel = rssViewViewModel;

        private IReadOnlyCollection<RssItemViewModel> GetItems()
            => (IReadOnlyCollection<RssItemViewModel>) this._rssViewViewModel.Groups.FirstOrDefault()?.Items ?? Array.Empty<RssItemViewModel>();

        [ModelProperty]
        public int TotalCount => this.GetItems().Count;

        [ModelProperty]
        public int AcceptedInView => this.GetItems().Count(z => z.RssItem.State == RssItemState.Accepted);

        [ModelProperty]
        public int RejectedInView => this.GetItems().Count(z => z.RssItem.State == RssItemState.Rejected);
    }
}
