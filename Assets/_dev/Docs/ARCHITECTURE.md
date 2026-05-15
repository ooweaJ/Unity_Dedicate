# 아키텍처

## 1. 전체 시스템 구성

```
┌──────────────────────────────────────────────────────────────┐
│                    CLIENT (모바일 / PC)                       │
│         Boot → Login → Loading → Lobby ↔ Battle             │
└────────────┬─────────────────────────┬───────────────────────┘
             │ HTTP REST               │ KCP (UDP / Mirror)
             ▼                         ▼
┌─────────────────────┐   ┌────────────────────────────────────┐
│    Backend API      │   │           Game Servers             │
│  api.jaewoo98.store │   │                                    │
│                     │   │  LobbyServer     :7777             │
│  /users/login       │   │  BattleServer    :7778             │
│  /match/acquire     │   │  BattleServer    :7779             │
│  /match/release     │   │  BattleServer    :7780             │
│  /gacha/draw        │   │                                    │
│  /inventory/*       │   │  (배틀 포트는 매치마다 동적 할당)    │
│  /equipment/enhance │   │  Mirror + KCP2K Transport          │
│  /users/transcend   │   └────────────────────────────────────┘
└─────────────────────┘
```

---

## 2. 씬 흐름

```
[BootScene]
    │ BootManager → LoginScene 로드
    ▼
[LoginScene]
    │ LoginController.HandleLogin()
    │ BackendManager.Login() ──────────────────▶ api/users/login
    │ PlayerDataManager.ApplyUserData()         ◀ userData JSON
    │   └─ inventory (캐릭터/장비/아이템) 복원
    │   └─ selectedCharacter 복원
    ▼
[LoadingScene]  (서버 연결)
    │ KcpTransport.port = 7777
    │ CustomNetworkManager.networkAddress = ServerConfig.GameServerIP
    │ StartClient() → 로비 서버 접속
    │ NetworkClient.Ready() + AddPlayer()
    │   └─ MyAuthenticator: AuthRequestMessage 전송
    │       { userId, nickname, selectedCharacter, stats }
    ▼
[MainLobbyScene]
    │ LobbyController
    │  ├─ 인벤토리 → InventoryController (장비 장착/강화/초월)
    │  ├─ 상점     → ShopController (가챠)
    │  └─ 매칭     → LobbyNetworkPlayer.CmdRequestMatch()
    │
    │  [서버] matchQueue 4명 → StartMatch()
    │    BackendManager.AcquirePort() ───────▶ api/match/acquire
    │    포트 수신                            ◀ { port: 7778 }
    │    TargetMoveToServer(ip, port) → 4명 클라에 전송
    │
    │  StopClient()
    │  SceneFlowManager.Load("BattleScene", ip, port)
    ▼
[LoadingScene]  (배틀 서버 연결)
    │ KcpTransport.port = 7778
    │ StartClient() → 배틀 서버 접속
    ▼
[BattleScene]
    │ BattleNetworkPlayer 스폰
    │   └─ teamId 배정 (0:남쪽 / 1:북쪽, 교대)
    │   └─ SpawnManager.GetSpawnPositionForTeam()
    │ BattleManager.RegisterPlayer() × 4 → StartBattle()
    │   └─ 전투 타이머 180초
    │
    │ [전투 종료]
    │ BattleManager.EndBattle()
    │   BackendManager.PostBattleResult() ──▶ api/users/:id/battle-result
    │   RpcShowResultAndPreload() → 결과 UI + 로비 씬 사전 로드
    │   BackendManager.ReleasePort() ───────▶ api/match/release
    │
    │  ReturnToLobby() → StopClient()
    ▼
[LoadingScene] → [MainLobbyScene]  (반복)
```

---

## 3. 네트워크 구조

### 서버 타입 분기

```
CustomNetworkManager.OnServerAddPlayer()
    │
    ├─ serverType == "lobby"
    │   └─ LobbyNetworkPlayer 스폰
    │       ├─ 매칭 큐 관리 (matchQueue[])
    │       └─ TargetRpc: TargetMoveToServer(ip, port)
    │
    └─ serverType == "battle"
        └─ BattleNetworkPlayer 스폰
            ├─ authData로 userId/nickname/stats 초기화
            ├─ teamId 배정 (playerCount % 2)
            ├─ SpawnManager.GetSpawnPositionForTeam(teamId)
            └─ BattleManager.RegisterPlayer()
```

### 통신 패턴 요약

```
CLIENT                              SERVER
  │                                   │
  │── CmdRequestMatch() ─────────────▶│ matchQueue 추가
  │                                   │ 4명 → AcquirePort() → StartMatch()
  │◀─ TargetMoveToServer(ip, port) ───│
  │                                   │
  │── CmdUseAttack(skillId, dir) ────▶│ ExecuteSkill()
  │                                   │  → TakeDamage() 판정
  │◀─ (SyncVar) currentHp ───────────│ HP 자동 동기화
  │◀─ RpcShowDamagePopup() ──────────│
  │◀─ RpcShowResultAndPreload() ─────│ 전투 종료 시
```

### 인증 흐름

```
MyAuthenticator.OnClientAuthenticate()
    PlayerDataManager → selectedCharacter, inventory
    StatUtils.Calculate() → CharacterStatData
    AuthRequestMessage 전송 { userId, nickname, level, stats }
         │
         ▼ (서버)
    OnAuthRequestMessage()
    conn.authenticationData = authData
    ServerAccept(conn)
         │
         ▼
    OnServerAddPlayer() → authData 꺼내서 NetworkPlayer 초기화
```

---

## 4. 전투 데이터 흐름

```
[입력 레이어]
MobileInputProvider ──┐
PlayerInputHandler  ──┘ IPlayerInputProvider
                        │
                        ▼
                   PlayerController  (라우터)
                    │           │
                    ▼           ▼
             PlayerMovement  PlayerCombat
              └─ Move()       └─ HandleSkillReleased(skillId)
              └─ Jump()           └─ CharacterWeapon.UseSkill()
              └─ StatusEffect              └─ CmdUseAttack() ──▶ [서버]

[서버 판정]
CharacterWeapon.ExecuteSkill()
    ├─ PerformMelee()
    │   Physics.OverlapSphere → IDamageable.TakeDamage()
    ├─ PerformProjectile()
    │   NetworkSpawn(ProjectileBase) → OnTriggerEnter → TakeDamage()
    └─ PerformDash()
        PlayerMovement.StartDash()

[데미지 처리]
PlayerStats.TakeDamage(DamageInfo)
    ├─ 팀 체크 (같은 팀 스킵)
    ├─ 최종 데미지 = ATK - DEF (최소 1)
    ├─ currentHp -= damage  ← SyncVar (자동 동기화)
    ├─ StatusEffectHandler.Apply() (기절/슬로우/넉백)
    ├─ BattleManager.RecordDamage(attacker, damage)
    ├─ RpcShowDamagePopup()
    └─ HP ≤ 0 → OnDead()
        └─ BattleManager.OnPlayerDead()
            └─ 팀 전멸 확인 → EndBattle()
```

---

## 5. 스탯 파이프라인

```
CharacterTableSO (마스터 데이터)
  baseHp / baseAtk / baseDef
        │
        │ + 장비 보너스
        │   atkBonus / defBonus
        │   (강화 단계마다 기본값의 10% 추가)
        ▼
StatUtils.Calculate(characterType, inventory)
        │
        ▼
CharacterStatData { finalMaxHp, finalAtk, finalDef }
        │
   ┌────┴────────┐
   │             │
인증 전송      SyncVar 동기화
(로그인 시)   (BattleNetworkPlayer.stats)
```

---

## 6. 경험치 계산

```
ExpCalculator.Calculate(result, kills, totalDamage)

승리: 150 + (킬 × 30) + (딜량 / 10)
패배:  50 + (킬 × 30) + (딜량 / 10)
무승부: 80 + (딜량 / 10)

랭크포인트:
  승리 +20 / 패배 -15 / 무승부 +5
```

---

## 7. 주요 매니저 책임

| 클래스 | 생존 범위 | 역할 |
|--------|-----------|------|
| `BootManager` | BootScene | 첫 씬 로드 |
| `SceneFlowManager` | 전체 | LoadRequest 전달, 씬 전환 |
| `PlayerDataManager` | 전체 | 유저 런타임 데이터 저장소 |
| `GameDataManager` | 전체 | 마스터 데이터 (ScriptableObject) |
| `BackendManager` | 전체 (static) | HTTP API 호출 |
| `CustomNetworkManager` | 전체 | Mirror 서버/클라 생명주기 |
| `LoadingSceneManager` | LoadingScene | 서버 연결 + 로딩 UI (5초 타임아웃) |
| `BattleManager` | BattleScene | 전투 상태/결과 판정 |
| `SpawnManager` | BattleScene | 팀별 스폰 위치 |
| `LobbyController` | MainLobbyScene | 로비 UI 이벤트 |
| `ShopController` | MainLobbyScene | 가챠 |
| `InventoryController` | MainLobbyScene | 장비/강화/초월 |

---

## 8. 서버 설정

```csharp
// ServerConfig.cs — 이 파일 하나만 바꾸면 됨
GameServerIP   = "192.168.1.39"  // 로컬 테스트: PC 로컬 IP
                                  // 배포 시: 공인 서버 IP로 교체
LobbyPort      = 7777
BattlePortBase = 7778             // 실제 포트는 AcquirePort()가 할당
```

---

## 9. 주요 패턴

| 패턴 | 사용 클래스 |
|------|------------|
| Singleton | GameDataManager, PlayerDataManager, BattleManager, LobbyController 등 |
| ScriptableObject | GameDatabase, CharacterDataSO, AttackDataSO, ItemTableSO |
| SyncVar | currentHp, teamId, selectedCharacter, stats, isStunned |
| Command | CmdRequestMatch, CmdUseAttack |
| TargetRpc | TargetMoveToServer |
| ClientRpc | RpcShowDamagePopup, RpcShowResultAndPreload |
| Interface | IDamageable (PlayerStats), IScoreService (MockScoreService) |
| Abstract | ProjectileBase → ExplosiveProjectile, BaseSlot → InventorySlot |
