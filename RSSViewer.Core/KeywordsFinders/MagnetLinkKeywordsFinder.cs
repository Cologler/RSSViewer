using RSSViewer.Abstractions;
using RSSViewer.Utils;

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RSSViewer.KeywordsFinders
{
    class MagnetLinkKeywordsFinder : IKeywordsFinder
    {
        public IEnumerable<string> GetKeywords(IRssItem item)
        {
            var magnetLink = item.GetProperty(RssItemProperties.MagnetLink);
            if (magnetLink is not null)
            {
                yield return magnetLink;

                if (MagnetLink.TryParse(magnetLink, out var ml))
                {
                    yield return ml.InfoHash;
                }
            }
        }
    }
}
