using System;

namespace GPOS.Core.FSM
{
    /// <summary>
    /// Delegate-based <see cref="IState"/> so simple states can be created inline
    /// without declaring a new class.
    /// </summary>
    public class State : IState
    {
        private readonly Action _onEnter;
        private readonly Action<float> _onUpdate;
        private readonly Action _onExit;

        public State(Action onEnter = null, Action<float> onUpdate = null, Action onExit = null)
        {
            _onEnter = onEnter;
            _onUpdate = onUpdate;
            _onExit = onExit;
        }

        public void OnEnter() => _onEnter?.Invoke();
        public void OnUpdate(float deltaTime) => _onUpdate?.Invoke(deltaTime);
        public void OnExit() => _onExit?.Invoke();
    }
}
