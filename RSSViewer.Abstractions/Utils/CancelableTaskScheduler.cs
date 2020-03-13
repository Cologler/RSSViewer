using System;
using System.Threading;
using System.Threading.Tasks;

namespace RSSViewer.Utils
{
    public class CancelableTaskScheduler
    {
        private CancellationTokenSource _cancellationTokenSource;

        public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> request)
        {
            if (this._cancellationTokenSource != null)
            {
                this._cancellationTokenSource?.Cancel();
                this._cancellationTokenSource.Dispose();
            }

            this._cancellationTokenSource = new CancellationTokenSource();
            return request(this._cancellationTokenSource.Token);
        }

        public Task RunAsync(Func<CancellationToken, Task> request)
        {
            if (this._cancellationTokenSource != null)
            {
                this._cancellationTokenSource?.Cancel();
                this._cancellationTokenSource.Dispose();
            }

            this._cancellationTokenSource = new CancellationTokenSource();
            return request(this._cancellationTokenSource.Token);
        }
    }
}
