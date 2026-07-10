using UnityEditor;
using UnityEngine;

namespace GPOS.Core.Editor
{
    /// <summary>
    /// G-POS 패키지의 모든 툴을 한 화면에서 실행할 수 있는 허브 창.
    /// 메뉴: G-POS/Tool Hub
    /// 도킹해두면 툴바처럼 사용할 수 있습니다.
    /// </summary>
    public class GPOSToolHub : EditorWindow
    {
        private Vector2 _scroll;
        private string _packageLabel;

        private static readonly GUILayoutOption ButtonHeight = GUILayout.Height(28);

        [MenuItem(GPOSMenu.Root + "Tool Hub", priority = GPOSMenu.HubPriority)]
        public static void Open()
        {
            var window = GetWindow<GPOSToolHub>("G-POS Hub");
            window.minSize = new Vector2(280, 420);
            window.Show();
        }

        [MenuItem(GPOSMenu.Root + "Settings", priority = GPOSMenu.SettingsPriority)]
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings("Project/GPOS Core");
        }

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            DrawHeader();

            DrawSection("AI 학습용 Export", "스크립트/프리팹을 마크다운으로 내보내 LLM 컨텍스트로 사용합니다.", () =>
            {
                if (GUILayout.Button("AI Export 열기", ButtonHeight))
                    AIExportWindow.Open();
            });

            DrawSection("Notion 연동", "Notion 데이터베이스/페이지를 JSON 으로 가져옵니다.", () =>
            {
                if (GUILayout.Button("Notion Import 열기", ButtonHeight))
                    NotionImporterWindow.Open();
            });

            DrawSection("Auto Singleton", "[AutoSingleton] 이 붙은 매니저의 프리팹과 레지스트리를 생성/갱신합니다.", () =>
            {
                if (GUILayout.Button("프리팹 + 레지스트리 생성", ButtonHeight))
                    MonoSingletonFileGenerator.GeneratePrefabsAndRegistry();

                bool enabled = MonoSingletonFileGenerator.IsAutoGenerationEnabled;
                bool toggled = EditorGUILayout.ToggleLeft(
                    $"스크립트 컴파일 시 자동 생성 ({(enabled ? "켜짐" : "꺼짐")})", enabled);
                if (toggled != enabled)
                    MonoSingletonFileGenerator.ToggleAutoGeneration();
            });

            DrawSection("샘플", "싱글톤·FSM·커맨드·풀·이벤트버스 사용 예제를 프로젝트로 임포트합니다.", () =>
            {
                string label = GPOSSampleImporter.IsImported ? "Core Examples 다시 임포트" : "Core Examples 임포트";
                if (GUILayout.Button(label, ButtonHeight))
                    GPOSSampleImporter.ImportCoreExamples();

                if (GPOSSampleImporter.IsImported)
                    EditorGUILayout.LabelField($"위치: {GPOSSampleImporter.DestinationPath}", EditorStyles.miniLabel);
            });

            DrawSection("외부 패키지", "팀 추천 외부 패키지를 설치합니다. (프로젝트 manifest.json 에 추가됨)", DrawExternalPackages);

            DrawSection("설정", "프리팹 저장 경로 등 팀 공유 설정을 변경합니다.", () =>
            {
                if (GUILayout.Button("Project Settings 열기 (GPOS Core)", ButtonHeight))
                    OpenSettings();
            });

            EditorGUILayout.EndScrollView();
        }

        private void DrawExternalPackages()
        {
            bool installing = GPOSPackageInstaller.InstallingName != null;
            if (installing)
                EditorGUILayout.HelpBox($"'{GPOSPackageInstaller.InstallingName}' 설치 중...", MessageType.Info);

            foreach (var package in GPOSPackageInstaller.Recommended)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(package.DisplayName, GUILayout.Width(120));

                    bool installed = GPOSPackageInstaller.IsInstalled(package);
                    using (new EditorGUI.DisabledScope(installed || installing))
                    {
                        if (GUILayout.Button(installed ? "설치됨" : "설치", GUILayout.Width(60)))
                            GPOSPackageInstaller.Install(package);
                    }
                }
                EditorGUILayout.LabelField(package.Description, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(8);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                var titleStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter
                };
                GUILayout.Label("G-POS Tool Hub", titleStyle);
                GUILayout.FlexibleSpace();

                // 헤더 우측 톱니바퀴 → GPOS Core 프로젝트 설정 바로가기
                var gearIcon = EditorGUIUtility.IconContent("SettingsIcon");
                gearIcon.tooltip = "GPOS Core 설정 열기";
                if (GUILayout.Button(gearIcon, EditorStyles.iconButton, GUILayout.Width(20), GUILayout.Height(20)))
                    OpenSettings();
                GUILayout.Space(8);
            }

            var subStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            EditorGUILayout.LabelField(GetPackageLabel(), subStyle);
            EditorGUILayout.Space(4);
        }

        /// <summary>package.json 의 displayName/version 을 부제로 사용합니다 (예: "GPOS Unity Core v1.1.0").</summary>
        private string GetPackageLabel()
        {
            if (_packageLabel == null)
            {
                var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GPOSToolHub).Assembly);
                _packageLabel = info != null ? $"{info.displayName} v{info.version}" : "GPOS Unity Core";
            }
            return _packageLabel;
        }

        private void DrawSection(string title, string description, System.Action drawContent)
        {
            EditorGUILayout.Space(6);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(2);
                drawContent();
                EditorGUILayout.Space(2);
            }
        }
    }
}
