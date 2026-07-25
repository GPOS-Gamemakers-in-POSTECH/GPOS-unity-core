using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GPOS.Core.Editor
{
    /// <summary>
    /// Notion REST API 를 호출하는 최소 클라이언트. 에디터 툴에서만 사용합니다.
    /// 응답은 가공하지 않은 JSON 문자열로 돌려주므로, 필요에 맞게 파싱해서 사용하세요.
    /// (에디터 동작이라 요청이 끝날 때까지 블로킹으로 대기합니다.)
    /// </summary>
    public class NotionApiClient
    {
        private const string BaseUrl = "https://api.notion.com/v1";

        private readonly string _token;
        private readonly string _notionVersion;
        private readonly int _timeoutSeconds;

        /// <param name="integrationToken">Notion Internal Integration Secret (secret_ 로 시작).</param>
        /// <param name="notionVersion">Notion-Version 헤더 값.</param>
        /// <param name="timeoutSeconds">요청 타임아웃(초). 에디터를 블로킹하므로 반드시 유한해야 합니다.</param>
        public NotionApiClient(string integrationToken, string notionVersion = "2022-06-28", int timeoutSeconds = 30)
        {
            _token = integrationToken;
            _notionVersion = notionVersion;
            _timeoutSeconds = Mathf.Max(1, timeoutSeconds);
        }

        /// <summary>
        /// 데이터베이스의 모든 페이지를 커서를 따라가며 조회합니다.
        /// 결과는 {"object":"list","pages":[페이지별 원본 JSON, ...]} 형태로 합쳐 반환합니다.
        /// </summary>
        public NotionResponse QueryDatabaseAllPages(string databaseId, string jsonBody = "{}", int maxPages = 50)
        {
            var pages = new System.Collections.Generic.List<string>();
            string cursor = null;

            for (int i = 0; i < maxPages; i++)
            {
                string body = cursor == null ? jsonBody : InjectStartCursor(jsonBody, cursor);
                NotionResponse response = SendRequest("POST", $"/databases/{databaseId}/query", body);

                if (!response.IsSuccess)
                    return pages.Count == 0 ? response
                        : NotionResponse.Failure(response.StatusCode,
                            $"{pages.Count}페이지까지 성공 후 실패: {response.Error}");

                pages.Add(response.Json);

                cursor = ExtractNextCursor(response.Json);
                if (cursor == null)
                    break;
            }

            string combined = "{\"object\":\"list\",\"pages\":[" + string.Join(",", pages) + "]}";
            return NotionResponse.Success(200, combined);
        }

        private static string InjectStartCursor(string jsonBody, string cursor)
        {
            string trimmed = string.IsNullOrEmpty(jsonBody) ? "{}" : jsonBody.Trim();
            string cursorField = $"\"start_cursor\":\"{cursor}\"";
            if (trimmed == "{}")
                return "{" + cursorField + "}";

            // 사용자가 준 body 의 첫 '{' 바로 뒤에 커서 필드를 끼워 넣습니다.
            int brace = trimmed.IndexOf('{');
            return trimmed.Insert(brace + 1, cursorField + ",");
        }

        private static string ExtractNextCursor(string json)
        {
            // JsonUtility 는 임의 구조를 파싱하지 못하므로 정규식으로 has_more / next_cursor 만 읽습니다.
            if (!System.Text.RegularExpressions.Regex.IsMatch(json, "\"has_more\"\\s*:\\s*true"))
                return null;

            var match = System.Text.RegularExpressions.Regex.Match(json, "\"next_cursor\"\\s*:\\s*\"([^\"]+)\"");
            return match.Success ? match.Groups[1].Value : null;
        }

        private NotionResponse SendRequest(string method, string endpoint, string jsonBody)
        {
            if (string.IsNullOrEmpty(_token))
                return NotionResponse.Failure(0, "Integration token 이 비어 있습니다.");

            string url = BaseUrl + endpoint;
            using var request = new UnityWebRequest(url, method)
            {
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = _timeoutSeconds
            };

            if (!string.IsNullOrEmpty(jsonBody) && method != "GET")
            {
                byte[] body = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(body);
            }

            request.SetRequestHeader("Authorization", $"Bearer {_token}");
            request.SetRequestHeader("Notion-Version", _notionVersion);
            request.SetRequestHeader("Content-Type", "application/json");

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            // 에디터에서 동기적으로 완료를 기다립니다.
            // request.timeout 이 1차 방어선이고, 만약을 대비해 2배 시간의 강제 탈출 장치를 둡니다.
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long hardLimitMs = _timeoutSeconds * 2000L;
            while (!operation.isDone)
            {
                if (stopwatch.ElapsedMilliseconds > hardLimitMs)
                {
                    request.Abort();
                    return NotionResponse.Failure(0, $"요청이 {_timeoutSeconds * 2}초 안에 끝나지 않아 중단했습니다.");
                }
                System.Threading.Thread.Sleep(10);
            }

            long status = request.responseCode;
            string text = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;

            if (request.result != UnityWebRequest.Result.Success)
            {
                string message = string.IsNullOrEmpty(text) ? request.error : text;
                return NotionResponse.Failure(status, message);
            }

            return NotionResponse.Success(status, text);
        }
    }

    /// <summary>Notion API 호출 결과.</summary>
    public readonly struct NotionResponse
    {
        public bool IsSuccess { get; }
        public long StatusCode { get; }
        public string Json { get; }
        public string Error { get; }

        private NotionResponse(bool isSuccess, long statusCode, string json, string error)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            Json = json;
            Error = error;
        }

        public static NotionResponse Success(long statusCode, string json) =>
            new(true, statusCode, json, null);

        public static NotionResponse Failure(long statusCode, string error) =>
            new(false, statusCode, null, error);
    }
}
