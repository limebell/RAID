# Raid 서버 및 전투 시스템 기획서

## 1. 프로젝트 개요

**Raid**는 최대 4명의 플레이어가 협력해 보스를 공략하는 실시간 멀티플레이 레이드 게임이다.

서버는 다음 두 실행 서버로 분리한다.

```text
Raid.LobbyServer
Raid.GameServer
```

* `Raid.LobbyServer`는 로그인 이후의 로비, 방, 매칭 및 게임 서버 할당을 담당한다.
* `Raid.GameServer`는 실제 레이드 전투와 연습장 세션을 실행한다.
* `Raid.Battle`은 별도로 실행되지 않는 순수 전투 시뮬레이션 라이브러리다.

전투 중 클라이언트는 `Raid.LobbyServer`를 경유하지 않고, 할당받은 `Raid.GameServer`에 직접 연결한다.

초기 전투 검증은 정식 레이드 전에 **`RaidMode.Practice`** 로 진행한다. 연습장은 별도 시스템이 아니라 일반 `RaidSession` / `BattleWorld`의 한 모드이며, 공격하지 않는 허수아비(`DummyEntity`)만 배치한다.

---

# 2. 핵심 설계 원칙

## 2.1 서버 권위형

다음 판정은 모두 서버가 결정한다.

* 이동 가능 여부
* 실제 위치
* 스킬 사용 가능 여부
* 마나 소비
* 쿨다운
* 명중 여부
* 피해와 회복
* 상태 효과
* 다운과 사망
* 부활
* 보스 AI와 어그로
* 페이즈 전환
* 클리어와 전멸

클라이언트는 입력을 전송하고 서버 결과를 표현한다.

---

## 2.2 예외 최소화

특수 상황도 가능하면 기존 시스템을 조합해 처리한다.

예:

```text
부활 대상에게 접근
→ 일반 MoveCommand
→ 도착 후 ReviveCommand 실행
```

```text
사거리 밖의 대상에게 스킬 사용
→ 일반 MoveCommand
→ 도착 후 UseSkillCommand 실행
```

```text
페이즈 전환 후 재배치
→ 최초 입장과 동일한 배치 시스템 사용
```

새 기능은 다음 순서로 검토한다.

1. 기존 명령을 그대로 사용할 수 있는가?
2. 기존 명령을 순서대로 조합할 수 있는가?
3. 기존 행동에 조건이나 정책만 추가하면 되는가?
4. 기존 단계나 효과를 조합할 수 있는가?
5. 위 방식으로 표현할 수 없을 때만 전용 로직을 추가한다.

---

## 2.3 네트워크와 전투 로직 분리

* SignalR, 인증, 연결 정보는 실행 서버가 담당한다.
* 이동, 피해, 행동, 보스 AI는 `Raid.Battle`이 담당한다.
* 전투 라이브러리는 ASP.NET Core, SignalR, DB를 알지 못한다.

---

# 3. 전체 서버 구성

```text
Unity Client
    │
    │ 로그인·방·매칭
    ▼
Raid.LobbyServer
    │
    │ 게임 서버 할당 및 세션 생성 요청
    ▼
Raid.GameServer
    │
    │ 실제 전투 또는 연습장 직접 연결
    ▼
RaidSession (Mode = Practice | Raid)
    └─ BattleWorld
```

## 실행 서버

```text
Raid.LobbyServer
Raid.GameServer
```

## 라이브러리

```text
Raid.Battle
Raid.Contracts
Raid.ServiceContracts
```

`Raid.Battle`은 프로세스나 컨테이너로 실행하지 않는다. `Raid.GameServer`에 포함되어 실행된다.

---

# 4. 솔루션 구조

```text
Raid.sln

src/
├─ Raid.LobbyServer/
├─ Raid.GameServer/
├─ Raid.Battle/
├─ Raid.Contracts/
└─ Raid.ServiceContracts/

tests/
├─ Raid.LobbyServer.Tests/
├─ Raid.GameServer.Tests/
└─ Raid.Battle.Tests/
```

---

# 5. 프로젝트별 역할

## 5.1 `Raid.LobbyServer`

로그인 이후 전투 시작 전까지의 게임 흐름을 담당하는 장기 실행 서버다.

### 담당 범위

* 사용자 인증 및 계정 정보
* 공개방 목록
* 비공개방과 입장 코드
* 방 생성·입장·퇴장
* 방장 권한
* 방장 이전
* 강퇴
* 방 제목 변경
* 공개·비공개 전환
* 클래스 선택
* 스킬 구성
* 준비 상태
* 방 채팅과 핑
* 매칭
* 게임 서버 검색 및 할당
* 레이드 세션 생성 요청
* 게임 서버 접속 정보 발급
* 레이드 결과 수신 및 저장
* 전투 종료 후 방 복귀 처리

### 담당하지 않는 범위

* 실시간 전투 입력 처리
* 전투 월드 틱 실행
* 이동과 충돌
* 피해와 회복
* 상태 효과
* 보스 AI
* 페이즈 전환 판정

### 주요 구조

```text
Raid.LobbyServer/
├─ Hubs/
│  └─ LobbyHub.cs
│
├─ Rooms/
│  ├─ RaidRoom.cs
│  ├─ RaidRoomManager.cs
│  └─ RaidRoomService.cs
│
├─ Matchmaking/
├─ GameServers/
│  ├─ GameServerRegistry.cs
│  ├─ GameServerAllocator.cs
│  └─ GameServerClient.cs
│
├─ Authentication/
├─ Users/
├─ Persistence/
└─ Program.cs
```

---

## 5.2 `Raid.GameServer`

실제 레이드 전투와 연습장 세션을 실행하는 독립 배포 서버다.

여러 인스턴스를 수평 확장할 수 있다.

### 담당 범위

* 게임 서버 접속 토큰 검증
* 전투 클라이언트 연결
* `RaidSession` 생성 및 제거
* `RaidMode`별 월드 프리셋 적용
* `BattleWorld` 생성
* 고정 틱 전투 실행
* 플레이어 전투 명령 수신
* 전투 이벤트 및 스냅샷 전송
* 연결 해제와 재접속 처리
* 세션 종료 결과 생성
* 결과를 `Raid.LobbyServer`에 보고
* 현재 서버 용량과 상태 보고

### 담당하지 않는 범위

* 공개방 검색
* 방장 권한
* 클래스 선택 UI 상태 관리
* 일반 계정 인증
* 장기 데이터 저장
* 보상 DB 반영
* 매칭

### 주요 구조

```text
Raid.GameServer/
├─ Controllers/
│  └─ StatusController.cs
│
├─ Hubs/
│  └─ BattleHub.cs
│
├─ Sessions/
│  ├─ RaidSession.cs
│  ├─ RaidSessionRegistry.cs
│  ├─ BattleDtoMapper.cs
│  └─ BattleSyncService.cs
│
├─ Scheduling/
│  ├─ RaidScheduler.cs
│  └─ RaidSimulationHostedService.cs
│
├─ Admission/
│  └─ RaidAdmissionService.cs
│
├─ Reporting/
│  └─ RaidResultReporter.cs
│
├─ Monitoring/
│  └─ GameServerStatusReporter.cs
│
└─ Program.cs
```

---

## 5.3 `Raid.Battle`

순수 전투 시뮬레이션 라이브러리다.

### 담당 범위

* `BattleWorld`
* `WorldLoop`
* 전투 엔티티
* 명령
* 행동
* 행동 단계
* 이동
* 충돌
* 위치 보정
* 피해와 회복
* 실드
* 버프와 디버프
* 다운과 사망
* 부활
* 어그로와 타깃
* 보스 AI
* 페이즈 전환
* `RaidStateMachine`
* 전투 이벤트 생성
* 모드별 월드 프리셋 (`BattleWorldFactory`)
* `DummyEntity`

### 의존하지 않는 것

* ASP.NET Core
* SignalR
* HTTP
* JWT 구현
* ConnectionId
* EF Core
* DB
* 방 코드
* 매칭
* 게임 서버 주소

### 주요 구조

```text
Raid.Battle/
├─ World/
├─ Entities/
├─ Commands/
├─ Actions/
├─ Movement/
├─ Combat/
├─ Effects/
├─ Life/
├─ Targeting/
├─ Bosses/
├─ RaidFlow/
├─ Spatial/
├─ Events/
└─ Definitions/
```

연습장은 `BattleWorldFactory.Create(RaidMode.Practice)` 프리셋으로만 구분한다. 전투 판정 자체는 일반 명령·행동·피해 시스템을 그대로 사용한다.

---

## 5.4 `Raid.Contracts`

Unity 클라이언트와 서버가 공유하는 공개 통신 계약이다. 타깃 프레임워크는 `netstandard2.1`이다.

```text
Raid.Contracts/
├─ Lobby/
│  ├─ Commands/
│  ├─ Events/
│  └─ Snapshots/
│
├─ Battle/
│  ├─ Commands/
│  ├─ Events/
│  └─ Snapshots/
│
└─ Common/
```

포함 대상:

* 방 요청 및 응답 DTO
* 전투 명령 DTO
* 전투 이벤트 DTO
* 전투 스냅샷 DTO
* 공용 식별자
* 클라이언트에 노출할 열거형

포함하지 않는 대상:

* `BattleEntity`
* `BattleWorld`
* `GameAction`
* `ThreatTable`
* 서버 내부 서비스 주소
* 서버 내부 인증 키

---

## 5.5 `Raid.ServiceContracts`

서버 간 통신 전용 계약이다.

Unity 클라이언트에는 포함하지 않는다.

```text
Raid.ServiceContracts/
├─ GameServers/
│  ├─ RegisterGameServerRequest.cs
│  ├─ GameServerHeartbeat.cs
│  ├─ GameServerCapacity.cs
│  ├─ CreateRaidSessionRequest.cs
│  └─ CreateRaidSessionResponse.cs
│
└─ Results/
   ├─ CompleteRaidRequest.cs
   └─ RaidPlayerResult.cs
```

---

# 6. 방과 전투 세션 구분

## 6.1 `RaidRoom`

`Raid.LobbyServer`에서 관리하는 전투 시작 전 로비 단위다.

포함 정보:

* 방 ID
* 방 제목
* 공개 여부
* 입장 코드
* 방장
* 참가자
* 참가자 슬롯
* 클래스 선택
* 스킬 구성
* 준비 상태
* 채팅
* 현재 방 상태

```text
RaidRoom
├─ Host
├─ Participants
├─ ReadyStates
├─ ClassSelections
├─ SkillLoadouts
└─ Visibility
```

---

## 6.2 `RaidSession`

`Raid.GameServer`에서 실행되는 실제 전투 단위다.

포함 정보:

* 세션 ID
* 참가자 정보
* 참가 슬롯
* 접속 상태
* `BattleWorld`
* `WorldLoop`
* 전투 이벤트
* 네트워크 송신 상태
* 재접속 유예 시간
* 세션 결과

```text
RaidSession
├─ BattleWorld
├─ WorldLoop
├─ ParticipantConnections
├─ SnapshotState
└─ SessionLifetime
```

`RaidRoom`과 `RaidSession`은 다른 객체다.

```text
RaidRoom
→ 방장이 전투 시작
→ RaidSession 생성
→ 전투 종료
→ 기존 RaidRoom으로 복귀
```

---

# 7. 게임 서버 인스턴스와 세션

하나의 `Raid.GameServer` 인스턴스가 여러 `RaidSession`을 실행한다.

```text
Raid.GameServer #1
├─ RaidSession A
├─ RaidSession B
└─ RaidSession C

Raid.GameServer #2
├─ RaidSession D
└─ RaidSession E
```

4인 레이드마다 별도 컨테이너를 생성하지 않는 것을 기본으로 한다.

이유:

* 세션 생성 지연 감소
* 컨테이너 기동 비용 절감
* 메모리 낭비 감소
* 초기 운영 단순화
* 작은 레이드 세션을 효율적으로 묶어 실행 가능

향후 전투가 무거워지면 한 인스턴스당 허용 세션 수를 줄일 수 있다.

```text
MaximumSessionCount = 10
MaximumSessionCount = 5
MaximumSessionCount = 1
```

애플리케이션 구조를 변경하지 않고 운영 설정으로 조정하는 것을 목표로 한다.

---

# 8. 게임 서버 할당

초기에는 별도의 `Raid.Orchestrator` 프로세스를 만들지 않는다.

게임 서버 할당 기능은 `Raid.LobbyServer` 내부의 `GameServerAllocator`가 담당한다.

```text
Raid.LobbyServer
└─ GameServerAllocator
```

## 할당 흐름

```text
1. 방장이 레이드 시작 요청
2. Raid.LobbyServer가 방 상태 검증
3. 참가자와 전투 설정 확정
4. 사용 가능한 Raid.GameServer 검색
5. 선택된 게임 서버에 세션 생성 요청
6. 게임 서버가 RaidSession 생성
7. 플레이어별 접속 토큰 발급
8. 클라이언트에 서버 주소와 토큰 반환
```

규모가 커지고 할당 로직이 복잡해질 때만 별도 서비스로 분리한다.

```text
초기:
Raid.LobbyServer
└─ GameServerAllocator

확장 후:
Raid.Orchestrator
```

`Raid.Orchestrator`는 현재 필수 프로젝트가 아니다.

---

# 9. 게임 서버 등록과 상태 보고

각 `Raid.GameServer` 인스턴스는 자신의 정보를 `Raid.LobbyServer`에 등록한다.

```csharp
public sealed record GameServerCapacity(
    string ServerId,
    string PublicEndpoint,
    int ActiveSessionCount,
    int MaximumSessionCount,
    double AverageTickDurationMs,
    bool AcceptingNewSessions
);
```

초기 할당 조건:

```text
AcceptingNewSessions == true
ActiveSessionCount < MaximumSessionCount
```

향후 고려 가능한 값:

* CPU 사용률
* 메모리 사용량
* 평균 틱 처리 시간
* 최대 틱 처리 시간
* 연결 플레이어 수
* 리전
* 게임 버전
* 전투 데이터 버전

가장 중요한 서버 상태 지표는 **틱을 제한 시간 안에 처리하는지**다.

20 TPS 기준 한 틱 간격은 50ms다. 평균 틱 실행 시간은 이보다 충분히 낮아야 한다.

---

# 10. 레이드 시작 흐름

```text
1. 클라이언트가 LobbyHub에 연결
2. 방 생성 또는 입장
3. 클래스와 스킬 선택
4. 모든 참가자가 준비
5. 방장이 StartRaid 요청
6. Raid.LobbyServer가 방 상태 확정
7. GameServerAllocator가 게임 서버 선택
8. Raid.GameServer에 CreateRaidSession 요청
9. Raid.GameServer가 BattleWorld 생성
10. 플레이어별 접속 토큰 생성
11. Raid.LobbyServer가 접속 정보를 클라이언트에 반환
12. 클라이언트가 BattleHub에 직접 연결
13. 전원 접속 또는 준비 조건 충족
14. RaidStateMachine이 Preparing 시작
15. 전투 시작
```

응답 예시:

```csharp
public sealed record StartRaidResponse(
    Guid RaidSessionId,
    string GameServerEndpoint,
    string ConnectionToken
);
```

---

# 11. 클라이언트 연결 구조

## 로비 연결

```text
Unity Client
→ Raid.LobbyServer / LobbyHub
```

담당 통신:

* 방 생성
* 입장
* 퇴장
* 준비
* 클래스 선택
* 스킬 선택
* 채팅
* 레이드 시작

## 전투 연결

```text
Unity Client
→ Raid.GameServer / BattleHub
```

담당 통신:

* 이동
* 기본 공격
* 스킬
* 부활
* 핑
* 전투 스냅샷
* 전투 이벤트

전투 통신은 `Raid.LobbyServer`를 경유하지 않는다.

```text
잘못된 흐름:
Client → LobbyServer → GameServer

권장 흐름:
Client ─────────────→ GameServer
```

로비 연결은 전투 중 유지하거나 종료할 수 있다. 초기 구현에서는 연결을 유지하되 로비 명령을 제한하는 방식이 단순하다.

---

# 12. 게임 서버 접속 토큰

일반 로그인 AccessToken과 전투 서버 접속 토큰을 분리한다.

전투 접속 토큰은 짧은 수명을 가진다.

포함 정보 예시:

```text
UserId
RaidSessionId
GameServerId
ParticipantSlot
IssuedAt
Expiration
Nonce
```

`Raid.GameServer`는 다음을 검증한다.

* 서명
* 만료 시간
* 자신의 서버 ID
* 세션 ID
* 사용자 ID
* 참가 슬롯
* 해당 세션 참가자 여부
* 토큰 재사용 여부

전투 접속 후에는 해당 연결을 `RaidSession`과 `PlayerEntity`에 매핑한다.

---

# 13. 전투 세션 생성

`Raid.LobbyServer`가 게임 서버에 전달할 정보:

```text
RaidSessionId
RaidDefinitionId
BossDefinitionId
RandomSeed
Participants
ParticipantSlots
SelectedClasses
SelectedSkills
InitialStats
ClientVersion
BattleDataVersion
```

예시:

```csharp
public sealed record CreateRaidSessionRequest(
    Guid RaidSessionId,
    string RaidDefinitionId,
    int RandomSeed,
    IReadOnlyList<RaidParticipantData> Participants
);
```

`Raid.GameServer`는 해당 데이터로 다음을 생성한다.

```text
RaidSession
└─ BattleWorld
   ├─ PlayerEntity
   ├─ BossEntity
   ├─ WorldLoop
   ├─ 전투 시스템
   └─ RaidStateMachine
```

---

# 13.1 연습장 모드

연습장은 별도 서버 경로가 아니라 **`RaidMode.Practice`** 다.

목적은 다음과 같다.

* 이동 검증
* 스킬 시전·발동·후딜 검증
* 피해 적용 검증
* 클라이언트·서버 연동 사전 확인

## 구성

```text
RaidSession (Mode = Practice)
└─ BattleWorld
   ├─ PlayerEntity 1
   ├─ DummyEntity 1
   ├─ WorldLoop
   ├─ CommandSystem
   ├─ ActionSystem
   ├─ MovementSystem
   └─ DamageSystem
```

`DummyEntity`는 반격하지 않는 고정 타깃이다.

* 보스 AI 없음
* 어그로 없음
* 패턴 없음
* 이동하지 않음
* HP는 충분히 크게 두거나 필요 시 리셋

연습장 전용 분기를 `BattleWorld` 곳곳에 흩뿌리지 않는다. `RaidMode`와 월드 프리셋만 다르게 둔다.

## 진입

초기 구현에서는 `Raid.LobbyServer`를 거치지 않는다.

```text
Unity Client
→ Raid.GameServer /hubs/battle (BattleHub)
→ JoinSession(Mode = Practice)
→ Move / UseSkill 입력
← BattleTick (이벤트 델타)
```

로비가 준비되면 나중에 연습장 입장 진입점을 추가할 수 있다.

세션 조작은 HTTP가 아니라 `BattleHub`만 사용한다.

`RaidSession`은 연결별 `SessionParticipant`를 가진다. 세션 생성 시 플레이어를 미리 만들지 않고, `JoinSession` 때 스폰한다. 인원 상한은 아직 두지 않는다.

BattleHub 클라이언트 호출 메서드:

```text
JoinSession(JoinSessionRequest) -> JoinSessionResponse
Move(MoveRequest)
UseSkill(UseSkillRequest)
RequestSnapshot() -> BattleSnapshotDto
```

`JoinSessionResponse`는 본인 `PlayerEntityId` / 슬롯 / 클래스·스킬과 **초기 전체 스냅샷**을 돌려준다. `BattleTick`은 이벤트가 있을 때만 델타를 푸시한다. 보정·재동기화가 필요하면 `RequestSnapshot`을 호출한다.

서버 푸시:

```text
BattleTick(BattleTickMessage)
- Tick
- Events
```

## 클래스

연습장 전용 스킬을 따로 만들지 않는다.

플레이어는 일반 클래스 정의를 들고 입장한다.

* 초기: 테스트용 클래스(`test`)만 사용한다.
* 이후: 원하는 클래스를 선택해 연습장에 입장할 수 있다.

정식 4클래스는 `32. 플레이어 클래스`를 따른다.

테스트용 클래스는 나중에 구현할 **스킬 타입마다 대표 스킬 1개**를 가진다.

현재 포함:

```text
test.instant_strike
- 즉시형
- Activation → Recovery

test.charged_strike
- 시전형
- Casting → Windup → Activation → Recovery
```

이후 스킬 타입이 추가되면 같은 클래스에 대표 스킬을 하나씩 추가한다.

```text
예:
채널형
지속형
```

세션 API는 클래스 로드아웃의 `SkillId`로 스킬을 조회한다.

`UseSkillRequest`는 타깃을 `SkillTargetDto`로 묶는다.

```text
UseSkillRequest
├─ SkillId
└─ Target?
   ├─ Mode: None | Entity | Point | Direction
   ├─ EntityId?
   ├─ Position?
   └─ Direction?
```

서버가 스킬 정의의 `SkillTargetingMode`와 `Target.Mode`/필드를 대조 검증한다.

## 정책

* 서버 권위형은 유지한다. 클라이언트가 피해량을 결정하지 않는다.
* 연습장도 일반 `RaidSession` / `BattleHub` / `BattleWorld`를 사용한다.

---

# 14. `BattleWorld`

`BattleWorld`는 하나의 레이드 전투 시뮬레이션 전체 상태다.

```csharp
public sealed class BattleWorld
{
    public long Tick { get; private set; }

    public EntityRegistry Entities { get; }
    public RaidStateMachine RaidStateMachine { get; }
    public BattleEventBuffer Events { get; }

    public CommandSystem Commands { get; }
    public ActionSystem Actions { get; }
    public MovementSystem Movement { get; }
    public CollisionSystem Collisions { get; }
    public CombatResolutionSystem Combat { get; }
    public StatusEffectSystem Effects { get; }
    public LifeStateSystem Life { get; }
    public ThreatSystem Threat { get; }
    public BossSystem Bosses { get; }
}
```

각 `RaidSession`은 독립적인 `BattleWorld`를 가진다.

`BattleWorld`는 싱글턴으로 만들지 않는다.

---

# 15. `WorldLoop`

`WorldLoop`는 고정 틱으로 전투 시스템을 실행한다.

초기 권장 틱레이트:

```text
20 TPS
FixedDeltaTime = 0.05초
```

권장 실행 순서:

```text
1. 현재 레이드 시퀀스 업데이트
2. 외부 입력 큐 소비
3. WorldCommand 처리
4. GameAction 업데이트
5. MovementIntent 적용
6. 충돌 및 위치 보정
7. 공격·피해·회복 판정
8. 상태 효과 업데이트
9. 생명 상태 업데이트
10. 어그로 업데이트
11. 보스 AI 업데이트
12. 레이드 전이 조건 수집
13. 전이 요청 우선순위 평가
14. 이벤트 생성
15. 틱 번호 증가
```

---

# 16. `RaidScheduler`

한 게임 서버 인스턴스 안의 여러 세션을 실행한다.

```text
RaidScheduler
├─ RaidSession A Tick
├─ RaidSession B Tick
├─ RaidSession C Tick
└─ ...
```

세션마다 OS 스레드를 하나씩 생성하지 않는다.

각 `BattleWorld`는 논리적으로 단일 스레드에서 수정한다.

```text
SignalR 스레드
→ 스레드 안전 명령 큐에 입력 추가

RaidScheduler
→ WorldLoop가 명령 큐 소비
→ BattleWorld 상태 변경
```

---

# 17. 명령과 행동

## `WorldCommand`

무엇을 시도할지 표현한다.

* 즉시 검증
* 즉시 성공 또는 실패
* 여러 틱 동안 유지되지 않음
* 성공 시 `GameAction` 생성 가능

## `GameAction`

실제로 진행 중인 행동이다.

* 여러 틱 동안 유지 가능
* 행동 단계 보유
* 완료 또는 중단될 때까지 업데이트

```text
Client Request
→ WorldCommand
→ 검증
→ GameAction 생성
→ 행동 단계 진행
```

---

# 18. 자동 접근

스킬, 기본 공격, 부활, 상호작용의 자동 접근은 공통 이동을 사용한다.

```text
사거리 안
→ 실행 명령

사거리 밖
→ MoveCommand
→ FollowUpCommand
```

예:

```text
MoveCommand
→ UseSkillCommand
```

```text
MoveCommand
→ ReviveCommand
```

도착 시 후속 명령을 다시 검증한다.

도착 후 대상이 사거리 밖이면 실패한다. 서버가 자동으로 재접근하지 않는다.

---

# 19. 행동 단계

기본 단계:

```text
Casting
Windup
Activation
Channeling
Recovery
```

행동은 필요한 단계만 사용한다.

```text
일반 공격
Windup → Activation → Recovery

시전 스킬
Casting → Windup → Activation → Recovery

부활
Channeling

지속형 스킬
Casting → Windup → Activation → Channeling → Recovery
```

## 기본 규칙

### Casting

* 이동 입력으로 취소 가능
* 취소 시 마나 소비 없음
* 취소 시 쿨다운 시작 없음
* 스턴·침묵·다운·사망으로 취소

### Windup

* 캐스팅 완료 후 실제 판정 전 준비 동작
* 이동 입력으로 취소되지 않음
* 무기 휘두르기, 도약 직전, 활 시위 고정 같은 모션 구간

### Activation

* 진입 시 마나 소비
* 진입 시 쿨다운 시작
* 이후 중단돼도 환불 없음
* 공격 판정 발생 가능

### Channeling

* 일정 시간 유지
* 반복 효과 또는 완료 효과 가능
* 대상과 거리 지속 검사 가능
* 이동이나 다른 입력으로 취소 가능

### Recovery

* 최신 입력 하나 예약
* 종료 시 다시 검증
* 유효하면 실행

---

# 20. 이동 시스템

행동이 직접 좌표를 변경하지 않는다.

```text
GameAction
→ MovementIntent 생성
→ MovementSystem이 위치 변경
```

이동 종류:

```text
일반 이동
행동 이동
강제 이동
즉시 위치 설정
```

즉시 위치 설정 사유 예시:

```text
Spawn
PhaseTransition
Correction
Reset
```

우선순위:

```text
PositionSet
> ForcedMovement
> ActionMovement
> NormalMovement
```

페이즈 전환 재배치와 연습장 스폰·리셋은 동일한 위치 설정 시스템을 사용한다.

---

# 21. 피해 처리

기본 처리 순서:

```text
1. 공격자와 대상 유효성 확인
2. 타깃 가능 여부
3. 무적 여부
4. 공격 데이터 계산
5. 치명타
6. 피해 감소
7. 실드 차감
8. HP 차감
9. 적중 효과
10. 어그로 생성
11. 다운 또는 사망
12. 결과 이벤트
```

무적 대상:

* 피해 없음
* 실드 소모 없음
* 디버프 없음
* 군중 제어 없음
* 어그로 없음
* 마나와 쿨다운 환불 없음
* 무적 표시 이벤트 생성

---

# 22. 다운과 부활

생명 상태:

```text
Alive
Downed
Dead
```

다운 시:

* 현재 행동 취소
* 예약 명령 제거
* 이전 입력 제거
* 다운 이동 규칙 적용
* 다운 HP 감소 시작

부활:

```text
MoveCommand
→ ReviveCommand
→ Channeling
→ 부활 완료
```

유지할 특수 규칙:

> 부활 채널 중 대상의 다운 HP 자연 감소를 멈춘다.

구현:

```text
ReviveAction 시작
→ PauseDownedDecayEffect 적용

ReviveAction 종료
→ PauseDownedDecayEffect 제거
```

---

# 23. 보스 AI와 어그로

어그로 요소:

* 피해
* 회복
* 도발
* 거리
* 근접 보정
* 타깃 변경 완화

보스 기본 흐름:

```text
현재 행동 있음
→ 행동 계속

현재 행동 없음
→ 타깃 확인
→ 패턴 후보 검색
→ 패턴 선택
→ 후보 없음: 이동 또는 WaitAction
```

보스 걷기와 달리기는 별도 패턴이 아니라 공통 이동을 사용한다.

---

# 24. 패턴 반복 감쇠

공통값:

```text
사용한 패턴 감쇠 단계 +3
다른 패턴 사용 시 기존 감쇠 단계 -1
단계별 가중치 ×0.5
최대 감쇠 단계 6
```

```text
AdjustedWeight
= BaseWeight × 0.5^PenaltyLevel
```

패턴별로는 기본 가중치만 설정한다.

실제 보스 패턴 콘텐츠는 기본 시스템 구현 이후 설계한다.

---

# 25. `RaidStateMachine`

상태:

```csharp
public enum RaidState
{
    Waiting,
    Preparing,
    Active,
    Completed
}
```

전투 중 모드:

```csharp
public enum ActiveRaidMode
{
    Running,
    Transitioning,
    Wiped
}
```

결과:

```csharp
public enum RaidResult
{
    None,
    Cleared,
    Failed,
    Abandoned
}
```

전체 흐름:

```text
Waiting
→ Preparing
→ Active.Running
↔ Active.Transitioning
→ Active.Wiped
→ Preparing 또는 Completed

Active.Running
→ Completed
```

---

# 26. 상태 전이 우선순위

개별 시스템은 상태를 직접 변경하지 않고 요청만 생성한다.

```text
BossDied
→ ClearRequest

AllPlayersDefeated
→ WipeRequest

PhaseThresholdReached
→ PhaseTransitionRequest
```

우선순위:

```text
Clear
> Wipe
> PhaseTransition
```

같은 틱에 보스와 모든 플레이어가 동시에 사망하면 클리어다.

현재 틱에 이미 생성된 판정은 끝까지 처리한다. 전이가 확정된 뒤에는 새로운 판정 생성을 막는다.

---

# 27. 페이즈 전환

흐름:

```text
페이즈 조건 충족
→ 현재 보스 행동 중단
→ 보스 정지 및 무적
→ 선택적 그로기 모션
→ 연출 시작
→ 플레이어 행동과 예약 명령 정리
→ 효과와 전투 오브젝트 제거
→ 최초 입장 규칙으로 재배치
→ 어그로 초기화
→ 다음 페이즈 적용
→ 최초 입장과 동일한 시작 절차
→ Active.Running
```

연출 시작 전:

* 보스만 정지
* 보스 무적
* 플레이어 행동 가능
* 무적 공격 규칙 적용

연출 시작 시:

* 플레이어 행동 취소
* 입력 잠금
* 버프, 디버프, 실드, DoT, HoT 제거
* 투사체, 장판, 소환물 제거

연출 중:

* 쿨다운 진행
* 지속시간 진행
* 행동과 이동 정지
* 자연 회복 정지
* 다운 HP 감소 정지

페이즈 전환 후:

* 다운 상태 유지
* 다운 HP 유지
* 자동 부활 없음
* 어그로 완전 초기화

---

# 28. 전투 종료와 결과 보고

전투 종료 시 `Raid.GameServer`가 결과를 확정한다.

```text
RaidStateMachine
→ Completed
→ RaidResult 확정
→ RaidResultReporter
→ Raid.LobbyServer에 결과 보고
```

결과 데이터 예시:

```text
RaidSessionId
RaidResult
BossId
ClearTime
ParticipantResults
Damage
Healing
Deaths
Revives
Disconnects
RandomSeed
ServerVersion
BattleDataVersion
```

`Raid.LobbyServer`가 담당할 후속 처리:

* 기록 저장
* 최고 기록 갱신
* 이전 기록 저장
* 개인 기여도 저장
* 방 상태 복구
* 결과 화면 데이터 생성

`Raid.GameServer`는 DB에 직접 기록하지 않는 것을 기본으로 한다.

---

# 29. 재접속과 장애 처리

## 클라이언트 연결 해제

```text
연결 해제
→ PlayerEntity 유지
→ 일정 시간 재접속 허용
→ 같은 Raid.GameServer에 재접속
→ 전체 스냅샷 전송
```

중간 접속 정책:

* 전투 중 재접속하면 관전
* 전멸 후 준비 구역에서는 다시 참가 가능

## 게임 서버 인스턴스 장애

초기 정책:

```text
Raid.GameServer 장애
→ 해당 레이드 실패
→ 플레이어 로비 복귀
→ 필요하면 소비 자원 복원
```

서버 장애 후 다른 인스턴스에서 전투를 이어가는 기능은 초기 범위에서 제외한다.

이를 구현하려면 다음이 추가로 필요하다.

* 주기적 월드 스냅샷
* 명령 로그
* 결정론적 재실행
* 세션 이전
* 새 서버 접속 정보 발급

---

# 30. 배포 및 스케일링

예시:

```text
Raid.LobbyServer #1
Raid.LobbyServer #2

Raid.GameServer #1
Raid.GameServer #2
Raid.GameServer #3
```

`Raid.LobbyServer`와 `Raid.GameServer`는 독립적으로 확장한다.

### 로비 서버 확장 기준

* 동시 연결 수
* 방 개수
* 매칭 요청 수
* API 요청 수

### 게임 서버 확장 기준

* 활성 세션 수
* 연결 플레이어 수
* CPU
* 메모리
* 평균 틱 시간
* 틱 지연
* 신규 세션 수용 가능 여부

---

# 31. 최종 의존 관계

```text
Raid.LobbyServer
├─ Raid.Contracts
└─ Raid.ServiceContracts

Raid.GameServer
├─ Raid.Battle
├─ Raid.Contracts
└─ Raid.ServiceContracts

Raid.Battle
└─ 외부 실행 서버 프로젝트 의존 없음

Unity Client
└─ Raid.Contracts
```

금지할 의존 관계:

```text
Raid.Battle
→ Raid.GameServer 참조 금지

Raid.Battle
→ Raid.LobbyServer 참조 금지

Raid.LobbyServer
→ Raid.Battle 참조 불필요

Unity Client
→ Raid.ServiceContracts 참조 금지
```

---

# 32. 플레이어 클래스

각 클래스는 **Q / W / E / R / D / F** 여섯 스킬을 가진다.

수치는 이후 밸런스에서 정한다. 이 장은 역할과 스킬 동작만 고정한다.

## 공통

### 스킬 슬롯

```text
Q W E R D F
```

### F — 행동 취소 이동

네 클래스의 F는 공통 규칙을 따른다.

* 현재 사용 중인 모든 선딜·후딜을 지운다.
* 선딜 도중 캔슬하면 캔슬된 스킬 자원은 소모되지만, 그 스킬의 공격은 발생하지 않는다.
* 클래스마다 이동 방식과 부가 효과만 다르다.

```text
전사   짧은 거리 구르기
마법사 긴 거리 점멸
궁수   일정 거리 이동 + 민첩함
성직자 일정 거리 이동
```

---

## 32.1 전사

근접 공격과 방어. 어그로를 끌며 스스로에게 실드와 상태이상 면역을 부여해 전열을 지킨다.

```text
Q  체인/즉발
   전방 범위를 내려치는 공격
   적을 맞추면 최대 3타까지 점점 강화

W  즉발
   전방 부채꼴을 베는 공격

E  즉발
   전방으로 돌진
   돌진이 끝난 곳에서 전방의 적에게 방패로 공격

R  차징
   차징이 끝나면 차징에 비례한 피해로 전방을 공격
   명중 시 적에게 출혈 부여

D  홀딩
   홀딩 동안 상태이상 면역, 실드 생성
   홀딩 종료 시 주변의 적 공격

F  즉발
   공통 F 규칙 + 짧은 거리 구르기
```

---

## 32.2 마법사

느린 공격, 긴 사거리, 강한 피해. 마나 관리가 중요하고 기동성은 낮다.

Q / W / E는 적중 시 최대 마나의 일정 퍼센트를 회복한다. R은 보유 마나를 전부 소모하고, D는 장판 피해마다 마나를 회복한다.

```text
Q  즉발 지점
   화염구를 포물선으로 던져 해당 지역에 피해
   적중 시 최대 마나의 일정 퍼센트 회복

W  차징 방향
   전방 부채꼴 범위를 공격
   차징이 길어질수록 사거리 증가
   적중 시 최대 마나의 일정 퍼센트 회복

E  캐스팅 방향
   투사체를 전방 일직선으로 발사
   적중 시 최대 마나의 일정 퍼센트 회복

R  캐스팅 지점
   지점에 보유 마나를 전부 소모해 운석을 떨굼
   소모한 마나 비율에 비례해 최종 피해량 증가
   적중 시 일정 시간 마나 회복률이 증가하는 버프

D  즉발 지점
   화염 장판을 설치
   적이 피해를 입을 때마다 마나 일정 회복

F  즉발
   공통 F 규칙 + 긴 거리 점멸
```

---

## 32.3 궁수

빠른 공격, 기본 공격 위주의 전투. D를 on/off로 바꿔 가며 싸운다.

```text
Q  즉발 방향
   D on  : 일자로 나아가는 화살 하나 발사
   D off : 전방 짧은 범위로 부채꼴 화살 여러 발

W  즉발 타게팅
   적에게 화살을 여러 발 쏴 피해

E  즉발 지점
   뒤로 조금 이동하며 해당 지점에 화살 비를 내림
   이펙트만 다단이고, 실제 피해는 한 번

R  차징 방향
   힘을 모아 강력한 화살을 전방에 발사
   민첩함 버프가 있으면 가하는 피해 증가

D  on/off
   기본 공격을 변경하는 모드 전환
   on  : 사거리 증가, 공격 피해 증가, 공격 속도 감소
   off : 사거리 감소, 공격 피해 감소, 공격 속도 증가

F  즉발
   공통 F 규칙 + 일정 거리 이동
   사용 이후 일정 시간 민첩함 부여
   민첩함 효과:
     D on  : 피해가 추가로 증가
     D off : 공격 속도가 추가로 증가
   기본 공격을 적에게 맞추면 이 스킬의 쿨타임이 고정 시간만큼 감소
```

---

## 32.4 성직자

아군을 보호한다. 공격력은 낮다.

```text
Q  즉발 지점
   지점에 빛의 폭발을 발생
   적 명중 시 본인 마나 일정 퍼센트 회복

W  즉발 방향
   성스러운 빛을 일자로 발사
   맞은 적은 피해, 아군은 회복

E  즉발 아군 타게팅
   아군으로 빠르게 이동
   아군과 본인에게 실드

R  즉발
   모든 아군에게 대량의 실드

D  장판
   장판을 설치
   장판 위의 아군이 지속적으로 체력 회복

F  즉발
   공통 F 규칙 + 일정 거리 이동
```

---

# 33. 구현 순서

현재까지의 진행 상태를 단계에 표시한다.

## 1단계: 솔루션 구성 ✅

```text
Raid.LobbyServer
Raid.GameServer
Raid.Battle
Raid.Contracts
Raid.ServiceContracts
Raid.Battle.Tests
```

## 2단계: 전투 월드 골격 ✅

* `BattleWorld`
* `WorldLoop`
* `BattleEntity`
* `EntityRegistry`
* `BattleEventBuffer`

## 3단계: 명령과 이동 ✅

* `IWorldCommand`
* `CommandSystem`
* `MoveCommand`
* `MovementSystem`
* 위치 보정 (`PositionCorrectionSystem`)

## 4단계: 행동 단계 ✅ (초기)

* `GameAction`
* `Casting`
* `Windup`
* `Activation`
* `Recovery`
* 시전 중 이동 취소
* Recovery 대기 명령
* `Channeling`은 이후 단계에서 보강

## 4.5단계: Practice 모드 수직 슬라이스 ✅

* `RaidMode.Practice`
* `DummyEntity`
* `BattleWorldFactory` Practice 프리셋
* 테스트용 클래스(`test`)와 타입별 대표 스킬
* `UseSkillCommand`
* 최소 피해 적용 (`DamageSystem`)
* 일반 `RaidSession` / `RaidSessionRegistry`
* 즉시형·시전형 스킬로 허수아비 타격 검증

## 4.6단계: 전투 네트워크 최소 연동 ✅

* `CommandSystem` 스레드 안전 큐
* 고정 틱 스케줄러 (`RaidSimulationHostedService`)
* `BattleHub` (`/hubs/battle`) + `JoinSession`
* 최소 스냅샷/이벤트 DTO (`Raid.Contracts`)
* `BattleTick` 푸시
* Unity 접속용 CORS
* 연습장은 별도 Hub가 아니라 `RaidMode.Practice`

5단계 피해/효과 고도화는 이 연동 이후에 계속한다.

## 5단계: 피해와 효과

* 명중 요청
* 피해와 회복 고도화
* 실드
* 상태 효과
* 행동 제약
* 연습장에서 효과 스킬 검증

## 6단계: 다운과 부활

* 다운 HP
* 사망
* 이동 후 부활
* 부활 채널

## 7단계: 어그로와 보스 AI

* 타깃
* 어그로
* 도발
* 패턴 선택
* 이동 폴백
* 반복 감쇠

## 8단계: `RaidStateMachine`

* 준비
* 시작
* 페이즈 전환
* 전멸
* 재도전
* 클리어

## 9단계: `Raid.GameServer`

* `RaidSession`
* `RaidScheduler`
* `BattleHub`
* 전투 토큰 검증
* 이벤트와 스냅샷 전송
* `JoinSession(Mode = Raid)` 확장

## 10단계: `Raid.LobbyServer`

* 방
* 준비 상태
* 클래스와 스킬 선택
* 게임 서버 등록
* 게임 서버 할당
* 세션 생성 요청
* 접속 정보 반환
* 결과 수신
* (선택) 연습장 입장 진입점

## 11단계: 실제 콘텐츠

* 플레이어 직업 (전사, 마법사, 궁수, 성직자)
* Q / W / E / R / D / F 스킬 (`32. 플레이어 클래스`)
* 보스 패턴
* 3페이즈
* 수치 밸런스
* 연출

---

# 34. 최종 명칭

```text
게임 및 솔루션
Raid
Raid.sln

로비·방·매칭 서버
Raid.LobbyServer

실제 전투 실행 서버
Raid.GameServer

순수 전투 라이브러리
Raid.Battle

클라이언트 공개 계약
Raid.Contracts

서버 간 내부 계약
Raid.ServiceContracts

상위 전투 상태 머신
RaidStateMachine

전투 한 판 실행 객체
RaidSession

전투 시뮬레이션 상태
BattleWorld

공격하지 않는 연습 타깃
DummyEntity

세션 모드
RaidMode

테스트용 클래스
test / TestClassDefinition

플레이어 클래스
전사
마법사
궁수
성직자
```

---

# 35. 최종 구조 요약

```text
Raid.LobbyServer
- 로그인 이후 로비와 방 관리
- 매칭
- 게임 서버 할당
- 결과 저장

Raid.GameServer
- 실제 전투 연결
- 여러 RaidSession 실행 (Practice | Raid)
- BattleWorld 업데이트
- 결과 보고

Raid.Battle
- 이동, 행동, 피해, 상태 효과
- 위치 보정
- 모드별 월드 프리셋과 DummyEntity
- 다운과 부활
- 어그로와 보스 AI
- RaidStateMachine

Raid.Contracts
- Unity와 공유하는 DTO

Raid.ServiceContracts
- 서버 간 통신 DTO
```

핵심 구조는 다음과 같다.

```text
방과 매칭
→ Raid.LobbyServer

실제 전투 (Practice | Raid)
→ Raid.GameServer

전투 규칙
→ Raid.Battle
```

초기 검증 경로:

```text
Practice 모드
→ RaidSession (Mode = Practice)
→ PlayerEntity(TestClass) + DummyEntity
→ MoveCommand / UseSkillCommand
→ GameAction / DamageSystem
```
