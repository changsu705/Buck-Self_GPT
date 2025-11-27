using UnityEngine;

/// <summary>
/// [카드] '재장전' 카드 로직.
/// 사용 시 실린더를 새로 장전하고, 재장전 효과음을 재생합니다.
/// </summary>
public class ReloadCard : CardLogic
{
    [Header("카드 설정")]
    [Tooltip("이 카드가 장전할 '실탄'의 개수입니다.")]
    public int numLiveRoundsToLoad = 2;

    // ⚠️ [추가됨] 재장전 효과음
    [Header("사운드")]
    [Tooltip("재장전 시 재생할 소리 (철컥, 차르르 등)")]
    public AudioClip reloadSound;

    private RevolverCylinder _cylinder; // 리볼버 실린더 스크립트 (캐시)

    public override void Initialize(PlayerHand ownerHand)
    {
        base.Initialize(ownerHand);
        _cylinder = FindObjectOfType<RevolverCylinder>();
        if (_cylinder == null)
        {
            Debug.LogError("[ReloadCard] 씬에서 RevolverCylinder.cs를 찾지 못했습니다!", this);
        }
    }

    public override bool Use()
    {
        // 1. 애니메이션 시작
        if (rpt != null)
        {
            rpt.PlayCardAnimation();

            // ⚠️ [추가] 재장전 사운드 재생 (RPT의 스피커 사용)
            if (rpt.audioSource != null && reloadSound != null)
            {
                rpt.audioSource.PlayOneShot(reloadSound);
            }
        }

        if (_cylinder == null)
        {
            Debug.LogError("[ReloadCard] Cylinder가 없어 재장전 불가!");
            return false;
        }

        Debug.Log($"<color=green>[CARD USED]</color> {playerHand.name}이(가) '재장전' 카드 사용! (실탄 {numLiveRoundsToLoad}개)");

        // 2. 장전 로직 실행
        _cylinder.LoadCylinder(numLiveRoundsToLoad);

        // 3. 턴 유지
        return base.ConsumeCardWithoutEndingTurn();
    }
}