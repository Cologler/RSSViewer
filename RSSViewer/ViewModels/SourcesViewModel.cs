using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Threading;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Services;
using RSSViewer.Utils;
using RSSViewer.ViewModels.Bases;

namespace RSSViewer.ViewModels
{
    public class SourcesViewModel : ListViewModel<SourcesViewModel.SourceViewModel>, IDisposable
    {
        private readonly bool _watchChanges;

        public SourcesViewModel(bool watchChanges = true) : base()
        {
            this.SelectFirst();
            this._watchChanges = watchChanges;
        }

        public void Dispose()
        {
            if (this._watchChanges)
            {
                App.RSSViewerHost.ServiceProvider.RemoveListener(EventNames.AddedRssItems, this.OnAddedRssItems);
            }
        }

        protected override IEnumerable<SourceViewModel> LoadItems() 
        {
            var serviceProvider = App.RSSViewerHost.ServiceProvider;
            if (this._watchChanges)
            {
                serviceProvider.AddListener(EventNames.AddedRssItems, this.OnAddedRssItems);
            }
            using var scope = serviceProvider.CreateScope();
            var sources = new List<SourceViewModel> { new SourceViewModel(null) };
            sources.AddRange(
                serviceProvider.GetRequiredService<RssItemsQueryService>()
                    .GetFeedIds()
                    .Select(z => new SourceViewModel(z)));
            return sources;
        }

        private void OnAddedRssItems(object sender, IReadOnlyCollection<IPartialRssItem> e)
        {
            var feedIds = e.Select(z => z.FeedId).Distinct().ToArray();
            System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
            {
                foreach (var item in feedIds.Except(this.Items.Select(z => z.FeedId)).ToArray())
                {
                    this.Items.Add(new SourceViewModel(item));
                }
            });
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
