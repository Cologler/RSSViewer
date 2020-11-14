using Microsoft.Extensions.DependencyInjection;

using RSSViewer.Abstractions;
using RSSViewer.Configuration;
using RSSViewer.Utils;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

namespace RSSViewer.Services
{
    public class AcceptHandlerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IViewerLogger _viewerLogger;
        private readonly Dictionary<string, IRssItemHandlerProvider> _sourceProviders;
        private ImmutableArray<IRssItemHandler> _acceptHandlers;

        public AcceptHandlerService(IServiceProvider serviceProvider, IViewerLogger viewerLogger)
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

                this._acceptHandlers = this._serviceProvider.GetServices<IRssItemHandler>()
                    .Concat(dynamicHandlers)
                    .ToImmutableArray();
            }

            this.AcceptHandlersChanged?.Invoke(this, EventArgs.Empty);
        }

        public IReadOnlyCollection<IRssItemHandler> GetHandlers() => this._acceptHandlers;

        public event EventHandler AcceptHandlersChanged;
    }
}
