using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace RSSViewer.ViewModels.Bases
{
    public class ItemsViewModel<T> : SelectorViewModel<T>
    {
        public ObservableCollection<T> Items { get; } = new();

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
