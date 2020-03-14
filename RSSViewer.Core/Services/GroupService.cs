using RSSViewer.LocalDb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RSSViewer.Services
{
    public class GroupService
    {
        private readonly List<Regex> _groupingTitleRegexes;
        public readonly Dictionary<string, string> _groupedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public GroupService(ConfigService config)
        {
            this._groupingTitleRegexes = config.App.Group.GroupingTitleRegexes
                .Select(z => new Regex(z, RegexOptions.IgnoreCase))
                .ToList();
        }

        private string GetGroupName(RssItem rssItem)
        {
            foreach (var re in this._groupingTitleRegexes)
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
                lock (this._groupedMap)
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

            lock (this._groupedMap)
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
