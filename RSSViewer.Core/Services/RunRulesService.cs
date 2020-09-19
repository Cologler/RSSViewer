﻿using Microsoft.Extensions.DependencyInjection;
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
    public class RunRulesService
    {
        private readonly object _syncRoot = new object();
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewerLogger _viewerLogger;
        private ImmutableArray<MatchRuleStateDecider> _matchRuleStateDeciders;

        public event Action<IRssItemsStateChangedInfo> AddedSingleRuleEffectedRssItemsStateChanged;

        public RunRulesService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;

            var configService = this._serviceProvider.GetRequiredService<ConfigService>();
            configService.MatchRulesChanged += this.ConfigService_MatchRulesChanged;
            this.OnUpdated(configService.ListMatchRules(true));
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
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            var factory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();

            var matcher = factory.Create(rule);
            var decider = new MatchRuleStateDecider(rule, matcher);

            lock (this._syncRoot)
            {
                this._matchRuleStateDeciders = this._matchRuleStateDeciders.Add(decider); 
            }

            Task.Run(() =>
            {
                var context = new MatchContext(this._serviceProvider);
                context.Run(ImmutableArray.Create(decider));
                this._viewerLogger.AddLine(
                    $"{rule.Action}ed {context.RejectedItems.Count} items by new rule ({rule.Argument})");
                this.AddedSingleRuleEffectedRssItemsStateChanged?.Invoke(context);
            });
        }

        private void OnUpdated(IEnumerable<MatchRule> rules)
        {
            if (rules is null)
                return;

            using (this._viewerLogger.EnterEvent("Rebuild matchers"))
            {
                var factory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();
                var matchers = rules.Select(z => (z, factory.Create(z)))
                    .ToArray();
                var deciders = rules
                    .Where(z => !z.IsDisabled)
                    .Select(z => new MatchRuleStateDecider(z, factory.Create(z)))
                    .ToImmutableArray();

                lock (this._syncRoot)
                {
                    this._matchRuleStateDeciders = deciders;
                }
            }
        }

        internal void AutoReject()
        {
            var context = new MatchContext(this._serviceProvider);
            using (this._viewerLogger.EnterEvent("Auto reject"))
            {
                context.Run(this._matchRuleStateDeciders);
                this._viewerLogger.AddLine(
                    $"Rejected {context.RejectedItems.Count} items from {context.SourceItems.Count} undecided items.");
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
                this.LastMatched = matchRule.LastMatched;
            }

            public DateTime LastMatched { get; set; }

            public int RuleId => this._matchRule.Id;

            public RssItemState GetNextState(RssItem rssItem)
            {
                return this._stringMatcher.IsMatch(rssItem.Title) ? this._state : RssItemState.Undecided;
            }
        }

        private class MatchContext : IRssItemsStateChangedInfo
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly RssItemsQueryService _queryService;
            private readonly RssItemsOperationService _operationService;

            public MatchContext(IServiceProvider serviceProvider)
            {
                this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                this._queryService = serviceProvider.GetRequiredService<RssItemsQueryService>();
                this._operationService = serviceProvider.GetRequiredService<RssItemsOperationService>();
            }

            public List<RssItem> SourceItems { get; } = new List<RssItem>();

            public List<RssItem> AcceptedItems { get; } = new List<RssItem>();

            public List<RssItem> RejectedItems { get; } = new List<RssItem>();

            public Dictionary<int, int> MatchedCounter { get; } = new Dictionary<int, int>();

            public DateTime Now { get; } = DateTime.UtcNow;

            public void Run(IReadOnlyCollection<MatchRuleStateDecider> deciders)
            {
                this.Scan(deciders);
                this.Commit();
            }

            private void Scan(IReadOnlyCollection<MatchRuleStateDecider> deciders)
            {
                if (deciders is null)
                    throw new ArgumentNullException(nameof(deciders));

                if (deciders.Count == 0)
                    return;

                var matchedCounter = this.MatchedCounter;
                deciders = deciders.OrderByDescending(z => z.LastMatched).ToList();

                var operation = this._operationService;

                this.SourceItems.AddRange(this._queryService.List(new[] { RssItemState.Undecided }));

                var results = this.SourceItems.AsParallel()
                    .Select(item =>
                    {
                        foreach (var decider in deciders)
                        {
                            var newState = decider.GetNextState(item);
                            if (newState != RssItemState.Undecided)
                            {
                                return new
                                {
                                    MatchRuleStateDecider = decider,
                                    Item = item,
                                    NewState = newState
                                };
                            }
                        }
                        return null;
                    })
                    .Where(z => z != null)
                    .ToList();

                foreach (var result in results)
                {
                    var newState = result.NewState;
                    if (newState != RssItemState.Undecided)
                    {
                        if (newState == RssItemState.Accepted)
                        {
                            this.AcceptedItems.Add(result.Item);
                        }
                        else if (newState == RssItemState.Rejected)
                        {
                            this.RejectedItems.Add(result.Item);
                        }

                        result.MatchRuleStateDecider.LastMatched = this.Now;
                        matchedCounter[result.MatchRuleStateDecider.RuleId] = 
                            matchedCounter.GetValueOrDefault(result.MatchRuleStateDecider.RuleId) + 1;
                    }
                }
            }

            private void Commit()
            {
                var matchedCounter = this.MatchedCounter;

                if (this.RejectedItems.Count + this.AcceptedItems.Count > 0)
                {
                    this._operationService.ChangeState(this.RejectedItems, RssItemState.Rejected);

                    if (this.AcceptedItems.Count > 0)
                        throw new NotImplementedException();

                    using (var scope = this._serviceProvider.CreateScope())
                    {
                        var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
                        foreach (var (ruleId, count) in matchedCounter)
                        {
                            var item = ctx.MatchRules.Find(ruleId);
                            if (item != null)
                            {
                                item.LastMatched = this.Now;
                                item.TotalMatchedCount += count;
                            }
                        }
                        ctx.SaveChanges();
                    }
                }
            }

            IEnumerable<RssItem> IRssItemsStateChangedInfo.GetItems(RssItemState newState)
            {
                switch (newState)
                {
                    case RssItemState.Rejected:
                        return this.RejectedItems;

                    case RssItemState.Accepted:
                        return this.AcceptedItems;
                }

                return Array.Empty<RssItem>();
            }
        }
    }
}
