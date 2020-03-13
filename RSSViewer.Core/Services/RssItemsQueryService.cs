using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RSSViewer.LocalDb;
using System;
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

        public Task<RssItem[]> ListAsync(RssState[] includes, CancellationToken token)
        {
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            if (includes.Length == 0)
                return Task.FromResult(Array.Empty<RssItem>());

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

            return Task.Run(() =>
            {
                using var scope = this._serviceProvider.CreateScope();
                var ctx = scope.ServiceProvider.GetRequiredService<LocalDbContext>();
                return CreateQueryable(ctx.RssItems).ToArrayAsync(token);
            }, token);
        }

        public async Task<RssItem[]> SearchAsync(string searchText, RssState[] includes, CancellationToken token)
        {
            if (searchText is null)
                throw new ArgumentNullException(nameof(searchText));
            if (includes is null)
                throw new ArgumentNullException(nameof(includes));

            var items = await this.ListAsync(includes, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            
            if (string.IsNullOrWhiteSpace(searchText))
                return items;

            return await Task.Run(() =>
            {
                var any = ".";
                var regex = new Regex(
                    Regex.Escape(searchText.Trim())
                        .Replace("\\*", any + "*")
                        .Replace("\\?", any),
                    RegexOptions.IgnoreCase
                );
                return items.Where(z => regex.IsMatch(z.Title)).ToArray();
            }).ConfigureAwait(false);
        }
    }
}
