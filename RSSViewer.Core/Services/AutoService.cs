using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Configuration;
using RSSViewer.LocalDb;
using RSSViewer.StringMatchers;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class AutoService
    {
        private readonly object _syncRoot = new object();
        private readonly IServiceProvider _serviceProvider;
        private ImmutableArray<IStringMatcher> _stringMatchers;

        public AutoService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            var configService = this._serviceProvider.GetRequiredService<ConfigService>();
            this.Reload(configService.AppConf);
            configService.OnAppConfChanged += this.Reload;
        }

        private static bool IsEnable(MatchStringConf conf, DateTime now)
        {
            if (conf.ExpiredAt != null && conf.ExpiredAt.DateTime > now)
            {
                return false;
            }

            if (conf.DisableAt != null && conf.DisableAt.DateTime > now)
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
        }

        public Task AutoRejectAsync() => Task.Run(this.AutoReject);
    }
}
