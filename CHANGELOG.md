# Changelog

이 패키지의 주요 변경 사항을 기록합니다. [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 형식을 따릅니다.

## [1.2.1] - 2026-07-25

### Added

- **Import JSON from Notion** : Database ID 칸에 DB 주소를 통째로 붙여넣으면 32자리 ID 만 자동으로 추출. 쿼리(`?v=...`)·프래그먼트·제목 슬러그·대시 UUID 를 모두 처리하며, 추출에 실패하면 입력값을 그대로 남겨 사용자가 확인할 수 있게 함

## [1.2.0] - 2026-07-25

### Added

- **Import JSON from Notion** : 이름 + Database ID 목록 등록, 항목별 `Fetch` / 일괄 `Fetch All` 지원
- `NotionImportProfile` : 가져오기 목록과 출력 폴더를 `ProjectSettings/GPOSNotionImportProfile.asset` 에 저장해 팀과 공유
- **Import JSON from Notion** : 긴 요청 동안 진행 상황 프로그레스 바 표시
- **Import JSON from Notion** : 목록에 열 제목("저장할 파일 이름" / "Notion Database ID")과 빈 칸 안내 문구 추가
- 유닛 테스트 (`Tests/Editor/NotionImporterWindowTests`) : 출력 폴더 검증, 파일 이름 정리 규칙

### Changed

- **Notion Import** → **Import JSON from Notion** 으로 메뉴 이름 변경 (`G-POS > Import JSON from Notion`)
- **Import JSON from Notion** : 토큰을 EditorPrefs 에 저장하되, 키에 프로젝트 GUID 를 붙여 프로젝트별로 분리 (EditorPrefs 는 머신 전역 저장소)
- **Import JSON from Notion** : Notion-Version 입력 필드 제거, `2022-06-28` 로 고정
- **Import JSON from Notion** : 출력 폴더를 `Assets/` 아래로 제한
- `Tests/Editor` 어셈블리가 `GPOS.Core.Editor` 를 참조하도록 변경 (에디터 툴 로직 테스트용, `InternalsVisibleTo`)

### Removed

- **Import JSON from Notion** : Page 소스 옵션 제거 (DB 가져오기 전용)
- `NotionApiClient` : 사용처가 없던 `QueryDatabase`, `RetrieveDatabase`, `RetrievePage`, `RetrieveBlockChildren` 제거

### Fixed

- **Import JSON from Notion** : 스크롤뷰 안에서 통신/저장 예외가 나면 `Mismatched LayoutGroup` 이 반복되던 문제 수정 (레이아웃이 끝난 뒤 실행하도록 변경)
- **Import JSON from Notion** : 출력 폴더가 비어 있거나 `/` 이면 예외가 나거나 드라이브 루트에 파일을 쓰던 문제 수정
- **Import JSON from Notion** : `Fetch All` 이 항목마다 `AssetDatabase.Refresh()` 를 호출하던 문제 수정 (전부 끝난 뒤 한 번만 호출)
- **Import JSON from Notion** : 공백뿐인 이름이 `.json` 파일로 저장되던 문제, 이름이 겹치는 항목이 조용히 덮어쓰던 문제 수정
- **Import JSON from Notion** : 파일 이름 정리에 `Path.GetInvalidFileNameChars()` 를 쓰던 문제 수정. 유닉스에서는 `/` 와 `\0` 만 걸러내 맥에서 만든 이름이 윈도우 팀원에게서 깨질 수 있어, 윈도우 기준 고정 집합으로 교체

## [1.1.0] - 2026-07-10

### Added

- **디자인 패턴**
  - FSM (`GPOS.Core.FSM`) : `IState` / `State` / `StateMachine`
  - Command (`GPOS.Core.Command`) : `ICommand` / `RelayCommand` / `CommandInvoker` (Undo·Redo)
  - Factory (`GPOS.Core.Factory`) : `IFactory<>` / 키 기반 `Factory<TKey, TProduct>`
  - ObjectPool (`GPOS.Core.Pool`) : `ObjectPool<T>` / `GameObjectPool<T>` / `IPoolable`
  - EventBus (`GPOS.Core.Events`) : 타입 기반 전역 이벤트 버스
- **자료구조** (`GPOS.Core.Collections`) : `PriorityQueue<T>`, `SerializableDictionary<TKey, TValue>`
- **에디터 툴** : 최상위 G-POS 메뉴, Tool Hub, AI Export(패키지 포함 옵션), Notion Import, 외부 패키지 설치 도우미
- 샘플 (`Samples~/CoreExamples`) : 주요 기능 사용 예제
- 유닛 테스트 (`Tests/Editor`) : PriorityQueue, CommandInvoker, Factory, EventBus, ObjectPool, StateMachine, SerializableDictionary
- CI (`.github/workflows/tests.yml`) : push / PR 마다 테스트 자동 실행 (GameCI)

### Fixed

- `MonoSingleton` : 씬 전환/수동 파괴 후 `Instance` 가 영구적으로 null 이 되던 버그 수정
- `MonoSingleton` : 도메인 리로드를 끈 환경에서 두 번째 플레이부터 싱글톤이 null 이 되던 문제 수정
- `SingletonBootstrapper` : 에디터 플레이 모드에서 레지스트리를 찾지 못하던 문제 수정 (Preloaded Assets 는 빌드 전용)
- `MonoSingletonFileGenerator` : `[AutoSingleton(loadOnStart: false)]` 가 무시되던 문제, 레지스트리 항목 제거 시 저장이 누락되던 문제, 로드 실패 어셈블리에서 예외가 발생할 수 있던 문제 수정
- `NotionApiClient` : 타임아웃 없이 에디터가 무한 정지할 수 있던 문제 수정, DB 쿼리 100건 제한(페이지네이션) 처리 추가

## [1.0.0] - 2026-05-12

### Added

- 초기 패키지 구조 셋업 (Runtime / Editor)
- `Singleton<T>`, `MonoSingleton<T>`, `[AutoSingleton]` 프리팹 자동 생성, `D` 로거
