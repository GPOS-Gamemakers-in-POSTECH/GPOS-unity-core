using UnityEngine;
using System.Linq;

namespace GPOS.Core
{
    public static class SingletonBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeAutoSingletons()
        {
            SingletonRegistry registry = FindRegistry();

            if (registry == null)
            {
                D.LogError("[AutoSingleton] Registry not found. Please generate it via 'G-POS/Auto Singleton/Generate Prefabs (Registry)'.");
                return;
            }

            foreach (var prefab in registry.prefabs)
            {
                if (prefab == null) continue;
                CreateSingletonInstance(prefab);
            }
        }

        private static SingletonRegistry FindRegistry()
        {
#if UNITY_EDITOR
            // Preloaded Assets 는 빌드에서만 자동 로드되므로, 에디터 플레이 모드에서는
            // 메모리에 없을 수 있어 AssetDatabase 로 직접 찾습니다.
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SingletonRegistry");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<SingletonRegistry>(path);
            }
            return null;
#else
            var registries = Resources.FindObjectsOfTypeAll<SingletonRegistry>();
            return registries.Length > 0 ? registries[0] : null;
#endif
        }

        private static void CreateSingletonInstance(GameObject prefab)
        {
            if (prefab.TryGetComponent<MonoBehaviour>(out var component))
            {
                var type = component.GetType();

                if (Object.FindFirstObjectByType(type) == null)
                {
                    GameObject instance = Object.Instantiate(prefab);
                    instance.name = prefab.name;
                    Object.DontDestroyOnLoad(instance);
                    D.Log($"[AutoSingleton] '{prefab.name}' created via Registry.");
                }
            }
        }
    }
}