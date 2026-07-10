using GPOS.Core;
using GPOS.Core.FSM;
using UnityEngine;

namespace GPOS.Core.Samples
{
    /// <summary>
    /// FSM 예제 — 2초마다 Idle / Patrol 상태를 오가는 간단한 AI.
    /// 씬에 배치하고 플레이한 뒤 콘솔을 확인하세요.
    /// </summary>
    public class FsmExample : MonoBehaviour
    {
        private readonly StateMachine _fsm = new();
        private IState _idle, _patrol;
        private float _timer;

        private void Awake()
        {
            _idle = new State(
                onEnter: () => D.Log("[Sample/FSM] Idle 시작"),
                onUpdate: dt =>
                {
                    _timer += dt;
                    if (_timer > 2f) _fsm.ChangeState(_patrol);
                },
                onExit: () => _timer = 0f);

            _patrol = new State(
                onEnter: () => D.Log("[Sample/FSM] Patrol 시작"),
                onUpdate: dt =>
                {
                    _timer += dt;
                    transform.Translate(Vector3.right * Mathf.Sin(Time.time) * dt);
                    if (_timer > 2f) _fsm.ChangeState(_idle);
                },
                onExit: () => _timer = 0f);

            _fsm.OnStateChanged += (prev, next) => D.Log("[Sample/FSM] 상태 전환됨");
            _fsm.ChangeState(_idle);
        }

        private void Update() => _fsm.Tick(Time.deltaTime);
    }
}
