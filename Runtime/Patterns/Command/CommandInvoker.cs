using System.Collections.Generic;

namespace GPOS.Core.Command
{
    /// <summary>
    /// Executes <see cref="ICommand"/>s while maintaining undo/redo history.
    /// Executing a new command clears the redo stack, matching typical editor behaviour.
    /// </summary>
    public class CommandInvoker
    {
        private readonly Stack<ICommand> _undoStack = new();
        private readonly Stack<ICommand> _redoStack = new();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;
        public int UndoCount => _undoStack.Count;
        public int RedoCount => _redoStack.Count;

        /// <summary>Runs the command and pushes it onto the undo stack.</summary>
        public void Execute(ICommand command)
        {
            if (command == null)
                return;

            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
        }

        /// <summary>Undoes the most recent command, if any.</summary>
        public void Undo()
        {
            if (_undoStack.Count == 0)
                return;

            ICommand command = _undoStack.Pop();
            command.Undo();
            _redoStack.Push(command);
        }

        /// <summary>Re-executes the most recently undone command, if any.</summary>
        public void Redo()
        {
            if (_redoStack.Count == 0)
                return;

            ICommand command = _redoStack.Pop();
            command.Execute();
            _undoStack.Push(command);
        }

        /// <summary>Discards all undo/redo history.</summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }
    }
}
