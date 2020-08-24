using Jasily.ViewModel;
using RSSViewer.LocalDb;
using System.Windows;

namespace RSSViewer.ViewModels
{
    public class RssItemViewModel : BaseViewModel, IRssItemsCount
    {
        public RssItem RssItem { get; }

        public RssItemViewModel(RssItem rssItem) : base()
        {
            this.RssItem = rssItem;
        }

        [ModelProperty]
        public Visibility AcceptedImageVisibility => this.RssItem.State == RssItemState.Accepted
            ? Visibility.Visible
            : Visibility.Collapsed;

        [ModelProperty]
        public Visibility RejectedImageVisibility => this.RssItem.State == RssItemState.Rejected
            ? Visibility.Visible
            : Visibility.Collapsed;

        [ModelProperty]
        public string Title
        {
            get
            {
                if (this.RssItem.FeedId is null)
                    return this.RssItem.Title;
                return $"{this.RssItem.Title} ({this.RssItem.FeedId})";
            }
        }

        int IRssItemsCount.Count => 1;
    }
}
