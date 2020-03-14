using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.Configuration
{
    public class AppConf
    {
        public GroupConf Group { get; set; }

        public void Upgrade()
        {
            if (this.Group == null)
            {
                this.Group = new GroupConf();                
            }

            this.Group.Upgrade();
        }
    }

    public class GroupConf
    {
        public List<string> FindGroupRegexes { get; set; }

        public void Upgrade()
        {
            if (this.FindGroupRegexes == null)
            {
                this.FindGroupRegexes = new List<string>()
                {
                    // *.ep1.* | *.s01e01.*
                    "^(?<name>.+)\\.(ep?\\d{1,3}|s\\d{1,3}e\\d{1,3})\\.(.*)$",

                    // *[01-12]*
                    "^(?<name>.+)\\[\\d{1,3}(?:-\\d{1,3})?\\](?:.*)$",

                    // *【01-12】*
                    "^(?<name>.+)【\\d{1,3}(?:-\\d{1,3})?】(?:.*)$",

                    // [*] * - 01 [*
                    "^\\[.+\\] (?<name>.+) - \\d{1,3} \\[.+$",
                };
            }
        }
    }
}
