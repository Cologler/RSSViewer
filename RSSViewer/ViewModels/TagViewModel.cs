using RSSViewer.RulesDb;
using RSSViewer.ViewModels.Bases;

namespace RSSViewer.ViewModels
{
    public class TagViewModel : BaseViewModel
    {
        public TagViewModel(Tag tag)
        {
            this.Tag = tag;
        }

        public Tag Tag { get; }

        public string Name => this.Tag.TagName;
    }
}
