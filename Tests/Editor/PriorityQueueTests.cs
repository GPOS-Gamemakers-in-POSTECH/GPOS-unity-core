using System.Collections.Generic;
using GPOS.Core.Collections;
using NUnit.Framework;

namespace GPOS.Core.Tests
{
    public class PriorityQueueTests
    {
        [Test]
        public void 작은_값이_먼저_나온다()
        {
            var pq = new PriorityQueue<int>();
            pq.Enqueue(5);
            pq.Enqueue(1);
            pq.Enqueue(3);

            Assert.AreEqual(1, pq.Dequeue());
            Assert.AreEqual(3, pq.Dequeue());
            Assert.AreEqual(5, pq.Dequeue());
        }

        [Test]
        public void 커스텀_비교자로_최대_힙을_만들_수_있다()
        {
            var maxHeap = new PriorityQueue<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
            maxHeap.Enqueue(1);
            maxHeap.Enqueue(5);
            maxHeap.Enqueue(3);

            Assert.AreEqual(5, maxHeap.Dequeue());
            Assert.AreEqual(3, maxHeap.Dequeue());
        }

        [Test]
        public void 빈_큐에서_Dequeue_는_예외를_던진다()
        {
            var pq = new PriorityQueue<int>();
            Assert.Throws<System.InvalidOperationException>(() => pq.Dequeue());
        }

        [Test]
        public void TryDequeue_와_TryPeek_는_빈_큐에서_false_를_반환한다()
        {
            var pq = new PriorityQueue<int>();
            Assert.IsFalse(pq.TryDequeue(out _));
            Assert.IsFalse(pq.TryPeek(out _));
        }

        [Test]
        public void Peek_은_제거하지_않는다()
        {
            var pq = new PriorityQueue<int>();
            pq.Enqueue(2);

            Assert.AreEqual(2, pq.Peek());
            Assert.AreEqual(1, pq.Count);
        }

        [Test]
        public void 많은_값을_넣어도_정렬_순서를_유지한다()
        {
            var pq = new PriorityQueue<int>();
            int[] values = { 9, 4, 7, 1, 8, 2, 6, 3, 5, 0, 9, 4 };
            foreach (int v in values)
                pq.Enqueue(v);

            int previous = int.MinValue;
            while (pq.Count > 0)
            {
                int current = pq.Dequeue();
                Assert.GreaterOrEqual(current, previous);
                previous = current;
            }
        }
    }
}
