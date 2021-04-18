using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using Jasily.ViewModel;

namespace RSSViewer.ViewModels.Bases
{
    public class ItemsViewModel<T> : SelectorViewModel<T>
    {
        public ObservableCollection<T> Items { get; } = new();

        /// <summary>
        /// call when a item need to update.
        /// </summary>
        public virtual void UpdateItem(T item) 
        {
            
        }

        /// <summary>
        /// call when a item need to remove.
        /// </summary>
        public virtual void RemoveItem(T item)
        {
            this.OnRemoveItem(item);
        }

        /// <summary>
        /// call when some items need to remove.
        /// </summary>
        public virtual void RemoveItems(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                this.OnRemoveItem(item);
            }
        }

        protected virtual void OnRemoveItem(T item)
        {
            this.Items.Remove(item);
        }

        public virtual void AddItems(IEnumerable<T> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            foreach (var item in items)
            {
                this.Items.Add(item);
            }
        }

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
