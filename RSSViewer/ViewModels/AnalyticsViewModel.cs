﻿using Jasily.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.ViewModels
{
    public class AnalyticsViewModel : BaseViewModel
    {
        private SessionViewModel _targetViewModel;
        private int _selected;

        public AnalyticsViewModel(SessionViewModel rssViewViewModel) => this._targetViewModel = rssViewViewModel;

        private IReadOnlyCollection<RssItemViewModel> GetItems()
            => (IReadOnlyCollection<RssItemViewModel>)this._targetViewModel.Groups.FirstOrDefault()?.Items ?? Array.Empty<RssItemViewModel>();

        [ModelProperty]
        public int TotalCount => this.GetItems().Count;

        [ModelProperty]
        public int AcceptedInView => this.GetItems().Count(z => z.RssItem.State == RssItemState.Accepted);

        [ModelProperty]
        public int RejectedInView => this.GetItems().Count(z => z.RssItem.State == RssItemState.Rejected);

        public int Selected
        {
            get => this._selected; 
            set => this.ChangeModelProperty(ref this._selected, value); 
        }
    }
}
