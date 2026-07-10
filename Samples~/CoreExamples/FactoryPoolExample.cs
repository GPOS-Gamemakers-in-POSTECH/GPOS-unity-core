using System.Collections.Generic;
using GPOS.Core;
using GPOS.Core.Pool;
using UnityEngine;

namespace GPOS.Core.Samples
{
    /// <summary>
    /// GameObjectPool 예제 — 0.5초마다 구체를 스폰하고 2초 뒤 풀로 반환합니다.
    /// 씬에 배치하고 플레이하면 하이어라키에서 오브젝트가 재사용되는 것을 볼 수 있습니다.
    /// </summary>
    public class FactoryPoolExample : MonoBehaviour
    {
        private GameObjectPool<Transform> _pool;
        private readonly Queue<(Transform obj, float releaseAt)> _active = new();
        private float _nextSpawn;

        private void Awake()
        {
            // 샘플이라 프리팹 대신 런타임에 원형(구체)을 만들어 템플릿으로 사용합니다.
            GameObject template = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            template.name = "PooledSphere";
            template.SetActive(false);

            _pool = new GameObjectPool<Transform>(template.transform, prewarmCount: 5);
        }

        private void Update()
        {
            if (Time.time >= _nextSpawn)
            {
                _nextSpawn = Time.time + 0.5f;
                Transform obj = _pool.Get(Random.insideUnitSphere * 3f);
                _active.Enqueue((obj, Time.time + 2f));
                D.Log($"[Sample/Pool] Get — 대기 중: {_pool.CountInactive}");
            }

            while (_active.Count > 0 && Time.time >= _active.Peek().releaseAt)
            {
                _pool.Release(_active.Dequeue().obj);
            }
        }
    }
}
