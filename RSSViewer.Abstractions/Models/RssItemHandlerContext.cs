﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RSSViewer.Abstractions;

namespace RSSViewer.Models
{
    public class RssItemHandlerContext : IRssItemHandlerContext
    {
        public RssItemHandlerContext(IPartialRssItem rssItem) =>
            this.RssItem = rssItem ?? throw new ArgumentNullException(nameof(rssItem));

        public IPartialRssItem RssItem { get; }

        public RssItemState OldState { get; set; }

        public RssItemState? NewState { get; set; }
    }
}
