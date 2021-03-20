using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.Helpers;
using RSSViewer.LocalDb;
using RSSViewer.RssItemHandlers;
using RSSViewer.RulesDb;
using RSSViewer.StringMatchers;
using RSSViewer.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class RunRulesService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewerLogger _viewerLogger;
        private readonly StringMatcherFactory _stringMatcherFactory;
        private readonly object _syncRoot = new();
        private ImmutableArray<RuleMatchTreeNode> _matchRules;

        public RunRulesService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;

            this._stringMatcherFactory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();

            var configService = this._serviceProvider.GetRequiredService<ConfigService>();
            configService.MatchRulesChanged += this.ConfigService_MatchRulesChanged;
            this.RebuildRules();
        }

        private RuleMatchTreeNode ToMatcher(MatchRule rule)
            => new RuleMatchTreeNode(rule, this._stringMatcherFactory.Create(rule));

        private void RebuildRules()
        {
            lock (this._syncRoot)
            {
                var rules = this._serviceProvider.GetRequiredService<ConfigService>().ListMatchRules(true);

                using (this._viewerLogger.EnterEvent("Rebuild matchers"))
                {
                    var setById = rules.Select(z => z.Id).ToHashSet();
                    var reachableItems = new List<MatchRule>();
                    var unreachableItems = new List<MatchRule>();
                    foreach (var r in rules)
                    {
                        if (r.ParentId is null || setById.Contains(r.ParentId.Value))
                        {
                            reachableItems.Add(r);
                        }
                        else
                        {
                            unreachableItems.Add(r);
                        }
                    }
                    var reachableMatchers = reachableItems.Select(this.ToMatcher).ToList();
                    var dictById = reachableMatchers.ToDictionary(z => z.Rule.Id);
                    foreach (var m in reachableMatchers.Where(z => z.Rule.ParentId is not null))
                    {
                        dictById[m.Rule.ParentId.Value].AddSubBranch(m);
                    }

                    var matchers = rules
                        .Where(z => !z.IsDisabled)
                        .Select(this.ToMatcher)
                        .ToImmutableArray();

                    this._matchRules = reachableMatchers.Where(z => z.Rule.ParentId is null).ToImmutableArray();
                }
            }
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
                    this.RunForChangedRules((IEnumerable<MatchRule>) e.Element);
                    break;

                default:
                    break;
            }
        }

        private void RunForAddedRule(MatchRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            var factory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();

            var matcher = this.ToMatcher(rule);
            var rootMatcher = rule.ParentId is null
                ? matcher
                : this._matchRules.Select(z => z.FindSubBranch(rule.Id)).Single(z => z is not null);

            if (rule.ParentId is null)
            {
                rootMatcher.AddSubBranch(matcher);
            }
            else
            {
                lock (this._syncRoot) this._matchRules = this._matchRules.Add(matcher);
            }

            var context = new MatchContext(this._serviceProvider);
            context.Rules.Add(rootMatcher);
            _ = Task.Run(async () =>
            {
                // TODO: this context run on entire root rule, not the new rule.
                await context.RunForAllAsync();
                this._viewerLogger.AddLine($"{context.GetResultMessage()} by new rule ({rule.Argument}).");
            });
        }

        private void RunForChangedRules(IEnumerable<MatchRule> rules)
        {
            if (rules is null)
                throw new ArgumentNullException(nameof(rules));

            this.RebuildRules();

            var context = new MatchContext(this._serviceProvider);
            context.Rules.AddRange(rules.Select(this.ToMatcher));
            _ = Task.Run(async () =>
            {
                await context.RunForAllAsync();
                this._viewerLogger.AddLine($"{context.GetResultMessage()} by changed rules.");
            });
        }

        internal void RunForAddedRssItem(object sender, IReadOnlyCollection<IPartialRssItem> e)
        {
            Task.Run(async () =>
            {
                var context = new MatchContext(this._serviceProvider);
                context.Rules.AddRange(this._matchRules);

                using (this._viewerLogger.EnterEvent("Run rules"))
                {
                    await context.RunForAsync(e);
                    this._viewerLogger.AddLine($"{context.GetResultMessage()} from {context.SourceItems.Count} new undecided items.");
                }
            });
        }

        public Task RunAllRulesAsync()
        {
            return Task.Run(async () =>
            {
                var context = new MatchContext(this._serviceProvider);
                context.Rules.AddRange(this._matchRules);

                using (this._viewerLogger.EnterEvent("Run rules"))
                {
                    await context.RunForAllAsync();
                    this._viewerLogger.AddLine($"{context.GetResultMessage()} from {context.SourceItems.Count} undecided items.");
                }
            });
        }

        private class MatchContext
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

            public List<IPartialRssItem> SourceItems { get; } = new();

            public List<IPartialRssItem> AcceptedItems { get; } = new();

            public List<IPartialRssItem> RejectedItems { get; } = new();

            public List<RuleMatchTreeNode> Rules { get; } = new();

            public Dictionary<int, int> MatchedCounter { get; } = new();

            public DateTime Now { get; } = DateTime.UtcNow;

            public async ValueTask RunForAllAsync()
            {
                if (this.Rules.Count == 0)
                    return;

                this.SourceItems.AddRange(this._queryService.List(new[] { RssItemState.Undecided }));

                await this.StartAsync();
            }

            public async ValueTask RunForAsync(IReadOnlyCollection<IPartialRssItem> rssItems)
            {
                if (this.Rules.Count == 0)
                    return;

                this.SourceItems.AddRange(rssItems);

                await this.StartAsync();
            }

            private async ValueTask StartAsync()
            {
                var now = this.Now;
                var rules = this.Rules.OrderByDescending(z => z.LastMatched).ToList();

                var results = this.SourceItems.AsParallel()
                    .Select(item =>
                    {
                        foreach (var rule in rules)
                        {
                            var rulesChain = rule.TryFindMatchedRule(item, now);
                            if (!rulesChain.IsDefault)
                            {
                                Debug.Assert(!rulesChain.IsEmpty);
                                return (RulesChain: rulesChain, Item: item);
                            }
                        }
                        return (default, null);
                    })
                    .Where(z => !z.RulesChain.IsDefaultOrEmpty)
                    .ToList();

                var matchedCounter = this.MatchedCounter;
                foreach (var result in results)
                {
                    result.RulesChain.Last().LastMatched = this.Now;
                    foreach (var rule in result.RulesChain)
                    {
                        matchedCounter[rule.Id] = matchedCounter.GetValueOrDefault(rule.Id) + 1;
                    }
                }

                var handlersService = this._serviceProvider.GetRequiredService<RssItemHandlersService>();
                foreach (var group in results.GroupBy(z => z.RulesChain.Last().HandlerId))
                {;
                    var handler = handlersService.GetRuleTargetHandler(group.Key);
                    if (handler is not null)
                    {
                        var handledItems = await handler.HandleAsync(group.Select(z => (z.Item, z.Item.State)).ToList()).ToListAsync();
                        foreach (var (item, newState) in handledItems)
                        {
                            if (newState != RssItemState.Undecided)
                            {
                                if (newState == RssItemState.Accepted)
                                {
                                    this.AcceptedItems.Add(item);
                                }
                                else if (newState == RssItemState.Rejected)
                                {
                                    this.RejectedItems.Add(item);
                                }
                            }
                        }
                    }
                }

                if (this.RejectedItems.Count + this.AcceptedItems.Count > 0)
                {
                    var operationSession = this._operationService.CreateOperationSession(false);
                    operationSession.ChangeState(this.AcceptedItems, RssItemState.Accepted);
                    operationSession.ChangeState(this.RejectedItems, RssItemState.Rejected);

                    using (var scope = this._serviceProvider.CreateScope())
                    {
                        var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
                        foreach (var (ruleId, count) in matchedCounter)
                        {
                            var item = ctx.MatchRules.Find(ruleId);
                            if (item is not null)
                            {
                                item.LastMatched = now;
                                item.TotalMatchedCount += count;
                            }
                        }
                        ctx.SaveChanges();
                    }
                }
            }

            public string GetResultMessage()
            {
                if (this.AcceptedItems.Count > 0 && this.RejectedItems.Count > 0)
                {
                    return $"Accepted {this.AcceptedItems.Count} items and rejected {this.RejectedItems.Count } items";
                }
                else if (this.AcceptedItems.Count > 0)
                {
                    return $"Accepted {this.AcceptedItems.Count} items";
                }
                else if (this.RejectedItems.Count > 0)
                {
                    return $"Rejected {this.RejectedItems.Count} items";
                }
                else
                {
                    return $"No items handled";
                }
            }
        }
    }
}
