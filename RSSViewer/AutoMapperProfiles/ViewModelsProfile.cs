
using AutoMapper;

using RSSViewer.Configuration;
using RSSViewer.ViewModels;
using RSSViewer.Windows;

namespace RSSViewer.AutoMapperProfiles
{
    public class ViewModelsProfile : Profile
    {
        public ViewModelsProfile()
        {
            this.CreateMap<AppConf, SettingsViewModel>()
                .ReverseMap();

            EditRuleWindow.ConfigureAutoMapperProfile(this);
            TagSnapshotViewModel.ConfigureAutoMapperProfile(this);
        }
    }
}
