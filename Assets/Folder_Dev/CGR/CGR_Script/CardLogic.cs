using UnityEngine;

/// <summary>
/// [추상 클래스] 모든 카드 기능(예: TurnSkip)이 상속받아야 하는 '설계도'입니다.
/// 이 스크립트 자체는 프리팹에 붙이지 않고, 'TurnSkip' 등이 이 스크립트를 상속받습니다.
/// </summary>
public abstract class CardLogic : MonoBehaviour
{
    // --- 내부 참조 ---
    protected CardVisual cardVisual; // 이 로직이 붙어있는 부모 CardVisual
    protected PlayerHand playerHand; // 이 카드를 소유한 플레이어의 손 (Initialize에서 받음)
    protected TurnManager turnManager; // 턴 매니저 (필요한 카드만 찾음)
    protected RevolverTurnPossession rpt; // 총기 매니저(RPT) 참조

    /// <summary>
    /// 카드가 생성될 때 CardVisual이 호출합니다. (초기화)
    /// </summary>
    public virtual void Initialize(PlayerHand ownerHand)
    {
        // 1. 이 로직이 붙어있는 부모 CardVisual 스크립트를 찾습니다.
        cardVisual = GetComponentInParent<CardVisual>();
        if (cardVisual == null)
        {
            Debug.LogError($"[CardLogic] {name}가 부모에게서 CardVisual.cs를 찾지 못했습니다!", this);
            return;
        }

        // 2. CardVisual이 넘겨준 소유자(PlayerHand)를 저장합니다.
        this.playerHand = ownerHand;
        if (playerHand == null)
        {
            Debug.LogError($"[CardLogic] {name}가 ownerHand(PlayerHand) 참조를 받지 못했습니다!", this);
            return;
        }

        // 3. 씬에서 RevolverTurnPossession을 찾아 캐시합니다. (카드 애니메이션 호출용)
        rpt = FindObjectOfType<RevolverTurnPossession>();
        if (rpt == null)
        {
            Debug.LogError($"[CardLogic] {name}가 씬에서 RevolverTurnPossession.cs를 찾지 못했습니다!", this);
        }

        // 4. (디버그 로그) 초기화 성공 알림
        Debug.Log($"[CardLogic] {cardVisual.name}의 로직 [{this.GetType().Name}] 초기화 완료. 소유자: {playerHand.name}");
    }

    /// <summary>
    /// [핵심] 이 카드를 '사용'했을 때 호출될 함수입니다. (상속받은 스크립트가 구현)
    /// </summary>
    /// <returns>true: 턴을 종료합니다. / false: 턴을 유지합니다.</returns>
    public abstract bool Use();

    /// <summary>
    /// [공용] 카드를 사용한 후, 카드를 파괴하고 '턴을 넘깁니다'.
    /// (예: 턴 스킵)
    /// </summary>
    /// <returns>true (턴 종료)</returns>
    protected bool ConsumeCardAndEndTurn()
    {
        string cardName = (cardVisual != null && cardVisual.Data != null) ? cardVisual.Data.cardName : "알 수 없는 카드";
        Debug.Log($"[CardLogic] {cardName} 카드 소모됨. 턴을 종료합니다.");

        // 1. 손에서 이 카드 제거
        if (playerHand != null)
        {
            playerHand.RemoveCard(cardVisual);
        }

        // 2. 턴 종료 요청
        if (rpt != null)
        {
            rpt.HandleActionComplete(); // RPT의 턴 종료 함수(딜레이 후 턴 넘김) 호출
        }
        else
        {
            Debug.LogError("[CardLogic] 씬에서 RevolverTurnPossession.cs를 찾지 못해 턴을 넘길 수 없습니다!");
        }

        // 3. 카드 오브젝트 파괴
        if (cardVisual != null)
        {
            Destroy(cardVisual.gameObject);
        }
        else
        {
            Destroy(this.gameObject); // (비상시 로직만 파괴)
        }

        return true; // 턴 종료
    }

    /// <summary>
    /// [공용] 카드를 사용한 후, 카드를 파괴하되 '턴을 넘기지 않습니다'.
    /// (예: 실린더 확인, HP+1, 데미지+1)
    /// </summary>
    /// <returns>false (턴 유지)</returns>
    protected bool ConsumeCardWithoutEndingTurn()
    {
        string cardName = (cardVisual != null && cardVisual.Data != null) ? cardVisual.Data.cardName : "알 수 없는 카드";
        Debug.Log($"[CardLogic] {cardName} 카드 소모됨. (턴 유지)");

        // 1. 손에서 이 카드 제거
        if (playerHand != null)
        {
            playerHand.RemoveCard(cardVisual);
        }

        // 2. (턴 종료 로직 없음)

        // 3. 카드 오브젝트 파괴
        if (cardVisual != null)
        {
            Destroy(cardVisual.gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }

        return false; // 턴 유지
    }
}