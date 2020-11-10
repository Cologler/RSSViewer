using System;
using System.Collections.Generic;
using System.Text;

namespace RSSViewer.Utils
{
    /// <summary>
    /// Use this handle to wrap the immutable collections to avoid read or write the partial struct.
    /// </summary>
    sealed class SafeHandle<T>
    {
        public readonly object SyncRoot = new object();
        private T _value;

        public T Value
        {
            get
            {
                lock (this.SyncRoot)
                    return this._value;
            }

            set
            {
                lock (this.SyncRoot)
                    this._value = value;
            }
        }

        public void Change(Func<T, T> func)
        {
            if (func is null)
                throw new ArgumentNullException(nameof(func));
            lock (this.SyncRoot)
                this._value = func(this._value);
        }
    }
}
