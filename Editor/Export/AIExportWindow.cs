using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GPOS.Core.Editor
{
    /// <summary>
    /// 스크립트/프리팹을 AI 학습용 마크다운으로 내보내는 에디터 창.
    /// 메뉴: G-POS/AI Export
    ///
    /// "패키지 포함" 을 켜면 프로젝트에 별도로 추가한 패키지(직접 의존성, 유니티 내장 모듈 제외)를
    /// 선택해서 함께 내보낼 수 있습니다. Git/PackageCache 설치 패키지도 지원합니다.
    /// </summary>
    public class AIExportWindow : EditorWindow
    {
        private string _scriptRoot = "Assets";
        private string _prefabRoot = "Assets";
        private string _outputFolder = "Assets/GPOS/Export";
        private bool _exportScripts = true;
        private bool _exportPrefabs = true;

        private bool _includePackages = false;
        private UnityEditor.PackageManager.PackageInfo[] _projectPackages;
        private readonly Dictionary<string, bool> _packageSelection = new();
        private Vector2 _packageScroll;

        // 스크립트 필터 (쉼표 구분 문자열로 편집/저장)
        private bool _showFilters;
        private string _excludeFolders = "Plugins, ThirdParty, Generated";
        private string _excludePatterns = "*.g.cs, *.Designer.cs";
        private int _maxFileSizeKB = 200;
        private bool _includeEditorFolders = true;

        [MenuItem(GPOSMenu.Root + "AI Export", priority = GPOSMenu.ExportPriority)]
        public static void Open()
        {
            var window = GetWindow<AIExportWindow>("AI Export");
            window.minSize = new Vector2(420, 380);
            window.Show();
        }

        // EditorPrefs 는 머신 전역이므로 productGUID 를 섞어 프로젝트별로 저장합니다.
        private static string PrefKey(string field) => $"GPOS_AIExport_{PlayerSettings.productGUID}_{field}";

        private void OnEnable()
        {
            RefreshPackageList();

            _scriptRoot = EditorPrefs.GetString(PrefKey("scriptRoot"), "Assets");
            _prefabRoot = EditorPrefs.GetString(PrefKey("prefabRoot"), "Assets");
            _outputFolder = EditorPrefs.GetString(PrefKey("outputFolder"), "Assets/GPOS/Export");
            _exportScripts = EditorPrefs.GetBool(PrefKey("exportScripts"), true);
            _exportPrefabs = EditorPrefs.GetBool(PrefKey("exportPrefabs"), true);
            _includePackages = EditorPrefs.GetBool(PrefKey("includePackages"), false);
            _excludeFolders = EditorPrefs.GetString(PrefKey("excludeFolders"), "Plugins, ThirdParty, Generated");
            _excludePatterns = EditorPrefs.GetString(PrefKey("excludePatterns"), "*.g.cs, *.Designer.cs");
            _maxFileSizeKB = EditorPrefs.GetInt(PrefKey("maxFileSizeKB"), 200);
            _includeEditorFolders = EditorPrefs.GetBool(PrefKey("includeEditorFolders"), true);
        }

        private void OnDisable() => SavePrefs();

        private void SavePrefs()
        {
            EditorPrefs.SetString(PrefKey("scriptRoot"), _scriptRoot);
            EditorPrefs.SetString(PrefKey("prefabRoot"), _prefabRoot);
            EditorPrefs.SetString(PrefKey("outputFolder"), _outputFolder);
            EditorPrefs.SetBool(PrefKey("exportScripts"), _exportScripts);
            EditorPrefs.SetBool(PrefKey("exportPrefabs"), _exportPrefabs);
            EditorPrefs.SetBool(PrefKey("includePackages"), _includePackages);
            EditorPrefs.SetString(PrefKey("excludeFolders"), _excludeFolders);
            EditorPrefs.SetString(PrefKey("excludePatterns"), _excludePatterns);
            EditorPrefs.SetInt(PrefKey("maxFileSizeKB"), _maxFileSizeKB);
            EditorPrefs.SetBool(PrefKey("includeEditorFolders"), _includeEditorFolders);
        }

        /// <summary>
        /// 프로젝트에 별도로 추가한 패키지 목록을 갱신합니다.
        /// (manifest.json 의 직접 의존성 중 유니티 내장 모듈 제외)
        /// </summary>
        private void RefreshPackageList()
        {
            _projectPackages = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Where(p => p.isDirectDependency && p.source != PackageSource.BuiltIn)
                .OrderBy(p => p.name)
                .ToArray();

            foreach (var package in _projectPackages)
            {
                if (!_packageSelection.ContainsKey(package.name))
                    _packageSelection[package.name] = true;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("AI 학습용 Export", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "프로젝트의 스크립트와 프리팹을 마크다운으로 내보냅니다. LLM 컨텍스트로 넘기기 좋은 형태입니다.",
                MessageType.Info);

            EditorGUILayout.Space();
            _exportScripts = EditorGUILayout.BeginToggleGroup("스크립트 내보내기 (Scripts)", _exportScripts);
            _scriptRoot = EditorGUILayout.TextField("Script Root", _scriptRoot);

            _showFilters = EditorGUILayout.Foldout(_showFilters, "필터 (Filters)", true);
            if (_showFilters)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    _excludeFolders = EditorGUILayout.TextField(
                        new GUIContent("제외 폴더", "쉼표로 구분. 이 이름의 폴더 하위는 전부 제외됩니다."), _excludeFolders);
                    _excludePatterns = EditorGUILayout.TextField(
                        new GUIContent("제외 파일 패턴", "쉼표로 구분. 와일드카드(*) 지원. 예: *.g.cs"), _excludePatterns);
                    _maxFileSizeKB = EditorGUILayout.IntField(
                        new GUIContent("파일 크기 상한 (KB)", "초과하는 파일은 본문 대신 생략 안내만 기록됩니다. 0 = 무제한"), _maxFileSizeKB);
                    _includeEditorFolders = EditorGUILayout.Toggle(
                        new GUIContent("Editor 폴더 포함", "끄면 Editor 폴더 하위 코드를 제외합니다."), _includeEditorFolders);
                }
            }
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();
            _exportPrefabs = EditorGUILayout.BeginToggleGroup("프리팹 내보내기 (Prefabs)", _exportPrefabs);
            _prefabRoot = EditorGUILayout.TextField("Prefab Root", _prefabRoot);
            EditorGUILayout.EndToggleGroup();

            EditorGUILayout.Space();
            DrawPackageSection();

            EditorGUILayout.Space();
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!_exportScripts && !_exportPrefabs))
            {
                if (GUILayout.Button("Export", GUILayout.Height(32)))
                    Export();
            }
        }

        private void DrawPackageSection()
        {
            _includePackages = EditorGUILayout.BeginToggleGroup("패키지 포함 (프로젝트에 추가한 패키지만)", _includePackages);

            if (_includePackages)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField($"감지된 패키지: {_projectPackages.Length}개", EditorStyles.miniLabel);
                        if (GUILayout.Button("새로고침", GUILayout.Width(70)))
                            RefreshPackageList();
                    }

                    if (_projectPackages.Length == 0)
                    {
                        EditorGUILayout.LabelField("별도로 추가한 패키지가 없습니다.", EditorStyles.wordWrappedMiniLabel);
                    }
                    else
                    {
                        _packageScroll = EditorGUILayout.BeginScrollView(
                            _packageScroll, GUILayout.MaxHeight(120));

                        foreach (var package in _projectPackages)
                        {
                            _packageSelection[package.name] = EditorGUILayout.ToggleLeft(
                                $"{package.displayName} ({package.name})", _packageSelection[package.name]);
                        }

                        EditorGUILayout.EndScrollView();
                    }
                }
            }

            EditorGUILayout.EndToggleGroup();
        }

        private UnityEditor.PackageManager.PackageInfo[] SelectedPackages()
        {
            if (!_includePackages)
                return new UnityEditor.PackageManager.PackageInfo[0];

            return _projectPackages
                .Where(p => _packageSelection.TryGetValue(p.name, out bool on) && on)
                .ToArray();
        }

        private void Export()
        {
            SavePrefs(); // 에디터 크래시에 대비해 내보내기 시점에도 저장

            Directory.CreateDirectory(_outputFolder);
            string folder = _outputFolder.TrimEnd('/');
            var packages = SelectedPackages();
            int done = 0;

            if (_exportScripts)
            {
                var roots = new List<ScriptExportRoot> { new(_scriptRoot, _scriptRoot) };
                // 패키지는 PackageCache 에 있을 수 있으므로 resolvedPath(실제 경로)로 읽고
                // 문서에는 "Packages/이름" 으로 표기합니다.
                roots.AddRange(packages.Select(p =>
                    new ScriptExportRoot($"Packages/{p.name}", p.resolvedPath)));

                var options = new ScriptExportOptions
                {
                    ExcludeFolders = SplitCsv(_excludeFolders),
                    ExcludeFilePatterns = SplitCsv(_excludePatterns),
                    MaxFileSizeKB = _maxFileSizeKB,
                    IncludeEditorFolders = _includeEditorFolders
                };

                string path = $"{folder}/scripts_export.md";
                ScriptExporter.ExportToFile(roots, path, options);
                D.LogGreen($"[AIExport] Scripts exported ({roots.Count} roots) -> {path}");
                done++;
            }

            if (_exportPrefabs)
            {
                var roots = new List<string> { _prefabRoot };
                // AssetDatabase 는 "Packages/이름" 가상 경로를 이해합니다.
                roots.AddRange(packages.Select(p => $"Packages/{p.name}"));

                string path = $"{folder}/prefabs_export.md";
                PrefabExporter.ExportToFile(roots.ToArray(), path);
                D.LogGreen($"[AIExport] Prefabs exported ({roots.Count} roots) -> {path}");
                done++;
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("AI Export", $"{done}개 파일을 내보냈습니다.\n{_outputFolder}", "확인");
        }

        private static string[] SplitCsv(string csv)
        {
            return csv.Split(',')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .ToArray();
        }
    }
}
