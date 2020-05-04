using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.LocalDb;
using RSSViewer.StringMatchers;
using RSSViewer.Utils;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class AutoService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewerLogger _viewerLogger;
        private ImmutableArray<IStringMatcher> _stringMatchers;

        public AutoService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;
            var configService = this._serviceProvider.GetRequiredService<ConfigService>();
            this.Reload(configService.AppConf);
            configService.OnAppConfChanged += this.Reload;
        }

        private static bool IsEnable(MatchStringConf conf, DateTime now)
        {
            if (conf.ExpiredAt != null && conf.ExpiredAt.Value < now)
            {
                return false;
            }

            if (conf.DisableAt != null && conf.DisableAt.Value < now)
            {
                return false;
            }

            return true;
        }

        void Reload(AppConf conf)
        {
            var now = DateTime.UtcNow;
            var factory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();
            this._stringMatchers = conf.AutoReject.Matches
                .Where(z => IsEnable(z, now))
                .Select(z => factory.Create(z))
                .ToImmutableArray();
        }

        internal void AutoReject()
        {
            using (this._viewerLogger.EnterEvent("Auto reject"))
            {
                var stringMatchers = this._stringMatchers;
                if (stringMatchers.Length == 0)
                    return;

                var query = this._serviceProvider.GetRequiredService<RssItemsQueryService>();
                var operation = this._serviceProvider.GetRequiredService<RssItemsOperationService>();

                var items = query.List(new[] { RssItemState.Undecided });

                var shouldReject = items
                    .Where(i => stringMatchers.Any(z => z.IsMatch(i.Title)))
                    .ToArray();

                operation.ChangeState(shouldReject, RssItemState.Rejected);

                this._viewerLogger.AddLine($"Rejected {shouldReject.Length} items from {items.Length} undecided items.");
            }                
        }

        public Task AutoRejectAsync() => Task.Run(this.AutoReject);
    }
}
