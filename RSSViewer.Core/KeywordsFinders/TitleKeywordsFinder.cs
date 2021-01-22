using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.KeywordsFinders
{
    class TitleKeywordsFinder : IKeywordsFinder
    {
        public IEnumerable<string> GetKeywords(IPartialRssItem item)
        {
            yield return item.Title;
        }
    }
}
