using UnityEngine;
using System.Collections.Generic; // List 사용
using System.Linq; // OrderBy (정렬) 사용
using System; // Action 이벤트 사용 (현재는 없지만 확장성)
using Photon.Pun;
using Photon.Realtime;


/// <summary>
/// 테이블 중심을 기준으로 플레이어들을 시계방향으로 정렬하고,
/// 시작 플레이어를 랜덤으로 정한 뒤 시계방향 순서대로 턴을 진행하는 매니저입니다.
/// </summary>
public class TurnManager : MonoBehaviour
{
    // ────────────────────────────── 필수 설정 ──────────────────────────────
    [Header("필수 설정")]
    [Tooltip("테이블(중심) Transform. 플레이어 정렬의 기준점이 됩니다. (비워두면 이 오브젝트 위치 사용)")]
    public Transform tableCenter;
    [Tooltip("플레이어 Transform 목록. (비워두면 'Player' 태그로 자동 수집)")]
    public List<Transform> players = new List<Transform>();

    // ────────────────────────────── 옵션 ──────────────────────────────
    [Header("옵션")]
    [Tooltip("각도 계산 시 XZ 평면(3D 탑다운)을 사용할지, XY 평면(2D)을 사용할지 결정")]
    public bool useXZPlane = true;
    [Tooltip("게임 시작(Start) 시 자동으로 랜덤 플레이어부터 턴을 시작할지")]
    public bool autoStart = true;
    [Tooltip("테스트용: Space 키를 누르면 다음 턴으로 넘기기")]
    public bool enableSpaceToNext = true;

    // ────────────────────────────── 내부 상태 ──────────────────────────────
    [Tooltip("시계방향으로 정렬된 플레이어 순서 (읽기 전용)")]
    [SerializeField] // 인스펙터에서 볼 수 있도록
    private List<Transform> clockwiseOrder = new List<Transform>();

    [SerializeField]
    private int currentIndex = -1; // 정렬된 리스트(clockwiseOrder) 기준 현재 턴 인덱스

    private Transform currentPlayer; // 현재 턴 플레이어 (빠른 조회를 위한 캐시)

    // ────────────────────────────── 이벤트 (필요시 사용) ──────────────────────────────
    // public event Action<Transform> OnTurnStarted; // 예: 새 턴이 시작될 때 (현재 플레이어)

    // ----- 라이프사이클 -----
    private void Awake()
    {
        // 1. 플레이어 자동 수집 (목록이 비어있을 경우)
        if (players == null || players.Count == 0)
        {
            var found = GameObject.FindGameObjectsWithTag("Player");
            foreach (var go in found) players.Add(go.transform);
        }

        // 2. 필수 항목 체크
        if (players.Count < 2)
        {
            Debug.LogError("[TurnManager] 플레이어가 2명 이상 필요합니다. 'Player' 태그를 확인하세요.", this);
            enabled = false;
            return;
        }
        if (tableCenter == null)
        {
            tableCenter = transform; // 기준점이 없으면 이 오브젝트의 위치를 사용
            Debug.LogWarning("[TurnManager] tableCenter가 지정되지 않았습니다. 이 오브젝트 위치를 중심으로 사용합니다.", this);
        }

        // 게임 시작 전, 플레이어들을 시계방향으로 정렬
        SortClockwise();
    }

    private void Start()
    {
        if (autoStart)
        {
            StartGame(); // 자동 시작이 켜져 있으면 게임 시작
        }
    }

    private void Update()
    {
        // Space 키로 다음 턴 테스트
        if (enableSpaceToNext && Input.GetKeyDown(KeyCode.Space))
        {
            NextTurn();
        }
    }

    // ────────────────────────────── 퍼블릭 API ──────────────────────────────

    /// <summary>
    /// 플레이어 순서를 랜덤하게 시작하고, 첫 번째 턴을 시작합니다.
    /// (ContextMenu: 인스펙터 우클릭 메뉴에서 이 함수를 테스트 실행할 수 있습니다.)
    /// </summary>
    [ContextMenu("Shuffle & Start Game")]
    public void StartGame()
    {
        if (clockwiseOrder.Count == 0) SortClockwise(); // 정렬이 안됐으면 다시 시도

        // 0 ~ (플레이어 수 - 1) 사이에서 랜덤한 인덱스 선택
        currentIndex = UnityEngine.Random.Range(0, clockwiseOrder.Count);

        LogOrder(); // 콘솔에 정렬 순서와 시작 플레이어 출력

        BeginTurn(currentIndex); // 첫 턴 시작
    }

    /// <summary>
    /// 시계방향으로 다음 플레이어에게 턴을 넘깁니다. (순환)
    /// (RevolverTurnPossession이 이 함수를 호출합니다.)
    /// </summary>
    public void NextTurn()
    {
        if (clockwiseOrder.Count == 0) return;

        // (현재 인덱스 + 1)을 플레이어 수로 나눈 나머지
        // 예: 4명(0,1,2,3)일 때
        // (0+1)%4 = 1
        // (3+1)%4 = 0  <- 마지막에서 처음으로 순환
        int next = (currentIndex + 1) % clockwiseOrder.Count;

        BeginTurn(next);
    }
    public void SetTurnByActor(int actorNumber)
    {
        // Photon Player 중 해당 ActorNumber 찾기
        var player = PhotonNetwork.PlayerList.FirstOrDefault(p => p.ActorNumber == actorNumber);
        if (player == null)
        {
            Debug.LogWarning($"[TurnManager] 다음 턴 플레이어를 찾을 수 없습니다: Actor {actorNumber}");
            return;
        }

        // 현재 턴을 해당 플레이어로 변경
        var playerTransform = players.FirstOrDefault(t => t.name == player.NickName || t.name.Contains(player.ActorNumber.ToString()));
        if (playerTransform != null)
        {
            currentPlayer = playerTransform;
            Debug.Log($"[TurnManager] 서버에서 턴 전환: {playerTransform.name} (Actor {actorNumber})");
        }
        else
        {
            Debug.LogWarning($"[TurnManager] Actor {actorNumber}에 해당하는 Transform을 찾을 수 없습니다.");
        }
    }
    public int GetCurrentActor()
    {
        // 현재 턴 주인의 Photon ActorNumber 반환
        var current = GetCurrentPlayer();
        if (current == null) return -1;
        var view = current.GetComponent<PhotonView>();
        return view != null ? view.Owner.ActorNumber : -1;
    }

    /// <summary>
    /// 현재 턴인 플레이어의 트랜스폼을 반환합니다.
    /// (RevolverTurnPossession, SkillCardManager 등이 이 함수로 현재 턴을 확인합니다.)
    /// </summary>
    public Transform GetCurrentPlayer() => currentPlayer;

    // ────────────────────────────── 내부 로직 ──────────────────────────────

    /// <summary>
    /// 지정된 인덱스로 턴을 시작하고 내부 상태(currentIndex, currentPlayer)를 갱신합니다.
    /// </summary>
    private void BeginTurn(int index)
    {
        currentIndex = index;
        currentPlayer = clockwiseOrder[currentIndex];
        Debug.Log($"▶️ 턴 시작: {currentPlayer.name} (index {currentIndex})");

        // (필요시) 턴 시작 이벤트 발생
        // OnTurnStarted?.Invoke(currentPlayer);
    }

    /// <summary>
    /// 테이블 중심을 기준으로 플레이어들을 시계방향으로 정렬하여 'clockwiseOrder' 리스트에 저장합니다.
    /// </summary>
    private void SortClockwise()
    {
        Vector3 center = tableCenter.position;

        // LINQ의 OrderByDescending 사용:
        // 'AngleDeg' (각도)가 큰 순서(내림차순)로 정렬해야 시계방향이 됩니다.
        // (Atan2는 +X축 기준 반시계 방향 각도를 반환하기 때문)
        clockwiseOrder = players
            .OrderByDescending(t => AngleDeg(center, t.position))
            .ToList(); // 정렬된 결과를 새 리스트로 만들어 저장
    }

    /// <summary>
    /// 중심→위치 벡터의 각도를 0~360도로 환산하여 반환합니다. (AngleDeg 값이 클수록 반시계 방향)
    /// </summary>
    private float AngleDeg(Vector3 center, Vector3 pos)
    {
        Vector2 v;
        if (useXZPlane) // 3D 환경 (XZ 평면)
        {
            v = new Vector2(pos.x - center.x, pos.z - center.z);
        }
        else            // 2D 환경 (XY 평면)
        {
            v = new Vector2(pos.x - center.x, pos.y - center.y);
        }

        float rad = Mathf.Atan2(v.y, v.x); // +X 축(오른쪽) 기준 반시계 방향 각도 (라디안)
        float deg = rad * Mathf.Rad2Deg;   // 라디안을 각도(degree)로 변환

        if (deg < 0) deg += 360f; // -180~180 범위를 0~360 범위로 정규화

        return deg;
    }

    /// <summary>
    /// (디버그용) 현재 정렬된 시계방향 순서와 시작 플레이어를 디버그 콘솔에 출력합니다.
    /// </summary>
    private void LogOrder()
    {
        if (clockwiseOrder.Count == 0) return;

        // 리스트의 모든 이름을 " → "로 연결하여 하나의 문자열로 만듭니다.
        string order = string.Join(" → ", clockwiseOrder.ConvertAll(t => t.name));

        Debug.Log($"[TurnManager] 🧭 시계방향 순서: {order} | 시작: {clockwiseOrder[currentIndex].name}");
    }


    // ────────────────────────────── 에디터 기즈모 ──────────────────────────────
#if UNITY_EDITOR // 유니티 에디터에서만 컴파일되는 코드
    /// <summary>
    /// 씬(Scene) 뷰에서만 보이며, 이 오브젝트를 선택했을 때 Gizmo를 그립니다.
    /// (플레이어 배치 및 중심점 확인용)
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (players == null || tableCenter == null) return;

        // 테이블 중심점에 노란색 구체 그리기
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(tableCenter.position, 0.2f);

        // 각 플레이어에게 하늘색 선 그리기
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i] == null) continue;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(tableCenter.position, players[i].position);
        }
    }
#endif
}