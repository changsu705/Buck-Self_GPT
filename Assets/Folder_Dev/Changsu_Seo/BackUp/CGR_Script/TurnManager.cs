using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Photon.Pun;
using Photon.Realtime;

/// <summary>
/// 테이블 중심을 기준으로 플레이어를 시계방향으로 정렬하고
/// 서버가 내려준 ActorNumber 기반으로 "진짜 턴 전환"을 수행한다.
/// - 동적 스폰 대응(플레이어 늦게 들어와도 등록/정렬 가능)
/// - 서버 브로드캐스트(nextTurnActor)에 의해 BeginTurn이 정확히 호출됨
/// </summary>
public class TurnManager : MonoBehaviour
{
    // ────────────────────────────── 필수 설정 ──────────────────────────────
    [Header("필수 설정")]
    [Tooltip("테이블(중심) Transform. (비워두면 자기 Transform 사용)")]
    public Transform tableCenter;

    [Tooltip("플레이어 Transform 목록. (비워두면 'Player' 태그로 자동 수집)")]
    public List<Transform> players = new List<Transform>();

    // ────────────────────────────── 옵션 ──────────────────────────────
    [Header("옵션")]
    [Tooltip("각도 계산 평면 (true: XZ, false: XY)")]
    public bool useXZPlane = true;

    [Tooltip("Start()에서 임의의 플레이어부터 자동 시작(로컬 테스트용)")]
    public bool autoStart = false;

    [Tooltip("Space 눌러 다음 턴(로컬 테스트용)")]
    public bool enableSpaceToNext = false;

    // ────────────────────────────── 내부 상태 ──────────────────────────────
    [SerializeField] private List<Transform> clockwiseOrder = new();
    [SerializeField] private int currentIndex = -1;
    private Transform currentPlayer;

    // ────────────────────────────── 라이프사이클 ──────────────────────────────
    private void Awake()
    {
        // 플레이어 자동 수집(씬에 미리 배치된 경우)
        if (players == null || players.Count == 0)
            RefreshPlayersFromScene();

        if (tableCenter == null)
        {
            tableCenter = transform;
            Debug.LogWarning("[TurnManager] tableCenter 미지정 → 자기 Transform 사용");
        }

        // 네트워크 스폰 타이밍을 고려해, 여기서는 컴포넌트를 끄지 않는다.
        if (players.Count >= 1)
            SortClockwise();
    }

    private void Start()
    {
        if (autoStart && clockwiseOrder.Count > 0)
        {
            currentIndex = Random.Range(0, clockwiseOrder.Count);
            BeginTurn(currentIndex);
        }
    }

    private void Update()
    {
        if (enableSpaceToNext && Input.GetKeyDown(KeyCode.Space))
            NextTurn();
    }

    // ────────────────────────────── 퍼블릭 API ──────────────────────────────

    /// <summary>서버가 브로드캐스트한 ActorNumber로 강제 턴 전환(네트워크 동기화 핵심)</summary>
    public void SetTurnByActor(int actorNumber)
    {
        // 플레이어가 아직 안 찼으면 한 번 모아본다
        if (players == null || players.Count == 0)
            RefreshPlayersFromScene();

        // 정렬 상태가 허물어졌다면 다시 정렬
        if (clockwiseOrder == null || clockwiseOrder.Count != players.Count || players.Count == 0)
            SortClockwise();

        // ActorNumber ↔ Transform 매칭(이름 비교 금지!)
        int foundClockwiseIndex = -1;
        foreach (var t in players)
        {
            var pv = t.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null && pv.Owner.ActorNumber == actorNumber)
            {
                foundClockwiseIndex = clockwiseOrder.IndexOf(t);
                break;
            }
        }

        if (foundClockwiseIndex >= 0)
        {
            BeginTurn(foundClockwiseIndex);
        }
        else
        {
            Debug.LogWarning($"[TurnManager] Actor {actorNumber} 매칭 실패 → 재정렬 후 재시도");
            SortClockwise();

            foreach (var t in players)
            {
                var pv = t.GetComponent<PhotonView>();
                if (pv != null && pv.Owner != null && pv.Owner.ActorNumber == actorNumber)
                {
                    BeginTurn(clockwiseOrder.IndexOf(t));
                    return;
                }
            }
            Debug.LogWarning($"[TurnManager] 여전히 Actor {actorNumber}를 찾지 못했습니다.");
        }
    }

    /// <summary>시계방향 다음 플레이어로 이동(로컬 테스트/임시용)</summary>
    public void NextTurn()
    {
        if (clockwiseOrder.Count == 0) return;
        int next = (currentIndex + 1) % clockwiseOrder.Count;
        BeginTurn(next);
    }

    /// <summary>현재 턴 주인의 ActorNumber 반환(입력 차단/활성화에 사용)</summary>
    public int GetCurrentActor()
    {
        var t = GetCurrentPlayer();
        if (t == null) return -1;
        var pv = t.GetComponent<PhotonView>();
        return (pv != null && pv.Owner != null) ? pv.Owner.ActorNumber : -1;
    }

    /// <summary>현재 턴 플레이어 Transform</summary>
    public Transform GetCurrentPlayer() => currentPlayer;

    /// <summary>플레이어 등록(동적 스폰 시 PlayerRig가 호출)</summary>
    public void RegisterPlayer(Transform t)
    {
        if (t == null) return;
        if (!players.Contains(t))
        {
            players.Add(t);
            SortClockwise();
        }
    }

    /// <summary>플레이어 해제(퇴장/파괴 시 PlayerRig가 호출)</summary>
    public void UnregisterPlayer(Transform t)
    {
        if (t == null) return;
        if (players.Remove(t))
        {
            SortClockwise();
            if (currentPlayer == t) currentPlayer = null;
        }
    }

    /// <summary>'Player' 태그로 씬에서 플레이어 수집</summary>
    public void RefreshPlayersFromScene()
    {
        players = GameObject.FindGameObjectsWithTag("Player").Select(go => go.transform).ToList();
        SortClockwise();
    }

    // ────────────────────────────── 내부 로직 ──────────────────────────────

    private void BeginTurn(int index)
    {
        if (clockwiseOrder.Count == 0) return;
        index = Mathf.Clamp(index, 0, clockwiseOrder.Count - 1);

        currentIndex = index;
        currentPlayer = clockwiseOrder[currentIndex];
        Debug.Log($"▶️ 턴 시작: {currentPlayer.name} (index {currentIndex})");
        // TODO: 필요하다면 여기서 UI/카메라/총 입력 허용 신호를 쏴도 좋다.
        // ex) OnTurnStarted?.Invoke(currentPlayer);
    }

    private void SortClockwise()
    {
        if (players == null) return;
        if (players.Count == 0) { clockwiseOrder.Clear(); return; }

        Vector3 center = (tableCenter != null ? tableCenter.position : transform.position);
        clockwiseOrder = players
            .Where(t => t != null)
            .OrderByDescending(t => AngleDeg(center, t.position))
            .ToList();
    }

    private float AngleDeg(Vector3 center, Vector3 pos)
    {
        Vector2 a, b;
        if (useXZPlane)
        {
            a = new Vector2(1, 0); // +X 기준
            Vector3 v = pos - center; v.y = 0;
            b = new Vector2(v.x, v.z);
        }
        else
        {
            a = new Vector2(1, 0);
            Vector3 v = pos - center; v.z = 0;
            b = new Vector2(v.x, v.y);
        }
        float rad = Mathf.Atan2(b.y, b.x) - Mathf.Atan2(a.y, a.x);
        float deg = rad * Mathf.Rad2Deg;
        if (deg < 0) deg += 360f;
        return deg; // 내림차순 정렬 시 시계방향
    }
}
