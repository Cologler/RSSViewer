using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Configuration;
using RSSViewer.Utils;
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
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public event Action<AppConf> OnAppConfChanged;

        public ConfigService(IServiceProvider serviceProvider, AppDirService appDir)
        {
            this._jsonSerializerOptions = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                IgnoreNullValues = true
            };
            foreach (var converter in serviceProvider.GetServices<JsonConverter>())
            {
                this._jsonSerializerOptions.Converters.Add(converter);
            }            

            this._appConfPath = appDir.GetDataFileFullPath(AppConfName);

            if (File.Exists(this._appConfPath))
            {
                this.AppConf = JsonSerializer.Deserialize<AppConf>(
                    File.ReadAllText(this._appConfPath, Encoding.UTF8), 
                    this._jsonSerializerOptions);
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
            lock (this._syncRoot)
            {
                FileSystemAtomicOperations.Write(this._appConfPath, JsonSerializer.Serialize(this.AppConf, this._jsonSerializerOptions));
            }

            this.OnAppConfChanged?.Invoke(this.AppConf);
        }

        public MatchStringConf CreateMatchStringConf()
        {
            var conf = new MatchStringConf
            {
                MatchMode = MatchStringMode.Contains,
                AsStringComparison = StringComparison.OrdinalIgnoreCase,
                MatchValue = string.Empty
            };
            if (this.AppConf.Defaults.AutoRejectRulesDisableAfter is TimeSpan da)
            {
                conf.DisableAt = DateTime.UtcNow + da;
            }
            if (this.AppConf.Defaults.AutoRejectRulesExpiredAfter is TimeSpan ea)
            {
                conf.ExpiredAt = DateTime.UtcNow + ea;
            }

            return conf;
        }
    }
}
