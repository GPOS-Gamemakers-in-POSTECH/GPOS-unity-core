using UnityEngine;
using System.Linq;

namespace GPOS.Core
{
    public static class SingletonBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeAutoSingletons()
        {
            var registries = Resources.FindObjectsOfTypeAll<SingletonRegistry>();
            var registry = registries.Length > 0 ? registries[0] : null;

            if (registry == null)
            {
                D.LogError("[AutoSingleton] Registry not found in Preloaded Assets. Please generate it via 'Tools/Auto Singleton/Generate Prefabs (Registry)'.");
                return;
            }

            foreach (var prefab in registry.prefabs)
            {
                if (prefab == null) continue;
                CreateSingletonInstance(prefab);
            }
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