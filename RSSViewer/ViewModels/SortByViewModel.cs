using Jasily.ViewModel;

using System;

namespace RSSViewer.ViewModels
{
    public class SortByViewModel : BaseViewModel
    {
        private SortBy _sortBy = SortBy.Title;

        public SortBy SortBy 
        { 
            get => this._sortBy;
            set => this.ChangeModelProperty(ref this._sortBy, value);
        }

        public SortBy[] SortByOptions { get; } = (SortBy[])Enum.GetValues(typeof(SortBy));
    }
}
