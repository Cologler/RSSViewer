using System;
using System.Collections.Generic;
using System.Windows.Data;

namespace RSSViewer.ViewModels.Bases
{
    public class ItemsViewerViewModel<T> : ItemsViewModel<T>
    {
        private string _searchText;

        public ItemsViewerViewModel()
        {
            this.ItemsView = new(this.Items);
        }

        public ListCollectionView ItemsView { get; }

        public string SearchText
        {
            get => this._searchText;
            set
            {
                if (this.ChangeModelProperty(ref this._searchText, value))
                {
                    this.UpdateItemsViewFilter();
                }
            }
        }

        public virtual void UpdateItemsViewFilter()
        {
            var filter = this.GetFilter(this.SearchText);
            this.ItemsView.Filter = filter is null ? null : o => filter((T)o);
        }

        protected virtual Predicate<T> GetFilter(string searchText) => null;

        public void ResetItems(IEnumerable<T> items, bool deferRefresh)
        {
            if (!deferRefresh)
            {
                this.ResetItems(items);
                return;
            }

            using var _ = this.ItemsView.DeferRefresh();
            this.ResetItems(items);
        }

        public override void RemoveItem(T item)
        {
            base.RemoveItem(item);
            this.UpdateItemsViewFilter();
        }

        public override void RemoveItems(IEnumerable<T> items)
        {
            base.RemoveItems(items);
            this.UpdateItemsViewFilter();
        }
    }
}
