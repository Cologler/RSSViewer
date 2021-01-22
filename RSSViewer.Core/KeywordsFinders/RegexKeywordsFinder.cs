using RSSViewer.Abstractions;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RSSViewer.KeywordsFinders
{
    class RegexKeywordsFinder : IKeywordsFinder
    {
        private readonly Regex _regex;

        public RegexKeywordsFinder(Regex regex)
        {
            this._regex = regex;
        }

        public IEnumerable<string> GetKeywords(IPartialRssItem rssItem)
        {
            foreach (var match in (IEnumerable<Match>)this._regex.Matches(rssItem.Title))
            {
                if (match.Groups.ContainsKey("keyword"))
                {
                    yield return match.Groups["keyword"].Value;
                    continue;
                }

                if (match.Groups.ContainsKey("kw"))
                {
                    yield return match.Groups["kw"].Value;
                    continue;
                }

                if (match.Groups.Count > 1)
                {
                    foreach (var gi in Enumerable.Range(1, match.Groups.Count - 1))
                    {
                        yield return match.Groups[gi].Value;
                    }
                    continue;
                }

                yield return match.Groups[0].Value;
            }
        }
    }
}
