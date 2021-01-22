﻿using RSSViewer.Abstractions;
using RSSViewer.Extensions;
using RSSViewer.Utils;

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace RSSViewer.KeywordsFinders
{
    class MagnetLinkKeywordsFinder : IKeywordsFinder
    {
        public IEnumerable<string> GetKeywords(IPartialRssItem item)
        {
            var magnetLink = item.GetPropertyOrDefault(RssItemProperties.MagnetLink);
            if (!string.IsNullOrWhiteSpace(magnetLink))
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
