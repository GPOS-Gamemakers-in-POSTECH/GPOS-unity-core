using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GPOS.Core.Editor
{
    /// <summary>
    /// Notion 데이터베이스를 불러와 프로젝트에 JSON 으로 저장하는 에디터 창.
    /// 메뉴: G-POS/Import JSON from Notion
    ///
    /// 사용 전 준비:
    /// 1) notion.so/my-integrations 에서 Internal Integration 을 만들고 Secret 을 복사합니다.
    /// 2) 가져올 Notion DB 의 우측 상단 ... > Connections 에서 해당 Integration 을 연결합니다.
    ///    (이 단계를 빠뜨리면 토큰이 맞아도 404 가 돌아옵니다.)
    /// 3) 목록에 왼쪽=저장할 파일 이름, 오른쪽=DB 주소를 넣습니다.
    ///    주소를 통째로 붙여넣으면 32자리 Database ID 만 자동으로 남습니다.
    ///
    /// 목록과 출력 폴더는 <see cref="NotionImportProfile"/> 을 통해 ProjectSettings 에 저장되어 팀과 공유되고,
    /// 토큰은 EditorPrefs 에 이 PC + 이 프로젝트 단위로만 저장됩니다.
    /// </summary>
    public class NotionImporterWindow : EditorWindow
    {
        private const string DefaultNotionVersion = "2022-06-28";

        /// <summary>출력 폴더로 허용하는 루트. 이 밖으로 나가면 AssetDatabase 가 인식하지 못합니다.</summary>
        private const string RequiredRoot = "Assets";

        /// <summary>
        /// 파일/폴더 이름에 쓸 수 없는 문자 (윈도우 기준).
        /// Path.GetInvalidFileNameChars() 는 플랫폼마다 다르므로 (유닉스는 '/' 와 '\0' 뿐)
        /// 맥에서 만든 파일 이름이 윈도우 팀원에게서 깨지지 않도록 고정된 집합을 씁니다.
        /// </summary>
        private const string InvalidNameChars = "\"<>|:*?\\/";

        private const float NameColumnWidth = 140f;
        private const float FetchButtonWidth = 50f;
        private const float RemoveButtonWidth = 24f;

        private static GUIStyle _placeholderStyle;

        /// <summary>
        /// EditorPrefs 는 프로젝트가 아니라 머신(사용자) 단위 저장소라, 키에 프로젝트 GUID 를 붙여 분리합니다.
        /// 토큰을 프로젝트 파일로 남기지 않으므로 실수로 커밋될 위험도 없습니다.
        /// </summary>
        private static string TokenPrefKey => "GPOS.Notion.IntegrationToken." + PlayerSettings.productGUID;

        private string _token = "";
        private Vector2 _scroll;
        private Vector2 _resultScroll;
        private string _lastResult = "";
        private bool _lastSuccess;

        [MenuItem(GPOSMenu.Root + "Import JSON from Notion", priority = GPOSMenu.ExportPriority + 1)]
        public static void Open()
        {
            var window = GetWindow<NotionImporterWindow>("Import JSON from Notion");
            window.minSize = new Vector2(520, 320);
            window.Show();
        }

        private void OnEnable()
        {
            _token = EditorPrefs.GetString(TokenPrefKey, "");
        }

        private void OnGUI()
        {
            NotionImportProfile profile = NotionImportProfile.instance;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Notion → JSON Import", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "목록의 왼쪽 칸은 저장할 파일 이름, 오른쪽 칸은 Notion Database ID 입니다. " +
                "오른쪽 칸에는 DB 주소를 통째로 붙여넣으면 ID 만 자동으로 뽑아냅니다. " +
                "Fetch 하면 '출력 폴더/이름.json' 으로 저장됩니다.\n" +
                "사용 전 notion.so/my-integrations 에서 Integration 을 만들고, 대상 DB 의 '... > Connections' 에 " +
                "연결해야 합니다. 연결하지 않으면 토큰이 맞아도 404 가 돌아옵니다.\n" +
                "목록과 출력 폴더는 ProjectSettings 에 저장되어 팀과 공유되고, " +
                "토큰은 이 PC 의 이 프로젝트에만 저장됩니다.",
                MessageType.Info);

            EditorGUILayout.Space();
            EditorGUI.BeginChangeCheck();
            _token = EditorGUILayout.PasswordField("Integration Token", _token);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetString(TokenPrefKey, _token);

            // 눌린 버튼만 기록해두고 실제 통신은 OnGUI 맨 끝에서 실행합니다.
            // 스크롤뷰 안에서 예외가 나면 EndScrollView 에 도달하지 못해
            // "Mismatched LayoutGroup" 이 창을 닫을 때까지 반복됩니다.
            int fetchIndex = -1;
            bool fetchAll = false;

            EditorGUI.BeginChangeCheck();

            profile.OutputFolder = EditorGUILayout.TextField("Output Folder", profile.OutputFolder);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Databases", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            // 어느 칸에 무엇을 넣어야 하는지 헷갈리지 않도록 열 제목을 함께 스크롤시킵니다.
            if (profile.Entries.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    "저장할 파일 이름", EditorStyles.miniBoldLabel, GUILayout.Width(NameColumnWidth));
                EditorGUILayout.LabelField(
                    "Notion Database ID (URL 붙여넣으면 자동 추출)", EditorStyles.miniBoldLabel);
                GUILayout.Space(FetchButtonWidth + RemoveButtonWidth + 10f);
                EditorGUILayout.EndHorizontal();
            }

            int removeIndex = -1;
            for (int i = 0; i < profile.Entries.Count; i++)
            {
                NotionImportProfile.Entry entry = profile.Entries[i];
                EditorGUILayout.BeginHorizontal();

                entry.Name = EditorGUILayout.TextField(entry.Name, GUILayout.Width(NameColumnWidth));
                DrawPlaceholder(entry.Name, "예: QuestTable");

                // URL 을 통째로 붙여넣어도 ID 만 남깁니다. 못 뽑아내면 입력한 값을 그대로 둡니다.
                string typedId = EditorGUILayout.TextField(entry.DatabaseId);
                if (typedId != entry.DatabaseId)
                    entry.DatabaseId = ExtractDatabaseId(typedId) ?? typedId;
                DrawPlaceholder(entry.DatabaseId, "DB URL 을 그대로 붙여넣으세요");

                using (new EditorGUI.DisabledScope(!CanFetch(entry)))
                {
                    if (GUILayout.Button("Fetch", GUILayout.Width(FetchButtonWidth)))
                        fetchIndex = i;
                }
                if (GUILayout.Button("✕", GUILayout.Width(RemoveButtonWidth)))
                    removeIndex = i;

                EditorGUILayout.EndHorizontal();
            }

            if (removeIndex >= 0)
            {
                profile.Entries.RemoveAt(removeIndex);
                fetchIndex = -1;
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("+ 추가"))
                profile.Entries.Add(new NotionImportProfile.Entry());

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
                profile.SaveProfile();

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_token) || profile.Entries.Count == 0))
            {
                if (GUILayout.Button("Fetch All", GUILayout.Height(32)))
                    fetchAll = true;
            }

            if (!string.IsNullOrEmpty(_lastResult))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
                var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                _resultScroll = EditorGUILayout.BeginScrollView(_resultScroll, GUILayout.Height(100));
                var color = GUI.color;
                GUI.color = _lastSuccess ? Color.white : new Color(1f, 0.6f, 0.6f);
                EditorGUILayout.TextArea(_lastResult, style);
                GUI.color = color;
                EditorGUILayout.EndScrollView();
            }

            // 레이아웃 그룹이 모두 닫힌 뒤에 실행하므로, 여기서 예외가 나도 IMGUI 스택은 멀쩡합니다.
            if (fetchAll)
                Fetch(profile, profile.Entries);
            else if (fetchIndex >= 0 && fetchIndex < profile.Entries.Count)
                Fetch(profile, new List<NotionImportProfile.Entry> { profile.Entries[fetchIndex] });
        }

        /// <summary>
        /// 바로 앞에 그린 입력 칸이 비어 있으면 그 위에 흐린 안내 문구를 겹쳐 그립니다.
        /// GUILayoutUtility.GetLastRect() 는 Repaint 때만 실제 위치를 돌려주므로 그때만 그립니다.
        /// </summary>
        private static void DrawPlaceholder(string value, string placeholder)
        {
            if (!string.IsNullOrEmpty(value) || Event.current.type != EventType.Repaint)
                return;

            _placeholderStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Italic,
                normal = { textColor = new Color(0.5f, 0.5f, 0.5f, 0.8f) }
            };

            Rect rect = GUILayoutUtility.GetLastRect();
            rect.xMin += 3f;
            GUI.Label(rect, placeholder, _placeholderStyle);
        }

        private bool CanFetch(NotionImportProfile.Entry entry) =>
            !string.IsNullOrWhiteSpace(_token) &&
            !string.IsNullOrWhiteSpace(entry.Name) &&
            !string.IsNullOrWhiteSpace(entry.DatabaseId);

        /// <summary>
        /// 대상 목록을 순서대로 내려받아 저장합니다.
        /// 요청은 에디터를 블로킹하므로 진행 상황을 프로그레스 바로 보여주고,
        /// AssetDatabase 갱신은 전부 끝난 뒤 한 번만 합니다.
        /// </summary>
        private void Fetch(NotionImportProfile profile, IReadOnlyList<NotionImportProfile.Entry> targets)
        {
            if (!TryResolveOutputFolder(profile.OutputFolder, out string folder, out string folderError))
            {
                _lastSuccess = false;
                _lastResult = $"[실패] {folderError}";
                D.LogError($"[Notion] {folderError}");
                Repaint();
                return;
            }

            var results = new StringBuilder();
            var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int success = 0;

            try
            {
                for (int i = 0; i < targets.Count; i++)
                {
                    NotionImportProfile.Entry entry = targets[i];
                    string label = string.IsNullOrWhiteSpace(entry.Name) ? $"{i + 1}번 항목" : entry.Name;

                    if (!CanFetch(entry))
                    {
                        results.AppendLine($"[건너뜀] {label}: 이름 또는 Database ID 가 비어 있습니다.");
                        continue;
                    }

                    string fileName = Sanitize(entry.Name);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        results.AppendLine($"[건너뜀] {label}: 파일 이름으로 쓸 수 있는 글자가 없습니다.");
                        continue;
                    }

                    if (!usedFileNames.Add(fileName))
                    {
                        results.AppendLine($"[건너뜀] {label}: '{fileName}.json' 이 앞 항목과 겹칩니다.");
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Notion Import",
                        $"{label} 가져오는 중... ({i + 1}/{targets.Count})",
                        (float)i / targets.Count);

                    results.AppendLine(FetchOne(entry, folder, fileName, out bool ok));
                    if (ok)
                        success++;
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            if (success > 0)
                AssetDatabase.Refresh();

            _lastSuccess = success == targets.Count;
            _lastResult = targets.Count == 1
                ? results.ToString().TrimEnd()
                : $"완료: {success}/{targets.Count}\n{results}";
            Repaint();
        }

        private string FetchOne(NotionImportProfile.Entry entry, string outputFolder, string fileName, out bool success)
        {
            success = false;
            string path = $"{outputFolder}/{fileName}.json";

            try
            {
                var client = new NotionApiClient(_token.Trim(), DefaultNotionVersion);
                NotionResponse response = client.QueryDatabaseAllPages(entry.DatabaseId.Trim());

                if (!response.IsSuccess)
                {
                    D.LogError($"[Notion] {entry.Name} fetch failed ({response.StatusCode}): {response.Error}");
                    return $"[실패] {entry.Name}: [{response.StatusCode}] {response.Error}";
                }

                Directory.CreateDirectory(outputFolder);
                File.WriteAllText(path, response.Json, Encoding.UTF8);

                success = true;
                D.LogGreen($"[Notion] Saved -> {path}");
                return $"[성공] {entry.Name} -> {path}";
            }
            catch (Exception e)
            {
                D.LogError($"[Notion] {entry.Name} 저장 실패: {e}");
                return $"[실패] {entry.Name}: {e.Message}";
            }
        }

        /// <summary>
        /// 출력 폴더를 검증하고 정규화합니다. (AssetDatabase 는 항상 '/' 구분자를 씁니다)
        /// 빈 값이나 "/" 를 그대로 두면 드라이브 루트에 파일을 쓰게 되므로 여기서 막습니다.
        /// </summary>
        internal static bool TryResolveOutputFolder(string folder, out string normalized, out string error)
        {
            normalized = null;
            string trimmed = (folder ?? string.Empty).Trim().Replace('\\', '/').TrimEnd('/');

            if (trimmed.Length == 0)
            {
                error = "Output Folder 가 비어 있습니다.";
                return false;
            }

            foreach (char c in trimmed)
            {
                if (c != '/' && IsInvalidNameChar(c))
                {
                    error = $"Output Folder 에 경로로 쓸 수 없는 문자가 있습니다: {folder}";
                    return false;
                }
            }

            if (trimmed != RequiredRoot && !trimmed.StartsWith(RequiredRoot + "/", StringComparison.Ordinal))
            {
                error = $"Output Folder 는 '{RequiredRoot}/' 아래 경로여야 합니다: {folder}";
                return false;
            }

            if (Array.IndexOf(trimmed.Split('/'), "..") >= 0)
            {
                error = $"Output Folder 에 '..' 은 쓸 수 없습니다: {folder}";
                return false;
            }

            error = null;
            normalized = trimmed;
            return true;
        }

        /// <summary>
        /// 파일 이름으로 쓸 수 없는 문자를 '_' 로 바꿉니다.
        /// 윈도우는 '.' 로 끝나는 이름을 허용하지 않으므로 끝의 마침표도 떼어냅니다.
        /// 남는 글자가 없으면 빈 문자열을 돌려주고, 호출부에서 건너뜁니다.
        /// </summary>
        internal static string Sanitize(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append(IsInvalidNameChar(c) ? '_' : c);

            return sb.ToString().Trim().TrimEnd('.').Trim();
        }

        /// <summary>제어 문자이거나 <see cref="InvalidNameChars"/> 에 속하는지 확인합니다.</summary>
        private static bool IsInvalidNameChar(char c) => c < ' ' || InvalidNameChars.IndexOf(c) >= 0;

        /// <summary>
        /// 붙여넣은 값에서 Database ID(32자리 hex)를 뽑아냅니다. URL 전체를 넣어도 되고 ID 만 넣어도 됩니다.
        /// 제목 슬러그에도 '-' 가 들어가므로(예: Quest-Table-a8ae...) 대시를 지운 뒤 '끝에서' 32자를 취합니다.
        /// 뽑아내지 못하면 null 을 돌려주고, 호출부는 입력을 그대로 둡니다.
        /// </summary>
        internal static string ExtractDatabaseId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            string text = input.Trim();

            // 쿼리(?v=..., &pvs=...)와 프래그먼트(#...) 제거
            int cut = text.IndexOfAny(new[] { '?', '#' });
            if (cut >= 0)
                text = text.Substring(0, cut);

            // 경로의 마지막 조각만 사용 (제목 슬러그 + ID 형태)
            text = text.TrimEnd('/');
            int slash = text.LastIndexOf('/');
            if (slash >= 0)
                text = text.Substring(slash + 1);

            text = text.Replace("-", "");
            if (text.Length < 32)
                return null;

            string id = text.Substring(text.Length - 32);
            foreach (char c in id)
            {
                if (!Uri.IsHexDigit(c))
                    return null;
            }
            return id;
        }
    }
}
