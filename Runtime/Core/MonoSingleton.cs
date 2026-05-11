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

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    D.LogWarning($"[MonoSingleton] {typeof(T)} instance is null because application is quitting.");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindFirstObjectByType(typeof(T));
                        if (_instance == null)
                        {
                            GameObject singletonObject = new();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = $"{typeof(T)} (Singleton)";
                        }
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
            if (_instance == this)
            {
                _applicationIsQuitting = true;
            }
        }
    }
}
