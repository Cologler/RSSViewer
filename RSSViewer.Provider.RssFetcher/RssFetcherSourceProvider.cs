using RSSViewer.Abstractions;
using RSSViewer.DefaultImpls;
using RSSViewer.Utils;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading.Tasks;

namespace RSSViewer.Provider.RssFetcher
{
    public class RssFetcherSourceProvider : ISourceProvider
    {
        public static readonly VariableInfo VarDatabase = VariableInfo.String("Database", true);
        public static readonly VariableInfo[] VariableInfos = new [] {
            VarDatabase
        };

        private string _database;
        private string _tableName = "rss";

        public string ProviderName => "RssFetcher";

        public IReadOnlyCollection<VariableInfo> GetVariableInfos() => VariableInfos;

        public ValueTask<bool> InitializeAsync(Dictionary<string, object> variables)
        {
            this._database = (string) VarDatabase.ReadFrom(variables);
            return new ValueTask<bool>(true);
        }

        public async ValueTask<ISourceRssItemPage> GetItemsListAsync(int? lastId = null, int? limit = null)
        {
            var sql = $"SELECT rowid,* from {this._tableName}";

            if (lastId is int rowid)
            {
                sql += $" WHERE rowid > {rowid}";
            }

            sql += " ORDER BY rowid ASC";

            if (limit is int count)
            {
                sql += $" LIMIT {count}";
            }

            using var connection = new SQLiteConnection($"Data Source=\"{this._database}\"");
            connection.Open();
            var command = connection.CreateCommand();
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            command.CommandText = sql;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
            using var reader = await command.ExecuteReaderAsync();

            var ordRowId = reader.GetOrdinal("rowid");
            var ordFeedId = reader.GetOrdinal("feed_id");
            var ordRssId = reader.GetOrdinal("rss_id");
            var ordRaw = reader.GetOrdinal("raw");

            int? rowId = null;
            var items = new List<SourceRssItem>();
            while (await reader.ReadAsync())
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
