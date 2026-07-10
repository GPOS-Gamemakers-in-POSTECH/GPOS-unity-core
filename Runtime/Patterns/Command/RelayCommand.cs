using System;

namespace GPOS.Core.Command
{
    /// <summary>
    /// Delegate-based <see cref="ICommand"/> so commands can be created inline without a new class.
    /// If no undo delegate is supplied, <see cref="Undo"/> is a no-op.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Action _undo;

        public RelayCommand(Action execute, Action undo = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _undo = undo;
        }

        public void Execute() => _execute();
        public void Undo() => _undo?.Invoke();
    }
}
