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
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class RunRulesService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewerLogger _viewerLogger;
        private readonly SafeHandle<ImmutableArray<MatchRuleWrapper>> _matchRuleStateDeciders;

        public event Action<IRssItemsStateChangedInfo> AddedSingleRuleEffectedRssItemsStateChanged;

        public RunRulesService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;

            this._matchRuleStateDeciders = new SafeHandle<ImmutableArray<MatchRuleWrapper>>();

            var configService = this._serviceProvider.GetRequiredService<ConfigService>();
            configService.MatchRulesChanged += this.ConfigService_MatchRulesChanged;
            this.OnUpdated(configService.ListMatchRules(true));
        }

        private void ConfigService_MatchRulesChanged(object sender, CollectionChangeEventArgs e)
        {
            switch (e.Action)
            {
                case CollectionChangeAction.Add:
                    this.RunForAddedRule(e.Element as MatchRule);
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

        internal void RunForAddedRssItem(object sender, IReadOnlyCollection<IRssItem> e)
        {
            Task.Run(async () =>
            {
                var context = new MatchContext(this._serviceProvider);
                context.Rules.AddRange(this._matchRuleStateDeciders.Value);

                using (this._viewerLogger.EnterEvent("Run rules"))
                {
                    await context.RunForAsync(e);
                    this._viewerLogger.AddLine($"{context.GetResultMessage()} from {context.SourceItems.Count} new undecided items.");
                }
            });
        }

        private void RunForAddedRule(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            var factory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();

            var matcher = factory.Create(rule);
            var decider = new MatchRuleWrapper(rule, matcher);
            this._matchRuleStateDeciders.Change(v => v.Add(decider));

            var context = new MatchContext(this._serviceProvider);
            context.Rules.Add(decider);

            _ = Task.Run(async () =>
              {
                  await context.RunForAllAsync();
                  this._viewerLogger.AddLine($"{context.GetResultMessage()} by new rule ({rule.Argument}).");
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
                    .Select(z => new MatchRuleWrapper(z, factory.Create(z)))
                    .ToImmutableArray();

                this._matchRuleStateDeciders.Value = deciders;
            }
        }

        public Task RunAllRulesAsync()
        {
            return Task.Run(async () =>
            {
                var context = new MatchContext(this._serviceProvider);
                context.Rules.AddRange(this._matchRuleStateDeciders.Value);

                using (this._viewerLogger.EnterEvent("Run rules"))
                {
                    await context.RunForAllAsync();
                    this._viewerLogger.AddLine($"{context.GetResultMessage()} from {context.SourceItems.Count} undecided items.");
                }
            });
        }

        private class MatchRuleWrapper
        {
            private readonly MatchRule _matchRule;
            private readonly IStringMatcher _stringMatcher;

            public MatchRuleWrapper(MatchRule matchRule, IStringMatcher stringMatcher)
            {
                this._matchRule = matchRule ?? throw new ArgumentNullException(nameof(matchRule));
                this._stringMatcher = stringMatcher ?? throw new ArgumentNullException(nameof(stringMatcher));
                this.LastMatched = matchRule.LastMatched;
            }

            public DateTime LastMatched { get; set; }

            public int RuleId => this._matchRule.Id;

            public bool IsMatch(RssItem rssItem) => this._stringMatcher.IsMatch(rssItem.Title);

            public string HandlerId => this._matchRule.HandlerId;
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

            public List<MatchRuleWrapper> Rules { get; } = new();

            public Dictionary<int, int> MatchedCounter { get; } = new();

            public DateTime Now { get; } = DateTime.UtcNow;

            public async ValueTask RunForAllAsync()
            {
                if (this.Rules.Count == 0)
                    return;

                this.SourceItems.AddRange(this._queryService.List(new[] { RssItemState.Undecided }));

                await this.Scan();
                this.Commit();
            }

            public async ValueTask RunForAsync(IReadOnlyCollection<IRssItem> rssItems)
            {
                if (this.Rules.Count == 0)
                    return;

                this.SourceItems.AddRange(rssItems.OfType<RssItem>().Where(z => z.State == RssItemState.Undecided));

                await this.Scan();
                this.Commit();
            }

            private async ValueTask Scan()
            {
                var rules = this.Rules.OrderByDescending(z => z.LastMatched).ToList();

                var results = this.SourceItems.AsParallel()
                    .Select(item =>
                    {
                        foreach (var rule in rules)
                        {
                            if (rule.IsMatch(item))
                            {
                                return (Rule: rule, Item: item);
                            }
                        }
                        return (null, null);
                    })
                    .Where(z => z.Rule != null)
                    .ToList();

                var matchedCounter = this.MatchedCounter;
                foreach (var result in results)
                {
                    result.Rule.LastMatched = this.Now;
                    matchedCounter[result.Rule.RuleId] = matchedCounter.GetValueOrDefault(result.Rule.RuleId) + 1;
                }

                var handlersService = this._serviceProvider.GetRequiredService<RssItemHandlersService>();
                foreach (var group in results.GroupBy(z => z.Rule.HandlerId))
                {
                    var handlerId = group.Key;
                    var handler = string.IsNullOrEmpty(handlerId)
                        ? handlersService.GetDefaultRuleTargetHandler()
                        : handlersService.GetRuleTargetHandlers().FirstOrDefault(z => z.Id == handlerId);

                    if (handler != null)
                    {
                        var handledItems = await handler.Accept(group.Select(z => ((IRssItem)z.Item, z.Item.State)).ToList()).ToListAsync();
                        foreach (var (item, newState) in handledItems)
                        {
                            if (newState != RssItemState.Undecided)
                            {
                                if (newState == RssItemState.Accepted)
                                {
                                    this.AcceptedItems.Add((RssItem)item);
                                }
                                else if (newState == RssItemState.Rejected)
                                {
                                    this.RejectedItems.Add((RssItem)item);
                                }
                            }
                        }
                    }
                } 
            }

            private void Commit()
            {
                var matchedCounter = this.MatchedCounter;

                if (this.RejectedItems.Count + this.AcceptedItems.Count > 0)
                {
                    this._operationService.ChangeState(this.AcceptedItems, RssItemState.Accepted);
                    this._operationService.ChangeState(this.RejectedItems, RssItemState.Rejected);

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

            public string GetResultMessage()
            {
                if (this.AcceptedItems.Count > 0 && this.RejectedItems.Count > 0)
                {
                    return $"Accepted {this.AcceptedItems.Count} items and rejected {this.RejectedItems.Count } items";
                }
                else if (this.AcceptedItems.Count > 0)
                {
                    return  $"Accepted {this.AcceptedItems.Count} items";
                }
                else if (this.RejectedItems.Count > 0)
                {
                    return  $"Rejected {this.RejectedItems.Count} items";
                }
                else
                {
                    return $"No items handled";
                }
            }
        }
    }
}
