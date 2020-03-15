using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.LocalDb;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RSSViewer.Services
{
    public class GroupService
    {
        private readonly object _syncRoot = new object();
        private ImmutableList<Regex> _regexes;
        public readonly Dictionary<string, string> _groupsCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public GroupService(ConfigService config)
        {
            this.Reload(config.AppConf);
            config.OnAppConfChanged += this.Reload;
        }

        private void Reload(AppConf config)
        {
            var regexes = config.Group.Matches
                .Select(z => new Regex(z, RegexOptions.IgnoreCase))
                .ToImmutableList();
            lock (this._syncRoot)
            {
                this._regexes = regexes;
                this._groupsCache.Clear();
            }
        }

        private static string GetGroupName(IImmutableList<Regex> regexes, RssItem rssItem)
        {
            foreach (var re in regexes)
            {
                var match = re.Match(rssItem.Title);
                if (match.Success)
                {
                    if (match.Groups.ContainsKey("name"))
                    {
                        return match.Groups["name"].Value;
                    }

                    if (match.Groups.Count > 1)
                    {
                        return match.Groups[1].Value;
                    }

                    return match.Groups[0].Value;
                }
            }

            return string.Empty;
        }

        public Dictionary<string, List<RssItem>> GetGroupsMap(IEnumerable<RssItem> source)
        {
            ImmutableList<Regex> regexes;
            Dictionary<string, string> cache;
            lock (this._syncRoot)
            {
                regexes = this._regexes;
                cache = this._groupsCache;
            }

            var ret = new Dictionary<string, List<RssItem>>(StringComparer.OrdinalIgnoreCase);

            void AddToRet(string key, RssItem value)
            {
                if (!ret.TryGetValue(key, out var ls))
                {
                    ls = new List<RssItem>();
                    ret.Add(key, ls);
                }
                ls.Add(value);
            }

            var unCachedItems = new List<RssItem>();
            lock (this._syncRoot)
            {
                if (!ReferenceEquals(regexes, this._regexes))
                    // conf updated
                    return this.GetGroupsMap(source);

                foreach (var item in source)
                {
                    if (cache.TryGetValue(item.Title, out var groupName))
                    {
                        AddToRet(groupName, item);
                    }
                    else
                    {
                        unCachedItems.Add(item);
                    }
                }
            }

            var newResolvedGroups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in unCachedItems)
            {
                var groupName = GetGroupName(regexes, item);
                AddToRet(groupName, item);
                newResolvedGroups[item.Title] = groupName;
            }

            lock (this._syncRoot)
            {
                if (!ReferenceEquals(regexes, this._regexes))
                    // conf updated
                    return this.GetGroupsMap(source);

                foreach (var kvp in newResolvedGroups)
                {
                    cache[kvp.Key] = kvp.Value;
                }
            }

            return ret;
        }
    }
}
