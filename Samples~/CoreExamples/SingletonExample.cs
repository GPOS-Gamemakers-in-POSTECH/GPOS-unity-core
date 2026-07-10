using GPOS.Core;
using UnityEngine;

namespace GPOS.Core.Samples
{
    /// <summary>순수 C# 싱글톤 예제. MonoBehaviour 가 아닌 데이터/로직 홀더에 사용합니다.</summary>
    public class GameSettingsExample : Singleton<GameSettingsExample>
    {
        public float MasterVolume { get; set; } = 1f;
    }

    /// <summary>
    /// MonoBehaviour 싱글톤 예제.
    /// 샘플이 프로젝트의 Auto Singleton 프리팹 생성에 끼어들지 않도록 어트리뷰트로 비활성화했습니다.
    /// 실제 매니저에서는 어트리뷰트 없이 상속만 하면 자동 생성됩니다.
    /// </summary>
    [AutoSingleton(loadOnStart: false, createPrefab: false)]
    public class SoundManagerExample : MonoSingleton<SoundManagerExample>
    {
        protected override void InitializeSingleton()
        {
            D.Log("[Sample] SoundManagerExample initialized.");
        }

        public void PlayBGM(string name) => D.Log($"[Sample] BGM: {name}");
    }

    /// <summary>씬에 배치하고 플레이하면 두 싱글톤 사용법을 보여줍니다.</summary>
    public class SingletonExample : MonoBehaviour
    {
        private void Start()
        {
            GameSettingsExample.Instance.MasterVolume = 0.5f;
            D.Log($"[Sample] MasterVolume = {GameSettingsExample.Instance.MasterVolume}");

            // 씬에 없어도 접근 시 자동 생성됩니다.
            SoundManagerExample.Instance.PlayBGM("Title");
        }
    }
}
