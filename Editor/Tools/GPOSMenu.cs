namespace GPOS.Core.Editor
{
    /// <summary>
    /// G-POS 에디터 메뉴 경로/우선순위 상수.
    /// 모든 패키지 메뉴는 최상위 "G-POS" 메뉴 아래에 모입니다.
    /// 우선순위 차이가 11 이상이면 유니티가 메뉴에 구분선을 넣어줍니다.
    /// </summary>
    public static class GPOSMenu
    {
        public const string Root = "G-POS/";

        public const int HubPriority = 0;         // Tool Hub (맨 위)
        public const int ExportPriority = 20;     // AI Export, Import JSON from Notion
        public const int SingletonPriority = 40;  // Auto Singleton 그룹
        public const int SettingsPriority = 100;  // Settings (맨 아래)
    }
}
