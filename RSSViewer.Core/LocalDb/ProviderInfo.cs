using System.ComponentModel.DataAnnotations;

namespace RSSViewer.LocalDb
{
    public class ProviderInfo
    {
        [Key]
        public string ProviderName { get; set; }

        public int? LastSyncId { get; set; }
    }
}
