// 파일 이름: PlayerStatusEffects.cs
using UnityEngine;

/// <summary>
/// 플레이어에게 부착되어 '데미지+1' 같은 일회성 상태 효과(버프/디버프)를 저장합니다.
/// 'RevolverTurnPossession'이 발사 시 이 스크립트를 확인하여 효과를 적용/소모합니다.
/// 
/// ⚠️ [중요] 이 스크립트는 'Player' 프리팹의 루트에
/// ('PlayerHealth' 등과 함께) 부착되어야 합니다.
/// </summary>
public class PlayerStatusEffects : MonoBehaviour
{
    // --- 내부 상태 ---
    // 다음 1회 공격의 데미지가 +1 증가하는지 여부
    private bool _hasDamageBoost = false;

    /// <summary>
    /// [읽기 전용] 현재 데미지 부스트 버프를 가지고 있는지 확인합니다.
    /// </summary>
    public bool HasDamageBoost => _hasDamageBoost;

    /// <summary>
    /// (DamageBoostCard 등이 호출) 데미지 부스트 버프를 '켭니다'.
    /// </summary>
    public void ApplyDamageBoost()
    {
        _hasDamageBoost = true;
        Debug.Log($"<color=cyan>[{name}]</color>이(가) '데미지+1' 버프를 얻었습니다!");
    }

    /// <summary>
    /// ('RevolverTurnPossession'이 호출) 데미지 부스트를 '사용(소모)'합니다.
    /// </summary>
    public void ConsumeDamageBoost()
    {
        _hasDamageBoost = false;
        Debug.Log($"<color=cyan>[{name}]</color>이(가) '데미지+1' 버프를 소모했습니다.");
    }
}