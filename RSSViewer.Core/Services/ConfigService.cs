using RSSViewer.Configuration;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RSSViewer.Services
{
    public class ConfigService
    {
        private const string AppConfPath = "app-conf.json";

        public ConfigService()
        {
            if (File.Exists(AppConfPath))
            {
                this.App = JsonSerializer.Deserialize<AppConf>(File.ReadAllText(AppConfPath, Encoding.UTF8));
            }
            else
            {
                this.App = new AppConf();
            }

            this.App.Upgrade();
        }

        public AppConf App { get; }

        public void Save()
        {
            File.WriteAllText(AppConfPath, JsonSerializer.Serialize(this.App));
        }
    }
}
