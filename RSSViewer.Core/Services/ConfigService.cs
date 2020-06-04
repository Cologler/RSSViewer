using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RSSViewer.Configuration;
using RSSViewer.RulesDb;
using RSSViewer.Utils;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace RSSViewer.Services
{
    public class ConfigService
    {
        private const string AppConfName = "app-conf.json";

        private readonly object _syncRoot = new object();
        private readonly string _appConfPath;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly IServiceProvider _serviceProvider;

        public event Action<AppConf> OnAppConfChanged;
        public event CollectionChangeEventHandler MatchRulesChanged;

        public ConfigService(IServiceProvider serviceProvider, AppDirService appDir)
        {
            this._serviceProvider = serviceProvider;

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

        public MatchRule CreateMatchRule(MatchAction matchAction)
        {
            var rule = new MatchRule
            {
                Action = matchAction,
            };

            switch (matchAction)
            {
                case MatchAction.Reject:
                    rule.Mode = MatchMode.Contains;
                    rule.OptionsAsStringComparison = StringComparison.OrdinalIgnoreCase;
                    rule.Argument = string.Empty;
                    rule.AutoDisabledAfterLastMatched = TimeSpan.FromDays(365 * 2);
                    rule.AutoExpiredAfterLastMatched = TimeSpan.FromDays(365 * 4);
                    break;

                case MatchAction.Accept:
                    break;
            }

            rule.LastMatched = DateTime.UtcNow;

            return rule;
        }

        public async Task AddMatchRuleAsync(MatchRule matchRule)
        {
            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            ctx.Add(matchRule);
            await ctx.SaveChangesAsync().ConfigureAwait(false);
            this.MatchRulesChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Add, matchRule));
        }

        public async Task RemoveMatchRuleAsync(MatchRule matchRule)
        {
            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            ctx.Attach(matchRule);
            ctx.Remove(matchRule);
            await ctx.SaveChangesAsync().ConfigureAwait(false);
            this.MatchRulesChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Remove, matchRule));
        }

        public async Task ReplaceMatchRulesAsync(MatchRule[] matchRules)
        {
            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            var ids = matchRules.Select(z => z.Id).ToHashSet();

            foreach (var rule in matchRules)
            {
                if (rule.Id == 0)
                {
                    ctx.Add(rule);
                }
                else
                {
                    ctx.Attach(rule);
                    ctx.Update(rule);
                }
            }

            var eInDb = ctx.MatchRules.ToDictionary(z => z.Id, z => z);

            var removed = eInDb
                .Where(z => !ids.Contains(z.Key))
                .Select(z => z.Value)
                .ToArray();
            ctx.RemoveRange(removed);

            await ctx.SaveChangesAsync().ConfigureAwait(false);

            this.MatchRulesChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Refresh, matchRules));
        }

        public async Task<MatchRule[]> ListMatchRulesAsync()
        {
            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            var items = await ctx.MatchRules.ToArrayAsync().ConfigureAwait(false);
            var offset = items.Length + 10;
            return items.OrderBy(z => z.OrderCode == 0 ? offset : z.OrderCode).ToArray();
        }

        internal MatchRule[] ListMatchRules()
        {
            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            return ctx.MatchRules.ToArray();
        }
    }
}
