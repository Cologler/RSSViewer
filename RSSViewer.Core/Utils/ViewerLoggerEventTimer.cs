using RSSViewer.Abstractions;
using System;
using System.Diagnostics;

namespace RSSViewer.Utils
{
    public struct ViewerLoggerEventTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch;
        private readonly IViewerLogger _viewerLogger;
        private readonly string _eventName;

        public ViewerLoggerEventTimer(IViewerLogger viewerLogger, string eventName)
        {
            this._viewerLogger = viewerLogger ?? throw new ArgumentNullException(nameof(viewerLogger));
            this._eventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
            this._stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (this._stopwatch is null)
                return;

            this._stopwatch.Stop();

            this._viewerLogger.AddLine($"{this._eventName} takes {this._stopwatch.Elapsed.TotalSeconds}s.");
        }
    }
}
