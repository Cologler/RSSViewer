using RSSViewer.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.KeywordsFinders
{
    class TitleKeywordsFinder : IKeywordsFinder
    {
        public IEnumerable<string> GetKeywords(IRssItem item)
        {
            yield return item.Title;
        }
    }

    class MagnetLinkKeywordsFinder : IKeywordsFinder
    {
        public IEnumerable<string> GetKeywords(IRssItem item)
        {
            yield return item.GetProperty(RssItemProperties.MagnetLink);
        }
    }
}
