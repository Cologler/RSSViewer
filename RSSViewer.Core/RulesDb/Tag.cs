using System.ComponentModel.DataAnnotations;

namespace RSSViewer.RulesDb
{
    public class Tag
    {
        /// <summary>
        /// For backward compatibility, use string type.
        /// </summary>
        [Key]
        public string Id { get; set; }

        public string TagGroupName { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string TagName { get; set; }
    }
}
