namespace GPOS.Core.Command
{
    /// <summary>
    /// An action that can be executed and reversed, used with <see cref="CommandInvoker"/>.
    /// </summary>
    public interface ICommand
    {
        /// <summary>Performs the action.</summary>
        void Execute();

        /// <summary>Reverses the effect of <see cref="Execute"/>.</summary>
        void Undo();
    }
}
