using System;
using System.Collections.Generic;
using UnityEditor;

namespace GPOS.Core.Editor
{
    /// <summary>
    /// Notion Import 창의 가져오기 목록과 출력 폴더.
    /// ProjectSettings/GPOSNotionImportProfile.asset 에 저장되므로 VCS 를 통해 팀과 공유됩니다.
    /// 토큰은 개인 정보이므로 여기 두지 않고 EditorPrefs(이 PC + 이 프로젝트) 에만 저장합니다.
    /// </summary>
    [FilePath("ProjectSettings/GPOSNotionImportProfile.asset", FilePathAttribute.Location.ProjectFolder)]
    public class NotionImportProfile : ScriptableSingleton<NotionImportProfile>
    {
        /// <summary>가져올 Notion DB 하나. <see cref="Name"/> 이 그대로 저장 파일 이름이 됩니다.</summary>
        [Serializable]
        public class Entry
        {
            /// <summary>저장 파일 이름 (확장자 제외). 목록 안에서 겹치면 해당 항목은 건너뜁니다.</summary>
            public string Name = "";

            /// <summary>Notion DB URL 끝의 32자리 ID.</summary>
            public string DatabaseId = "";
        }

        public List<Entry> Entries = new();

        /// <summary>JSON 을 저장할 폴더. Assets/ 아래여야 AssetDatabase 가 인식합니다.</summary>
        public string OutputFolder = "Assets/GPOS/Notion";

        /// <summary>변경 내용을 ProjectSettings 파일에 즉시 기록합니다.</summary>
        public void SaveProfile() => Save(true);
    }
}
