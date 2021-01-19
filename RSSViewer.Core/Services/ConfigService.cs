﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json.Linq;

using RSSViewer.Configuration;
using RSSViewer.RulesDb;
using RSSViewer.Utils;
using System;
using System.Collections.Generic;
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
                IgnoreNullValues = true,
                WriteIndented = true
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

        public MatchRule CreateMatchRule()
        {
            var rule = new MatchRule
            {
                HandlerId = this._serviceProvider.GetRequiredService<RssItemHandlersService>()
                    .GetDefaultRuleTargetHandler()
                    .Id
            };

            rule.Mode = MatchMode.Contains;
            rule.OptionsAsStringComparison = StringComparison.OrdinalIgnoreCase;
            rule.Argument = string.Empty;
            rule.AutoDisabledAfterLastMatched = TimeSpan.FromDays(365 * 2);
            rule.AutoExpiredAfterLastMatched = TimeSpan.FromDays(365 * 4);

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

        private void RaiseMatchRulesChanged(IEnumerable<MatchRule> changedRules)
        {
            if (changedRules is null)
                throw new ArgumentNullException(nameof(changedRules));

            Task.Run(() =>
            {
                this.MatchRulesChanged?.Invoke(this, new CollectionChangeEventArgs(CollectionChangeAction.Refresh, SortMatchRules(changedRules.ToArray())));
            });
        }

        public async Task UpdateMatchRulesAsync(MatchRule[] updateRules, MatchRule[] addRules, MatchRule[] removeRules)
        {
            if (updateRules is null)
                throw new ArgumentNullException(nameof(updateRules));
            if (addRules is null)
                throw new ArgumentNullException(nameof(addRules));
            if (removeRules is null)
                throw new ArgumentNullException(nameof(removeRules));

            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();

            //ctx.AttachRange(updateRules);
            ctx.UpdateRange(updateRules);

            ctx.AddRange(addRules);

            //ctx.AttachRange(removeRules);
            ctx.RemoveRange(removeRules);

            await ctx.SaveChangesAsync().ConfigureAwait(false);

            this.RaiseMatchRulesChanged(updateRules.Concat(addRules));
        }

        public Task<MatchRule[]> ListMatchRulesAsync() => Task.Run(() => this.ListMatchRules(true));

        /// <summary>
        /// list match rules on sync way.
        /// </summary>
        /// <returns></returns>
        internal MatchRule[] ListMatchRules(bool sort)
        {
            using var scope = this._serviceProvider.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<RulesDbContext>();
            var rules = ctx.MatchRules.ToArray();
            if (sort)
            {
                rules = SortMatchRules(rules);
            }
            if (ctx.UpdateMatchRulesLifetime() > 0)
            {
                ctx.SaveChanges();
            }
            return rules;
        }

        private static MatchRule[] SortMatchRules(MatchRule[] rules)
        {
            if (rules is null)
                throw new ArgumentNullException(nameof(rules));

            var offset = rules.Length + 10;
            return rules.OrderBy(z => z.OrderCode == 0 ? offset : z.OrderCode).ToArray();
        }
    }
}
