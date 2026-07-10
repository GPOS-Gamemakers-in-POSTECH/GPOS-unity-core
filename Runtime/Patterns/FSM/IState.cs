namespace GPOS.Core.FSM
{
    /// <summary>
    /// A single state used by a <see cref="StateMachine"/>.
    /// </summary>
    public interface IState
    {
        /// <summary>Called once when this state becomes the current state.</summary>
        void OnEnter();

        /// <summary>Called every tick while this state is active.</summary>
        void OnUpdate(float deltaTime);

        /// <summary>Called once when the machine leaves this state.</summary>
        void OnExit();
    }
}
