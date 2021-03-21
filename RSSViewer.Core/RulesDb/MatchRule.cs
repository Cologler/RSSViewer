
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace RSSViewer.RulesDb
{
    public class MatchRule
    {
        /// <summary>
        /// The Id of this <see cref="MatchRule"/>
        /// </summary>
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// The Id of the parent <see cref="MatchRule"/>
        /// </summary>
        public int? ParentId { get; set; }

        public MatchMode Mode { get; set; }

        /// <summary>
        /// The match argument
        /// </summary>
        public string Argument { get; set; }

        public bool IgnoreCase { get; set; }

        public string HandlerId { get; set; }

        public bool IsDisabled { get; set; }

        /// <summary>
        /// After the time, <see cref="MatchRule"/> will be removed from database.
        /// </summary>
        public TimeSpan? AutoExpiredAfterLastMatched { get; set; }

        /// <summary>
        /// After the time, <see cref="MatchRule"/> will auto set to <see cref="IsDisabled"/>.
        /// </summary>
        public TimeSpan? AutoDisabledAfterLastMatched { get; set; }

        /// <summary>
        /// The last matched time or create time. (UTC)
        /// </summary>
        public DateTime LastMatched { get; set; }

        public int TotalMatchedCount { get; set; }

        public int OrderCode { get; set; }

        public string OnFeedId { get; set; }

        public bool IsRootRule() => this.ParentId is null;

        /// <summary>
        /// a string repr for debug.
        /// </summary>
        /// <returns></returns>
        public string ToDebugString() => $"({this.Mode}) {this.Argument}";
    }
}
