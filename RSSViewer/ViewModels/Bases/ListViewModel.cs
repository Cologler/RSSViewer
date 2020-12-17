using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Jasily.ViewModel;

namespace RSSViewer.ViewModels.Bases
{
    public abstract class ListViewModel<T> : BaseViewModel
    {
        public ListViewModel()
        {
            this.Items = new(this.LoadItems() ?? Enumerable.Empty<T>());
            this.InitializeAsync();
        }

        public ObservableCollection<T> Items { get; }

        protected virtual ValueTask InitializeAsync()
        {
            return this.LoadItemsAsync();
        }

        protected virtual IEnumerable<T> LoadItems() => Enumerable.Empty<T>();

        protected virtual ValueTask LoadItemsAsync() => ValueTask.CompletedTask;
    }
}
