using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Configuration;
using RSSViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.ViewModels
{
    public class SettingsViewModel
    {
        public AutoRejectSettingsViewModel AutoRejectView { get; } = new AutoRejectSettingsViewModel();

        public DefaultsViewModel DefaultsView { get; } = new DefaultsViewModel();

        public void Load()
        {
            var conf = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>().AppConf;
            this.AutoRejectView.Load(conf);
            this.DefaultsView.Load(conf);
        }

        internal void Save()
        {
            var confService = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>();
            var conf = confService.AppConf;
            this.AutoRejectView.Save(conf);
            this.DefaultsView.Save(conf);
            confService.Save();
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
