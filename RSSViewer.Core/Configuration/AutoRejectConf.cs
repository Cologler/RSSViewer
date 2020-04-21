using System;
using System.Collections.Generic;
using System.Linq;

namespace RSSViewer.Configuration
{
    public class AutoRejectConf
    {
        public List<MatchStringConf> Matches { get; set; }

        public void Upgrade()
        {
            var now = DateTime.UtcNow;
            var matches = this.Matches ?? (this.Matches = new List<MatchStringConf>());
            this.Matches = matches
                .Where(z => z.ExpiredAt is null || z.ExpiredAt.DateTime > now)
                .ToList();
        }
    }
}
