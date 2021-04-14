using System.Windows.Data;

namespace RSSViewer.ViewModels.Bases
{
    public class ItemsViewerViewModel<T> : ItemsViewModel<T>
    {
        public ItemsViewerViewModel()
        {
            this.ItemsView = new(this.Items);
        }

        public ListCollectionView ItemsView { get; }
    }
}
