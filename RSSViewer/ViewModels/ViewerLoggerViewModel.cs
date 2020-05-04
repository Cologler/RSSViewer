using Jasily.ViewModel;
using RSSViewer.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace RSSViewer.ViewModels
{
    public class ViewerLoggerViewModel : BaseViewModel, IViewerLogger
    {
        private readonly object _syncRoot = new object();
        private volatile bool _isUpdated = false;
        private Queue<string> _messages = new Queue<string>();

        private void AddMessage(string message)
        {
            this._messages.Enqueue($"[{DateTime.Now.ToLongTimeString()}] {message}");
        }

        private void TrimMessages()
        {
            while (this._messages.Count > 100)
                this._messages.Dequeue();
        }

        private void Updated()
        {
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                lock (this._syncRoot)
                {
                    if (this._isUpdated)
                    {
                        this.RefreshProperties();
                        this._isUpdated = false;
                    }
                }                
            });
        }

        public void AddLine(string line)
        {
            if (line is null)
                throw new ArgumentNullException(nameof(line));
            
            lock (this._syncRoot)
            {
                this.AddMessage(line);
                this._isUpdated = true;
                this.TrimMessages();
            }

            this.Updated();
        }

        public void AddLines(IEnumerable<string> lines)
        {
            if (lines is null)
                throw new ArgumentNullException(nameof(lines));

            lock (this._syncRoot)
            {
                foreach (var line in lines)
                {
                    this.AddMessage(line);
                }
                this._isUpdated = true;
                this.TrimMessages();
            }

            this.Updated();
        }

        [ModelProperty]
        public string MessageText
        {
            get
            { 
                return String.Join("\r\n", this._messages.Reverse());
            }
        }
    }
}
