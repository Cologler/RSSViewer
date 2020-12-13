using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Configuration;
using RSSViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.ViewModels
{
    public class SettingsViewModel
    {
        public AutoRulesViewModel AutoRejectView { get; } = new AutoRulesViewModel();

        public DefaultsViewModel DefaultsView { get; } = new DefaultsViewModel();

        public async Task Load()
        {
            var configService = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>();
            this.DefaultsView.Load(configService.AppConf);
            await this.AutoRejectView.Load(configService);
        }

        internal void Save()
        {
            var configService = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>();
            this.AutoRejectView.Save(configService);
            this.DefaultsView.Save(configService.AppConf);
            configService.Save();
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
