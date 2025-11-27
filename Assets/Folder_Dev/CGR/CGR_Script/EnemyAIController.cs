using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// AI 플레이어에게 부착되어 턴마다 행동을 결정합니다.
/// 1. (확률) 카드 사용 (무료 행동 / 턴 종료)
/// 2. (카드 미사용 시) (확률) 자신 쏘기 또는 상대 쏘기
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerHand))]
public class EnemyAIController : MonoBehaviour
{
    [Header("핵심 참조 (비워두면 자동으로 찾음)")]
    [Tooltip("게임의 턴을 관리하는 TurnManager")]
    public TurnManager turnManager;
    [Tooltip("총의 모든 행동을 관리하는 RevolverTurnPossession")]
    public RevolverTurnPossession revolverTurnPossession;

    [Header("AI 행동 설정")]
    [Tooltip("AI가 행동을 결정하기 전까지 대기하는 시간(초)")]
    public float decisionDelay = 1.5f;
    [Tooltip("AI가 스스로에게 총을 쏠 확률 (0.0 = 0%, 0.5 = 50%)")]
    [Range(0f, 1f)]
    public float selfShotChance = 0.5f;

    [Header("AI 카드 사용 설정")]
    [Tooltip("AI가 (카드가 있을 경우) 카드를 사용할 확률 (0.0 = 0%, 0.5 = 50%)")]
    [Range(0f, 1f)]
    public float useCardChance = 0.3f;
    [Tooltip("AI가 카드를 사용하기로 결정했을 때의 추가 딜레이(초)")]
    public float cardDecisionDelay = 1.0f;

    // --- 내부 상태 변수 ---
    private bool _hasActedThisTurn = false; // 이번 턴에 행동을 이미 했는지 (중복 행동 방지)
    private PlayerHealth _myHealth;         // AI 자신의 체력
    private PlayerHand _myHand;             // AI 자신의 손 (카드 확인용)

    void Awake()
    {
        // 1. 자신의 컴포넌트 캐시
        _myHealth = GetComponent<PlayerHealth>();
        _myHand = GetComponent<PlayerHand>();

        // 2. 자동 참조 찾기 (인스펙터에서 연결하는 것을 권장)
        if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
        if (revolverTurnPossession == null) revolverTurnPossession = FindObjectOfType<RevolverTurnPossession>();

        // 3. 필수 컴포넌트 유효성 검사
        if (turnManager == null)
        {
            Debug.LogError($"[EnemyAIController] {name}: TurnManager를 씬에서 찾을 수 없습니다!", this);
            enabled = false;
        }
        if (revolverTurnPossession == null)
        {
            Debug.LogError($"[EnemyAIController] {name}: RevolverTurnPossession을 씬에서 찾을 수 없습니다!", this);
            enabled = false;
        }
        if (_myHand == null)
        {
            Debug.LogError($"[EnemyAIController] {name}: PlayerHand.cs를 찾지 못했습니다! (AI 프리팹에 추가 필요)", this);
            enabled = false;
        }
    }

    void Update()
    {
        // 1. 턴 매니저가 없거나, AI가 죽었으면 아무것도 하지 않음
        if (turnManager == null || _myHealth.IsDead)
        {
            return;
        }

        // 2. 현재 턴 플레이어 확인
        Transform currentPlayer = turnManager.GetCurrentPlayer();

        // 3. '내 턴'인지 확인
        if (currentPlayer == this.transform)
        {
            // 4. '내 턴'이지만 아직 행동하지 않았다면, 행동 시작
            if (!_hasActedThisTurn)
            {
                _hasActedThisTurn = true;
                StartCoroutine(Co_DecideAndAct());
            }
        }
        else
        {
            // 5. '내 턴'이 아니면, 다음 턴을 위해 행동 플래그 리셋
            _hasActedThisTurn = false;
        }
    }

    /// <summary>
    /// [코루틴] AI가 잠시 고민(대기)한 후, 행동(카드 또는 사격)을 결정하고 실행합니다.
    /// </summary>
    private IEnumerator Co_DecideAndAct()
    {
        // 1. 결정 딜레이 ("고민하는 척")
        yield return new WaitForSeconds(decisionDelay);

        // --- [AI 카드 사용 결정] ---
        List<CardVisual> cardsInHand = _myHand.GetCards();
        if (cardsInHand.Count > 0) // 손에 카드가 1장 이상 있고,
        {
            if (Random.value < useCardChance) // 카드를 사용하기로 결정했다면
            {
                // 1-1. 사용할 카드 선택 (일단 첫 번째 카드)
                CardVisual cardToUse = cardsInHand[0];
                CardLogic logic = cardToUse.GetComponentInChildren<CardLogic>();

                if (logic != null)
                {
                    Debug.Log($"<color=magenta>[AI DECISION]</color> {name}이(가) 카드 [{cardToUse.Data.cardName}]을(를) 사용합니다!");
                    yield return new WaitForSeconds(cardDecisionDelay);

                    // 1-2. 카드 사용! (Use()가 턴 종료 여부(bool)를 반환)
                    bool turnEnded = logic.Use();

                    // 1-3. 카드가 '턴 종료' 카드(예: TurnSkip)였다면 행동 종료
                    if (turnEnded)
                    {
                        yield break; // 턴 종료 (사격 안 함)
                    }
                    else
                    {
                        // 1-4. '무료 행동' 카드였다면, '애니메이션이 끝날 때까지 기다렸다가'
                        // 사격하는 새 코루틴을 시작하고, 이 코루틴은 종료합니다.
                        StartCoroutine(Co_ShootAfterFreeAction());
                        yield break;
                    }
                }
                else
                {
                    // (디버깅) CardData에는 로직이 있는데 프리팹에 스크립트가 없는 경우
                    Debug.LogWarning($"<color=magenta>[AI DECISION]</color> {name}이(가) {cardToUse.name}을(를) 사용하려 했으나, CardLogic.cs를 찾지 못했습니다.");
                }
            }
        }
        // --- [여기까지] ---

        // --- 2. 사격 결정 (카드를 사용하지 않았을 경우) ---
        // (이 로직은 Co_ShootAfterFreeAction()에도 동일하게 존재합니다)
        DecideShootingAction();
    }

    /// <summary>
    /// [코루틴] '무료 행동' 카드 사용 후, 카드 애니메이션이 끝날 때까지 기다렸다가 사격합니다.
    /// </summary>
    private IEnumerator Co_ShootAfterFreeAction()
    {
        // 1. RPT(총)의 'IsCardAnimating' 플래그가 false가 될 때까지 (애니메이션이 끝날 때까지) 대기
        if (revolverTurnPossession != null)
        {
            // (RPT가 IsCardAnimating을 public 프로퍼티로 제공해야 함)
            yield return new WaitUntil(() => revolverTurnPossession.IsCardAnimating == false);
        }

        // 2. (선택적) 애니메이션이 끝난 후 추가 딜레이 (사람처럼 보이게)
        yield return new WaitForSeconds(decisionDelay * 0.5f); // (기본 딜레이의 절반)

        // 3. '사격 결정' 로직 실행
        DecideShootingAction();
    }

    /// <summary>
    /// [공용] AI의 최종 행동으로 '사격'을 결정합니다. (자신 또는 상대)
    /// (Co_DecideAndAct와 Co_ShootAfterFreeAction이 공통으로 사용)
    /// </summary>
    private void DecideShootingAction()
    {
        float randomValue = Random.value;
        if (randomValue < selfShotChance)
        {
            // --- 결정: 자신에게 쏜다 ---
            Debug.Log($"<color=magenta>[AI DECISION]</color> {name}이(가) 스스로를 쏘기로 결정했습니다. (확률: {selfShotChance * 100}%)");
            revolverTurnPossession.ShootSelf();
        }
        else
        {
            // --- 결정: 상대를 쏜다 ---
            Transform target = FindLivingOpponent(); // (무작위 상대 찾기)
            if (target != null)
            {
                Debug.Log($"<color=magenta>[AI DECISION]</color> {name}이(가) 상대 [{target.name}]을(를) 쏘기로 결정했습니다.");
                revolverTurnPossession.AI_HandleAimRequest(target);
            }
            else
            {
                // [예외 처리] 쏠 상대가 없으면 (모두 죽었으면) 어쩔 수 없이 자신을 쏩니다.
                Debug.LogWarning($"<color=magenta>[AI DECISION]</color> {name}이(가) 상대를 쏘려 했지만, 살아있는 상대가 없어 스스로를 쏩니다.");
                revolverTurnPossession.ShootSelf();
            }
        }
    }

    /// <summary>
    /// '나'를 제외하고, '살아있는' 다른 플레이어를 '무작위로 1명' 찾아 그 Transform을 반환합니다.
    /// </summary>
    private Transform FindLivingOpponent()
    {
        // 1. TurnManager가 알고 있는 모든 플레이어 목록을 가져옵니다.
        List<Transform> allPlayers = turnManager.players;

        // 2. LINQ의 'Where'를 사용하여 '나'를 제외하고 '살아있는' 모든 상대방 리스트 생성
        List<Transform> livingOpponents = allPlayers.Where(player =>
            player != this.transform &&                  // 1. '나' 자신이 아니고,
            player.GetComponent<PlayerHealth>() != null && // 2. PlayerHealth 컴포넌트가 있으며,
            !player.GetComponent<PlayerHealth>().IsDead    // 3. 죽지 않은(IsDead == false) 플레이어
        ).ToList();

        // 3. 살아있는 상대 목록이 비어있는지 확인
        if (livingOpponents.Count == 0)
        {
            return null; // 쏠 상대가 아무도 없음
        }

        // 4. 살아있는 상대 목록에서 무작위로 1명의 인덱스를 선택
        int randomIndex = Random.Range(0, livingOpponents.Count);

        // 5. 해당 인덱스의 상대방 Transform을 반환
        return livingOpponents[randomIndex];
    }
}