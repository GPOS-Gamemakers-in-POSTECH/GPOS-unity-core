# Changelog

이 패키지의 주요 변경 사항을 기록합니다. [Keep a Changelog](https://keepachangelog.com/ko/1.1.0/) 형식을 따릅니다.

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
