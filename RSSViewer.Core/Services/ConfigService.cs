using RSSViewer.Configuration;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RSSViewer.Services
{
    public class ConfigService
    {
        private const string AppConfName = "app-conf.json";

        private readonly string _appConfPath;

        public event Action<AppConf> OnAppConfChanged;

        public ConfigService(AppDirService appDir)
        {
            this._appConfPath = appDir.GetDataFileFullPath(AppConfName);

            if (File.Exists(this._appConfPath))
            {
                this.App = JsonSerializer.Deserialize<AppConf>(File.ReadAllText(this._appConfPath, Encoding.UTF8));
            }
            else
            {
                this.App = new AppConf();
            }

            this.App.Upgrade();
            this.Save();
        }

        public AppConf App { get; }

        public void Save()
        {
            File.WriteAllText(this._appConfPath, JsonSerializer.Serialize(this.App));
            this.OnAppConfChanged?.Invoke(this.App);
        }
    }
}
