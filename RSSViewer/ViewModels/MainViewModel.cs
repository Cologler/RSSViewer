
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.ViewModels.Bases;

using System.Collections.Generic;
using System.Windows.Data;

namespace RSSViewer.ViewModels
{
    public class MainViewModel : ItemsViewerViewModel<SessionViewModel>
    {
        public MainViewModel()
        {
            this.LoggerMessage = this.ServiceProvider.GetRequiredService<ViewerLoggerViewModel>();

            this.ItemsView.NewItemPlaceholderPosition = System.ComponentModel.NewItemPlaceholderPosition.AtEnd;

            this.Items.Add(this.CreateSession(false));
            this.SelectFirst();
        }

        public ViewerLoggerViewModel LoggerMessage { get; }

        public AnalyticsViewModel AnalyticsView { get; } = new();

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
