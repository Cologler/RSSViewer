using System.ComponentModel.DataAnnotations;

namespace RSSViewer.HttpCacheDb
{
    public class CachedRequest
    {
        [Key]
        public string Uri { get; set; }

        public byte[] ResponseBody { get; set; }
    }
}
