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

        [MenuItem("Tools/Auto Singleton/Enable Auto Generation")]
        private static void ToggleAutoGeneration()
        {
            bool isEnabled = EditorPrefs.GetBool(AUTO_GEN_PREF_KEY, true);
            EditorPrefs.SetBool(AUTO_GEN_PREF_KEY, !isEnabled);
            D.Log($"[AutoSingleton] Auto Generation is now {(!isEnabled ? "Enabled" : "Disabled")}");
        }

        [MenuItem("Tools/Auto Singleton/Enable Auto Generation", true)]
        private static bool ToggleAutoGenerationValidate()
        {
            Menu.SetChecked("Tools/Auto Singleton/Enable Auto Generation", EditorPrefs.GetBool(AUTO_GEN_PREF_KEY, true));
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

        [MenuItem("Tools/Auto Singleton/Generate Prefabs (Registry)")]
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

            registry.prefabs.Clear();
            bool isDirty = false;

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
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

                if (prefab != null && !registry.prefabs.Contains(prefab))
                {
                    registry.prefabs.Add(prefab);
                    isDirty = true;
                }
            }

            if (isDirty)
            {
                EditorUtility.SetDirty(registry);
                AssetDatabase.SaveAssets();
                D.Log($"<color=green>[AutoSingleton] Registry updated ({registry.prefabs.Count} prefabs).</color>");
            }

            AddToPreloadedAssets(registry);
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