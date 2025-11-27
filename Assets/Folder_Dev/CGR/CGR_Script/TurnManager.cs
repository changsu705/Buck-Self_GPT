using UnityEngine;
using System.Collections.Generic; // 리스트(List) 사용
using System.Linq; // 정렬(OrderBy) 및 리스트 변환(ToList) 사용
using UnityEngine.SceneManagement; // 게임 재시작(Scene load)을 위해 필수

/// <summary>
/// [게임 매니저] 게임의 전체 흐름을 관리하는 핵심 스크립트입니다.
/// 1. 플레이어들의 턴 순서를 정함 (테이블 중심 기준 시계방향)
/// 2. 턴을 넘기고, 죽은 플레이어는 건너뜀
/// 3. 플레이어 사망 시 승리/패배 조건을 확인하고 게임을 종료함
/// 4. UI 버튼(재시작, 종료) 기능을 제공함
/// </summary>
public class TurnManager : MonoBehaviour
{
    // ==================================================================================
    // 1. 필수 설정 (인스펙터 할당)
    // ==================================================================================
    [Header("1. 필수 설정")]
    [Tooltip("플레이어 순서 정렬의 기준점 (보통 테이블 정중앙의 오브젝트)")]
    public Transform tableCenter;

    [Tooltip("게임에 참여하는 모든 플레이어 (AI, 사람 포함)")]
    public List<Transform> players = new List<Transform>();

    [Header("2. 옵션")]
    [Tooltip("순서 정렬 시 사용할 평면 (true: XZ평면/3D, false: XY평면/2D)")]
    public bool useXZPlane = true;

    [Tooltip("게임 시작 시 자동으로 턴을 시작할지 여부")]
    public bool autoStart = true;

    // ==================================================================================
    // 2. UI 연결 (승리/패배/메인)
    // ==================================================================================
    [Header("3. 게임 종료 UI")]
    [Tooltip("승리 시 활성화할 UI 패널 (Win Panel)")]
    public GameObject winPanel;

    [Tooltip("패배 시 활성화할 UI 패널 (Lose Panel)")]
    public GameObject losePanel;

    [Tooltip("메인 메뉴 씬 이름 (메인으로 가기 버튼 기능용)")]
    public string mainMenuSceneName = "MainMenu";

    // ==================================================================================
    // 내부 상태 변수 (로직용)
    // ==================================================================================
    // 시계 방향으로 정렬된 실제 플레이 순서 리스트
    private List<Transform> clockwiseOrder = new List<Transform>();

    // 현재 턴을 진행 중인 플레이어의 인덱스 (clockwiseOrder 기준)
    private int currentIndex = -1;

    // 현재 턴 주인 (외부에서 접근 가능)
    private Transform currentPlayer;

    // '사람' 플레이어 (승패 판정의 기준)
    private Transform humanPlayer;

    // 다음 턴을 스킵해야 하는지 여부 (아이템 효과 등)
    private bool _skipNextTurn = false;

    // 게임이 이미 끝났는지 여부 (중복 실행 방지)
    private bool isGameEnded = false;

    // ==================================================================================
    // Unity 라이프사이클 (초기화)
    // ==================================================================================
    private void Awake()
    {
        // 1. 플레이어 자동 탐색 (인스펙터에서 할당 안 했을 경우 대비)
        if (players == null || players.Count == 0)
        {
            var found = GameObject.FindGameObjectsWithTag("Player");
            foreach (var go in found) players.Add(go.transform);
        }

        // 2. 테이블 센터 확인 (없으면 내 위치 기준)
        if (tableCenter == null)
        {
            tableCenter = transform;
            Debug.LogWarning("TableCenter가 없어 TurnManager 위치를 기준으로 잡습니다.");
        }

        // 3. 플레이어 위치를 기준으로 시계 방향 정렬 (순서 확정)
        SortClockwise();

        // 4. 모든 플레이어의 사망 이벤트(OnDied)에 감시 함수 연결
        foreach (Transform player in clockwiseOrder)
        {
            if (player == null) continue;
            PlayerHealth health = player.GetComponent<PlayerHealth>();
            if (health != null)
            {
                // 누군가 죽으면 HandlePlayerDeath 함수가 자동으로 실행됨
                health.OnDied += HandlePlayerDeath;
            }
        }

        // 5. 시작 시 종료 패널들이 켜져 있다면 끄기
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }

    private void Start()
    {
        // 자동 시작 옵션이 켜져 있으면 게임 시작
        if (autoStart) StartGame();
    }

    // ==================================================================================
    // 외부 설정용 함수
    // ==================================================================================

    /// <summary>
    /// 사람 플레이어가 누구인지 등록합니다. (PlayerSetupManager에서 호출)
    /// 승/패 판정의 기준이 됩니다.
    /// </summary>
    public void SetHumanPlayer(Transform player)
    {
        humanPlayer = player;
    }

    // ==================================================================================
    // UI 버튼 연결용 함수 (Button OnClick 이벤트에 연결하세요)
    // ==================================================================================

    /// <summary>
    /// [버튼용] 현재 게임을 처음부터 다시 시작합니다. (Restart)
    /// </summary>
    public void RestartGame()
    {
        // 게임이 멈춰있을 수 있으므로 시간 배속을 정상화
        Time.timeScale = 1f;

        // 현재 열려있는 씬을 다시 로드 (초기화 효과)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// [버튼용] 메인 메뉴 씬으로 이동합니다.
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// [버튼용] 게임을 완전히 종료합니다. (빌드 버전)
    /// </summary>
    public void QuitGame()
    {
        Application.Quit(); // 빌드된 게임 종료

        // 유니티 에디터에서는 플레이 모드를 중지
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ==================================================================================
    // 핵심 로직: 승패 판정 (Win / Lose)
    // ==================================================================================

    /// <summary>
    /// 누군가 죽었을 때(이벤트) 호출되어 게임 종료 조건을 검사합니다.
    /// </summary>
    private void HandlePlayerDeath()
    {
        CheckGameEndConditions();
    }

    /// <summary>
    /// 승리 또는 패배 조건을 확인하고 게임을 종료시킵니다.
    /// </summary>
    private void CheckGameEndConditions()
    {
        if (isGameEnded) return; // 이미 끝났으면 무시
        if (humanPlayer == null) return; // 사람 정보가 없으면 판정 불가

        // 1. [패배 조건] 내가(사람이) 죽었는지 확인
        PlayerHealth humanHealth = humanPlayer.GetComponent<PlayerHealth>();
        if (humanHealth != null && humanHealth.IsDead)
        {
            EndGame(false); // 패배 처리
            return;
        }

        // 2. [승리 조건] 나를 제외한 적(AI)들이 다 죽었는지 확인
        int livingOpponents = 0;
        foreach (Transform player in clockwiseOrder)
        {
            if (player == humanPlayer) continue; // 나는 카운트 제외

            PlayerHealth health = player.GetComponent<PlayerHealth>();
            // 살아있는 적이 한 명이라도 있으면 게임 계속 진행
            if (health != null && !health.IsDead)
            {
                livingOpponents++;
                break; // 더 볼 필요 없음
            }
        }

        // 살아있는 적이 0명이면 승리
        if (livingOpponents == 0)
        {
            EndGame(true); // 승리 처리
        }
    }

    /// <summary>
    /// 게임을 실제로 종료하고 결과 화면을 띄웁니다.
    /// </summary>
    /// <param name="didWin">true면 승리, false면 패배</param>
    private void EndGame(bool didWin)
    {
        isGameEnded = true;

        // 1. 결과에 맞는 UI 패널 활성화
        if (didWin)
        {
            Debug.Log("<color=green><b>[VICTORY]</b> 플레이어 승리! 축하합니다.</color>");
            if (winPanel != null) winPanel.SetActive(true);
        }
        else
        {
            Debug.Log("<color=red><b>[DEFEAT]</b> 플레이어 패배... 다시 도전하세요.</color>");
            if (losePanel != null) losePanel.SetActive(true);
        }

        // 2. 게임 로직 정지 (더 이상 진행되지 않도록)
        StopAllCoroutines(); // 진행 중인 턴/연출 중지
        this.enabled = false; // TurnManager 업데이트 정지

        // (선택 사항) 총기 제어 매니저도 정지시켜서 발사 불가능하게 만듦
        RevolverTurnPossession rpt = FindObjectOfType<RevolverTurnPossession>();
        if (rpt != null) rpt.enabled = false;

        // 3. 마우스 커서 복구 (버튼 클릭을 위해)
        Cursor.lockState = CursorLockMode.None; // 잠금 해제
        Cursor.visible = true; // 커서 보이기
    }

    // ==================================================================================
    // 핵심 로직: 턴 관리 (Turn System)
    // ==================================================================================

    /// <summary>
    /// 게임을 시작합니다. (살아있는 플레이어 중 랜덤 1명 선택)
    /// </summary>
    public void StartGame()
    {
        if (clockwiseOrder.Count == 0) SortClockwise();

        // 살아있는 플레이어들의 인덱스만 모음
        List<int> livingIndices = new List<int>();
        for (int i = 0; i < clockwiseOrder.Count; i++)
        {
            if (!clockwiseOrder[i].GetComponent<PlayerHealth>().IsDead)
                livingIndices.Add(i);
        }

        if (livingIndices.Count == 0) return; // 모두 죽었으면 시작 불가

        // 랜덤하게 한 명을 뽑아 시작
        int randomStart = livingIndices[UnityEngine.Random.Range(0, livingIndices.Count)];
        currentIndex = randomStart;

        BeginTurn(currentIndex);
    }

    /// <summary>
    /// 다음 플레이어에게 턴을 넘깁니다. (죽은 사람은 건너뜀)
    /// </summary>
    public void NextTurn()
    {
        if (isGameEnded || clockwiseOrder.Count == 0) return;

        // 턴 스킵 아이템이 사용되었다면 2칸 이동, 아니면 1칸 이동
        int skips = _skipNextTurn ? 2 : 1;
        _skipNextTurn = false; // 스킵 사용 후 초기화

        int checks = clockwiseOrder.Count; // 무한루프 방지용 카운트
        int nextIdx = currentIndex;

        // 스킵 횟수만큼 반복해서 다음 사람을 찾음
        for (int s = 0; s < skips; s++)
        {
            bool found = false;
            // 전체 인원수만큼 돌면서 살아있는 사람 탐색
            for (int i = 0; i < checks; i++)
            {
                // 인덱스 1 증가 (배열 끝에 도달하면 0으로 순환 %)
                nextIdx = (nextIdx + 1) % clockwiseOrder.Count;

                Transform p = clockwiseOrder[nextIdx];
                // 살아있는 플레이어라면 당첨
                if (p != null && !p.GetComponent<PlayerHealth>().IsDead)
                {
                    found = true;
                    break;
                }
            }

            // 살아있는 사람을 못 찾았으면 게임 종료 로직 호출
            if (!found)
            {
                CheckGameEndConditions();
                return;
            }
        }

        // 찾은 다음 사람으로 턴 시작
        BeginTurn(nextIdx);
    }

    /// <summary>
    /// 다음 턴을 한 번 건너뛰게 설정합니다. (카드 아이템용)
    /// </summary>
    public void SkipNextTurn() { _skipNextTurn = true; }

    /// <summary>
    /// 현재 턴인 플레이어를 반환합니다.
    /// </summary>
    public Transform GetCurrentPlayer() => currentPlayer;

    /// <summary>
    /// 현재 플레이어의 정면에 앉은 상대를 찾습니다. (AI 연출용)
    /// </summary>
    public Transform FindOpponentInFront(Transform current)
    {
        int idx = clockwiseOrder.IndexOf(current);
        if (idx == -1) return null;
        // 리스트의 절반만큼 떨어진 인덱스가 맞은편 사람
        return clockwiseOrder[(idx + clockwiseOrder.Count / 2) % clockwiseOrder.Count];
    }

    // 지정된 인덱스의 플레이어로 턴 시작
    private void BeginTurn(int index)
    {
        currentIndex = index;
        currentPlayer = clockwiseOrder[currentIndex];
        Debug.Log($"▶ 턴 시작: {currentPlayer.name}");
    }

    // 테이블 중심을 기준으로 각도를 계산해 시계 방향 정렬
    private void SortClockwise()
    {
        clockwiseOrder = players.OrderByDescending(t =>
        {
            // 플레이어 위치와 테이블 중심 간의 벡터 계산
            Vector2 v = useXZPlane
                ? new Vector2(t.position.x - tableCenter.position.x, t.position.z - tableCenter.position.z)
                : new Vector2(t.position.x - tableCenter.position.x, t.position.y - tableCenter.position.y);

            // 아크탄젠트(Atan2)로 각도(라디안 -> 도) 계산
            float deg = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;

            // 0~360도 범위로 변환
            return deg < 0 ? deg + 360 : deg;
        }).ToList();
    }
}