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
                this.Matches = new List<string>()
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
