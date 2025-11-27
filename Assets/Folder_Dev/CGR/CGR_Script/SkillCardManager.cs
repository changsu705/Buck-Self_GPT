/*
 * SkillCardManager.cs (죽은 플레이어 참조 오류 수정)
 * [수정 사항]
 * - TryUpdateOwner()에서 _lastOwner가 파괴되었는지 확인하는 안전장치 추가
 * - AudioSource 자동 생성 로직 유지
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// [게임 매니저] 현재 턴인 플레이어에게 카드를 1장 뽑아(Draw) → 
/// 카메라 앞에서 보여준(Preview) → 책상 위에 배치(Place)하는 전체 연출을 관리합니다.
/// </summary>
public class SkillCardManager : MonoBehaviour
{
    [Header("필수 참조")]
    public TurnManager turnManager;
    public CardDeck cardDeck;
    public GameObject cardPrefab;
    public Transform tableCenter;
    public Transform cardSpawnPoint;

    [Header("0. 시작 딜레이")]
    public float initialDelay = 0.5f;

    [Header("1. 프리뷰 연출 (카메라 앞)")]
    public string headChildName = "Head";
    public string aiHeadChildName = "Head";
    public Vector3 previewLocalPosition = new Vector3(0, -0.2f, 0.8f);
    public Vector3 previewLocalRotation = new Vector3(0, 180f, 0);
    public float previewHoldSeconds = 2.0f;
    public float moveToPreviewDuration = 0.4f;

    [Header("2. 책상 배치 연출")]
    [Range(0f, 1f)] public float placeRatio = 0.8f;
    public float placeHorizontalOffset = -0.7f;
    public float cardSpacing = 0.3f;
    public float heightOffset = 0.75f;
    public float moveToDeskDuration = 0.5f;

    [Header("3. 사운드")]
    [Tooltip("카드를 뽑을 때 재생할 효과음")]
    public AudioClip cardDrawSound;
    [Tooltip("소리를 재생할 오디오 소스 (비워두면 자동으로 찾거나 생성함)")]
    public AudioSource audioSource;

    // --- 내부 변수 ---
    private Transform _lastOwner; // 이전 턴 플레이어
    private Coroutine _cardSequenceCoroutine; // 현재 실행 중인 카드 뽑기 코루틴
    public bool IsDrawingCard => _cardSequenceCoroutine != null; // 외부 확인용

    void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    void Update()
    {
        TryUpdateOwner();
    }

    /// <summary>
    /// 턴 매니저를 확인하여, 턴이 바뀌었으면 카드 드로우/배치 코루틴을 시작합니다.
    /// </summary>
    private void TryUpdateOwner()
    {
        if (turnManager == null || cardDeck == null || cardPrefab == null) return;

        // ⚠️ [수정] 이전 주인이 죽어서 파괴되었는지(Missing Reference) 확인
        // 유니티에서 Destroy된 오브젝트를 비교하려고 하면 에러가 날 수 있으므로 null 체크를 먼저 확실하게 함
        if (_lastOwner != null && _lastOwner.gameObject == null)
        {
            _lastOwner = null; // 파괴된 객체라면 참조를 비워줌
        }

        // 1. 현재 턴 플레이어 확인
        Transform currentPlayer = turnManager.GetCurrentPlayer();

        // 플레이어가 없거나(게임 종료), 턴이 바뀌지 않았다면 무시
        if (currentPlayer == null || currentPlayer == _lastOwner) return;

        // 2. 턴 변경 감지 -> 갱신
        _lastOwner = currentPlayer;

        // 3. 기존 코루틴 중지 (턴이 너무 빨리 넘어가면)
        if (_cardSequenceCoroutine != null)
        {
            StopCoroutine(_cardSequenceCoroutine);
        }

        // 4. 새 카드 뽑기/배치 코루틴 시작
        _cardSequenceCoroutine = StartCoroutine(Co_DrawAndPlaceCard(currentPlayer));
    }

    /// <summary>
    /// [핵심 코루틴] 카드를 드로우하고, 프리뷰 연출 후 책상에 배치하는 전체 시퀀스입니다.
    /// </summary>
    private IEnumerator Co_DrawAndPlaceCard(Transform player)
    {
        // 0. 초기 딜레이
        if (initialDelay > 0f)
        {
            yield return new WaitForSeconds(initialDelay);
        }

        // ⚠️ [안전 장치] 딜레이 동안 플레이어가 죽었을 수도 있음
        if (player == null)
        {
            _cardSequenceCoroutine = null;
            yield break;
        }

        // 1. 플레이어의 손(PlayerHand) 컴포넌트 확인
        PlayerHand playerHand = player.GetComponent<PlayerHand>();
        if (playerHand == null)
        {
            Debug.LogError($"[SkillCardManager] {player.name}에게 PlayerHand.cs 스크립트가 없습니다!", player);
            _cardSequenceCoroutine = null;
            yield break;
        }

        Debug.Log($"<color=green>[{player.name}]</color>의 새 카드를 뽑습니다.");

        // 2. 카드 덱에서 카드 데이터(CardData) 1장 뽑기
        CardData dataToDraw = cardDeck.DrawCard();
        if (dataToDraw == null)
        {
            Debug.LogError("덱에서 카드를 뽑는 데 실패했습니다. (CardDeck 확인 필요)");
            _cardSequenceCoroutine = null;
            yield break;
        }

        // 3. 카드 오브젝트 생성(Instantiate) 및 초기화(Initialize)
        Vector3 spawnPos = (cardSpawnPoint != null) ? cardSpawnPoint.position : tableCenter.position;
        if (cardSpawnPoint == null)
        {
            Debug.LogWarning($"[SkillCardManager] 'Card Spawn Point'가 비어있습니다. 테이블 중앙에서 생성합니다.", this);
        }

        GameObject cardGO = Instantiate(cardPrefab, spawnPos, Quaternion.identity);

        // 사운드 재생
        if (audioSource != null && cardDrawSound != null)
        {
            audioSource.PlayOneShot(cardDrawSound);
        }

        CardVisual newCard = cardGO.GetComponent<CardVisual>();
        if (newCard == null)
        {
            Debug.LogError("카드 프리팹(cardPrefab)에 CardVisual.cs 스크립트가 없습니다!");
            Destroy(cardGO);
            _cardSequenceCoroutine = null;
            yield break;
        }

        // CardVisual 초기화 (소유자 전달)
        newCard.Initialize(dataToDraw, playerHand);

        // 카드를 손(PlayerHand)에 추가
        playerHand.AddCard(newCard);

        // 4. 프리뷰 위치/회전 계산 (사람/AI 구분)
        EnemyAIController ai = player.GetComponent<EnemyAIController>();
        bool isAI = (ai != null && ai.enabled);
        string targetHeadName = isAI ? aiHeadChildName : headChildName;

        Transform head = player.Find(targetHeadName);
        Transform previewTarget = head ? head : player;

        // 테이블을 바라보는 방향으로 프리뷰 계산
        Vector3 lookDir = (tableCenter.position - previewTarget.position);
        lookDir.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);

        Vector3 previewPos = previewTarget.position + (lookRotation * previewLocalPosition); // 로컬 오프셋 적용
        Quaternion previewRot = lookRotation * Quaternion.Euler(previewLocalRotation);

        // 5. [연출 1] 프리뷰 위치로 이동 (스폰 -> 카메라 앞)
        yield return StartCoroutine(Co_MoveCard(newCard.transform,
            spawnPos, previewPos,
            newCard.transform.rotation, previewRot,
            moveToPreviewDuration));

        // 6. [연출 2] 프리뷰 유지 시간 대기
        yield return new WaitForSeconds(previewHoldSeconds);

        // ⚠️ [안전 장치] 대기 시간 동안 플레이어가 죽었을 수도 있음
        if (player == null)
        {
            // 플레이어가 죽었으면 카드를 그냥 파괴하고 종료
            if (newCard != null) Destroy(newCard.gameObject);
            _cardSequenceCoroutine = null;
            yield break;
        }

        // 7. 책상 배치 위치/회전 계산 (모든 카드 재정렬)
        List<CardVisual> cardsInHand = playerHand.GetCards();
        int cardCount = cardsInHand.Count;
        Vector3 playerPos = player.position;
        Vector3 center = tableCenter.position;
        Vector3 dir = playerPos - center; dir.y = 0f;
        float dist = dir.magnitude;
        Vector3 dirNorm = dist > 1e-4f ? dir / dist : Vector3.forward;
        Vector3 rightDir = new Vector3(dirNorm.z, 0, -dirNorm.x); // 직각 방향
        Quaternion deskRot = Quaternion.LookRotation(dirNorm, Vector3.up); // 테이블 방향

        // 카드 간격 계산
        float totalWidth = (cardCount - 1) * cardSpacing;
        float startOffset = placeHorizontalOffset - (totalWidth / 2.0f); // 중앙 정렬

        // 8. [연출 3] 책상 배치 위치로 이동 (모든 카드 재정렬)
        for (int i = 0; i < cardCount; i++)
        {
            CardVisual card = cardsInHand[i];
            if (card == null) continue;

            // i번째 카드의 목표 위치 계산
            float currentCardOffset = startOffset + (i * cardSpacing);
            Vector3 flatTarget = center
                + (dirNorm * (dist * Mathf.Clamp01(placeRatio)))
                + (rightDir * currentCardOffset);
            Vector3 deskPos = new Vector3(flatTarget.x, center.y + heightOffset, flatTarget.z);

            if (card == newCard)
            {
                // [새 카드] 프리뷰 위치 -> 책상 위치
                yield return StartCoroutine(Co_MoveCard(card.transform,
                    previewPos, deskPos,
                    previewRot, deskRot,
                    moveToDeskDuration));
            }
            else
            {
                // [기존 카드] 현재 위치 -> 재정렬된 책상 위치
                StartCoroutine(Co_MoveCard(card.transform,
                    card.transform.position, deskPos,
                    card.transform.rotation, deskRot,
                    moveToDeskDuration));
            }
        }

        // 9. 코루틴 완료
        _cardSequenceCoroutine = null;
    }

    /// <summary>
    /// [헬퍼 코루틴] 카드를 부드럽게(SmoothStep) 이동/회전시킵니다.
    /// </summary>
    private IEnumerator Co_MoveCard(Transform card, Vector3 fromPos, Vector3 toPos, Quaternion fromRot, Quaternion toRot, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            // 카드가 중간에 파괴되었으면 코루틴 중단
            if (card == null) yield break;

            t += Time.deltaTime / Mathf.Max(0.001f, duration);
            float e = t * t * (3f - 2f * t); // SmoothStep
            card.position = Vector3.LerpUnclamped(fromPos, toPos, e);
            card.rotation = Quaternion.SlerpUnclamped(fromRot, toRot, e);
            yield return null;
        }
        if (card != null)
        {
            card.position = toPos;
            card.rotation = toRot;
        }
    }
}