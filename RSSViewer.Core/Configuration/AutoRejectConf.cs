using System.Collections.Generic;

namespace RSSViewer.Configuration
{
    public class AutoRejectConf
    {
        public List<MatchStringConf> Matches { get; set; }

        public void Upgrade()
        {
            _ = this.Matches ?? (this.Matches = new List<MatchStringConf>());
        }
    }
}
