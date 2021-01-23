using System;
using System.Net.Http;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using RSSViewer.HttpCacheDb;

namespace RSSViewer.Services
{
    [SupportedOSPlatform("windows")]
    public class HttpService
    {
        private readonly HttpClient _httpClient = new(new WinHttpHandler());
        private readonly IServiceProvider _serviceProvider;

        public HttpService(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public void Dispose() => ((IDisposable)this._httpClient).Dispose();

        public Task<HttpResult<string>> TryGetStringAsync(string uri, bool fallbackToCache = true, CancellationToken token = default)
        {
            if (uri is null)
                throw new ArgumentNullException(nameof(uri));

            return Task.Run(async () =>
            {
                string r = null;
                try
                {
                    r = await this._httpClient.GetStringAsync(uri);
                }
                catch (HttpRequestException) { }

                token.ThrowIfCancellationRequested();

                if (fallbackToCache)
                {
                    using var scope = this._serviceProvider.CreateScope();
                    using var ctx = scope.ServiceProvider.GetRequiredService<HttpCacheDbContext>();

                    if (r is null)
                    {
                        var data = ctx.Requests.Find(uri)?.ResponseBody;
                        if (data is not null)
                        {
                            return new HttpResult<string>(Encoding.UTF8.GetString(data), true);
                        }
                    }
                    else
                    {
                        var data = Encoding.UTF8.GetBytes(r);
                        var e = ctx.Requests.Find(uri);
                        if (e is null)
                        {
                            ctx.Requests.Add(new()
                            {
                                Uri = uri,
                                ResponseBody = data
                            });
                        }
                        else
                        {
                            e.ResponseBody = data;
                        }
                        ctx.SaveChanges();
                        return new HttpResult<string>(r, false);
                    }
                }

                return null;
            }, token);
        }

        public class HttpResult<T>
        {
            public HttpResult(T value, bool fromCache)
            {
                this.Value = value;
                this.FromCache = fromCache;
            }

            public T Value { get; }

            public bool FromCache { get; }
        }
    }
}
