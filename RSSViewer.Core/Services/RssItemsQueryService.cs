using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Extensions;
using RSSViewer.LocalDb;
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
        private readonly IServiceProvider _serviceProvider;

        public RssItemsQueryService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        private IQueryable<PartialRssItem> CreateQueryable(IQueryable<RssItem> queryable, RssItemState[] includes, string feedId)
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

            return queryable.Select(z => new PartialRssItem
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
            return this.CreateQueryable(ctx.RssItems, includes, null).ToArray();
        }

        private Task<PartialRssItem[]> ListCoreAsync(RssItemState[] includes, string feedId, CancellationToken token)
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
                return this.CreateQueryable(ctx.RssItems, includes, feedId).ToArrayAsync(token);
            }, token);
        }

        public async Task<IReadOnlyCollection<IPartialRssItem>> ListAsync(RssItemState[] includes, string feedId, CancellationToken token) 
            => await this.ListCoreAsync(includes, feedId, token);

        public async Task<IReadOnlyCollection<IPartialRssItem>> SearchAsync(string searchText, RssItemState[] includes, string feedId, CancellationToken token)
        {
            if (searchText is null)
                throw new ArgumentNullException(nameof(searchText));
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            var items = await this.ListAsync(includes, feedId, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            
            if (string.IsNullOrWhiteSpace(searchText))
                return items;

            return await Task.Run(() =>
            {
                var regex = WildcardUtils.WildcardToRegex(searchText);
                return items.Where(z => 
                {
                    if (regex.IsMatch(z.Title))
                        return true;
                    if (z.GetPropertyOrDefault(RssItemProperties.MagnetLink)?.Contains(searchText) == true)
                        return true;
                    return false;
                }).ToArray();
            }).ConfigureAwait(false);
        }

        public string[] GetFeedIds()
        {
            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            return ctx.GetFeedIds();
        }
    }
}
