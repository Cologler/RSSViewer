using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RSSViewer.Utils
{
    public class SingletonTaskFactory
    {
        private readonly object _syncRoot = new object();
        private readonly Action _action;
        private readonly Func<Task> _actionTask;
        private Task _runningTask;

        public SingletonTaskFactory(Action action)
        {
            this._action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public SingletonTaskFactory(Func<Task> actionTask)
        {
            this._actionTask = actionTask ?? throw new ArgumentNullException(nameof(actionTask));
        }

        public virtual Task RunAsync()
        {
            lock (this._syncRoot)
            {
                if (this._runningTask is null)
                {
                    return this._runningTask = Task.Run(async () =>
                    {
                        if (this._action != null)
                        {
                            this._action();
                        }
                        else
                        {
                            await this._actionTask();
                        }

                        this.OnAfterRun();

                        lock (this._syncRoot)
                        {
                            this._runningTask = null;
                        }
                    });
                }
                else
                {
                    return this._runningTask;
                }
            }
        }

        protected virtual void OnAfterRun() { }
    }
}
