using UnityEngine;

/// <summary>
/// [카드] '턴 스킵' 카드 로직.
/// 사용 시 'TurnManager.SkipNextTurn()'을 호출하여 다음 플레이어의 턴을 건너뛰게 합니다.
/// 
/// ⚠️ [수정됨] 카드를 사용해도 총이 움직이는 연출(PlayCardAnimation)을 하지 않습니다.
/// </summary>
public class TurnSkip : CardLogic
{
    public override void Initialize(PlayerHand ownerHand)
    {
        base.Initialize(ownerHand);
    }

    /// <summary>
    /// [핵심 기능] 카드를 사용했을 때 호출됩니다.
    /// </summary>
    public override bool Use()
    {
        // 1. TurnManager 참조 확인
        if (turnManager == null)
        {
            turnManager = FindObjectOfType<TurnManager>();
        }
        if (turnManager == null)
        {
            Debug.LogError("[TurnSkip] TurnManager가 연결되지 않아 턴을 스킵할 수 없습니다!");
            return false;
        }

        // ❌ [삭제됨] 애니메이션 호출 제거 (총이 움직이지 않음)
        // if (rpt != null)
        // {
        //     rpt.PlayCardAnimation();
        // }

        Debug.Log($"<color=green>[CARD USED]</color> {playerHand.name}이(가) '턴 스킵' 카드를 사용! (다음 턴 건너뜀 예약)");

        // 2. 턴 매니저에게 '다음 턴 스킵' 예약
        turnManager.SkipNextTurn();

        // 3. 카드를 소모하되, 턴은 종료하지 않음 (총 쏘기 가능)
        return base.ConsumeCardWithoutEndingTurn();
    }
}