using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace GPOS.Core.Editor
{
    // 1. 설정 데이터를 담는 클래스
    [Serializable]
    public class GPOSSettingsData
    {
        public string prefabSavePath = "Assets/GPOS/MonoSingletons";
        public string registryFullPath = "Assets/GPOS/Settings/SingletonRegistry.asset";
    }

    // 2. Project Settings 창에 메뉴를 띄우고 JSON으로 저장/로드하는 클래스
    public static class GPOSCoreSettingsProvider
    {
        // ProjectSettings 폴더는 깃허브에 올라가는 폴더이므로 팀원들과 공유됩니다.
        private const string SettingsFilePath = "ProjectSettings/GPOSCoreSettings.json";
        private static GPOSSettingsData _currentSettings;

        public static GPOSSettingsData Settings
        {
            get
            {
                if (_currentSettings == null) LoadSettings();
                return _currentSettings;
            }
        }

        private static void LoadSettings()
        {
            if (File.Exists(SettingsFilePath))
            {
                string json = File.ReadAllText(SettingsFilePath);
                _currentSettings = JsonUtility.FromJson<GPOSSettingsData>(json);
            }
            else
            {
                _currentSettings = new GPOSSettingsData();
            }
        }

        public static void SaveSettings()
        {
            string json = JsonUtility.ToJson(Settings, true);
            File.WriteAllText(SettingsFilePath, json);
        }

        // 유니티 Project Settings 창에 탭을 추가해주는 마법의 어트리뷰트
        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            var provider = new SettingsProvider("Project/GPOS Core", SettingsScope.Project)
            {
                label = "GPOS Core",
                guiHandler = (searchContext) =>
                {
                    var settings = Settings;

                    EditorGUI.BeginChangeCheck();

                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Auto Singleton Paths", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Set the paths for automatically generated singleton prefabs and the registry. These settings are shared across the team.", MessageType.Info);

                    EditorGUILayout.Space();
                    settings.prefabSavePath = EditorGUILayout.TextField("Prefab Save Path", settings.prefabSavePath);
                    settings.registryFullPath = EditorGUILayout.TextField("Registry Full Path", settings.registryFullPath);

                    // 값이 변경되었을 때만 파일에 저장
                    if (EditorGUI.EndChangeCheck())
                    {
                        SaveSettings();
                    }
                }
            };

            return provider;
        }
    }
}