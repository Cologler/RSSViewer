using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.LocalDb;
using RSSViewer.RulesDb;
using RSSViewer.StringMatchers;
using RSSViewer.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class AutoService
    {
        private readonly object _syncRoot = new object();
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewerLogger _viewerLogger;
        private ImmutableArray<IStringMatcher> _stringMatchers;

        public AutoService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;

            var configService = this._serviceProvider.GetRequiredService<ConfigService>();
            configService.MatchRulesChanged += this.ConfigService_MatchRulesChanged;
            this.OnUpdated(configService.ListMatchRules());
        }

        private void ConfigService_MatchRulesChanged(object sender, CollectionChangeEventArgs e)
        {
            switch (e.Action)
            {
                case CollectionChangeAction.Add:
                    this.OnAdded(e.Element as MatchRule);
                    break;

                case CollectionChangeAction.Remove:
                    break;

                case CollectionChangeAction.Refresh:
                    this.OnUpdated(e.Element as IEnumerable<MatchRule>);
                    break;

                default:
                    break;
            }
        }

        private void OnAdded(MatchRule rule)
        {
            if (rule is null) return;
            if (rule.Action != MatchAction.Reject) return;

            var factory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();

            var matcher = factory.Create(rule);

            lock (this._syncRoot)
            {
                this._stringMatchers = this._stringMatchers.Add(matcher);
            }
        }

        private void OnUpdated(IEnumerable<MatchRule> rules)
        {
            if (rules is null)
                return;

            var factory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();
            var matchers = rules.Where(z => z.Action == MatchAction.Reject)
                .Select(z => factory.Create(z))
                .ToArray();

            lock (this._syncRoot)
            {
                this._stringMatchers = matchers.ToImmutableArray();
            }
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
