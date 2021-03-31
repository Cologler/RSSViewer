using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        }
    }
}
