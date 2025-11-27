using UnityEngine;

/// <summary>
/// 캐릭터의 애니메이션 상태(조준, 발사, 줍기, 사망)를 관리하는 매니저입니다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class CharacterAnimManager : MonoBehaviour
{
    private Animator _animator;

    // Animator 파라미터 이름 해시 (성능 최적화)
    private static readonly int IsAimingHash = Animator.StringToHash("IsAiming"); // Bool (조준 중?)
    private static readonly int PickupHash = Animator.StringToHash("tPickup");    // Trigger (줍기)
    private static readonly int FireHash = Animator.StringToHash("tFire");        // Trigger (발사)
    private static readonly int PutDownHash = Animator.StringToHash("tPutDown");  // Trigger (내려놓기)
    private static readonly int DieHash = Animator.StringToHash("tDie");          // Trigger (사망)

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary> 조준 상태 설정 (true: 조준 자세, false: 대기 자세) </summary>
    public void SetAimingState(bool isAiming)
    {
        if (_animator) _animator.SetBool(IsAimingHash, isAiming);
    }

    /// <summary> 총 줍기 동작 </summary>
    public void TriggerPickup()
    {
        if (_animator) _animator.SetTrigger(PickupHash);
    }

    /// <summary> 발사 동작 </summary>
    public void TriggerFire()
    {
        if (_animator) _animator.SetTrigger(FireHash);
    }

    /// <summary> 총 내려놓기 동작 </summary>
    public void TriggerPutDown()
    {
        if (_animator) _animator.SetTrigger(PutDownHash);
    }

    /// <summary> 사망 동작 </summary>
    public void TriggerDeath()
    {
        if (_animator) _animator.SetTrigger(DieHash);
    }
}