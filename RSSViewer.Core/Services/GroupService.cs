using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.LocalDb;
using RSSViewer.Utils;

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
        private readonly RegexCache _regexCache;

        public GroupService(ConfigService config, RegexCache regexCache)
        {
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

            lock (this._syncRoot)
            {
                this._regexes = regexes.ToImmutableList();
                this._groupsCache.Clear();
            }
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

        public Dictionary<string, List<IPartialRssItem>> GetGroupsMap(IEnumerable<IPartialRssItem> source)
        {
            ImmutableList<Regex> regexes;
            Dictionary<string, string> cache;
            lock (this._syncRoot)
            {
                regexes = this._regexes;
                cache = this._groupsCache;
            }

            var ret = new Dictionary<string, List<IPartialRssItem>>(StringComparer.OrdinalIgnoreCase);

            void AddToRet(string key, IPartialRssItem value)
            {
                if (!ret.TryGetValue(key, out var ls))
                {
                    ls = new List<IPartialRssItem>();
                    ret.Add(key, ls);
                }
                ls.Add(value);
            }

            var unCachedItems = new List<IPartialRssItem>();
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
