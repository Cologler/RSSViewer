
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.ViewModels.Bases;

using System.Collections.Generic;
using System.Windows.Data;

namespace RSSViewer.ViewModels
{
    public class RssViewViewModel : ListViewModel<SessionViewModel>
    {
        public RssViewViewModel()
        {
            var serviceProvider = App.RSSViewerHost.ServiceProvider;

            this.LoggerMessage = serviceProvider.GetRequiredService<ViewerLoggerViewModel>();

            this.ItemsView = new ListCollectionView(this.Items);
            this.ItemsView.NewItemPlaceholderPosition = System.ComponentModel.NewItemPlaceholderPosition.AtEnd;
        }

        public ViewerLoggerViewModel LoggerMessage { get; }

        public AnalyticsViewModel AnalyticsView { get; } = new();

        public ListCollectionView ItemsView { get; }

        public override SessionViewModel SelectedItem
        {
            get => base.SelectedItem;
            set
            {
                if (this.ItemsView.IsAddingNew)
                    return;

                if (value is null) // add new item
                {
                    var session = this.CreateSession(true);
                    this.ItemsView.AddNewItem(session);
                    this.ItemsView.CommitNew();
                    value = session;
                    _ = session.RefreshContentAsync(10);
                }
                base.SelectedItem = value;
            }
        }

        protected override IEnumerable<SessionViewModel> LoadItems()
        {
            return new[]
            {
                this.CreateSession(false)
            };
        }

        private SessionViewModel CreateSession(bool removable)
        {
            var session = new SessionViewModel();
            session.Removable = removable;
            session.SessionStateChanged += this.Session_SessionStateChanged;
            return session;
        }

        private void Session_SessionStateChanged(object sender, System.EventArgs e)
        {
            var session = (SessionViewModel)sender;
            if (session == this.SelectedItem)
            {
                this.AnalyticsView.RefreshPropertiesFrom(session);
            }
        }
    }
}
