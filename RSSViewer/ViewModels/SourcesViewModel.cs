using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Services;
using RSSViewer.ViewModels.Bases;

namespace RSSViewer.ViewModels
{
    public class SourcesViewModel : SelectableListViewModel<SourcesViewModel.SourceViewModel>
    {
        public SourcesViewModel() : base()
        {
            this.SelectFirst();
        }

        protected override IEnumerable<SourceViewModel> LoadItems() 
        {
            var serviceProvider = App.RSSViewerHost.ServiceProvider;
            using var scope = serviceProvider.CreateScope();
            var sources = new List<SourceViewModel> { new SourceViewModel(null) };
            sources.AddRange(
                serviceProvider.GetRequiredService<RssItemsQueryService>()
                    .GetFeedIds()
                    .Select(z => new SourceViewModel(z)));
            return sources;
        }

        public class SourceViewModel
        {
            private string _feedId;

            public SourceViewModel(string feedId) => this._feedId = feedId;

            public string Name => this._feedId ?? "*";

            public string FeedId => this._feedId;
        }
    }
}
