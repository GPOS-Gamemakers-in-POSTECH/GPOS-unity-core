using GPOS.Core.FSM;
using NUnit.Framework;

namespace GPOS.Core.Tests
{
    public class StateMachineTests
    {
        [Test]
        public void 상태_전환_시_Exit_와_Enter_가_순서대로_호출된다()
        {
            var log = new System.Collections.Generic.List<string>();
            var a = new State(onEnter: () => log.Add("a.enter"), onExit: () => log.Add("a.exit"));
            var b = new State(onEnter: () => log.Add("b.enter"));

            var fsm = new StateMachine();
            fsm.ChangeState(a);
            fsm.ChangeState(b);

            CollectionAssert.AreEqual(new[] { "a.enter", "a.exit", "b.enter" }, log);
        }

        [Test]
        public void Tick_은_현재_상태만_업데이트한다()
        {
            float received = 0;
            var state = new State(onUpdate: dt => received += dt);

            var fsm = new StateMachine();
            fsm.Tick(1f); // 상태 없음 — 안전해야 함
            fsm.ChangeState(state);
            fsm.Tick(0.5f);

            Assert.AreEqual(0.5f, received);
        }

        [Test]
        public void 같은_상태로의_전환은_무시된다()
        {
            int enterCount = 0;
            var state = new State(onEnter: () => enterCount++);

            var fsm = new StateMachine();
            fsm.ChangeState(state);
            fsm.ChangeState(state);

            Assert.AreEqual(1, enterCount);
        }

        [Test]
        public void OnStateChanged_이벤트가_발생한다()
        {
            var fsm = new StateMachine();
            IState from = null, to = null;
            fsm.OnStateChanged += (prev, next) => { from = prev; to = next; };

            var state = new State();
            fsm.ChangeState(state);

            Assert.IsNull(from);
            Assert.AreSame(state, to);
        }

        [Test]
        public void Stop_은_현재_상태를_종료한다()
        {
            bool exited = false;
            var state = new State(onExit: () => exited = true);

            var fsm = new StateMachine();
            fsm.ChangeState(state);
            fsm.Stop();

            Assert.IsTrue(exited);
            Assert.IsNull(fsm.CurrentState);
        }
    }
}
