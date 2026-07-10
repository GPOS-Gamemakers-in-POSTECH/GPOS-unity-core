using System.Text.RegularExpressions;
using GPOS.Core.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GPOS.Core.Tests
{
    public class EventBusTests
    {
        private struct TestEvent
        {
            public int Value;
        }

        [TearDown]
        public void TearDown() => EventBus.ClearAll();

        [Test]
        public void 구독자가_이벤트를_수신한다()
        {
            int received = 0;
            EventBus.Subscribe<TestEvent>(e => received = e.Value);

            EventBus.Publish(new TestEvent { Value = 42 });

            Assert.AreEqual(42, received);
        }

        [Test]
        public void 구독_해제_후에는_수신하지_않는다()
        {
            int count = 0;
            void Handler(TestEvent e) => count++;

            EventBus.Subscribe<TestEvent>(Handler);
            EventBus.Unsubscribe<TestEvent>(Handler);

            EventBus.Publish(new TestEvent());

            Assert.AreEqual(0, count);
        }

        [Test]
        public void 여러_구독자가_모두_수신한다()
        {
            int count = 0;
            EventBus.Subscribe<TestEvent>(_ => count++);
            EventBus.Subscribe<TestEvent>(_ => count++);

            EventBus.Publish(new TestEvent());

            Assert.AreEqual(2, count);
        }

        [Test]
        public void 구독자_하나가_예외를_던져도_나머지는_호출된다()
        {
            int count = 0;
            EventBus.Subscribe<TestEvent>(_ => throw new System.Exception("boom"));
            EventBus.Subscribe<TestEvent>(_ => count++);

            // 예외를 삼키는 대신 에러 로그를 남기므로, 테스트에서는 해당 로그를 기대 처리합니다.
            LogAssert.Expect(LogType.Error, new Regex("EventBus.*threw"));

            EventBus.Publish(new TestEvent());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void 구독자가_없어도_Publish_는_안전하다()
        {
            Assert.DoesNotThrow(() => EventBus.Publish(new TestEvent()));
        }

        [Test]
        public void Clear_는_해당_타입만_지운다()
        {
            int a = 0, b = 0;
            EventBus.Subscribe<TestEvent>(_ => a++);
            EventBus.Subscribe<int>(_ => b++);

            EventBus.Clear<TestEvent>();
            EventBus.Publish(new TestEvent());
            EventBus.Publish(1);

            Assert.AreEqual(0, a);
            Assert.AreEqual(1, b);
        }
    }
}
