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
        private ImmutableArray<MatchRuleStateDecider> _matchRuleStateDeciders;

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
            var decider = new MatchRuleStateDecider(rule, matcher);

            lock (this._syncRoot)
            {
                this._matchRuleStateDeciders = this._matchRuleStateDeciders.Add(decider); 
            }
        }

        private void OnUpdated(IEnumerable<MatchRule> rules)
        {
            if (rules is null)
                return;

            var factory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();
            var matchers = rules.Where(z => z.Action == MatchAction.Reject)
                .Select(z => (z, factory.Create(z)))
                .ToArray();
            var deciders = rules
                .Select(z => new MatchRuleStateDecider(z, factory.Create(z)))
                .ToImmutableArray();

            lock (this._syncRoot)
            {
                this._matchRuleStateDeciders = deciders;
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
                var deciders = this._matchRuleStateDeciders;
                if (deciders.Length == 0)
                    return;

                var state = new Dictionary<int, int>();

                var query = this._serviceProvider.GetRequiredService<RssItemsQueryService>();
                var operation = this._serviceProvider.GetRequiredService<RssItemsOperationService>();

                var items = query.List(new[] { RssItemState.Undecided });

                var shouldAccept = new List<RssItem>();
                var shouldReject = new List<RssItem>();

                foreach (var item in items)
                {
                    foreach (var decider in deciders)
                    {
                        var decidedState = decider.GetNextState(item);
                        if (decidedState != RssItemState.Undecided)
                        {
                            if (decidedState == RssItemState.Accepted)
                            {
                                shouldAccept.Add(item);
                            }
                            else if (decidedState == RssItemState.Rejected)
                            {
                                shouldReject.Add(item);
                            }

                            state[decider.RuleId] = state.GetValueOrDefault(decider.RuleId) + 1;

                            break;
                        }
                    }
                }

                if (shouldReject.Count + shouldAccept.Count > 0)
                {
                    operation.ChangeState(shouldReject, RssItemState.Rejected);

                    if (shouldAccept.Count > 0)
                        throw new NotImplementedException();

                    using (var scope = this._serviceProvider.CreateScope())
                    {
                        var now = DateTime.UtcNow;
                        var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
                        foreach (var (k, v) in state)
                        {
                            var item = ctx.MatchRules.Find(k);
                            if (item != null)
                            {
                                item.LastMatched = now;
                                item.TotalMatchedCount += v;
                            }
                        }
                        ctx.SaveChanges();
                    }
                }

                this._viewerLogger.AddLine($"Rejected {shouldReject.Count} items from {items.Length} undecided items.");
            }                
        }

        public Task AutoRejectAsync() => Task.Run(this.AutoReject);

        private class MatchRuleStateDecider
        {
            private readonly MatchRule _matchRule;
            private readonly IStringMatcher _stringMatcher;
            private readonly RssItemState _state;

            public MatchRuleStateDecider(MatchRule matchRule, IStringMatcher stringMatcher)
            {
                this._matchRule = matchRule ?? throw new ArgumentNullException(nameof(matchRule));
                this._stringMatcher = stringMatcher ?? throw new ArgumentNullException(nameof(stringMatcher));
                switch (this._matchRule.Action)
                {
                    case MatchAction.Reject:
                        this._state = RssItemState.Rejected;
                        break;

                    case MatchAction.Accept:
                        this._state = RssItemState.Accepted;
                        break;

                    default:
                        this._state = RssItemState.Undecided;
                        break;
                }
            }

            public int RuleId => this._matchRule.Id;

            public RssItemState GetNextState(RssItem rssItem)
            {
                return this._stringMatcher.IsMatch(rssItem.Title) ? this._state : RssItemState.Undecided;
            }
        }
    }
}
