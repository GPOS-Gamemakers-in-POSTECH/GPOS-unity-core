namespace GPOS.Core.Pool
{
    /// <summary>
    /// 풀에서 꺼내거나 반환될 때 알림을 받고 싶은 오브젝트가 구현하는 인터페이스. (선택 사항)
    /// </summary>
    public interface IPoolable
    {
        /// <summary>풀에서 꺼내질 때 호출됩니다. 상태 초기화에 사용하세요.</summary>
        void OnSpawn();

        /// <summary>풀로 반환될 때 호출됩니다. 이벤트 해제 등 정리에 사용하세요.</summary>
        void OnDespawn();
    }
}
