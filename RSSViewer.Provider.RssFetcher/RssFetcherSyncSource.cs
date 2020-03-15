using RSSViewer.Abstractions;
using RSSViewer.Annotations;
using RSSViewer.DefaultImpls;
using RSSViewer.Utils;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace RSSViewer.Provider.RssFetcher
{
    class RssFetcherSyncSource : ISyncSource
    {
        public RssFetcherSyncSource(string syncSourceId) => this.SyncSourceId = syncSourceId;

        public string SyncSourceId { get; }

        public string ProviderName => "RssFetcher";

        [UserVariable, Required]
        public string Database { get; set; }

        public async ValueTask<ISourceRssItemPage> TryGetItemsAsync(int? lastId, CancellationToken cancellationToken, 
            int? limit = null)
        {
            var sql = $"SELECT rowid,* from rss";

            if (lastId is int rowid)
            {
                sql += $" WHERE rowid > {rowid}";
            }

            sql += " ORDER BY rowid ASC";

            if (limit is int count)
            {
                sql += $" LIMIT {count}";
            }

            using var connection = new SQLiteConnection($"Data Source=\"{this.Database}\"");
            connection.Open();
            var command = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            command.CommandText = sql;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var ordRowId = reader.GetOrdinal("rowid");
            var ordFeedId = reader.GetOrdinal("feed_id");
            var ordRssId = reader.GetOrdinal("rss_id");
            var ordRaw = reader.GetOrdinal("raw");

            int? rowId = null;
            var items = new List<SourceRssItem>();
            while (await reader.ReadAsync(cancellationToken))
            {
                rowId = reader.GetInt32(ordRowId);
                var feedId = reader.GetString(ordFeedId);
                var rssId = reader.GetString(ordRssId);
                var raw = reader.GetString(ordRaw);
                var rssItem = new SourceRssItem(feedId, rssId, raw);
                rssItem.LoadFrom(RssItemXmlReader.FromString(raw));
                items.Add(rssItem);
            }

            var page = new SourceRssItemPage(rowId ?? lastId, items);
            return page;
        }
    }
}
