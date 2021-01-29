using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.Configuration
{
    public class AppConf
    {
        public GroupConf Group { get; set; }

        public KeywordsConf Keywords { get; set; }

        public Dictionary<string, SyncSourceSection> SyncSources { get; set; }

        public Dictionary<string, AcceptHandlerSection> AcceptHandlers { get; set; }

        public DefaultsConf Defaults { get; set; }

        public void Upgrade()
        {
            (this.Group ?? (this.Group = new GroupConf())).Upgrade();
            (this.Keywords ?? (this.Keywords = new KeywordsConf())).Upgrade();
            if (this.SyncSources is null)
                this.SyncSources = new Dictionary<string, SyncSourceSection>();
            if (AcceptHandlers is null)
                this.AcceptHandlers = new Dictionary<string, AcceptHandlerSection>();
            if (this.Defaults is null)
                this.Defaults = new DefaultsConf();
        }
    }
}
