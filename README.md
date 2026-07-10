# GPOS Unity Core

G-POS 동아리 팀원들이 공통으로 사용하는 유니티 코어 패키지입니다.

## 설치 방법 (Installation)

- 유니티 에디터에서 Window > Package Manager를 엽니다.

- 좌측 상단의 + 버튼을 누르고 Add package from git URL... 을 선택합니다.

- 아래의 GitHub 주소를 입력하고 Add를 누릅니다.

  - https://github.com/GPOS-Gamemakers-in-POSTECH/GPOS-unity-core.git

## 폴더 구조 (Structure)

- Runtime/ : 게임 실행 시 실제로 포함되는 공용 스크립트, 프리팹, 셰이더 등이 들어갑니다.

- Editor/ : 유니티 에디터 환경에서만 작동하는 커스텀 인스펙터나 툴 스크립트가 들어갑니다.

- Tests/ : 유닛 테스트. Test Runner 에서 보려면 프로젝트의 `Packages/manifest.json` 에 `"testables": ["com.gpos.core"]` 를 추가하세요. push / PR 시 GitHub Actions 에서도 자동 실행됩니다 (최초 1회 시크릿 설정 필요 — `.github/workflows/tests.yml` 주석 참고).

- Samples~/ : 사용 예제. Package Manager 의 Samples 탭 또는 Tool Hub 에서 임포트합니다.

## 기능 (Features)

### 디자인 패턴 (Design Patterns)

- **Singleton** (`GPOS.Core`) : `Lazy<T>` 기반의 쓰레드 안전 싱글톤 (`Singleton<T>`).
- **MonoSingleton** (`GPOS.Core`) : `lock` 으로 쓰레드 안정화된 `MonoBehaviour` 싱글톤. `[AutoSingleton]` 으로 프리팹 자동 생성 지원.
- **FSM** (`GPOS.Core.FSM`) : `IState` / `State`(델리게이트 기반) / `StateMachine` 유한 상태 머신.
- **Command** (`GPOS.Core.Command`) : `ICommand` / `RelayCommand` / `CommandInvoker` (Undo·Redo 스택).
- **Factory** (`GPOS.Core.Factory`) : `IFactory<>` / 키 기반 등록형 `Factory<TKey, TProduct>`.
- **ObjectPool** (`GPOS.Core.Pool`) : 순수 C# 용 `ObjectPool<T>` / 프리팹용 `GameObjectPool<T>` / 스폰·회수 콜백 `IPoolable`.
- **EventBus** (`GPOS.Core.Events`) : 타입 기반 전역 이벤트 버스. 매니저 간 직접 참조 없이 통신.

### 자료구조 (Data Structures)

- **PriorityQueue** (`GPOS.Core.Collections`) : 이진 힙 기반 우선순위 큐. 기본 최소 힙, `IComparer<T>` 주입 가능.
- **SerializableDictionary** (`GPOS.Core.Collections`) : 유니티가 직렬화 가능한 `Dictionary`. 인스펙터 노출 시 `[Serializable]` 파생 클래스를 선언해 사용.

### 에디터 툴 (Editor Tools)

모든 툴은 유니티 상단의 **G-POS** 메뉴에 모여 있습니다.

- **Tool Hub** : `G-POS > Tool Hub` — 모든 툴을 한 화면에서 실행하는 허브 창. 도킹해두면 툴바처럼 사용 가능.
- **AI 학습용 Export** : `G-POS > AI Export` — 스크립트/프리팹을 마크다운으로 내보내 LLM 컨텍스트로 사용. "패키지 포함" 을 켜면 프로젝트에 별도로 추가한 패키지(직접 의존성, 유니티 내장 모듈 제외)를 선택해 함께 내보낼 수 있음 (Git/PackageCache 설치 패키지 지원).
- **Notion Import** : `G-POS > Notion Import` — Notion Integration Token 으로 DB/페이지를 불러와 JSON 으로 저장.
- **Auto Singleton** : `G-POS > Auto Singleton` — `[AutoSingleton]` 프리팹/레지스트리 자동 생성.
- **Settings** : `G-POS > Settings` — GPOS Core 프로젝트 설정 바로가기.
- **외부 패키지 설치** : Tool Hub 의 "외부 패키지" 섹션 — NuGetForUnity, R3, UniTask 등 팀 추천 패키지를 버튼 한 번으로 설치 (UPM 은 Git URL 의존성을 지원하지 않아 Package Manager API 로 프로젝트 manifest 에 추가하는 방식).

> Notion Import 사용 전: [notion.so/my-integrations](https://www.notion.so/my-integrations) 에서 Internal Integration 을 만들고, 대상 페이지/DB 의 `... > Connections` 에 연결한 뒤 토큰과 32자리 ID 를 입력하세요.

## 사용법 (Usage)

### 싱글톤 (Singleton)

일반 C# 클래스는 `Singleton<T>` 를 상속합니다. (쓰레드 안전, `Lazy` 기반)

```csharp
using GPOS.Core;

public class GameDataManager : Singleton<GameDataManager>
{
    public int Gold { get; set; }
}

// 어디서든
GameDataManager.Instance.Gold += 100;
```

`MonoBehaviour` 는 `MonoSingleton<T>` 를 상속합니다.

```csharp
using GPOS.Core;

public class SoundManager : MonoSingleton<SoundManager>
{
    protected override void InitializeSingleton() { /* Awake 시 1회 초기화 */ }
    protected override bool ShouldPersist() => true; // 씬 전환 시 유지 (기본값 true)

    public void PlayBGM() { }
}

// 어디서든
SoundManager.Instance.PlayBGM();
```

- 씬에 없으면 접근 시 자동 생성됩니다. `HasInstance` 로 생성 없이 존재 여부만 확인할 수 있습니다.
- **자동 프리팹 생성**: `MonoSingleton` 상속 클래스는 컴파일 시(또는 `G-POS > Auto Singleton > Generate Prefabs`) 프리팹과 레지스트리가 자동 생성되고, 게임 시작 시 자동으로 씬에 올라옵니다. 원하지 않으면 클래스에 `[AutoSingleton(createPrefab: false)]` 를 붙여 덮어쓰세요. 저장 경로는 `G-POS > Settings` 에서 변경합니다.

### FSM (상태 머신)

```csharp
using GPOS.Core.FSM;

public class EnemyAI : MonoBehaviour
{
    private readonly StateMachine _fsm = new();
    private IState _idle, _chase;

    void Awake()
    {
        // 간단한 상태는 델리게이트로 생성
        _idle = new State(
            onEnter:  () => animator.Play("Idle"),
            onUpdate: dt => { if (PlayerNear()) _fsm.ChangeState(_chase); },
            onExit:   () => D.Log("Idle 종료"));

        _chase = new State(onUpdate: dt => MoveToPlayer(dt));

        _fsm.OnStateChanged += (prev, next) => D.Log($"{prev} -> {next}");
        _fsm.ChangeState(_idle);
    }

    void Update() => _fsm.Tick(Time.deltaTime); // 매 프레임 호출 필수
}
```

복잡한 상태는 `IState` 를 직접 구현한 클래스로 만드세요.

### 커맨드 패턴 (Undo/Redo)

```csharp
using GPOS.Core.Command;

var invoker = new CommandInvoker();

// 간단한 커맨드는 델리게이트로
int hp = 100;
invoker.Execute(new RelayCommand(
    execute: () => hp -= 10,
    undo:    () => hp += 10));

invoker.Undo();  // hp 다시 100
invoker.Redo();  // hp 다시 90
// invoker.CanUndo / CanRedo 로 버튼 활성화 제어

// 복잡한 커맨드는 ICommand 구현 (레벨 에디터, 빌드 시스템 등)
public class PlaceBlockCommand : ICommand
{
    public void Execute() { /* 블록 배치 */ }
    public void Undo()    { /* 블록 제거 */ }
}
```

### 팩토리 패턴

```csharp
using GPOS.Core.Factory;

public enum EnemyType { Slime, Goblin }

var factory = new Factory<EnemyType, Enemy>();
factory.Register(EnemyType.Slime,  () => new Slime());
factory.Register(EnemyType.Goblin, () => new Goblin());

Enemy e = factory.Create(EnemyType.Slime);                  // 없는 키면 예외
if (factory.TryCreate(EnemyType.Goblin, out var g)) { }     // 예외 없이 시도
```

프리팹 스폰에 쓰려면 `Register(type, () => Instantiate(prefab))` 처럼 등록하면 됩니다.

### 오브젝트 풀 (ObjectPool)

프리팹은 `GameObjectPool<T>` 를 사용합니다. Get 시 활성화, Release 시 비활성화됩니다.

```csharp
using GPOS.Core.Pool;

public class BulletSpawner : MonoBehaviour
{
    [SerializeField] private Bullet bulletPrefab;
    private GameObjectPool<Bullet> _pool;

    void Awake() => _pool = new GameObjectPool<Bullet>(bulletPrefab, prewarmCount: 20);

    public void Fire(Vector3 pos, Quaternion rot)
    {
        Bullet b = _pool.Get(pos, rot);
        b.OnHit = () => _pool.Release(b); // 다 쓰면 Destroy 대신 Release!
    }
}
```

순수 C# 객체는 `ObjectPool<T>` 를 사용합니다.

```csharp
var pool = new ObjectPool<StringBuilder>(
    createFunc: () => new StringBuilder(),
    onRelease:  sb => sb.Clear(),
    prewarmCount: 4);
```

풀에서 나올 때/돌아갈 때 초기화가 필요하면 `IPoolable` 을 구현하세요 (`OnSpawn` / `OnDespawn` 자동 호출).

### 이벤트 버스 (EventBus)

이벤트를 struct/class 로 정의하고, 타입으로 구독/발행합니다.

```csharp
using GPOS.Core.Events;

public struct PlayerDiedEvent { public int Score; }

// 구독 (UI 매니저 등)
void OnEnable()  => EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
void OnDisable() => EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied); // 반드시 해제!
void OnPlayerDied(PlayerDiedEvent e) => ShowGameOver(e.Score);

// 발행 (플레이어 쪽) — 발행자는 구독자를 전혀 몰라도 됩니다
EventBus.Publish(new PlayerDiedEvent { Score = 100 });
```

구독자 하나가 예외를 던져도 나머지 구독자는 정상 호출되며, 도메인 리로드를 꺼둔 환경에서도 플레이 시작 시 자동 초기화됩니다.

### 우선순위 큐 (PriorityQueue)

기본은 최소 힙(작은 값이 먼저)이며, 비교자를 주입해 바꿀 수 있습니다.

```csharp
using GPOS.Core.Collections;

var pq = new PriorityQueue<int>();
pq.Enqueue(5); pq.Enqueue(1); pq.Enqueue(3);
pq.Dequeue(); // 1

// 최대 힙
var maxHeap = new PriorityQueue<int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
```

길찾기(A*)나 스케줄링 등에 사용하세요.

### 직렬화 딕셔너리 (SerializableDictionary)

인스펙터에 노출하려면 **구체 타입 파생 클래스**를 먼저 선언해야 합니다. (유니티 제네릭 직렬화 제약)

```csharp
using GPOS.Core.Collections;

[Serializable] public class ItemPriceDict : SerializableDictionary<string, int> { }

public class ShopData : MonoBehaviour
{
    [SerializeField] private ItemPriceDict prices = new();

    void Start() => D.Log(prices["potion"]); // 일반 Dictionary 처럼 사용
}
```

### 로그 (D)

```csharp
using GPOS.Core;

D.Log("일반 로그");
D.LogGreen("성공!");   // 색상 로그 (Red / Yellow / Cyan 도 있음)
D.LogWarning("경고");
```

`Debug.Log` 와 같지만 **에디터/개발 빌드에서만 컴파일**되고 릴리즈 빌드에서는 호출 자체가 제거됩니다. 팀 코드에서는 `Debug.Log` 대신 `D` 사용을 권장합니다.

### 에디터 툴

- **Tool Hub** (`G-POS > Tool Hub`) : 아래 툴 전부 한 창에서 실행. 도킹해두고 사용하세요.
- **AI Export** : Root 경로 지정 → Export → `scripts_export.md` / `prefabs_export.md` 생성. "패키지 포함" 을 켜면 직접 추가한 패키지도 선택해 포함. 생성된 md 를 LLM 에 붙여넣어 질문하는 용도.
- **Notion Import** : Integration 생성 → 대상 페이지/DB 에 Connections 연결 → 토큰과 32자리 ID 입력 → Fetch & Save → JSON 저장.

### 외부 패키지 설치 (R3, UniTask 등)

Tool Hub 의 **외부 패키지** 섹션에서 버튼 한 번으로 설치할 수 있습니다.

- **R3 설치 순서** (2단계 필요):
  1. `NuGetForUnity` 설치 버튼 클릭
  2. 상단 메뉴 `NuGet > Manage NuGet Packages` 에서 `R3` 검색 후 설치 (코어 DLL)
  3. `R3 (Unity)` 설치 버튼 클릭 (유니티 통합)
- **UniTask** : 설치 버튼 한 번이면 끝.

> 참고: UPM 은 package.json 의 Git URL 의존성을 지원하지 않으므로, 이 패키지가 R3 등을 자동으로 함께 설치하지는 못합니다. 대신 위 버튼이 프로젝트의 manifest.json 에 추가해줍니다. 추천 목록에 패키지를 더하려면 `Editor/Tools/GPOSPackageInstaller.cs` 의 `Recommended` 배열에 한 줄 추가하면 됩니다.

## 버전 업데이트 내역

- 1.1.0 : 디자인 패턴(FSM, Command, Factory, ObjectPool), EventBus, 자료구조(PriorityQueue, SerializableDictionary), AI Export / Notion Import / 외부 패키지 설치 에디터 툴 추가
- 1.0.0 : 초기 패키지 구조 셋업 완료
