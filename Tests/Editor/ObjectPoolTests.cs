using GPOS.Core.Pool;
using NUnit.Framework;

namespace GPOS.Core.Tests
{
    public class ObjectPoolTests
    {
        private class Dummy : IPoolable
        {
            public int SpawnCount;
            public int DespawnCount;

            public void OnSpawn() => SpawnCount++;
            public void OnDespawn() => DespawnCount++;
        }

        [Test]
        public void 반환한_객체가_재사용된다()
        {
            var pool = new ObjectPool<Dummy>(() => new Dummy());

            Dummy first = pool.Get();
            pool.Release(first);
            Dummy second = pool.Get();

            Assert.AreSame(first, second);
        }

        [Test]
        public void Prewarm_만큼_미리_생성된다()
        {
            int created = 0;
            var pool = new ObjectPool<Dummy>(() => { created++; return new Dummy(); }, prewarmCount: 3);

            Assert.AreEqual(3, created);
            Assert.AreEqual(3, pool.CountInactive);
        }

        [Test]
        public void MaxSize_초과_반환분은_버려진다()
        {
            var pool = new ObjectPool<Dummy>(() => new Dummy(), maxSize: 1);

            pool.Release(new Dummy());
            pool.Release(new Dummy());

            Assert.AreEqual(1, pool.CountInactive);
        }

        [Test]
        public void IPoolable_콜백이_호출된다()
        {
            var pool = new ObjectPool<Dummy>(() => new Dummy());

            Dummy item = pool.Get();
            Assert.AreEqual(1, item.SpawnCount);

            pool.Release(item);
            Assert.AreEqual(1, item.DespawnCount);
        }

        [Test]
        public void onGet_onRelease_콜백이_호출된다()
        {
            int getCalls = 0, releaseCalls = 0;
            var pool = new ObjectPool<Dummy>(
                () => new Dummy(),
                onGet: _ => getCalls++,
                onRelease: _ => releaseCalls++);

            Dummy item = pool.Get();
            pool.Release(item);

            Assert.AreEqual(1, getCalls);
            Assert.AreEqual(1, releaseCalls);
        }

        [Test]
        public void null_반환은_무시된다()
        {
            var pool = new ObjectPool<Dummy>(() => new Dummy());
            Assert.DoesNotThrow(() => pool.Release(null));
            Assert.AreEqual(0, pool.CountInactive);
        }
    }
}
