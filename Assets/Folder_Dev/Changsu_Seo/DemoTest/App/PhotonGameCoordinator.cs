using System;
using System.Linq;
using Buckshot.Contracts;
using Buckshot.Core;
using Buckshot.PhotonInfra;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Buckshot.App
{
    [RequireComponent(typeof(PhotonView))]
    public class PhotonGameCoordinator : MonoBehaviourPunCallbacks, INetTransport
    {
        [Header("Config")]
        [SerializeField] int startHp = 3;
        [SerializeField] int liveCount = 2;
        [SerializeField] int blankCount = 4;
        [SerializeField] private string playerPrefabName = "PlayerRig";
        [SerializeField] private Transform spawnPointsParent;
        [SerializeField] private TurnManager turnManager;

        private PhotonRoomStore _store;
        private IRuleEngine _ruleEngine;
        private IShellDeckBuilder _deckBuilder;
        private IFirstTurnPolicy _turnPolicy;

        // 중복 방지용
        private bool gameStarted = false;

        // ────────────────────────────── Unity / PUN 라이프사이클 ──────────────────────────────
        void Awake()
        {
            _ruleEngine = new BasicRuleEngine();
            _deckBuilder = new DefaultShellDeckBuilder();
            _turnPolicy = new RandomFirstTurnPolicy();
        }

        public override void OnJoinedRoom()
        {
            _store = new PhotonRoomStore(PhotonNetwork.CurrentRoom);
            TrySpawnPlayerRig();

            if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom.PlayerCount == 2)
                Host_StartNewGame();
        }
        private void TrySpawnPlayerRig()
        {
            if (spawnPointsParent == null)
            {
                Debug.LogError("[Coord] SpawnPointsParent가 설정되지 않았습니다!");
                return;
            }

            // 플레이어 인덱스(0,1,2...) 결정
            int playerIndex = PhotonNetwork.CurrentRoom.PlayerCount - 1;

            // 해당 인덱스의 스폰 위치 선택
            Transform spawn = spawnPointsParent.GetChild(playerIndex % spawnPointsParent.childCount);
            Vector3 pos = spawn.position;
            Quaternion rot = spawn.rotation;

            // ✅ 네트워크 상에서 플레이어 생성
            GameObject rig = PhotonNetwork.Instantiate(playerPrefabName, pos, rot);

            // 자기 자신(PlayerHealth, RevolverTurn 등 참조용)
            PhotonNetwork.LocalPlayer.TagObject = rig;

            Debug.Log($"[Coord] PlayerRig 생성 완료 ({rig.name}) at SpawnPoint {spawn.name}");
        }
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            // 🔹 새로 입장한 플레이어에게 자신의 PlayerRig 생성 요청
            photonView.RPC(nameof(RPC_SpawnPlayerRig), newPlayer);

            // 🔹 인원 2명일 때 게임 시작
            if (PhotonNetwork.CurrentRoom.PlayerCount == 2 && !gameStarted)
            {
                if (_store == null)
                    _store = new PhotonRoomStore(PhotonNetwork.CurrentRoom);

                if (_store.Shells.Length == 0)
                {
                    gameStarted = true;
                    Host_StartNewGame();
                }
            }
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            if (!PhotonNetwork.IsMasterClient) return;

            if (_store == null)
                _store = new PhotonRoomStore(PhotonNetwork.CurrentRoom);

            if (_store.Shells.Length == 0)
                Host_StartNewGame();
        }

        // ────────────────────────────── PlayerRig 생성 관리 ──────────────────────────────

        [PunRPC]
        private void RPC_SpawnPlayerRig()
        {
            TrySpawnPlayerRig();
        }

        //private void TrySpawnPlayerRig()
        //{
        //    // 이미 생성된 경우 무시
        //    if (PhotonNetwork.LocalPlayer.TagObject != null)
        //        return;

        //    // Resources 폴더에서 PlayerRig 프리팹 로드
        //    GameObject prefab = Resources.Load<GameObject>("PhotonPrefabs/PlayerRig");
        //    if (prefab == null)
        //    {
        //        Debug.LogError("[Coord] PlayerRig prefab을 Resources/PhotonPrefabs/ 폴더에 넣어주세요!");
        //        return;
        //    }

        //    // 마스터와 일반 플레이어의 위치 구분
        //    Vector3 spawnPos = PhotonNetwork.IsMasterClient
        //        ? new Vector3(-1.2f, 0f, 0f)
        //        : new Vector3(1.2f, 0f, 0f);

        //    GameObject rig = PhotonNetwork.Instantiate(prefab.name, spawnPos, Quaternion.identity);
        //    PhotonNetwork.LocalPlayer.TagObject = rig;

        //    Debug.Log($"[Coord] PlayerRig 생성 완료: {PhotonNetwork.LocalPlayer.NickName}");
        //}

        // ────────────────────────────── 게임/라운드 세팅 ──────────────────────────────

        private void Host_StartNewGame()
        {
            Debug.Log("[Coord] Host_StartNewGame()");

            foreach (var actor in _store.AllActors)
                _store.SetHp(actor, startHp);

            Host_SetupNewRound();
            BroadcastGameStart();
        }

        private void Host_SetupNewRound()
        {
            int seed = UnityEngine.Random.Range(0, int.MaxValue);
            RoundSetup.InitializeRound(_store, _deckBuilder, _turnPolicy, seed, liveCount, blankCount);

            Debug.Log($"[Coord] Host_SetupNewRound(): seed={seed}, shells={_store.Shells.Length}, firstTurn={_store.CurrentTurnActor}");
            BroadcastNewRound();
        }

        // ────────────────────────────── 호스트: 발사 처리 ──────────────────────────────

        [PunRPC]
        private void RPC_HostHandleShoot(int shooterActor, int targetActor, PhotonMessageInfo info)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (info.Sender.ActorNumber != shooterActor) return;

            var state = new ReadonlyGameStateAdapter(_store, me => GetOpponent(me));

            ShotResult result;
            try
            {
                result = _ruleEngine.ResolveShot(state, new ShootRequest
                {
                    ShooterActor = shooterActor,
                    TargetActor = targetActor
                });
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Coord] Invalid shoot: {e.Message}");
                return;
            }

            if (!result.IsRoundOver)
            {
                RoundSetup.ApplyShotResult(_store, result);

                bool liveEmpty = true;
                for (int i = _store.ShellIndex; i < _store.Shells.Length; i++)
                {
                    if (_store.Shells[i] == ShellType.Live)
                    {
                        liveEmpty = false;
                        break;
                    }
                }

                bool deckExhausted = liveEmpty;
                bool someoneDead = _store.AllActors.Any(a => _store.GetHp(a) <= 0);
                if (deckExhausted || someoneDead)
                    result.IsRoundOver = true;

                BroadcastShotResult(result);

                if (someoneDead)
                {
                    int winner = _store.AllActors.OrderByDescending(a => _store.GetHp(a)).FirstOrDefault();
                    BroadcastGameOver(winner);
                    gameStarted = false; // 재시작 가능 상태로
                    Host_StartNewGame();
                }
                else if (deckExhausted)
                {
                    Host_SetupNewRound();
                }
            }
            else
            {
                BroadcastShotResult(result);
                Host_SetupNewRound();
            }
        }

        private int GetOpponent(int me)
        {
            foreach (var a in _store.AllActors)
                if (a != me) return a;
            return me;
        }

        // ────────────────────────────── INetTransport 구현 ──────────────────────────────

        public void SendShootRequestToHost(ShootRequest req)
        {
            photonView.RPC(nameof(RPC_HostHandleShoot), RpcTarget.MasterClient, req.ShooterActor, req.TargetActor);
        }

        public void BroadcastShotResult(ShotResult result)
        {
            photonView.RPC(nameof(RPC_ClientOnShotResult), RpcTarget.All,
                result.ShooterActor, result.TargetActor, (int)result.Shell,
                result.NewTargetHp, result.IsRoundOver, result.NextTurnActor);
        }

        public void BroadcastNewRound() => photonView.RPC(nameof(RPC_ClientOnNewRound), RpcTarget.All);
        public void BroadcastGameStart() => photonView.RPC(nameof(RPC_ClientOnGameStart), RpcTarget.All);
        public void BroadcastGameOver(int winnerActor) => photonView.RPC(nameof(RPC_ClientOnGameOver), RpcTarget.All, winnerActor);

        // ────────────────────────────── 클라이언트 수신 RPC ──────────────────────────────

        [PunRPC]
        private void RPC_ClientOnShotResult(int shooter, int target, int shell, int newTargetHp, bool isRoundOver, int nextTurn)
        {
            var players = FindObjectsOfType<PlayerHealth>();
            foreach (var ph in players)
            {
                int actor = ph.GetComponent<PhotonView>()?.Owner?.ActorNumber ?? -1;
                if (actor == target)
                {
                    ph.ApplyNetworkedDamage(newTargetHp);
                    break;
                }
            }

            turnManager.SetTurnByActor(nextTurn);

            Buckshot.UI.UIManager.I?.OnShotResolved(
                shooter, target, (ShellType)shell, newTargetHp, isRoundOver, nextTurn
            );
        }

        [PunRPC] private void RPC_ClientOnNewRound() => Buckshot.UI.UIManager.I?.OnNewRound();
        [PunRPC] private void RPC_ClientOnGameStart() => Buckshot.UI.UIManager.I?.OnGameStart();
        [PunRPC] private void RPC_ClientOnGameOver(int winnerActor) => Buckshot.UI.UIManager.I?.OnGameOver(winnerActor);

        // ────────────────────────────── 버튼 핸들러 ──────────────────────────────

        public void TryShootSelf()
        {
            if (!PhotonNetwork.InRoom) return;
            var me = PhotonNetwork.LocalPlayer.ActorNumber;
            SendShootRequestToHost(new ShootRequest { ShooterActor = me, TargetActor = me });
        }

        public void TryShootOpponent()
        {
            if (!PhotonNetwork.InRoom) return;
            var me = PhotonNetwork.LocalPlayer.ActorNumber;
            var opp = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber != me)?.ActorNumber ?? -1;
            if (opp == -1) return;
            SendShootRequestToHost(new ShootRequest { ShooterActor = me, TargetActor = opp });
        }
    }
}
