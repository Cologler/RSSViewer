
using RSSViewer.RulesDb;

namespace RSSViewer.ViewModels
{
    public class TagViewModel : Bases.BaseViewModel
    {
        public TagViewModel(Tag tag)
        {
            this.Tag = tag;
        }

        public Tag Tag { get; }

        public string Name => this.Tag.TagName;

        public bool IsChanged { get; set; }

        public bool IsAdded { get; set; }
    }
}
