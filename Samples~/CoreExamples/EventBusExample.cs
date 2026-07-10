using GPOS.Core;
using GPOS.Core.Events;
using UnityEngine;

namespace GPOS.Core.Samples
{
    /// <summary>점수가 바뀔 때 발행되는 이벤트. 필요한 데이터를 필드로 담습니다.</summary>
    public struct ScoreChangedEvent
    {
        public int Score;
    }

    /// <summary>
    /// EventBus 예제 — 버튼(발행자)과 로그 출력(구독자)이 서로를 전혀 모른 채 통신합니다.
    /// 씬에 배치하고 플레이한 뒤 화면의 버튼을 눌러보세요.
    /// </summary>
    public class EventBusExample : MonoBehaviour
    {
        private int _score;

        // 구독자: OnEnable 에서 구독, OnDisable 에서 반드시 해제합니다.
        private void OnEnable() => EventBus.Subscribe<ScoreChangedEvent>(OnScoreChanged);
        private void OnDisable() => EventBus.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);

        private void OnScoreChanged(ScoreChangedEvent e)
        {
            D.LogGreen($"[Sample/EventBus] 점수 변경 수신: {e.Score}");
        }

        // 발행자: 구독자가 누구인지 몰라도 됩니다.
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 220, 220, 60));
            if (GUILayout.Button("점수 +10 (Publish)"))
            {
                _score += 10;
                EventBus.Publish(new ScoreChangedEvent { Score = _score });
            }
            GUILayout.EndArea();
        }
    }
}
