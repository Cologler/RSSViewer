
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.AcceptHandlers;
using RSSViewer.RssItemHelper;
using RSSViewer.ViewModels.Bases;

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Text;
using System.Windows;

namespace RSSViewer.ViewModels
{
    public class RssViewViewModel : SelectableListViewModel<SessionViewModel>
    {
        public RssViewViewModel()
        {
            var serviceProvider = App.RSSViewerHost.ServiceProvider;

            this.LoggerMessage = serviceProvider.GetRequiredService<ViewerLoggerViewModel>();
        }

        public ViewerLoggerViewModel LoggerMessage { get; }

        public AnalyticsViewModel AnalyticsView { get; } = new();

        protected override IEnumerable<SessionViewModel> LoadItems()
        {
            return new[]
            {
                this.CreateSession()
            };
        }

        private SessionViewModel CreateSession()
        {
            var session = new SessionViewModel();
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
