using System;
using System.Collections.Generic;

namespace GPOS.Core.Pool
{
    /// <summary>
    /// 순수 C# 오브젝트 풀. 생성 비용이 있는 객체를 재사용해 GC 부담을 줄입니다.
    /// 유니티 오브젝트(프리팹)에는 <see cref="GameObjectPool{T}"/> 를 사용하세요.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Stack<T> _pool = new();
        private readonly Func<T> _createFunc;
        private readonly Action<T> _onGet;
        private readonly Action<T> _onRelease;
        private readonly int _maxSize;

        /// <summary>현재 풀에 대기 중인 개수.</summary>
        public int CountInactive => _pool.Count;

        /// <param name="createFunc">풀이 비었을 때 새 객체를 만드는 함수.</param>
        /// <param name="onGet">꺼낼 때마다 호출 (초기화).</param>
        /// <param name="onRelease">반환할 때마다 호출 (정리).</param>
        /// <param name="prewarmCount">미리 만들어둘 개수.</param>
        /// <param name="maxSize">풀에 보관할 최대 개수. 초과 반환분은 버려집니다.</param>
        public ObjectPool(Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null,
            int prewarmCount = 0, int maxSize = int.MaxValue)
        {
            _createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            _onGet = onGet;
            _onRelease = onRelease;
            _maxSize = maxSize;

            for (int i = 0; i < prewarmCount; i++)
                _pool.Push(_createFunc());
        }

        /// <summary>풀에서 객체를 꺼냅니다. 비어 있으면 새로 생성합니다.</summary>
        public T Get()
        {
            T item = _pool.Count > 0 ? _pool.Pop() : _createFunc();
            _onGet?.Invoke(item);
            (item as IPoolable)?.OnSpawn();
            return item;
        }

        /// <summary>객체를 풀로 반환합니다.</summary>
        public void Release(T item)
        {
            if (item == null)
                return;

            (item as IPoolable)?.OnDespawn();
            _onRelease?.Invoke(item);

            if (_pool.Count < _maxSize)
                _pool.Push(item);
        }

        public void Clear() => _pool.Clear();
    }
}
