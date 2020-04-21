using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace RSSViewer.Configuration
{
    public class MatchStringConf
    {
        public MatchStringMode MatchMode { get; set; }

        public string MatchValue { get; set; }

        public int MatchOptions { get; set; }

        public DateTime? ExpiredAt { get; set; }

        public DateTime? DisableAt { get; set; }

        [JsonIgnore]
        public StringComparison AsStringComparison
        {
            get => (StringComparison) this.MatchOptions;
            set => this.MatchOptions = (int) value;
        }

        [JsonIgnore]
        public RegexOptions AsRegexOptions
        {
            get => (RegexOptions)this.MatchOptions;
            set => this.MatchOptions = (int)value;
        }
    }
}
