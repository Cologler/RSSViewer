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
        public readonly Dictionary<string, string> _groupedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public GroupService(ConfigService config)
        {
            this.Reload(config.App);
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
                this._groupedMap.Clear();
            }
        }

        private string GetGroupName(RssItem rssItem)
        {
            foreach (var re in this._regexes)
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
            var ret = new Dictionary<string, List<RssItem>>(StringComparer.OrdinalIgnoreCase);
            var miss = new List<RssItem>();

            void AddToRet(string key, RssItem value)
            {
                if (!ret.TryGetValue(key, out var ls))
                {
                    ls = new List<RssItem>();
                    ret.Add(key, ls);
                }
                ls.Add(value);
            }

            List<RssItem> ResolveMissing()
            {
                lock (this._syncRoot)
                {
                    return source.Where(z =>
                    {
                        if (this._groupedMap.TryGetValue(z.Title, out var groupName))
                        {
                            AddToRet(groupName, z);
                            return false;
                        }
                        return true;
                    }).ToList();
                }
            }

            Dictionary<string, string> ResolveAdded()
            {
                var @new = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in ResolveMissing())
                {
                    var g = GetGroupName(item);
                    AddToRet(g, item);
                    @new[item.Title] = g;
                }
                return @new;
            }

            var needAddToCache = ResolveAdded();

            lock (this._syncRoot)
            {
                foreach (var kvp in needAddToCache)
                {
                    this._groupedMap[kvp.Key] = kvp.Value;
                }
            }

            return ret;
        }
    }
}
