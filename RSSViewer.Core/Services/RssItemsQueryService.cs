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

        /// <summary>
        /// a sync version use for core service.
        /// </summary>
        /// <param name="includes"></param>
        /// <returns></returns>
        internal RssItem[] List(RssItemState[] includes)
        {
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            if (includes.Length == 0)
                return Array.Empty<RssItem>();

            includes = includes.Distinct().ToArray();

            IQueryable<RssItem> CreateQueryable(IQueryable<RssItem> queryable)
            {
                if (includes.Length == 3) // all
                    return queryable;
                else if (includes.Length == 1)
                    return queryable.Where(z => z.State == includes[0]);
                else
                    return queryable.Where(z => z.State == includes[0] || z.State == includes[1]);
            }

            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
            return CreateQueryable(ctx.RssItems).ToArray();
        }

        private Task<PartialRssItem[]> ListCoreAsync(RssItemState[] includes, string feedId, CancellationToken token)
        {
            includes = includes.Distinct().ToArray();

            IQueryable<RssItem> CreateQueryable(IQueryable<RssItem> queryable)
            {
                if (feedId is not null)
                {
                    queryable = queryable.Where(z => z.FeedId == feedId);
                }

                if (includes.Length == 3) // all
                    return queryable;
                else if (includes.Length == 1)
                    return queryable.Where(z => z.State == includes[0]);
                else
                    return queryable.Where(z => z.State == includes[0] || z.State == includes[1]);
            }

            return Task.Run(() =>
            {
                using var scope = this._serviceProvider.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                return CreateQueryable(ctx.RssItems)
                    .Select(z => new PartialRssItem { 
                        FeedId = z.FeedId,
                        RssId = z.RssId,
                        State = z.State,
                        Title = z.Title,
                        MagnetLink = z.MagnetLink
                    })
                    .ToArrayAsync(token);
            }, token);
        }

        public async Task<IReadOnlyCollection<IPartialRssItem>> ListAsync(RssItemState[] includes, string feedId, CancellationToken token)
        {
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            if (includes.Length == 0)
                return Array.Empty<PartialRssItem>();

            return await this.ListCoreAsync(includes, feedId, token);
        }

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
