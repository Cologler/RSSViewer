using Jasily.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.ViewModels
{
    public class AnalyticsViewModel : BaseViewModel
    {
        private int _selected;

        [ModelProperty]
        public int TotalCount { get; private set; }

        [ModelProperty]
        public int AcceptedCount { get; private set; }

        [ModelProperty]
        public int RejectedCount { get; private set; }

        public int SelectedCount
        {
            get => this._selected; 
            set => this.ChangeModelProperty(ref this._selected, value); 
        }

        public void RefreshPropertiesFrom(SessionViewModel sessionViewModel)
        {
            if (sessionViewModel is null)
                throw new ArgumentNullException(nameof(sessionViewModel));

            var items = (IReadOnlyCollection<RssItemViewModel>) sessionViewModel.Groups.FirstOrDefault()?.Items 
                ?? Array.Empty<RssItemViewModel>();

            this.TotalCount = items.Count;
            this.AcceptedCount = items.Count(z => z.RssItem.State == RssItemState.Accepted);
            this.RejectedCount = items.Count(z => z.RssItem.State == RssItemState.Rejected);

            base.RefreshProperties();
        }
    }
}
