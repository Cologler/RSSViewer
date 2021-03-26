using AutoMapper;

using Jasily.ViewModel;

using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Configuration;
using RSSViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private bool _addToQueueTop;

        public DefaultsViewModel DefaultsView { get; } = new();

        public void Load()
        {
            var serviceProvider = App.RSSViewerHost.ServiceProvider;

            var mapper = serviceProvider.GetRequiredService<IMapper>();
            var configService = serviceProvider.GetRequiredService<ConfigService>();

            mapper.Map(configService.AppConf, this);
        }

        internal void Save()
        {
            var serviceProvider = App.RSSViewerHost.ServiceProvider;

            var mapper = serviceProvider.GetRequiredService<IMapper>();
            var configService = serviceProvider.GetRequiredService<ConfigService>();

            mapper.Map(this, configService.AppConf);
            configService.Save();
        }

        public bool AddToQueueTop 
        { 
            get => _addToQueueTop;
            set => this.ChangeModelProperty(ref _addToQueueTop, value);
        }

        public class DefaultsViewModel
        {
            public TimeSpan? AutoRejectRulesExpiredAfter { get; set; }

            public TimeSpan? AutoRejectRulesDisableAfter { get; set; }

            internal void Load(AppConf conf)
            {
                this.AutoRejectRulesExpiredAfter = conf.Defaults.AutoRejectRulesExpiredAfter;
                this.AutoRejectRulesDisableAfter = conf.Defaults.AutoRejectRulesDisableAfter;
            }

            internal void Save(AppConf conf)
            {
                conf.Defaults.AutoRejectRulesExpiredAfter = this.AutoRejectRulesExpiredAfter;
                conf.Defaults.AutoRejectRulesDisableAfter = this.AutoRejectRulesDisableAfter;
            }
        }
    }
}
