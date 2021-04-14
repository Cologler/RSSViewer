using AutoMapper;

using Jasily.ViewModel;

using RSSViewer.RulesDb;

namespace RSSViewer.ViewModels
{
    public class TagSnapshotViewModel : Bases.BaseViewModel
    {
        [ModelProperty]
        public string TagGroupName { get; set; }

        [ModelProperty]
        public string TagName { get; set; }

        public static void ConfigureAutoMapperProfile(Profile profile)
        {
            profile.CreateMap<TagSnapshotViewModel, Tag>()
                .AfterMap((v, m) =>
                {
                    m.TagGroupName = string.IsNullOrWhiteSpace(m.TagGroupName) ? null : m.TagGroupName.Trim();
                })
                .ReverseMap()
                .AfterMap((m, v) =>
                {
                    v.RefreshProperties();
                });
        }
    }
}
