﻿using System;
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
                    var filter = this.GetFilter(value);
                    this.ItemsView.Filter = filter is null ? null : o => filter((T)o);
                }
            }
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
    }
}
