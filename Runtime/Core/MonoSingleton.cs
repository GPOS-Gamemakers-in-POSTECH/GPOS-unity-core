using UnityEngine;

namespace GPOS.Core
{
    [AutoSingleton]
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static readonly object _lock = new();
        private static bool _applicationIsQuitting = false;
        public static bool HasInstance => _instance != null;

        // 도메인 리로드가 꺼진 환경에서도 플레이 시작마다 static 상태를 초기화합니다.
        static MonoSingleton()
        {
            SingletonPlayModeReset.Register(() =>
            {
                _instance = null;
                _applicationIsQuitting = false;
            });
        }

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance != null)
                        return _instance;

                    if (_applicationIsQuitting)
                    {
                        D.LogWarning($"[MonoSingleton] {typeof(T)} instance is not created because application is quitting.");
                        return null;
                    }

                    _instance = (T)FindFirstObjectByType(typeof(T));
                    if (_instance == null)
                    {
                        GameObject singletonObject = new();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = $"{typeof(T)} (Singleton)";
                    }
                    return _instance;
                }
            }
        }

        public static void DestroyInstance()
        {
            lock (_lock)
            {
                if (_instance == null)
                    return;
                GameObject toDestroy = _instance.gameObject;
                _instance = null;

                if (Application.isPlaying)
                {
                    Destroy(toDestroy);
                }
                else
                {
                    DestroyImmediate(toDestroy);
                }
            }
        }

        protected virtual void InitializeSingleton() { }
        protected virtual bool ShouldPersist() => true;

        protected virtual void Awake()
        {
            if (_applicationIsQuitting)
            {
                Destroy(gameObject);
                return;
            }

            if (_instance == null)
            {
                _instance = this as T;

                InitializeSingleton();

                if (ShouldPersist())
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            // 씬 전환이나 수동 파괴로도 호출되므로 여기서 종료 플래그를 켜면 안 됩니다.
            // (종료 판정은 OnApplicationQuit 담당) 참조만 정리해 재생성이 가능하게 둡니다.
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
