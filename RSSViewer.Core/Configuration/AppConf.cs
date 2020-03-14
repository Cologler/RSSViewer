using System;
using System.Text;

namespace RSSViewer.Configuration
{
    public class AppConf
    {
        public GroupConf Group { get; set; }

        public KeywordsConf Keywords { get; set; }

        public AutoRejectConf AutoReject { get; set; }

        public void Upgrade()
        {
            (this.Group ?? (this.Group = new GroupConf())).Upgrade();
            (this.Keywords ?? (this.Keywords = new KeywordsConf())).Upgrade();
            (this.AutoReject ?? (this.AutoReject = new AutoRejectConf())).Upgrade();
        }
    }
}
