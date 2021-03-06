﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

using RSSViewer.Abstractions;
using RSSViewer.Utils;

namespace RSSViewer
{
    public static class EventNames
    {
        public static readonly TypedEventName<IReadOnlyCollection<(IPartialRssItem, RssItemState)>> RssItemsStateChanged =
            new(nameof(RssItemsStateChanged));

        public static readonly TypedEventName<IReadOnlyCollection<IPartialRssItem>> AddedRssItems =
            new(nameof(AddedRssItems));
    }
}
