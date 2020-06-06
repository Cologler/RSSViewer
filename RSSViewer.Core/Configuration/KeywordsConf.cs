using System.Collections.Generic;

namespace RSSViewer.Configuration
{
    public class KeywordsConf
    {
        public List<string> Matches { get; set; }

        public List<string> Excludes { get; set; }

        public void Upgrade()
        {
            if (this.Matches == null)
            {
                this.Matches = new List<string>()
                {
                    // for movie
                    // *.2019.*
                    "^(?<kw>.*)\\.\\d{4}\\.(?:.*)$",

                    // for series
                    // *.ep1.* | *.s01e01.*
                    "^(?<kw>.*)\\.(?:ep?\\d{1,3}|s\\d{1,3}e\\d{1,3})\\.(?:.*)$",
                    // *.ep1.* | *.s01e01.* without english name suffix
                    "^(?<kw>.*[^\\. a-z\\d])[\\. a-z\\d]*\\.(?:ep?\\d{1,3}|s\\d{1,3}e\\d{1,3})\\.(?:.*)$",

                    // for animes
                    // *[01]* | *【01】* | *[01-12]* |  *【01-12】* 
                    "^(?<kw>.*)(?:\\[\\d{1,3}(-d{1,3})?\\]|【\\d{1,3}(-d{1,3})?】)(?:.*)$",

                    // for tags
                    "\\[([^\\]]+)\\]",
                };
            }

            if (this.Excludes == null)
            {
                this.Excludes = new List<string>();
            }
        }
    }
}
