using IFSEngine.Model;
using IFSEngine.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfDisplay.Models;

namespace WpfDisplay.Helper
{
    public class IFSHistoryTracker
    {
        private Stack<string> UndoStack { get; } = new();
        private Stack<string> RedoStack { get; } = new();

        public bool IsHistoryUndoable => UndoStack.Count > 0;
        public bool IsHistoryRedoable => RedoStack.Count > 0;

        public IFS Undo(IFS current, IEnumerable<Transform> transforms)
        {
            if (!IsHistoryUndoable)
                throw new InvalidOperationException();

            RedoStack.Push(IfsSerializer.SerializeJsonString(current));
            string lastState = UndoStack.Pop();
            IFS restoredObject = IfsSerializer.DeserializeJsonString(lastState, transforms, true);
            return restoredObject;
        }

        public IFS Redo(IFS current, IEnumerable<Transform> transforms)
        {
            if (!IsHistoryRedoable)
                throw new InvalidOperationException();

            UndoStack.Push(IfsSerializer.SerializeJsonString(current));
            string nextState = RedoStack.Pop();
            IFS restoredObject = IfsSerializer.DeserializeJsonString(nextState, transforms, true);
            return restoredObject;
        }

        public void TakeSnapshot(IFS trackedObject)
        {
            string snapshot = IfsSerializer.SerializeJsonString(trackedObject);
            RedoStack.Clear();
            UndoStack.Push(snapshot);
        }
        public void Clear()
        {
            UndoStack.Clear();
            RedoStack.Clear();
        }

    }
}
