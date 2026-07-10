using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;

namespace GPOS.Core.Editor
{
    /// <summary>
    /// 스크립트 내보내기의 대상 루트. Label 은 문서에 표시할 이름(예: "Assets", "Packages/com.gpos.core"),
    /// FileSystemPath 는 실제 디스크 경로입니다. 패키지는 PackageCache 에 있을 수 있어 둘이 다를 수 있습니다.
    /// </summary>
    public readonly struct ScriptExportRoot
    {
        public string Label { get; }
        public string FileSystemPath { get; }

        public ScriptExportRoot(string label, string fileSystemPath)
        {
            Label = label;
            FileSystemPath = fileSystemPath;
        }
    }

    /// <summary>
    /// 스크립트 내보내기 필터. AI 컨텍스트에 넣을 가치가 없는 코드를 걸러 토큰을 아끼는 용도입니다.
    /// </summary>
    public class ScriptExportOptions
    {
        /// <summary>이 이름의 폴더 하위는 전부 제외합니다. (대소문자 무시)</summary>
        public string[] ExcludeFolders = { "Plugins", "ThirdParty", "Generated" };

        /// <summary>이 와일드카드 패턴에 맞는 파일명은 제외합니다.</summary>
        public string[] ExcludeFilePatterns = { "*.g.cs", "*.Designer.cs" };

        /// <summary>이 크기(KB)를 넘는 파일은 본문 대신 생략 안내만 기록합니다. 0 이하면 무제한.</summary>
        public int MaxFileSizeKB = 200;

        /// <summary>false 면 Editor 폴더 하위를 제외합니다. (런타임 코드만 필요할 때)</summary>
        public bool IncludeEditorFolders = true;
    }

    /// <summary>
    /// C# 스크립트를 하나의 마크다운 문서로 합쳐서 내보냅니다.
    /// LLM(AI)에게 프로젝트 전체 코드를 컨텍스트로 넘길 때 사용하기 좋은 형태입니다.
    /// </summary>
    public static class ScriptExporter
    {
        /// <summary>단일 폴더의 모든 .cs 파일을 마크다운으로 반환합니다.</summary>
        public static string BuildMarkdown(string rootFolder)
        {
            return BuildMarkdown(new[] { new ScriptExportRoot(rootFolder, rootFolder) });
        }

        /// <summary>
        /// 여러 루트(프로젝트 Assets + 선택한 패키지들)의 .cs 파일을 하나의 마크다운으로 합쳐 반환합니다.
        /// </summary>
        public static string BuildMarkdown(IReadOnlyList<ScriptExportRoot> roots, ScriptExportOptions options = null)
        {
            options ??= new ScriptExportOptions();
            var patternRegexes = options.ExcludeFilePatterns
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(WildcardToRegex)
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("# Scripts Export");
            sb.AppendLine();

            foreach (ScriptExportRoot root in roots)
            {
                sb.AppendLine($"# Root: {root.Label}");
                sb.AppendLine();

                if (!Directory.Exists(root.FileSystemPath))
                {
                    sb.AppendLine($"> [ScriptExporter] Folder not found: {root.FileSystemPath}");
                    sb.AppendLine();
                    continue;
                }

                string basePath = Path.GetFullPath(root.FileSystemPath).Replace('\\', '/');

                string[] allFiles = Directory
                    .GetFiles(root.FileSystemPath, "*.cs", SearchOption.AllDirectories)
                    .OrderBy(f => f)
                    .ToArray();

                var files = allFiles
                    .Where(f => !IsExcluded(f, basePath, options, patternRegexes))
                    .ToArray();

                sb.AppendLine($"- File count: {files.Length}" +
                    (allFiles.Length != files.Length ? $" (필터로 {allFiles.Length - files.Length}개 제외)" : ""));
                sb.AppendLine();

                foreach (string file in files)
                {
                    // PackageCache 의 실제 경로 대신 "Packages/이름/..." 형태의 라벨 기준 경로로 표기합니다.
                    string relative = Path.GetFullPath(file).Replace('\\', '/');
                    if (relative.StartsWith(basePath))
                        relative = root.Label.TrimEnd('/') + relative.Substring(basePath.Length);

                    sb.AppendLine($"## {relative}");
                    sb.AppendLine();

                    long sizeKB = new FileInfo(file).Length / 1024;
                    if (options.MaxFileSizeKB > 0 && sizeKB > options.MaxFileSizeKB)
                    {
                        sb.AppendLine($"> 파일 크기 {sizeKB}KB 가 상한({options.MaxFileSizeKB}KB)을 넘어 본문을 생략했습니다.");
                        sb.AppendLine();
                        continue;
                    }

                    sb.AppendLine("```csharp");
                    sb.AppendLine(File.ReadAllText(file).TrimEnd());
                    sb.AppendLine("```");
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        private static bool IsExcluded(string file, string basePath,
            ScriptExportOptions options, System.Text.RegularExpressions.Regex[] patternRegexes)
        {
            string relative = Path.GetFullPath(file).Replace('\\', '/');
            if (relative.StartsWith(basePath))
                relative = relative.Substring(basePath.Length).TrimStart('/');

            string[] segments = relative.Split('/');

            // 마지막 요소는 파일명이므로 폴더 검사에서 제외합니다.
            for (int i = 0; i < segments.Length - 1; i++)
            {
                string folder = segments[i];

                if (!options.IncludeEditorFolders && folder.Equals("Editor", StringComparison.OrdinalIgnoreCase))
                    return true;

                foreach (string excluded in options.ExcludeFolders)
                {
                    if (!string.IsNullOrWhiteSpace(excluded) &&
                        folder.Equals(excluded.Trim(), StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            string fileName = segments[segments.Length - 1];
            foreach (var regex in patternRegexes)
            {
                if (regex.IsMatch(fileName))
                    return true;
            }

            return false;
        }

        private static System.Text.RegularExpressions.Regex WildcardToRegex(string pattern)
        {
            string escaped = System.Text.RegularExpressions.Regex.Escape(pattern.Trim())
                .Replace("\\*", ".*")
                .Replace("\\?", ".");
            return new System.Text.RegularExpressions.Regex($"^{escaped}$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        /// <summary>스크립트 마크다운을 파일로 저장합니다.</summary>
        public static void ExportToFile(string rootFolder, string outputPath)
        {
            ExportToFile(new[] { new ScriptExportRoot(rootFolder, rootFolder) }, outputPath);
        }

        /// <summary>여러 루트의 스크립트 마크다운을 파일로 저장합니다.</summary>
        public static void ExportToFile(IReadOnlyList<ScriptExportRoot> roots, string outputPath, ScriptExportOptions options = null)
        {
            string markdown = BuildMarkdown(roots, options);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, markdown, Encoding.UTF8);
            AssetDatabase.Refresh();
        }
    }
}
