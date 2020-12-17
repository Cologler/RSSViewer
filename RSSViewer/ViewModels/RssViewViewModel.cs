
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

        protected override IEnumerable<SessionViewModel> LoadItems()
        {
            return new[]
            {
                new SessionViewModel()
            };
        }
    }
}
