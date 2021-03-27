using System.Linq;

using Jasily.ViewModel;

namespace RSSViewer.ViewModels.Bases
{
    public abstract class SelectorViewModel<T> : BaseViewModel
    {
        private T _selectedItem;

        public SelectorViewModel()
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

        public virtual void SelectFirst() { }
    }
}
