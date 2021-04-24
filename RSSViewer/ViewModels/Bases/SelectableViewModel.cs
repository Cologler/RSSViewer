using System;

namespace RSSViewer.ViewModels.Bases
{
    public class SelectableViewModel<T> : BaseViewModel where T : BaseViewModel
    {
        private bool _isSelected;

        public SelectableViewModel(T viewModel)
        {
            this.InnerModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public T InnerModel { get; }

        public bool IsSelected { get => _isSelected; set => this.ChangeModelProperty(ref _isSelected, value); }

        public override string ToString() => this.InnerModel.ToString();
    }
}
