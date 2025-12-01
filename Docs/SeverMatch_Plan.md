# SeverMatchScene 사전 분석

## 1. `SeverDemmoLoomdd` 씬의 PUN2 네트워크 흐름
- **서버 접속**: `Buckshot.PhotonInfra.NetworkLauncher`가 `Start` 시 자동 접속 옵션에 따라 `ConnectUsingSettings`를 호출하며 PUN 게임 버전과 동기화 설정을 초기화한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/Net/NetworkLauncher.cs†L13-L24】
- **마스터 서버 연결 후 룸 생성/입장**: 같은 런처에서 `OnConnectedToMaster` 시 `JoinOrCreateRoom`을 사용해 2인 룸을 찾거나 만든다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/Net/NetworkLauncher.cs†L26-L39】
- **플레이어 생성/입장 처리**: `PhotonGameCoordinator`가 `OnJoinedRoom`에서 `PhotonRoomStore`를 준비하고 스폰 포인트에 `PhotonNetwork.Instantiate`로 `PlayerRig`를 생성해 `TagObject`로 보관한다. 마스터는 인원 2명이 되면 게임을 시작한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/App/PhotonGameCoordinator.cs†L39-L90】
- **추가 입장/퇴장 대응**: 마스터는 `OnPlayerEnteredRoom`에서 새 플레이어에게 RPC로 리그 생성 명령을 보내고 인원 충족 시 라운드를 초기화한다. 마스터 변경 시에도 빈 라운드일 경우 새 게임을 시작한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/App/PhotonGameCoordinator.cs†L71-L101】
- **턴/게임 상태 전환**: 호스트는 `Host_StartNewGame`와 `Host_SetupNewRound`에서 HP와 탄창(실탄/공포탄) 덱을 세팅하고 라운드/게임 시작을 브로드캐스트한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/App/PhotonGameCoordinator.cs†L138-L156】
- **발사 처리 및 결과 동기화**: 호스트 RPC `RPC_HostHandleShoot`가 요청 유효성(턴 주인, 발신자) 확인 후 `BasicRuleEngine`으로 탄종과 HP/턴 결과를 계산하고 커스텀 프로퍼티를 갱신한다. 라운드 종료 여부에 따라 다음 라운드/게임오버를 브로드캐스트한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/App/PhotonGameCoordinator.cs†L160-L220】【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/Game/GameCore.cs†L66-L148】
- **클라이언트 동기화**: `RPC_ClientOnShotResult` 등에서 타겟 `PlayerHealth`에 HP를 적용하고 `TurnManager.SetTurnByActor`로 턴을 맞춘다. 버튼 핸들러 `TryShootSelf/TryShootOpponent`는 자신의 액터 번호로 호스트 RPC를 요청한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/App/PhotonGameCoordinator.cs†L248-L291】

## 2. `BotMatchScene`(싱글) 게임 규칙/턴/총/HP 흐름
- **턴 관리**: `TurnManager`가 플레이어 리스트를 시계방향 정렬하고 현재 턴을 기억한다. `SetTurnByActor`는 네트워크 액터 번호나 수동 호출로 턴을 강제 지정하며 `GetCurrentActor/GetCurrentPlayer`로 입력 허용 주체를 판단한다.【F:Assets/Folder_Dev/Changsu_Seo/BackUp/CGR_Script/TurnManager.cs†L7-L214】
- **총 조작/턴 연동**: `RevolverTurnPossession`는 `TurnManager`와 연동해 현재 턴 소유자만 입력을 처리하고, 카메라/픽업/자기 사격 시퀀스를 관리한다. `Update`에서 자신의 턴이 아니면 입력을 무시하고, 총 집기/조준/자기 사격 입력 시 `PhotonGameCoordinator`로 발사 요청을 위임한다.【F:Assets/Folder_Dev/Changsu_Seo/BackUp/CGR_Script/RevolverTurnPossession.cs†L119-L200】
- **발사 입력/유효 타겟 판정**: `RevolverController`는 총을 든 상태에서만 입력을 받고, 좌클릭 시 카메라 정면으로 `Raycast`하여 유효 타겟(`hitMask`)을 맞춘 경우에만 `OnAimRequest` 이벤트로 조준/발사 시퀀스를 트리거한다. 발사 후에는 리코일 코루틴으로 반동 연출을 기다린다.【F:Assets/Folder_Dev/Changsu_Seo/BackUp/CGR_Script/RevolverController.cs†L13-L140】
- **탄창/턴 규칙**: 네트워크 흐름과 공유되는 `BasicRuleEngine`과 `RoundSetup`이 실탄/공포탄 덱을 셔플하고 현재 턴/HP를 업데이트한다. 실탄이면 상대 HP를 1 감소시키고 턴을 상대에게 넘기며, 공포탄이면 HP를 유지하고 사수 턴을 유지한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/Game/GameCore.cs†L29-L148】
- **HP 관리/사망 처리**: `PlayerHealth`는 `TakeDamage`로 실탄만 HP 감소·사망 이벤트를 발생시키고 공포탄(0 데미지)은 로그만 남긴다. 체력이 0 이하가 되면 `OnDied`를 호출하고 필요 시 오브젝트를 비활성화/파괴하며, 네트워크 동기화용 `ApplyNetworkedDamage`도 제공한다.【F:Assets/Folder_Dev/Changsu_Seo/BackUp/CGR_Script/PlayerHealth.cs†L13-L159】

## 3. `SeverMatchScene` 설계 초안
- **재사용 예정 컴포넌트**
  - 접속/룸 로직은 `Buckshot.PhotonInfra.NetworkLauncher`를 그대로 사용해 Photon 설정과 룸 조인을 담당한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/Net/NetworkLauncher.cs†L13-L39】
  - 게임 규칙/탄창/턴 계산은 `PhotonGameCoordinator` + `BasicRuleEngine` + `RoundSetup` 조합을 활용해 호스트 계산·브로드캐스트를 유지한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/App/PhotonGameCoordinator.cs†L138-L220】【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/Game/GameCore.cs†L66-L148】
  - 턴 입력 차단/총 이동 UI는 `TurnManager`, `RevolverTurnPossession`, `RevolverController`, `PlayerHealth` 등 Bot 씬에서 검증된 플레이어 로직을 공유한다.【F:Assets/Folder_Dev/Changsu_Seo/BackUp/CGR_Script/TurnManager.cs†L7-L214】【F:Assets/Folder_Dev/Changsu_Seo/BackUp/CGR_Script/RevolverTurnPossession.cs†L119-L200】【F:Assets/Folder_Dev/Changsu_Seo/BackUp/CGR_Script/RevolverController.cs†L13-L140】【F:Assets/Folder_Dev/Changsu_Seo/BackUp/CGR_Script/PlayerHealth.cs†L13-L159】
- **어댑터/래퍼 제안**
  - **로컬·원격 입력 분리**: `RevolverTurnPossession`가 `PhotonNetwork.InRoom`/`IsMyTurn` 체크 후 입력을 막는 구조이므로, 멀티 전용 씬에서는 로컬 플레이어만 이 스크립트를 활성화하고 원격 뷰어용 별도 뷰 스크립트를 두는 래퍼를 추가한다.【F:Assets/Folder_Dev/Changsu_Seo/BackUp/CGR_Script/RevolverTurnPossession.cs†L174-L200】
  - **스폰 연동**: `PhotonGameCoordinator.TrySpawnPlayerRig`가 `spawnPointsParent` 기반으로 프리팹을 생성하므로, 새로운 씬의 스폰 포인트/프리팹 이름을 설정하는 전용 설정 ScriptableObject 혹은 컴포넌트를 만들어 씬 의존성을 축소한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/App/PhotonGameCoordinator.cs†L47-L69】
  - **UI/상태 브릿지**: `RPC_ClientOnShotResult`에서 `UIManager` 호출과 턴 세팅을 동시에 수행하므로, 멀티 씬의 UI 계층을 교체하기 위한 인터페이스 어댑터를 두어 네트워크 결과를 UI와 입력 차단 로직으로 전달한다.【F:Assets/Folder_Dev/Changsu_Seo/DemoTest/App/PhotonGameCoordinator.cs†L248-L291】
