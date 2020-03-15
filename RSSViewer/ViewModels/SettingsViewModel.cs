using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.ViewModels
{
    public class SettingsViewModel
    {
        public AutoRejectSettingsViewModel AutoRejectView { get; } = new AutoRejectSettingsViewModel();

        public void Load()
        {
            var conf = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>().AppConf;
            this.AutoRejectView.Load(conf);
        }

        internal void Save()
        {
            var confService = App.RSSViewerHost.ServiceProvider.GetRequiredService<ConfigService>();
            var conf = confService.AppConf;
            this.AutoRejectView.Save(conf);
            confService.Save();
        }
    }
}
