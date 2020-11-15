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
    public class RunRulesService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewerLogger _viewerLogger;
        private readonly SafeHandle<ImmutableArray<MatchRuleStateDecider>> _matchRuleStateDeciders;

        public event Action<IRssItemsStateChangedInfo> AddedSingleRuleEffectedRssItemsStateChanged;

        public RunRulesService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;

            this._matchRuleStateDeciders = new SafeHandle<ImmutableArray<MatchRuleStateDecider>>();

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

        internal void RunForAdded(object sender, IReadOnlyCollection<IRssItem> e)
        {
            var context = new MatchContext(this._serviceProvider);
            context.Deciders.AddRange(this._matchRuleStateDeciders.Value);

            using (this._viewerLogger.EnterEvent("Auto reject"))
            {
                context.RunFor(e);
                this._viewerLogger.AddLine(
                    $"Rejected {context.RejectedItems.Count} items from {context.SourceItems.Count} new undecided items.");
            }
        }

        private void OnAdded(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            var factory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();

            var matcher = factory.Create(rule);
            var decider = new MatchRuleStateDecider(rule, matcher);
            this._matchRuleStateDeciders.Change(v => v.Add(decider));

            var context = new MatchContext(this._serviceProvider);
            context.Deciders.Add(decider);

            Task.Run(() =>
            {
                context.RunForAll();
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

                this._matchRuleStateDeciders.Value = deciders;
            }
        }

        internal void AutoReject()
        {
            var context = new MatchContext(this._serviceProvider);
            context.Deciders.AddRange(this._matchRuleStateDeciders.Value);

            using (this._viewerLogger.EnterEvent("Auto reject"))
            {
                context.RunForAll();
                this._viewerLogger.AddLine(
                    $"Rejected {context.RejectedItems.Count} items from {context.SourceItems.Count} undecided items.");
            }
        }

        public Task RunAllRulesAsync()
        {
            return Task.Run(this.AutoReject);
        }

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

            public List<RssItem> SourceItems { get; } = new();

            public List<RssItem> AcceptedItems { get; } = new();

            public List<RssItem> RejectedItems { get; } = new();

            public List<MatchRuleStateDecider> Deciders { get; } = new();

            public Dictionary<int, int> MatchedCounter { get; } = new();

            public DateTime Now { get; } = DateTime.UtcNow;

            public void RunForAll()
            {
                if (this.Deciders.Count == 0)
                    return;

                this.SourceItems.AddRange(this._queryService.List(new[] { RssItemState.Undecided }));

                this.Scan();
                this.Commit();
            }

            public void RunFor(IReadOnlyCollection<IRssItem> rssItems)
            {
                if (this.Deciders.Count == 0)
                    return;

                this.SourceItems.AddRange(rssItems.OfType<RssItem>().Where(z => z.State == RssItemState.Undecided));

                this.Scan();
                this.Commit();
            }

            private void Scan()
            {
                var deciders = this.Deciders.OrderByDescending(z => z.LastMatched).ToList();

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

                var matchedCounter = this.MatchedCounter;
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
