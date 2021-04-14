using Jasily.ViewModel;

using RSSViewer.Abstractions;

using System.Windows;

namespace RSSViewer.ViewModels
{
    public class RssItemViewModel : BaseViewModel, IRssItemsCount
    {
        public IPartialRssItem RssItem { get; }

        public RssItemViewModel(IPartialRssItem rssItem) : base()
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
        public Visibility ArchivedImageVisibility => this.RssItem.State == RssItemState.Archived
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

        public class ClassifyContext : Models.ClassifyContext<IPartialRssItem>
        {
            public ClassifyContext(IPartialRssItem item) : base(item)
            {
                this.ViewModel = new(item);
            }

            public RssItemViewModel ViewModel { get; }
        }
    }
}
