﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace RSSViewer.ViewModels.Bases
{
    public class ListViewModel<T> : SelectorViewModel<T>
    {
        public ListViewModel()
        {
            this.Items = new(this.LoadItems() ?? Enumerable.Empty<T>());
            _ = this.InitializeAsync();
        }

        public ObservableCollection<T> Items { get; }

        protected virtual ValueTask InitializeAsync()
        {
            return this.LoadItemsAsync();
        }

        protected virtual IEnumerable<T> LoadItems() => Enumerable.Empty<T>();

        protected virtual ValueTask LoadItemsAsync() => ValueTask.CompletedTask;

        public virtual void ResetItems(IEnumerable<T> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            this.Items.Clear();
            foreach (var item in items)
            {
                this.Items.Add(item);
            }
        } 

        public override void SelectFirst()
        {
            this.SelectedItem = this.Items.FirstOrDefault();
        }
    }
}
