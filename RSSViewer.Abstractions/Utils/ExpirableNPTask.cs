using System;
using System.Threading.Tasks;

namespace RSSViewer.Utils
{
    public class ExpirableNPTask : NPTask
    {
        private readonly TimeSpan _timeout;
        private DateTime? LastUpdateTime;

        public ExpirableNPTask(TimeSpan timeout, Action action) : base(action)
        {
            this._timeout = timeout;
        }

        public ExpirableNPTask(TimeSpan timeout, Func<Task> actionTask) : base(actionTask)
        {
            this._timeout = timeout;
        }

        public override Task RunAsync()
        {
            if (this.LastUpdateTime != null && DateTime.UtcNow - this.LastUpdateTime < this._timeout)
            {
                return Task.CompletedTask;
            }

            return base.RunAsync();
        }

        protected override void OnAfterRun()
        {
            this.LastUpdateTime = DateTime.UtcNow;

            base.OnAfterRun();
        }
    }
}
