using UnityEngine;

/// <summary>
/// [카드] '실린더 확인' 카드 로직.
/// 사용 시 실린더를 살짝 열어보고, 효과음을 재생합니다.
/// </summary>
public class PeekCard : CardLogic
{
    // ⚠️ [추가됨] 확인 효과음
    [Header("사운드")]
    [Tooltip("실린더를 확인할 때 재생할 소리 (딸깍, 스르륵 등)")]
    public AudioClip peekSound;

    private RevolverCylinderPeek _peeker; // 실린더 확인 로직 (캐시)

    public override void Initialize(PlayerHand ownerHand)
    {
        base.Initialize(ownerHand);
        _peeker = FindObjectOfType<RevolverCylinderPeek>();
        if (_peeker == null)
        {
            Debug.LogError("[PeekCard] 씬에서 RevolverCylinderPeek.cs를 찾지 못했습니다!", this);
        }
    }

    public override bool Use()
    {
        // 1. 애니메이션 시작
        if (rpt != null)
        {
            rpt.PlayCardAnimation();

            // ⚠️ [추가] 확인 사운드 재생 (RPT의 스피커 사용)
            if (rpt.audioSource != null && peekSound != null)
            {
                rpt.audioSource.PlayOneShot(peekSound);
            }
        }

        if (_peeker == null)
        {
            Debug.LogError("[PeekCard] Peeker가 없어 확인 불가!");
            return false;
        }

        Debug.Log($"<color=green>[CARD USED]</color> {playerHand.name}이(가) '실린더 확인' 카드 사용!");

        // 2. 확인(Peek) 연출 실행
        _peeker.PlayOnce();

        // 3. 턴 유지
        return base.ConsumeCardWithoutEndingTurn();
    }
}