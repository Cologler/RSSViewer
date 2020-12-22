using System.Linq;

namespace RSSViewer.ViewModels.Bases
{
    public abstract class SelectableListViewModel<T> : ListViewModel<T>
    {
        private T _selectedItem;

        public SelectableListViewModel()
        {
        }

        public virtual T SelectedItem
        {
            get => _selectedItem;
            set
            {
                var oldValue = this._selectedItem;
                if (this.ChangeModelProperty(ref this._selectedItem, value))
                    this.OnSelectedItemChanged(oldValue, value);
            }
        }

        protected virtual void OnSelectedItemChanged(T oldValue, T newValue) { }

        public void SelectFirst()
        {
            this.SelectedItem = this.Items.FirstOrDefault();
        }
    }
}
