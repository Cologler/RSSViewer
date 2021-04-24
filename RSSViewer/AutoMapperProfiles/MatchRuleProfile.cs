
using AutoMapper;

using RSSViewer.RulesDb;
using RSSViewer.Windows;

namespace RSSViewer.AutoMapperProfiles
{
    public class MatchRuleProfile : Profile
    {
        public MatchRuleProfile()
        {
            this.CreateMap<MatchRule, MatchRule>()
                .ForMember(z => z.Id, opt => opt.Ignore())
                // ensure not deep copy
                .ForMember(z => z.Parent, opt => opt.Ignore())
                .AfterMap((s, d) => d.Parent = s.Parent)
                ;
            

            EditRuleWindow.ConfigureAutoMapperProfile(this);
        }
    }
}
