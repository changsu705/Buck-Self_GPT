using UnityEngine;
using System.Collections.Generic; // List 사용
using System.Linq; // OrderBy (무작위 정렬) 사용

/// <summary>
/// [총기 시스템] 리볼버 실린더의 '총알(실탄/공포탄)' 데이터를 관리합니다.
/// </summary>
public class RevolverCylinder : MonoBehaviour
{
    [Header("실린더 기본 설정")]
    [Tooltip("리볼버 실린더의 총 칸 수 (예: 6)")]
    public int totalChambers = 6;

    // ⚠️ [수정됨] 고정 값 대신 랜덤 범위를 사용합니다.
    [Header("랜덤 장전 설정 (범위)")]
    [Tooltip("자동 장전 시 실탄의 최소 개수 (보통 1)")]
    [Range(1, 6)] public int minLiveRounds = 1;

    [Tooltip("자동 장전 시 실탄의 최대 개수 (보통 4~5)")]
    [Range(1, 6)] public int maxLiveRounds = 4;

    // --- 내부 상태 ---
    // true = 실탄(Live), false = 공포탄(Blank)
    private List<bool> chambers = new List<bool>();
    private int currentChamberIndex = 0; // 현재 발사할 총알 인덱스

    void Awake()
    {
        // 게임 시작 시 랜덤 설정으로 첫 장전
        LoadCylinder();
    }

    /// <summary>
    /// [공용] 실린더를 '무작위로 섞습니다(회전)'.
    /// </summary>
    [ContextMenu("디버그: 실린더 회전 (Shuffle Chambers)")]
    public void ShuffleChambers()
    {
        // 1. 무작위로 섞기 (LINQ 사용)
        chambers = chambers.OrderBy(_ => Random.value).ToList();

        // 2. 발사 인덱스 초기화
        currentChamberIndex = 0;

        // (디버그용) 장전된 순서 로그 출력
        LogCylinderState("실린더 회전(Shuffle) 완료.");
    }

    /// <summary>
    /// [자동 재장전] 설정된 최소~최대 범위 내에서 랜덤하게 실탄 수를 정해 장전합니다.
    /// (Awake 또는 총알 소진 시 호출됨)
    /// </summary>
    [ContextMenu("디버그: 랜덤 재장전 (Load Random)")]
    public void LoadCylinder()
    {
        // ⚠️ [핵심 수정] Random.Range(min, max + 1) -> max는 제외되므로 +1 해줘야 함
        // 예: (1, 5)를 넣으면 1, 2, 3, 4 중 하나가 나옴. (1, 4)를 원하면 (1, 5) 입력.
        // 안전 장치: max가 total보다 크지 않게, min이 max보다 크지 않게 보정
        int safeMax = Mathf.Min(maxLiveRounds, totalChambers);
        int safeMin = Mathf.Clamp(minLiveRounds, 1, safeMax);

        int randomCount = Random.Range(safeMin, safeMax + 1);

        Debug.Log($"<color=cyan>[Cylinder]</color> 랜덤 장전 실행! (실탄: {randomCount}발 / 전체: {totalChambers}칸)");

        // 결정된 랜덤 개수로 장전 함수 호출
        LoadCylinder(randomCount);
    }

    /// <summary>
    /// [카드/수동 기능] 지정된 '실탄' 개수만큼 실린더를 '새로' 장전합니다.
    /// </summary>
    /// <param name="numLiveRounds">새로 장전할 실탄 개수</param>
    public void LoadCylinder(int numLiveRounds)
    {
        chambers.Clear(); // 기존 총알 모두 제거

        // 1. 실탄 추가
        int live = Mathf.Clamp(numLiveRounds, 0, totalChambers);
        for (int i = 0; i < live; i++)
        {
            chambers.Add(true); // true = 실탄
        }

        // 2. 공포탄 추가 (총 칸 수 - 실탄 수)
        int blank = totalChambers - live;
        for (int i = 0; i < blank; i++)
        {
            chambers.Add(false); // false = 공포탄
        }

        // 3. 무작위로 섞기 (위치 변경)
        ShuffleChambers();
    }

    /// <summary>
    /// 다음 총알을 발사하고, 실탄(true)인지 공포탄(false)인지 반환합니다.
    /// ⚠️ 만약 총알이 다 떨어졌으면, 자동으로 '랜덤' 재장전하고 첫 번째 총알을 발사합니다.
    /// </summary>
    public bool FireNextRound()
    {
        // 1. 총알이 다 떨어졌는지 확인
        if (currentChamberIndex >= chambers.Count)
        {
            Debug.LogWarning("[Cylinder] 총알이 모두 소모되어 '랜덤' 재장전합니다.");
            LoadCylinder(); // ⚠️ 랜덤 재장전 호출
        }

        // 2. 현재 인덱스의 총알 상태(실탄/공포탄) 가져오기
        bool isLive = chambers[currentChamberIndex];

        // 3. 다음 발사를 위해 인덱스 증가
        currentChamberIndex++;

        // 4. 총알 상태 반환
        return isLive;
    }

    /// <summary>
    /// 현재 실린더에 남은 총알 개수를 반환합니다.
    /// </summary>
    public int GetRemainingRounds()
    {
        return chambers.Count - currentChamberIndex;
    }

    private void LogCylinderState(string contextMessage)
    {
        string order = string.Join(", ", chambers.Select(b => b ? "■" : "□")); // ■:실탄, □:공포탄
        Debug.Log($"<color=yellow>[Cylinder]</color> {contextMessage} (남은 탄: {GetRemainingRounds()}) | 배치: [{order}]");
    }
}