using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GPOS.Core.Editor
{
    /// <summary>
    /// Notion 데이터베이스/페이지를 불러와 프로젝트에 JSON 으로 저장하는 에디터 창.
    /// 메뉴: G-POS/Notion Import
    ///
    /// 사용 전 준비:
    /// 1) notion.so/my-integrations 에서 Internal Integration 을 만들고 Secret 을 복사합니다.
    /// 2) 가져올 Notion 페이지/DB 의 우측 상단 ... > Connections 에서 해당 Integration 을 연결합니다.
    /// 3) 페이지/DB URL 끝의 32자리 ID 를 Database/Page ID 칸에 붙여넣습니다.
    /// </summary>
    public class NotionImporterWindow : EditorWindow
    {
        private enum Source { Database, Page }

        private string _token = "";
        private string _notionVersion = "2022-06-28";
        private Source _source = Source.Database;
        private string _targetId = "";
        private string _outputFolder = "Assets/GPOS/Notion";
        private Vector2 _scroll;
        private string _lastResult = "";
        private bool _lastSuccess;

        [MenuItem(GPOSMenu.Root + "Notion Import", priority = GPOSMenu.ExportPriority + 1)]
        public static void Open()
        {
            var window = GetWindow<NotionImporterWindow>("Notion Import");
            window.minSize = new Vector2(460, 360);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Notion → Unity Import", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Notion Integration Token 과 Database/Page ID 로 내용을 불러와 JSON 파일로 저장합니다.\n" +
                "대상 페이지/DB 에 Integration 이 연결(Connections)되어 있어야 합니다.",
                MessageType.Info);

            EditorGUILayout.Space();
            _token = EditorGUILayout.PasswordField("Integration Token", _token);
            _notionVersion = EditorGUILayout.TextField("Notion-Version", _notionVersion);

            EditorGUILayout.Space();
            _source = (Source)EditorGUILayout.EnumPopup("Source", _source);
            _targetId = EditorGUILayout.TextField(_source == Source.Database ? "Database ID" : "Page ID", _targetId);
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_token) || string.IsNullOrEmpty(_targetId)))
            {
                if (GUILayout.Button("Fetch & Save", GUILayout.Height(32)))
                    Fetch();
            }

            if (!string.IsNullOrEmpty(_lastResult))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
                var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(120));
                var color = GUI.color;
                GUI.color = _lastSuccess ? Color.white : new Color(1f, 0.6f, 0.6f);
                EditorGUILayout.TextArea(_lastResult, style);
                GUI.color = color;
                EditorGUILayout.EndScrollView();
            }
        }

        private void Fetch()
        {
            var client = new NotionApiClient(_token.Trim(), _notionVersion.Trim());
            string id = _targetId.Trim();

            NotionResponse response = _source == Source.Database
                ? client.QueryDatabaseAllPages(id) // 100건 초과 DB 도 커서를 따라 전부 가져옵니다.
                : client.RetrievePage(id);

            _lastSuccess = response.IsSuccess;

            if (!response.IsSuccess)
            {
                _lastResult = $"[{response.StatusCode}] {response.Error}";
                D.LogError($"[Notion] Fetch failed ({response.StatusCode}): {response.Error}");
                return;
            }

            string fileName = $"notion_{_source.ToString().ToLower()}_{Sanitize(id)}.json";
            // AssetDatabase 는 항상 '/' 구분자를 사용하므로 Path.Combine(백슬래시)을 쓰지 않습니다.
            string path = $"{_outputFolder.TrimEnd('/')}/{fileName}";
            Directory.CreateDirectory(_outputFolder);
            File.WriteAllText(path, response.Json, Encoding.UTF8);
            AssetDatabase.Refresh();

            _lastResult = $"저장 완료: {path}\n\n{Preview(response.Json)}";
            D.LogGreen($"[Notion] Saved -> {path}");

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset != null)
                EditorGUIUtility.PingObject(asset);
        }

        private static string Sanitize(string id) => id.Replace("-", "").Trim();

        private static string Preview(string json)
        {
            const int max = 800;
            return json.Length <= max ? json : json.Substring(0, max) + "\n... (truncated)";
        }
    }
}
