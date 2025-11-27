using UnityEngine;

/// <summary>
/// [카드 기능] 데미지+1 버프 카드입니다.
/// CardLogic을 상속받아 Use() 함수를 구현합니다.
/// (이 카드는 '무료 행동'이며 턴을 소모하지 않습니다.)
/// </summary>
public class DamageBoostCard : CardLogic
{
    /// <summary>
    /// 카드가 생성될 때 1회 호출됩니다. (참조 설정)
    /// </summary>
    public override void Initialize(PlayerHand ownerHand)
    {
        // 부모(CardLogic)의 초기화 함수를 호출하여 'playerHand'를 설정합니다.
        base.Initialize(ownerHand);
    }

    /// <summary>
    /// [핵심 기능] 카드를 사용했을 때 호출됩니다.
    /// </summary>
    /// <returns>false (턴 유지)</returns>
    public override bool Use()
    {
        if (playerHand == null)
        {
            Debug.LogError("[DamageBoostCard] PlayerHand가 연결되지 않았습니다!");
            return false;
        }

        // 1. 카드를 사용한 플레이어의 PlayerStatusEffects 컴포넌트를 찾습니다.
        PlayerStatusEffects status = playerHand.GetComponent<PlayerStatusEffects>();
        if (status == null)
        {
            Debug.LogError($"[DamageBoostCard] {playerHand.name}에게 PlayerStatusEffects.cs가 없습니다! (플레이어 프리팹에 추가 필요)", playerHand);
            return false;
        }

        Debug.Log($"<color=green>[CARD USED]</color> {playerHand.name}이(가) '데미지+1' 카드를 사용!");

        // 2. 해당 플레이어에게 데미지 부스트 버프를 적용합니다.
        status.ApplyDamageBoost();

        // 3. 턴을 종료하지 않는 함수를 호출하고, false(턴 유지)를 반환합니다.
        return base.ConsumeCardWithoutEndingTurn();
    }
}