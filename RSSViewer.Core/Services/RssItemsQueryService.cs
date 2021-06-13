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
            return ctx.RssItems
                .WithStates(includes)
                .ToPartialRssItem()
                .ToArray();
        }

        private async Task<PartialRssItem[]> ListCoreAsync(RssItemState[] includes, string feedId, SearchExpression searchExpr, CancellationToken token)
        {
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            if (includes.Length == 0)
                return Array.Empty<PartialRssItem>();

            includes = includes.Distinct().ToArray();

            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            var queryable = ctx.RssItems
                .WithFeedId(feedId)
                .WithStates(includes)
                .WithDbSideFilter(searchExpr)
                .ToPartialRssItem();
            var count = await queryable.CountAsync(token).ConfigureAwait(false);
            var take = Math.Min(this.GetMaxLoadItems(), count);
            var skip = Math.Max(count - take, 0);
            var items = await queryable.Skip(skip).ToArrayAsync(token).ConfigureAwait(false);
            return items.TakeLast(take).ToArray();
        }

        public async Task<IReadOnlyCollection<IPartialRssItem>> SearchAsync(string searchText, RssItemState[] includes, string feedId, CancellationToken token)
        {
            if (searchText is null)
                throw new ArgumentNullException(nameof(searchText));
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            var searchExpr = SearchExpression.Parse(searchText);
            var items = await this.ListCoreAsync(includes, feedId, searchExpr, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();

            if (items.WithClientSideFilter(searchExpr) is var filtered && !ReferenceEquals(filtered, items))
            {
                items = filtered.ToArray();
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
