using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Extensions;
using RSSViewer.LocalDb;
using RSSViewer.Search;
using RSSViewer.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class RssItemsQueryService
    {
        private const int DefaultMaxLoadItems = 10000;
        private readonly IServiceProvider _serviceProvider;
        private readonly ConfigService _configService;

        public RssItemsQueryService(IServiceProvider serviceProvider, ConfigService configService)
        {
            if (configService is null)
                throw new ArgumentNullException(nameof(configService));
            this._serviceProvider = serviceProvider;
            this._configService = configService;
        }

        private int GetMaxLoadItems()
        {
            var value = this._configService.AppConf.MaxLoadItems ?? DefaultMaxLoadItems;
            return value < 1 ? DefaultMaxLoadItems : value;
        }

        private IQueryable<PartialRssItem> CreateQueryable(IQueryable<RssItem> queryable, 
            RssItemState[] includes, string feedId, SearchExpression searchExpr)
        {
            if (queryable is null)
                throw new ArgumentNullException(nameof(queryable));
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            if (feedId is not null)
            {
                queryable = queryable.Where(z => z.FeedId == feedId);
            }

            if (includes.Length == 1)
                queryable = queryable.Where(z => z.State == includes[0]);
            else if (includes.Length == 2)
                queryable = queryable.Where(z => z.State == includes[0] || z.State == includes[1]);
            else if (includes.Length == 3)
                queryable = queryable.Where(z => z.State == includes[0] || z.State == includes[1] || z.State == includes[2]);

            if (searchExpr is not null)
            {
                foreach (var part in searchExpr.Parts.OfType<IDbSearchPart>())
                {
                    queryable = part.Where(queryable);
                }
            }

            return queryable
                .Take(this.GetMaxLoadItems())
                .Select(z => new PartialRssItem
                {
                    FeedId = z.FeedId,
                    RssId = z.RssId,
                    State = z.State,
                    Title = z.Title,
                    MagnetLink = z.MagnetLink
                });
        }

        /// <summary>
        /// a sync version use for core service.
        /// </summary>
        /// <param name="includes"></param>
        /// <returns></returns>
        internal PartialRssItem[] List(RssItemState[] includes)
        {
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            if (includes.Length == 0)
                return Array.Empty<PartialRssItem>();

            includes = includes.Distinct().ToArray();

            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            return this.CreateQueryable(ctx.RssItems, includes, null, null).ToArray();
        }

        private Task<PartialRssItem[]> ListCoreAsync(RssItemState[] includes, string feedId, SearchExpression searchExpr, CancellationToken token)
        {
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            if (includes.Length == 0)
                return Task.FromResult(Array.Empty<PartialRssItem>());

            includes = includes.Distinct().ToArray();

            return Task.Run(() =>
            {
                using var scope = this._serviceProvider.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                return this.CreateQueryable(ctx.RssItems, includes, feedId, searchExpr).ToArrayAsync(token);
            }, token);
        }

        public async Task<IReadOnlyCollection<IPartialRssItem>> ListAsync(RssItemState[] includes, string feedId, CancellationToken token) 
            => await this.ListCoreAsync(includes, feedId, null, token);

        public async Task<IReadOnlyCollection<IPartialRssItem>> SearchAsync(string searchText, RssItemState[] includes, string feedId, CancellationToken token)
        {
            if (searchText is null)
                throw new ArgumentNullException(nameof(searchText));
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            var searchExpr = SearchExpression.Parse(searchText);
            var items = await this.ListCoreAsync(includes, feedId, searchExpr, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            if (searchExpr.Parts.OfType<IAppSearchPart>().Any())
            {
                items = await Task.Run(() =>
                {
                    IEnumerable<PartialRssItem> r = items;
                    foreach (var part in searchExpr.Parts.OfType<IAppSearchPart>())
                    {
                        r = part.Where(r);
                    }
                    return r.ToArray();
                }).ConfigureAwait(false);
            }

            return items;
        }

        public string[] GetFeedIds()
        {
            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            return ctx.GetFeedIds();
        }
    }
}
