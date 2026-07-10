using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace GPOS.Core.Editor
{
    /// <summary>
    /// 팀에서 자주 쓰는 외부 패키지를 버튼 한 번으로 설치하는 도우미.
    /// UPM 은 package.json 의 Git URL 의존성을 지원하지 않으므로,
    /// Package Manager API(Client.Add)로 프로젝트 manifest.json 에 직접 추가하는 방식을 사용합니다.
    /// </summary>
    public static class GPOSPackageInstaller
    {
        public readonly struct ExternalPackage
        {
            public string DisplayName { get; }
            public string PackageName { get; }   // 설치 여부 확인용 UPM 이름
            public string InstallUrl { get; }    // Client.Add 에 넘길 Git URL
            public string Description { get; }

            public ExternalPackage(string displayName, string packageName, string installUrl, string description)
            {
                DisplayName = displayName;
                PackageName = packageName;
                InstallUrl = installUrl;
                Description = description;
            }
        }

        /// <summary>추천 외부 패키지 목록. 새 패키지는 여기에 한 줄만 추가하면 Tool Hub 에 나타납니다.</summary>
        public static readonly ExternalPackage[] Recommended =
        {
            new(
                "NuGetForUnity",
                "com.github-glitchenzo.nugetforunity",
                "https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity",
                "NuGet 패키지를 유니티에서 설치하는 툴. R3 코어 설치에 필요합니다."),
            new(
                "R3 (Unity)",
                "com.cysharp.r3",
                "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity",
                "Cysharp 의 차세대 Reactive Extensions. 먼저 NuGetForUnity 로 'R3' 코어를 설치한 뒤 추가하세요."),
            new(
                "UniTask",
                "com.cysharp.unitask",
                "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask",
                "제로 할당 async/await. 코루틴 대체로 사실상 표준입니다."),
        };

        private static AddRequest _currentRequest;
        private static string _installingName;

        /// <summary>현재 설치가 진행 중인 패키지의 표시 이름. 없으면 null.</summary>
        public static string InstallingName => _installingName;

        public static bool IsInstalled(ExternalPackage package)
        {
            return UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .Any(p => p.name == package.PackageName);
        }

        public static void Install(ExternalPackage package)
        {
            if (_currentRequest != null)
            {
                D.LogWarning($"[PackageInstaller] '{_installingName}' 설치가 끝날 때까지 기다려주세요.");
                return;
            }

            D.Log($"[PackageInstaller] Installing {package.DisplayName} ...");
            _installingName = package.DisplayName;
            _currentRequest = Client.Add(package.InstallUrl);
            EditorApplication.update += Progress;
        }

        private static void Progress()
        {
            if (_currentRequest == null || !_currentRequest.IsCompleted)
                return;

            if (_currentRequest.Status == StatusCode.Success)
                D.LogGreen($"[PackageInstaller] Installed: {_currentRequest.Result.displayName} {_currentRequest.Result.version}");
            else
                D.LogError($"[PackageInstaller] Install failed: {_currentRequest.Error?.message}");

            _currentRequest = null;
            _installingName = null;
            EditorApplication.update -= Progress;
        }
    }
}
