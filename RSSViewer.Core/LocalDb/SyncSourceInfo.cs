using System.ComponentModel.DataAnnotations;

namespace RSSViewer.LocalDb
{
    public class SyncSourceInfo
    {
        [Key]
        public string SyncSourceId { get; set; }

        public int? LastSyncId { get; set; }
    }
}
