using System.Collections.Generic;
using UnityEngine;

namespace GPOS.Core.Pool
{
    /// <summary>
    /// 프리팹(컴포넌트) 전용 오브젝트 풀. Get 시 활성화, Release 시 비활성화 후 풀 부모 아래로 되돌립니다.
    /// 총알, 이펙트, 몬스터 스폰 등에 사용하세요.
    /// </summary>
    public class GameObjectPool<T> where T : Component
    {
        private readonly Stack<T> _pool = new();
        private readonly T _prefab;
        private readonly Transform _parent;

        /// <summary>현재 풀에 대기 중인 개수.</summary>
        public int CountInactive => _pool.Count;

        /// <param name="prefab">복제할 프리팹.</param>
        /// <param name="parent">비활성 오브젝트를 담아둘 부모. null 이면 자동으로 빈 오브젝트를 만듭니다.</param>
        /// <param name="prewarmCount">미리 생성해둘 개수.</param>
        public GameObjectPool(T prefab, Transform parent = null, int prewarmCount = 0)
        {
            _prefab = prefab;

            if (parent == null)
            {
                var root = new GameObject($"{prefab.name} Pool");
                parent = root.transform;
            }
            _parent = parent;

            for (int i = 0; i < prewarmCount; i++)
                _pool.Push(CreateInstance());
        }

        private T CreateInstance()
        {
            T instance = Object.Instantiate(_prefab, _parent);
            instance.gameObject.SetActive(false);
            return instance;
        }

        /// <summary>풀에서 꺼내 활성화합니다. 비어 있으면 새로 생성합니다.</summary>
        public T Get() => Get(Vector3.zero, Quaternion.identity);

        public T Get(Vector3 position) => Get(position, Quaternion.identity);

        public T Get(Vector3 position, Quaternion rotation)
        {
            // 반환을 잊었거나 외부에서 파괴된 오브젝트는 건너뜁니다.
            T item = null;
            while (_pool.Count > 0 && item == null)
                item = _pool.Pop();

            if (item == null)
                item = CreateInstance();

            item.transform.SetPositionAndRotation(position, rotation);
            item.gameObject.SetActive(true);

            (item as IPoolable)?.OnSpawn();
            return item;
        }

        /// <summary>오브젝트를 비활성화하고 풀로 반환합니다.</summary>
        public void Release(T item)
        {
            if (item == null)
                return;

            (item as IPoolable)?.OnDespawn();

            item.gameObject.SetActive(false);
            item.transform.SetParent(_parent, false);
            _pool.Push(item);
        }

        /// <summary>풀에 대기 중인 오브젝트를 전부 파괴합니다.</summary>
        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T item = _pool.Pop();
                if (item != null)
                    Object.Destroy(item.gameObject);
            }
        }
    }
}
