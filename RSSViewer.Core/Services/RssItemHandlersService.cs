using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.RssItemHandlers;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace RSSViewer.Services
{
    public class RssItemHandlersService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewerLogger _viewerLogger;
        private readonly Dictionary<string, IRssItemHandlerProvider> _sourceProviders;
        private ImmutableArray<IRssItemHandler> _handlers;

        public RssItemHandlersService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
        {
            this._serviceProvider = serviceProvider;
            this._viewerLogger = viewerLogger;
            this._sourceProviders = serviceProvider.GetServices<IRssItemHandlerProvider>().ToDictionary(z => z.ProviderName);

            var configService = serviceProvider.GetRequiredService<ConfigService>();
            configService.OnAppConfChanged += this.Reload;
            this.Reload(configService.AppConf);
        }

        private void Reload(AppConf appConf)
        {
            using (this._viewerLogger.EnterEvent("Rebuild accept handlers"))
            {
                var missingProviders = new HashSet<string>();
                var dynamicHandlers = appConf.AcceptHandlers.Select(z =>
                    {
                        var providerName = z.Value.ProviderName;
                        var sourceProvider = this._sourceProviders.GetValueOrDefault(providerName);
                        if (sourceProvider != null)
                        {
                            try
                            {
                                var handler = sourceProvider.GetRssItemHandler(z.Key, z.Value.Variables);
                                Debug.Assert(handler.Id == z.Key);
                                return handler;
                            }
                            catch (VariablesHelper.MissingRequiredVariableException e)
                            {
                                this._viewerLogger.AddLine(
                                    $"Accept handler \"{ z.Key}\": missing required variable \"{e.VariableName}\"");
                            }
                            catch (VariablesHelper.UnableConvertVariableException e)
                            {
                                this._viewerLogger.AddLine(
                                    $"Accept handler \"{ z.Key}\": unable convert {e.FromValue} to {e.ToType.Name}");
                            }
                        }
                        else
                        {
                            if (missingProviders.Add(providerName))
                            {
                                this._viewerLogger.AddLine($"Missing accept handler provider: {providerName}");
                            }
                        }

                        return null;
                    })
                    .Where(z => z != null)
                    .ToArray();

                this._handlers = this._serviceProvider.GetServices<IRssItemHandler>()
                    .Concat(dynamicHandlers)
                    .ToImmutableArray();
            }

            this.AcceptHandlersChanged?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyCollection<IRssItemHandler> GetHandlers() => this._handlers;

        public IReadOnlyCollection<IRssItemHandler> GetRuleTargetHandlers() => this._handlers.Where(z => z.CanbeRuleTarget).ToList();

        /// <summary>
        /// Return <see langword="null"/> if the handler is deleted by user.
        /// </summary>
        /// <param name="handlerId"></param>
        /// <returns></returns>
        public IRssItemHandler GetRuleTargetHandler(string handlerId)
        {
            handlerId = string.IsNullOrEmpty(handlerId) ? KnownHandlerIds.DefaultHandlerId : handlerId;
            return this.GetRuleTargetHandlers().FirstOrDefault(z => z.Id == handlerId);
        }

        public IRssItemHandler GetDefaultRuleTargetHandler()
        {
            return this._handlers.Where(z => z.Id == KnownHandlerIds.DefaultHandlerId).Single();
        }

        public event EventHandler AcceptHandlersChanged;
    }
}
