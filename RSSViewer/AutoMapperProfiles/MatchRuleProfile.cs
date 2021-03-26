
using AutoMapper;

using RSSViewer.Configuration;
using RSSViewer.RulesDb;
using RSSViewer.ViewModels;

namespace RSSViewer.AutoMapperProfiles
{
    public class MatchRuleProfile : Profile
    {
        public MatchRuleProfile()
        {
            this.CreateMap<MatchRule, MatchRule>()
                .ForMember(z => z.Id, opt => opt.Ignore());
        }
    }
}
