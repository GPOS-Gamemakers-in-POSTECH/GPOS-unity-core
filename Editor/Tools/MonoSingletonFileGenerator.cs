using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;

namespace GPOS.Core.Editor
{
    public class MonoSingletonFileGenerator : AssetPostprocessor
    {
        private const string AUTO_GEN_PREF_KEY = "GPOS_AutoSingleton_Enabled";

        private const string AutoGenMenuPath = GPOSMenu.Root + "Auto Singleton/Enable Auto Generation";

        public static bool IsAutoGenerationEnabled => EditorPrefs.GetBool(AUTO_GEN_PREF_KEY, true);

        [MenuItem(AutoGenMenuPath, priority = GPOSMenu.SingletonPriority + 1)]
        public static void ToggleAutoGeneration()
        {
            bool isEnabled = EditorPrefs.GetBool(AUTO_GEN_PREF_KEY, true);
            EditorPrefs.SetBool(AUTO_GEN_PREF_KEY, !isEnabled);
            D.Log($"[AutoSingleton] Auto Generation is now {(!isEnabled ? "Enabled" : "Disabled")}");
        }

        [MenuItem(AutoGenMenuPath, true)]
        private static bool ToggleAutoGenerationValidate()
        {
            Menu.SetChecked(AutoGenMenuPath, EditorPrefs.GetBool(AUTO_GEN_PREF_KEY, true));
            return true;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            if (EditorPrefs.GetBool(AUTO_GEN_PREF_KEY, true))
            {
                EditorApplication.delayCall += GeneratePrefabsAndRegistry;
            }
        }

        [MenuItem(GPOSMenu.Root + "Auto Singleton/Generate Prefabs (Registry)", priority = GPOSMenu.SingletonPriority)]
        public static void GeneratePrefabsAndRegistry()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string prefabDir = GPOSCoreSettingsProvider.Settings.prefabSavePath;
            string regPath = GPOSCoreSettingsProvider.Settings.registryFullPath;

            EnsureDirectory(prefabDir);
            EnsureDirectory(Path.GetDirectoryName(regPath));

            var registry = AssetDatabase.LoadAssetAtPath<SingletonRegistry>(regPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<SingletonRegistry>();
                AssetDatabase.CreateAsset(registry, regPath);
                ForceImport(regPath);
                registry = AssetDatabase.LoadAssetAtPath<SingletonRegistry>(regPath);
                D.Log($"[Generator] Created Registry: {regPath}");
            }

            var previousPrefabs = registry.prefabs.ToList();
            registry.prefabs.Clear();

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetTypesSafe)
                .Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(typeof(MonoBehaviour)) && Attribute.IsDefined(t, typeof(AutoSingletonAttribute)));

            foreach (var type in types)
            {
                var attr = (AutoSingletonAttribute)Attribute.GetCustomAttribute(type, typeof(AutoSingletonAttribute));
                if (!attr.CreatePrefab) continue;
                string path = $"{prefabDir}/{type.Name}.prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null)
                {
                    var go = new GameObject(type.Name);
                    go.AddComponent(type);
                    PrefabUtility.SaveAsPrefabAsset(go, path);
                    UnityEngine.Object.DestroyImmediate(go);

                    ForceImport(path);
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    D.Log($"[Generator] Created Prefab: {type.Name}");
                }
                else if (prefab.GetComponent(type) == null)
                {
                    D.LogWarning($"[Generator] Missing component in {type.Name} at {path}");
                    continue;
                }

                // 게임 시작 시 자동 생성은 LoadOnStart 인 것만. (프리팹 자체는 위에서 이미 생성됨)
                if (prefab != null && attr.LoadOnStart && !registry.prefabs.Contains(prefab))
                {
                    registry.prefabs.Add(prefab);
                }
            }

            // 항목이 추가된 경우뿐 아니라 제거된 경우(어트리뷰트 삭제 등)에도 저장해야 합니다.
            if (!registry.prefabs.SequenceEqual(previousPrefabs))
            {
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
                D.Log($"<color=green>[AutoSingleton] Registry updated ({registry.prefabs.Count} prefabs).</color>");
            }

            AddToPreloadedAssets(registry);
        }

        private static Type[] GetTypesSafe(System.Reflection.Assembly assembly)
        {
            // 로드에 실패한 어셈블리는 GetTypes 가 예외를 던지므로, 로드된 타입만 사용합니다.
            try
            {
                return assembly.GetTypes();
            }
            catch (System.Reflection.ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
        }

        private static void AddToPreloadedAssets(SingletonRegistry registry)
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets().ToList();

            preloadedAssets.RemoveAll(x => x == null);

            if (!preloadedAssets.Contains(registry))
            {
                preloadedAssets.Add(registry);
                PlayerSettings.SetPreloadedAssets(preloadedAssets.ToArray());
                D.Log("[Generator] Added Registry to PlayerSettings.PreloadedAssets.");
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static void ForceImport(string path) => AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    }
}