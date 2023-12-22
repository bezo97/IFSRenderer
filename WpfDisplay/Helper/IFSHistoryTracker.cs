using System;
using System.Collections.Generic;

using IFSEngine.Model;

using WpfDisplay.Serialization;

namespace WpfDisplay.Helper;

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

        RedoStack.Push(IfsNodesSerializer.SerializeJsonString(current));
        string lastState = UndoStack.Pop();
        IFS restoredObject = IfsNodesSerializer.DeserializeJsonString(lastState, transforms, true);
        return restoredObject;
    }

    public IFS Redo(IFS current, IEnumerable<Transform> transforms)
    {
        if (!IsHistoryRedoable)
            throw new InvalidOperationException();

        UndoStack.Push(IfsNodesSerializer.SerializeJsonString(current));
        string nextState = RedoStack.Pop();
        IFS restoredObject = IfsNodesSerializer.DeserializeJsonString(nextState, transforms, true);
        return restoredObject;
    }

    public void TakeSnapshot(IFS trackedObject)
    {
        string snapshot = IfsNodesSerializer.SerializeJsonString(trackedObject);
        RedoStack.Clear();
        UndoStack.Push(snapshot);
    }
    public void Clear()
    {
        UndoStack.Clear();
        RedoStack.Clear();
    }

}
