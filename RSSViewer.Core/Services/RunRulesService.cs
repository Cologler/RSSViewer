using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.Filter;
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
using System.Text;
using System.Threading.Tasks;

using TreeCollections;

namespace RSSViewer.Services
{
    public class RunRulesService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewerLogger _viewerLogger;
        private readonly RssItemFilterFactory _stringMatcherFactory;
        private readonly object _syncRoot = new();
        private RuleMatchTree _ruleMatchTree { get; set; }

        public RunRulesService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;

            this._stringMatcherFactory = this._serviceProvider.GetRequiredService<RssItemFilterFactory>();

            var configService = this._serviceProvider.GetRequiredService<ConfigService>();
            configService.MatchRulesChanged += this.ConfigService_MatchRulesChanged;
            this.RebuildRules();
        }

        private void RebuildRules()
        {
            lock (this._syncRoot)
            {
                var rules = this._serviceProvider.GetRequiredService<ConfigService>().ListActionRules();

                using (this._viewerLogger.EnterEvent("Rebuild matchers"))
                {
                    var newTree = new RuleMatchTree(this._stringMatcherFactory);
                    newTree.AddOrUpdate(rules);
                    this._ruleMatchTree = newTree;
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

            var factory = this._stringMatcherFactory;
            this._ruleMatchTree.AddOrUpdate(new[] { rule });

            var clonedTree = this._ruleMatchTree.DeepClone();
            clonedTree.DisableAll();
            clonedTree.EnableFor(new[] { rule });

            var context = new MatchContext(this._serviceProvider);
            context.RuleMatchTree = clonedTree;
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

            var clonedTree = this._ruleMatchTree.DeepClone();
            clonedTree.DisableAll();
            clonedTree.EnableFor(rules);

            var context = new MatchContext(this._serviceProvider);
            context.RuleMatchTree = clonedTree;
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
                context.RuleMatchTree = this._ruleMatchTree;

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
                context.RuleMatchTree = this._ruleMatchTree;

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
            private readonly bool _isDebuggerAttached = Debugger.IsAttached;

            public MatchContext(IServiceProvider serviceProvider)
            {
                this._serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                this._queryService = serviceProvider.GetRequiredService<RssItemsQueryService>();
                this._operationService = serviceProvider.GetRequiredService<RssItemsOperationService>();
            }

            public RuleMatchTree RuleMatchTree { get; set; }

            public List<IPartialRssItem> SourceItems { get; } = new();

            public List<IPartialRssItem> AcceptedItems { get; } = new();

            public List<IPartialRssItem> RejectedItems { get; } = new();

            public List<IPartialRssItem> ArchivedItems { get; } = new();

            public DateTime Now { get; } = DateTime.UtcNow;

            public async ValueTask RunForAllAsync()
            {
                if (this.RuleMatchTree is null)
                    return;

                this.SourceItems.AddRange(this._queryService.List(new[] { RssItemState.Undecided }));

                await this.StartAsync();
            }

            public async ValueTask RunForAsync(IReadOnlyCollection<IPartialRssItem> rssItems)
            {
                if (this.RuleMatchTree is null)
                    return;

                this.SourceItems.AddRange(rssItems);

                await this.StartAsync();
            }

            private void LogMatched(ImmutableArray<MatchRule> matchedRulesChain, IPartialRssItem item)
            {
                if (!this._isDebuggerAttached)
                    return;

                var matchedRulesChainText = string.Join(" -> ", matchedRulesChain.Select(z => z.ToDebugString()));
                Debug.WriteLine(@"match {0} by {1}", item.Title, matchedRulesChainText);
            }

            private async ValueTask StartAsync()
            {
                var now = this.Now;
                var matchTree = this.RuleMatchTree;
                if (matchTree is null)
                    return;

                var results = this.SourceItems.AsParallel()
                    .Select(item =>
                    {
                        var rulesChain = matchTree.TryFindMatchedRule(item, now);
                        if (rulesChain.IsDefault)
                            return (default, null);
                        Debug.Assert(!rulesChain.IsEmpty);
                        this.LogMatched(rulesChain, item);
                        return (RulesChain: rulesChain, Item: item);
                    })
                    .Where(z => !z.RulesChain.IsDefaultOrEmpty)
                    .ToList();

                var matchedCounter = new Dictionary<int, int>();
                foreach (var (RulesChain, Item) in results)
                {
                    foreach (var rule in RulesChain)
                    {
                        matchedCounter[rule.Id] = matchedCounter.GetValueOrDefault(rule.Id) + 1;
                    }
                }

                var handlersService = this._serviceProvider.GetRequiredService<RssItemHandlersService>();
                foreach (var group in results.GroupBy(z => z.RulesChain.Last().HandlerId))
                {
                    var handler = handlersService.GetRuleTargetHandler(group.Key);
                    if (handler is not null)
                    {
                        var applyItems = group
                            .Select(z => (z.Item, z.Item.State))
                            .ToList();

                        var handledItems = await handler.HandleAsync(applyItems).ToListAsync();
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
                                else if (newState == RssItemState.Archived)
                                {
                                    this.ArchivedItems.Add(item);
                                }
                            }
                        }
                    }
                }

                if (this.RejectedItems.Count + this.AcceptedItems.Count + this.ArchivedItems.Count > 0)
                {
                    var operationSession = this._operationService.CreateOperationSession(false);
                    operationSession.ChangeState(this.AcceptedItems, RssItemState.Accepted);
                    operationSession.ChangeState(this.RejectedItems, RssItemState.Rejected);
                    operationSession.ChangeState(this.ArchivedItems, RssItemState.Archived);

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
                var messageBuilder = new StringBuilder();

                if (this.AcceptedItems.Count > 0)
                {
                    messageBuilder.Append($"Accepted {this.AcceptedItems.Count} items");
                }

                if (this.RejectedItems.Count > 0)
                {
                    if (messageBuilder.Length > 0)
                        messageBuilder.Append(" and ");
                    messageBuilder.Append($"rejected {this.RejectedItems.Count} items");
                }

                if (this.ArchivedItems.Count > 0)
                {
                    if (messageBuilder.Length > 0)
                        messageBuilder.Append(" and ");
                    messageBuilder.Append($"archived {this.ArchivedItems.Count} items");
                }

                if (messageBuilder.Length == 0)
                {
                    messageBuilder.Append("No items handled");
                }
                else
                {
                    messageBuilder[0] = char.ToUpper(messageBuilder[0]);
                }

                return messageBuilder.ToString();
            }
        }
    }
}
