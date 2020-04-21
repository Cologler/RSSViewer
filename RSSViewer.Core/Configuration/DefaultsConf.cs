using System;

namespace RSSViewer.Configuration
{
    public class DefaultsConf
    {
        public TimeSpan? AutoRejectRulesExpiredAfter { get; set; }

        public TimeSpan? AutoRejectRulesDisableAfter { get; set; }
    }
}
