using System.Collections.Generic;

namespace RSSViewer.Configuration
{
    public class GroupConf
    {
        public List<string> Matches { get; set; }

        public void Upgrade()
        {
            if (this.Matches == null)
            {
                this.Matches = new ();
            }
        }
    }
}
