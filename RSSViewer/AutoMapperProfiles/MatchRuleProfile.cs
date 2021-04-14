
using AutoMapper;

using RSSViewer.RulesDb;

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
