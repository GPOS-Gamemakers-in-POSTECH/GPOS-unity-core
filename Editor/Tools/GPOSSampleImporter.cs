using System.IO;
using UnityEditor;
using UnityEngine;

namespace GPOS.Core.Editor
{
    /// <summary>
    /// Samples~ 폴더의 샘플을 Package Manager 와 같은 위치(Assets/Samples/...)로 복사하는 도우미.
    /// Tool Hub 에서 버튼 한 번으로 임포트할 수 있게 합니다.
    /// </summary>
    public static class GPOSSampleImporter
    {
        private const string SampleFolderName = "CoreExamples";
        private const string SampleDisplayName = "Core Examples";

        private static UnityEditor.PackageManager.PackageInfo PackageInfo =>
            UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(GPOSSampleImporter).Assembly);

        /// <summary>임포트되는 대상 경로 (Assets/Samples/표시이름/버전/샘플이름).</summary>
        public static string DestinationPath
        {
            get
            {
                var info = PackageInfo;
                return info == null ? null
                    : $"Assets/Samples/{info.displayName}/{info.version}/{SampleDisplayName}";
            }
        }

        public static bool IsImported => DestinationPath != null && Directory.Exists(DestinationPath);

        public static void ImportCoreExamples()
        {
            var info = PackageInfo;
            if (info == null)
            {
                D.LogError("[Samples] 패키지 정보를 찾지 못했습니다.");
                return;
            }

            string source = Path.Combine(info.resolvedPath, "Samples~", SampleFolderName);
            if (!Directory.Exists(source))
            {
                D.LogError($"[Samples] 샘플 폴더가 없습니다: {source}");
                return;
            }

            string destination = DestinationPath;
            if (Directory.Exists(destination) &&
                !EditorUtility.DisplayDialog("샘플 임포트",
                    "이미 임포트된 샘플이 있습니다. 덮어쓸까요?", "덮어쓰기", "취소"))
            {
                return;
            }

            CopyDirectory(source, destination);
            AssetDatabase.Refresh();

            var folder = AssetDatabase.LoadAssetAtPath<Object>(destination);
            if (folder != null)
                EditorGUIUtility.PingObject(folder);

            D.LogGreen($"[Samples] 임포트 완료: {destination}");
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);

            foreach (string file in Directory.GetFiles(source))
            {
                if (file.EndsWith(".meta")) continue;
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
            }

            foreach (string dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
        }
    }
}
