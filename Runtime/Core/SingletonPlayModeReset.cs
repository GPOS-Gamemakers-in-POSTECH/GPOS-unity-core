using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPOS.Core
{
    /// <summary>
    /// 도메인 리로드를 끈(Enter Play Mode Options) 환경에서 제네릭 싱글톤의 static 상태를
    /// 플레이 시작마다 초기화하기 위한 레지스트리.
    ///
    /// 제네릭 클래스의 static 메서드에는 [RuntimeInitializeOnLoadMethod] 가 호출되지 않으므로,
    /// 각 MonoSingleton&lt;T&gt; 가 static 생성자에서 리셋 액션을 여기에 등록하고
    /// 비제네릭인 이 클래스가 플레이 시작 시 일괄 실행합니다.
    /// </summary>
    internal static class SingletonPlayModeReset
    {
        private static readonly List<Action> _resets = new();

        public static void Register(Action reset)
        {
            if (reset != null)
                _resets.Add(reset);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetAll()
        {
            foreach (Action reset in _resets)
                reset();
        }
    }
}
