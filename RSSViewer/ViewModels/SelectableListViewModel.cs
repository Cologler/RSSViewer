using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Jasily.ViewModel;

namespace RSSViewer.ViewModels
{
    public abstract class SelectableListViewModel<T> : BaseViewModel
    {
        private T _selectedItem;

        public SelectableListViewModel()
        {
            this.Items = new(this.LoadItems() ?? Enumerable.Empty<T>());
            this.LoadItemsAsync();
        }

        public ObservableCollection<T> Items { get; }

        public T SelectedItem
        {
            get => _selectedItem;
            set
            {
                var oldValue = this._selectedItem;
                if (this.ChangeModelProperty(ref this._selectedItem, value))
                {
                    this.OnSelectedItemChanged(oldValue, value);
                }
            }
        }

        protected virtual IEnumerable<T> LoadItems() => Enumerable.Empty<T>();

        protected virtual ValueTask LoadItemsAsync() => ValueTask.CompletedTask;

        protected virtual void OnSelectedItemChanged(T oldValue, T newValue) { }

        public void SelectFirst()
        {
            this.SelectedItem = this.Items.FirstOrDefault();
        }
    }
}
