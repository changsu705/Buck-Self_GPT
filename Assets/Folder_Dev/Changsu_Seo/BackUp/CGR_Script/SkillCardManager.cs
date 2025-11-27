using UnityEngine;
using System.Collections;

/// <summary>
/// TurnManager와 연동하여 턴이 바뀔 때마다 스킬 카드를 드로우하고,
/// 1. 플레이어 카메라 앞으로 가져와 보여준 뒤(프리뷰),
/// 2. 플레이어 앞 책상 위치에 배치하는 '연출'을 관리합니다.
/// - ⚠️ 현재 드로우 중인지 상태(IsDrawingCard)를 외부에 공개하여 입력 차단에 사용됩니다.
/// </summary>
public class SkillCardManager : MonoBehaviour
{
    // ────────────────────────────── 필수 참조 ──────────────────────────────
    [Header("필수 참조")]
    public TurnManager turnManager; // 현재 턴 플레이어를 확인하기 위해 필요
    public CardDeck cardDeck;     // 카드 데이터를 뽑아오기 위해 필요
    public GameObject cardPrefab; // 생성할 3D 카드 오브젝트 프리팹
    public Transform tableCenter;  // 배치 위치 계산의 기준점
    [Tooltip("플레이어의 머리(카메라) 오브젝트 이름 (프리뷰 위치 계산용)")]
    public string headChildName = "Head";

    // ────────────────────────────── 0. 시작 딜레이 ──────────────────────────────
    [Header("0. 시작 딜레이")]
    [Tooltip("턴이 바뀐 후, 이 시간(초)만큼 기다렸다가 카드 드로우를 시작합니다.")]
    public float initialDelay = 0.5f;

    // ────────────────────────────── 1. 프리뷰 연출 (카메라 앞) ──────────────────────────────
    [Header("1. 프리뷰 연출 (카메라 앞)")]
    [Tooltip("카메라 기준 카드 프리뷰 로컬 위치 오프셋")]
    public Vector3 previewLocalPosition = new Vector3(0, -0.2f, 0.8f);
    [Tooltip("카드 프리뷰 로컬 회전 (예: 0, 180, 0 = 카메라를 바라보도록)")]
    public Vector3 previewLocalRotation = new Vector3(0, 180f, 0);
    [Tooltip("카메라 앞에서 프리뷰를 유지하는 시간(초)")]
    public float previewHoldSeconds = 2.0f;
    [Tooltip("카메라 앞(프리뷰 위치)까지 날아오는 데 걸리는 시간(초)")]
    public float moveToPreviewDuration = 0.4f;

    // ────────────────────────────── 2. 책상 배치 연출 ──────────────────────────────
    [Header("2. 책상 배치 연출")]
    [Tooltip("테이블 중심에서 플레이어까지의 배치 비율")]
    [Range(0f, 1f)] public float placeRatio = 0.8f;
    [Tooltip("책상 위 배치 시 좌우 수평 오프셋")]
    public float placeHorizontalOffset = -0.7f;
    [Tooltip("책상 위 배치 시 높이 오프셋")]
    public float heightOffset = 0.75f;
    [Tooltip("책상 위치로 이동하는 데 걸리는 시간(초)")]
    public float moveToDeskDuration = 0.5f;

    // ────────────────────────────── 내부 상태 ──────────────────────────────
    private Transform _lastOwner; // 이전 턴 플레이어 (턴 변경 감지용)
    private Coroutine _cardSequenceCoroutine; // 현재 진행 중인 카드 드로우/배치 시퀀스 (코루틴)

    // ⚠️ --- 추가된 부분 --- ⚠️
    /// <summary>
    /// [읽기 전용] 현재 카드 드로우/배치 시퀀스 코루틴이 실행 중인지 여부입니다.
    /// (RevolverTurnPossession이 이 값을 확인하여 IsUIOpen이 true일 때 입력을 차단합니다.)
    /// (_cardSequenceCoroutine이 null이 아니면 true, null이면 false를 반환)
    /// </summary>
    public bool IsDrawingCard => _cardSequenceCoroutine != null;
    // ⚠️ --- 여기까지 --- ⚠️

    void Update()
    {
        TryUpdateOwner(); // 매 프레임 턴 플레이어 변경을 감지
    }

    /// <summary>
    /// 턴 매니저를 확인하여, 턴 플레이어가 바뀌었으면 카드 드로우 시퀀스를 시작합니다.
    /// </summary>
    private void TryUpdateOwner()
    {
        if (turnManager == null || cardDeck == null || cardPrefab == null) return;

        Transform currentPlayer = turnManager.GetCurrentPlayer();
        if (currentPlayer == _lastOwner) return; // 턴 플레이어 변경 없음

        _lastOwner = currentPlayer; // 현재 턴 플레이어 갱신
        if (currentPlayer == null) return;

        // 이전 턴의 카드 드로우 코루틴이 아직 실행 중이라면 강제 중지
        if (_cardSequenceCoroutine != null)
        {
            StopCoroutine(_cardSequenceCoroutine);
            // (참고: _cardSequenceCoroutine 변수 자체는 코루틴이 끝날 때 null로 설정됨)
        }

        // 새 플레이어를 위한 카드 드로우 시퀀스(코루틴) 시작
        // _cardSequenceCoroutine 변수에 실행 중인 코루틴을 저장 (IsDrawingCard가 true가 됨)
        _cardSequenceCoroutine = StartCoroutine(Co_DrawAndPlaceCard(currentPlayer));
    }

    /// <summary>
    /// 카드를 드로우하고, 프리뷰 연출 후 책상에 배치하는 전체 코루틴 시퀀스입니다.
    /// </summary>
    private IEnumerator Co_DrawAndPlaceCard(Transform player)
    {
        // 0. 초기 딜레이 (턴 시작 후 바로 카드가 나오지 않도록)
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        // 1. 플레이어의 손(PlayerHand) 컴포넌트 확인
        // (PlayerHand.cs가 있어야 카드를 관리할 수 있음)
        PlayerHand playerHand = player.GetComponent<PlayerHand>();
        if (playerHand == null)
        {
            Debug.LogError($"[SkillCardManager] {player.name}에게 PlayerHand.cs 스크립트가 없습니다!");
            _cardSequenceCoroutine = null; // ⚠️ 시퀀스 종료 처리 (IsDrawingCard = false)
            yield break; // 코루틴 즉시 종료
        }

        /* // ⚠️ (원래 요청 사항) 플레이어 카드 수 확인 로직. 현재 주석 처리되어 매 턴 드로우됨.
        if (playerHand.CardCount >= 3)
        {
            Debug.Log($"<color=orange>[{player.name}]</color>의 카드 수가 {playerHand.CardCount}장이므로, 카드를 뽑지 않고 턴을 시작합니다.");
            _cardSequenceCoroutine = null;
            yield break;
        }
        */

        Debug.Log($"<color=green>[{player.name}]</color>의 카드 수가 {playerHand.CardCount}장이므로, 새 카드를 뽑습니다.");

        // 2. 카드 덱(CardDeck)에서 카드 데이터(CardData) 1장 뽑기
        CardData dataToDraw = cardDeck.DrawCard();
        if (dataToDraw == null)
        {
            Debug.LogError("덱에서 카드를 뽑는 데 실패했습니다. (CardDeck 확인 필요)");
            _cardSequenceCoroutine = null; // ⚠️ 시퀀스 종료 처리
            yield break;
        }

        // 3. 카드 오브젝트 생성(Instantiate) 및 초기화(Initialize)
        // (생성 위치는 우선 tableCenter로 설정)
        GameObject cardGO = Instantiate(cardPrefab, tableCenter.position, Quaternion.identity);
        CardVisual newCard = cardGO.GetComponent<CardVisual>();
        if (newCard == null)
        {
            Debug.LogError("카드 프리팹(cardPrefab)에 CardVisual.cs 스크립트가 없습니다!");
            Destroy(cardGO);
            _cardSequenceCoroutine = null; // ⚠️ 시퀀스 종료 처리
            yield break;
        }
        newCard.Initialize(dataToDraw); // CardVisual에 CardData를 전달하여 머티리얼 등 설정
        playerHand.AddCard(newCard); // (아직 PlayerHand.cs는 없지만) 플레이어 핸드에 카드 추가

        // 4. 프리뷰 위치/회전 계산 (카메라 앞)
        Transform head = player.Find(headChildName);
        Transform previewTarget = head ? head : player; // 머리(Head)가 있으면 머리 기준, 없으면 플레이어 루트 기준

        // 카메라가 테이블을 바라보는 방향으로 카드의 방향(회전) 설정
        Vector3 lookDir = (tableCenter.position - previewTarget.position);
        lookDir.y = 0; // 수평 방향만 사용
        Quaternion lookRotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        // 최종 프리뷰 위치/회전 계산
        Vector3 previewPos = previewTarget.position + (lookRotation * previewLocalPosition);
        Quaternion previewRot = lookRotation * Quaternion.Euler(previewLocalRotation);

        // 5. 프리뷰 위치로 이동 연출 (Co_MoveCard 코루틴 호출 및 대기)
        yield return StartCoroutine(Co_MoveCard(newCard.transform,
            tableCenter.position, previewPos, // 시작 위치(테이블 중앙) -> 목표 위치(카메라 앞)
            newCard.transform.rotation, previewRot,
            moveToPreviewDuration));

        // 6. 프리뷰 유지 시간 대기 (플레이어가 카드를 확인할 시간)
        yield return new WaitForSeconds(previewHoldSeconds);

        // 7. 책상 배치 위치/회전 계산 (RevolverTurnPossession의 CoMoveToPlayer와 유사)
        Vector3 playerPos = player.position;
        Vector3 center = tableCenter.position;
        Vector3 dir = playerPos - center; dir.y = 0f;
        float dist = dir.magnitude;
        Vector3 dirNorm = dist > 1e-4f ? dir / dist : Vector3.forward;
        Vector3 rightDir = new Vector3(dirNorm.z, 0, -dirNorm.x);

        Vector3 flatTarget = center
            + (dirNorm * (dist * Mathf.Clamp01(placeRatio)))
            + (rightDir * placeHorizontalOffset);
        Vector3 deskPos = new Vector3(flatTarget.x, center.y + heightOffset, flatTarget.z);
        Quaternion deskRot = Quaternion.LookRotation(dirNorm, Vector3.up); // 플레이어를 향하는 회전

        // 8. 책상 배치 위치로 이동 연출 (Co_MoveCard 코루틴 호출 및 대기)
        yield return StartCoroutine(Co_MoveCard(newCard.transform,
            previewPos, deskPos, // 시작 위치(카메라 앞) -> 목표 위치(책상 위)
            previewRot, deskRot,
            moveToDeskDuration));

        // 모든 시퀀스 완료
        _cardSequenceCoroutine = null; // 시퀀스 종료 (IsDrawingCard가 false가 됨)
    }

    /// <summary>
    /// 카드를 한 위치/회전에서 다른 위치/회전으로 SmoothStep 보간하며 이동시키는 코루틴입니다.
    /// </summary>
    private IEnumerator Co_MoveCard(Transform card, Vector3 fromPos, Vector3 toPos, Quaternion fromRot, Quaternion toRot, float duration)
    {
        float t = 0f; // 진행도 (0.0 ~ 1.0)
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, duration); // 0으로 나누기 방지
            float e = t * t * (3f - 2f * t); // SmoothStep 이징 함수

            card.position = Vector3.LerpUnclamped(fromPos, toPos, e); // 위치 보간
            card.rotation = Quaternion.SlerpUnclamped(fromRot, toRot, e); // 회전 보간
            yield return null; // 1프레임 대기
        }
        // 최종 위치/회전으로 스냅
        card.position = toPos;
        card.rotation = toRot;
    }
}