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

        private readonly object _syncRoot = new object();
        private readonly string _appConfPath;

        public event Action<AppConf> OnAppConfChanged;

        public ConfigService(AppDirService appDir)
        {
            this._appConfPath = appDir.GetDataFileFullPath(AppConfName);

            if (File.Exists(this._appConfPath))
            {
                this.AppConf = JsonSerializer.Deserialize<AppConf>(File.ReadAllText(this._appConfPath, Encoding.UTF8));
            }
            else
            {
                this.AppConf = new AppConf();
            }

            this.AppConf.Upgrade();
            this.Save();
        }

        public AppConf AppConf { get; }

        public void Save()
        {
            var jso = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            lock (this._syncRoot)
            {
                File.WriteAllText(this._appConfPath, JsonSerializer.Serialize(this.AppConf, jso));
            }
            
            this.OnAppConfChanged?.Invoke(this.AppConf);
        }
    }
}
