using Buckshot.Contracts;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Buckshot.UI
{
    /// <summary>
    /// UI 레이어.
    /// 룸 프로퍼티를 읽어 현재 상태를 표시하고, 조작 버튼을 담당한다.
    /// 네트워크/규칙 엔진 구현 세부에는 의존하지 않는다.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager I;

        [Header("UI References")]
        public TextMeshProUGUI turnText;
        public TextMeshProUGUI myHpText;
        public TextMeshProUGUI oppHpText;
        public Button btnShootSelf;
        public Button btnShootOpp;

        [Header("Coordinator")]
        public Buckshot.App.PhotonGameCoordinator coordinator;

        void Awake() => I = this;

        /// <summary>
        /// 버튼 바인딩 및 초기 상태 갱신.
        /// </summary>
        void Start()
        {
            btnShootSelf.onClick.AddListener(() => coordinator.TryShootSelf());
            btnShootOpp.onClick.AddListener(() => coordinator.TryShootOpponent());
            RefreshAll();
        }

        /// <summary>
        /// 룸 프로퍼티를 기반으로 전체 UI를 갱신한다.
        /// </summary>
        public void RefreshAll()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

            var room = PhotonNetwork.CurrentRoom;

            int my = PhotonNetwork.LocalPlayer.ActorNumber;
            int opp = -1;
            foreach (var p in PhotonNetwork.PlayerList)
                if (p.ActorNumber != my) { opp = p.ActorNumber; break; }

            int turn = room.CustomProperties.TryGetValue("turnActor", out object t) ? (int)t : -1;
            int myHp = room.CustomProperties.TryGetValue("hp_" + my, out object h1) ? (int)h1 : 0;
            int oppHp = (opp != -1 && room.CustomProperties.TryGetValue("hp_" + opp, out object h2)) ? (int)h2 : 0;

            turnText.text = (turn == my) ? "내 턴" : "상대 턴";
            myHpText.text = $"ME HP: {myHp}";
            oppHpText.text = $"OPP HP: {oppHp}";

            bool myTurn = (turn == my);
            btnShootSelf.interactable = myTurn;
            btnShootOpp.interactable = myTurn && opp != -1;
        }

        /// <summary>
        /// 판정 결과 수신 시 호출된다.
        /// 이펙트/사운드 트리거 후 UI를 갱신한다.
        /// </summary>
        public void OnShotResolved(int shooter, int target, ShellType shell, int newTargetHp, bool isRoundOver, int nextTurn)
        {
            // TODO: 카메라 셰이크, 사운드, 파티클 등 연출 트리거
            Debug.Log($"Shot resolved: {shooter}->{target}, {shell}, HP={newTargetHp}, roundOver={isRoundOver}");
            RefreshAll();
        }

        /// <summary>
        /// 새 라운드 시작 시 호출된다. UI 초기화 연출을 수행한다.
        /// </summary>
        public void OnNewRound()
        {
            // TODO: 라운드 시작 애니메이션, 탄 UI 초기화 등
            Debug.Log("New Round");
            RefreshAll();
        }
        public void OnGameStart()
        {
            // 배너/토스트/라운드 UI 초기화 등
        }

        public void OnGameOver(int winnerActor)
        {
            // 승자 표시, 재시작 버튼 노출 등
        }
    }
}
