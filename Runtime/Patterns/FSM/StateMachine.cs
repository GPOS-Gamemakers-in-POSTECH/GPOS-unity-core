using System;

namespace GPOS.Core.FSM
{
    /// <summary>
    /// Lightweight finite state machine operating on <see cref="IState"/> instances.
    /// Drive it by calling <see cref="Tick"/> every frame and <see cref="ChangeState"/> on transitions.
    /// </summary>
    public class StateMachine
    {
        /// <summary>The state currently being ticked, or null before the first transition.</summary>
        public IState CurrentState { get; private set; }

        /// <summary>Raised after a transition completes. Arguments are (previous, next); previous may be null.</summary>
        public event Action<IState, IState> OnStateChanged;

        /// <summary>
        /// Transitions to <paramref name="newState"/>. Calls OnExit on the previous state and
        /// OnEnter on the new one. No-op if the target is null or already current.
        /// </summary>
        public void ChangeState(IState newState)
        {
            if (newState == null || ReferenceEquals(CurrentState, newState))
                return;

            IState previous = CurrentState;
            previous?.OnExit();

            CurrentState = newState;
            OnStateChanged?.Invoke(previous, newState);

            newState.OnEnter();
        }

        /// <summary>Advances the current state. Safe to call before any state is set.</summary>
        public void Tick(float deltaTime) => CurrentState?.OnUpdate(deltaTime);

        /// <summary>Exits the current state without entering a new one.</summary>
        public void Stop()
        {
            if (CurrentState == null)
                return;

            IState previous = CurrentState;
            previous.OnExit();
            CurrentState = null;
            OnStateChanged?.Invoke(previous, null);
        }
    }
}
