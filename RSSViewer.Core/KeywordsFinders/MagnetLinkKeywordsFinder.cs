using RSSViewer.Abstractions;

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RSSViewer.KeywordsFinders
{
    class MagnetLinkKeywordsFinder : IKeywordsFinder
    {
        private static readonly Regex BtihUrnRegex = new Regex("urn:btih:(?<btih>[0-9a-f]{32}(?:[0-9a-f]{8})?)(?:$|&)", RegexOptions.IgnoreCase); 

        public IEnumerable<string> GetKeywords(IRssItem item)
        {
            var magnetLink = item.GetProperty(RssItemProperties.MagnetLink);
            if (magnetLink is not null)
            {
                yield return magnetLink;

                var match = BtihUrnRegex.Match(magnetLink);
                if (match.Success)
                {
                    yield return match.Groups["btih"].Value;
                }
            }
        }
    }
}
