using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.Helpers;
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
        private readonly SafeHandle<ImmutableArray<RssItemMatcher>> _matchRules;
        private readonly StringMatcherFactory _stringMatcherFactory;

        public RunRulesService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;

            this._stringMatcherFactory = this._serviceProvider.GetRequiredService<StringMatcherFactory>();

            this._matchRules = new SafeHandle<ImmutableArray<RssItemMatcher>>();

            var configService = this._serviceProvider.GetRequiredService<ConfigService>();
            configService.MatchRulesChanged += this.ConfigService_MatchRulesChanged;
            this.RebuildRules();
        }

        private RssItemMatcher ToMatcher(MatchRule rule)
            => new RssItemMatcher(rule, this._stringMatcherFactory.Create(rule));

        private void RebuildRules()
        {
            lock (this._matchRules.SyncRoot)
            {
                var rules = this._serviceProvider.GetRequiredService<ConfigService>().ListMatchRules(true);

                using (this._viewerLogger.EnterEvent("Rebuild matchers"))
                {
                    var matchers = rules
                        .Where(z => !z.IsDisabled)
                        .Select(this.ToMatcher)
                        .ToImmutableArray();

                    this._matchRules.Value = matchers;
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
            this._matchRules.Change(v => v.Add(matcher));

            var context = new MatchContext(this._serviceProvider);
            context.Rules.Add(matcher);
            _ = Task.Run(async () =>
            {
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
                context.Rules.AddRange(this._matchRules.Value);

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
                context.Rules.AddRange(this._matchRules.Value);

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

            public List<RssItemMatcher> Rules { get; } = new();

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

            public async ValueTask RunForAsync(IReadOnlyCollection<IPartialRssItem> rssItems)
            {
                if (this.Rules.Count == 0)
                    return;

                this.SourceItems.AddRange(rssItems);

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
            }

            private void Commit()
            {
                var matchedCounter = this.MatchedCounter;

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
