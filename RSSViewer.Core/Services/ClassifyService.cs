using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.Filter;
using RSSViewer.LocalDb;
using RSSViewer.Models;
using RSSViewer.RulesDb;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class ClassifyService
    {
        private ImmutableList<Regex> _regexes;
        private readonly IServiceProvider _serviceProvider;
        private readonly RegexCache _regexCache;

        public ClassifyService(IServiceProvider serviceProvider, ConfigService config, RegexCache regexCache)
        {
            this._serviceProvider = serviceProvider;
            this._regexCache = regexCache;
            config.OnAppConfChanged += this.Reload;
            this.Reload(config.AppConf);
        }

        private void Reload(AppConf config)
        {
            var regexes = new List<Regex>();

            foreach (var match in config.Group.Matches.Where(z => z is not null))
            {
                var regex = this._regexCache.TryGet(match, RegexOptions.IgnoreCase);
                if (regex is not null)
                {
                    regexes.Add(regex);
                }
            }

            this._regexes = regexes.ToImmutableList();
        }

        private static string GetGroupName(IImmutableList<Regex> regexes, IPartialRssItem rssItem)
        {
            foreach (var re in regexes)
            {
                var match = re.Match(rssItem.Title);
                if (match.Success)
                {
                    if (match.Groups.ContainsKey("name"))
                    {
                        return match.Groups["name"].Value.Trim();
                    }

                    if (match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value.Trim();
                    }

                    return match.Groups[0].Value;
                }
            }

            return string.Empty;
        }

        public void Classify(IEnumerable<ClassifyContext<IPartialRssItem>> source, CancellationToken token)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (token.IsCancellationRequested)
                return;

            var regexes = this._regexes;

            source.AsParallel()
                .WithCancellation(token)
                .ForAll(item =>
                {
                    if (token.IsCancellationRequested)
                        return;
                    item.GroupName = GetGroupName(regexes, item.Item);
                });

            this.Tagify(source, token);
        }

        public void Tagify(IEnumerable<ClassifyContext<IPartialRssItem>> source, CancellationToken token)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (token.IsCancellationRequested)
                return;

            using var scope = this._serviceProvider.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<RssItemFilterFactory>();
            ;
            using var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

            var tags = ctx.Tags.AsQueryable().AsNoTracking().ToDictionary(z => z.Id);
            var rules = ctx.MatchRules.AsQueryable()
                .AsNoTracking()
                .Where(z => z.HandlerType == HandlerType.SetTag)
                .ToList();

            var testRules = new List<(MatchRule, IRssItemFilter, Tag)>();
            foreach (var rule in rules)
            {
                if (tags.TryGetValue(rule.HandlerId, out var tag))
                {
                    var filter = factory.Create(rule);
                    testRules.Add((rule, filter, tag));
                }
            }

            var groupedTestRules = testRules.GroupBy(z => z.Item3.TagGroupName);

            if (token.IsCancellationRequested)
                return;

            source.AsParallel()
                .WithCancellation(token)
                .ForAll(item =>
                {
                    if (token.IsCancellationRequested)
                        return;
                    foreach (var group in groupedTestRules)
                    {
                        var match = false;
                        foreach (var testRule in group)
                        {
                            if (testRule.Item2.IsMatch(item.Item))
                            {
                                item.Tags.Add(testRule.Item3);
                                match = true;
                            }
                        }
                        if (!match && !string.IsNullOrEmpty(group.Key))
                        {
                            item.TagGroupWithoutTag.Add(group.Key);
                        }
                    }
                });
        }
    }
}
