using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GPOS.Core.Editor
{
    /// <summary>
    /// 프리팹의 계층 구조와 컴포넌트 목록을 사람이 읽을 수 있는 텍스트로 내보냅니다.
    /// AI 학습/분석용으로 프리팹 구성을 넘길 때 사용합니다.
    /// </summary>
    public static class PrefabExporter
    {
        /// <summary>
        /// <paramref name="rootFolder"/> 하위의 모든 프리팹을 마크다운 트리 형태로 정리해 반환합니다.
        /// </summary>
        public static string BuildMarkdown(string rootFolder)
        {
            return BuildMarkdown(new[] { rootFolder });
        }

        /// <summary>
        /// 여러 루트(Assets 폴더 + "Packages/이름" 가상 경로) 하위의 프리팹을 하나의 마크다운으로 합쳐 반환합니다.
        /// </summary>
        public static string BuildMarkdown(string[] rootFolders)
        {
            var sb = new StringBuilder();
            string[] guids = AssetDatabase.FindAssets("t:Prefab", rootFolders);

            sb.AppendLine("# Prefab Export");
            sb.AppendLine();
            foreach (string root in rootFolders)
                sb.AppendLine($"- Root: `{root}`");
            sb.AppendLine($"- Prefab count: {guids.Length}");
            sb.AppendLine();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                sb.AppendLine($"## {path}");
                sb.AppendLine();
                sb.AppendLine("```");
                AppendTransform(prefab.transform, sb, 0);
                sb.AppendLine("```");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static void AppendTransform(Transform transform, StringBuilder sb, int depth)
        {
            string indent = new string(' ', depth * 2);
            var components = transform.GetComponents<Component>();

            var names = new StringBuilder();
            for (int i = 0; i < components.Length; i++)
            {
                if (i > 0)
                    names.Append(", ");
                // 누락된(스크립트 깨진) 컴포넌트는 null 로 들어올 수 있습니다.
                names.Append(components[i] != null ? components[i].GetType().Name : "<Missing Script>");
            }

            sb.AppendLine($"{indent}- {transform.name} [{names}]");

            for (int i = 0; i < transform.childCount; i++)
                AppendTransform(transform.GetChild(i), sb, depth + 1);
        }

        /// <summary>프리팹 마크다운을 파일로 저장합니다.</summary>
        public static void ExportToFile(string rootFolder, string outputPath)
        {
            ExportToFile(new[] { rootFolder }, outputPath);
        }

        /// <summary>여러 루트의 프리팹 마크다운을 파일로 저장합니다.</summary>
        public static void ExportToFile(string[] rootFolders, string outputPath)
        {
            string markdown = BuildMarkdown(rootFolders);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, markdown, Encoding.UTF8);
            AssetDatabase.Refresh();
        }
    }
}
