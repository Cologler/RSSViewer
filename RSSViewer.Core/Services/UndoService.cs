
using RSSViewer.Abstractions;

using System;
using System.Collections.Generic;

namespace RSSViewer.Services
{
    public class UndoService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Stack<IUndoable> _operations = new();
        private readonly object _syncRoot = new();

        public UndoService(IServiceProvider serviceProvider) => this._serviceProvider = serviceProvider;

        public void Push(IUndoable undoable)
        {
            lock (this._syncRoot)
            {
                this._operations.Push(undoable);
            }
        }

        public void Undo()
        {
            IUndoable undoable;

            lock (this._syncRoot)
            {
                if (this._operations.Count == 0)
                    return;

                undoable = this._operations.Pop();
            }

            undoable.Undo(this._serviceProvider);
        }
    }
}
