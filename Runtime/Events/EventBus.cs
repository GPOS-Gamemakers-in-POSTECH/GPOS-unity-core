using System;
using System.Collections.Generic;
using UnityEngine;

namespace GPOS.Core.Events
{
    /// <summary>
    /// 타입 기반 전역 이벤트 버스. 매니저 간 직접 참조 없이 이벤트로 통신할 때 사용합니다.
    ///
    /// <code>
    /// public struct PlayerDiedEvent { public int Score; }
    ///
    /// EventBus.Subscribe&lt;PlayerDiedEvent&gt;(OnPlayerDied);
    /// EventBus.Publish(new PlayerDiedEvent { Score = 100 });
    /// EventBus.Unsubscribe&lt;PlayerDiedEvent&gt;(OnPlayerDied); // OnDestroy 에서 반드시 해제!
    /// </code>
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new();

        /// <summary>이벤트 구독. MonoBehaviour 라면 OnDestroy 에서 반드시 Unsubscribe 하세요.</summary>
        public static void Subscribe<T>(Action<T> handler)
        {
            if (handler == null)
                return;

            Type type = typeof(T);
            _handlers[type] = _handlers.TryGetValue(type, out Delegate existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        /// <summary>이벤트 구독 해제.</summary>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (handler == null)
                return;

            Type type = typeof(T);
            if (!_handlers.TryGetValue(type, out Delegate existing))
                return;

            Delegate remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
                _handlers.Remove(type);
            else
                _handlers[type] = remaining;
        }

        /// <summary>이벤트를 발행합니다. 구독자가 없으면 아무 일도 일어나지 않습니다.</summary>
        public static void Publish<T>(T eventData)
        {
            if (!_handlers.TryGetValue(typeof(T), out Delegate handler))
                return;

            // 구독자 하나의 예외가 나머지 구독자 호출을 막지 않도록 개별 호출합니다.
            foreach (Delegate d in handler.GetInvocationList())
            {
                try
                {
                    ((Action<T>)d).Invoke(eventData);
                }
                catch (Exception e)
                {
                    D.LogError($"[EventBus] Handler for {typeof(T).Name} threw: {e}");
                }
            }
        }

        /// <summary>특정 이벤트 타입의 구독을 전부 제거합니다.</summary>
        public static void Clear<T>() => _handlers.Remove(typeof(T));

        /// <summary>모든 구독을 제거합니다.</summary>
        public static void ClearAll() => _handlers.Clear();

        // Enter Play Mode Options(도메인 리로드 꺼짐) 환경에서 이전 플레이의 구독이 남지 않도록 초기화합니다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlayMode() => ClearAll();
    }
}
