using UnityEngine;
using System; // Action (이벤트) 사용을 위해 추가

/// <summary>
/// 각 플레이어에 부착하는 간단한 HP(체력) 컴포넌트입니다.
/// - TakeDamage(데미지 받기), Heal(치유) 함수를 제공합니다.
/// - HP가 0 이하가 되면 사망 처리(isDead=true)를 합니다.
/// - 외부 스크립트(예: PlayerVisuals, PlayerHealthDisplay)가 구독할 수 있는
///   이벤트(OnDamaged, OnHealed, OnDied)를 제공합니다.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("HP 설정")]
    [Tooltip("최대 체력 (PlayerVisuals, PlayerHealthDisplay 등과 이 값을 맞춰야 함)")]
    public int maxHP = 3;
    [SerializeField] // private이지만 인스펙터에서 볼 수 있도록 설정
    private int currentHP; // 현재 HP
    [SerializeField]
    private bool isDead;   // 사망 여부 플래그

    [Header("사망 처리 옵션")]
    [Tooltip("사망 시, 이 게임 오브젝트를 비활성화할지 (예: 카메라가 못 보게)")]
    public bool deactivateOnDeath = false;
    [Tooltip("사망 시, 이 게임 오브젝트를 파괴할지 (권장하지 않음)")]
    public bool destroyOnDeath = false;

    // ────────────────────────────── 이벤트 ──────────────────────────────

    /// <summary>
    /// '실탄'(데미지 > 0)에 맞았을 때만 발생하는 이벤트입니다.
    /// (int damageAmount: 받은 데미지, int newHP: 데미지 적용 후 현재 HP)
    /// (PlayerVisuals가 이 이벤트를 구독하여 머티리얼을 변경합니다.)
    /// </summary>
    public event Action<int, int> OnDamaged;

    /// <summary>
    /// 치유(Heal)가 발생했을 때 발생하는 이벤트입니다.
    /// (int healAmount: 치유량, int newHP: 치유 적용 후 현재 HP)
    /// (PlayerHealthDisplay가 이 이벤트를 구독하여 하트를 켭니다.)
    /// </summary>
    public event Action<int, int> OnHealed;

    /// <summary>
    /// HP가 0 이하가 되어 '사망 상태(isDead=true)'가 되는 순간 1회 발생하는 이벤트입니다.
    /// </summary>
    public event Action OnDied;

    // ────────────────────────────── 프로퍼티 (읽기 전용) ──────────────────────────────

    /// <summary>현재 HP를 외부에서 안전하게 읽어갈 수 있도록 합니다. (읽기 전용)</summary>
    public int CurrentHP => currentHP;

    /// <summary>사망 여부를 외부에서 안전하게 읽어갈 수 있도록 합니다. (읽기 전용)</summary>
    public bool IsDead => isDead;

    // ────────────────────────────── 라이프사이클 ──────────────────────────────

    void Awake()
    {
        // 씬이 로드될 때(게임 시작 시) HP를 최대치로 초기화
        currentHP = Mathf.Max(1, maxHP);    // 0이 되지 않도록 최소 1 보장
        isDead = false;                     // 살아있는 상태로 시작
    }

    // ────────────────────────────── 핵심 함수 ──────────────────────────────

    /// <summary>
    /// 데미지를 적용합니다. (RevolverTurnPossession이 호출)
    /// (amount가 0 이하면 '공포탄'으로 간주하고 OnDamaged 이벤트를 발생시키지 않습니다.)
    /// </summary>
    public void TakeDamage(int amount)
    {
        if (isDead) return;                 // 이미 죽었으면 무시
        int dmg = Mathf.Max(0, amount);     // 음수 데미지 방지 (0으로 보정)

        // ⚠️ 핵심: 데미지가 0이면(즉, 공포탄이면)
        // OnDamaged 이벤트를 발생시키지 않고(PlayerVisuals가 반응하지 않음) 종료
        if (dmg == 0)
        {
            Debug.Log($"[PlayerHealth] {name}이(가) 공포탄에 맞았습니다. (데미지 없음)");
            return;
        }

        // --- 여기부터는 데미지가 1 이상인 경우 (실탄) ---
        Debug.Log($"[PlayerHealth] {name}이(가) 실탄에 맞았습니다! (데미지: {dmg})");

        // 현재 HP에서 데미지만큼 감소 (최저 0)
        currentHP = Mathf.Max(0, currentHP - dmg);

        // [이벤트 발생] OnDamaged를 구독한 모든 스크립트(예: PlayerVisuals, PlayerHealthDisplay)에게 알림
        OnDamaged?.Invoke(dmg, currentHP);

        // 사망 확인
        if (currentHP <= 0)
        {
            isDead = true;                          // 사망 플래그 ON
            OnDied?.Invoke();                       // [이벤트 발생] 사망 이벤트 알림

            // 사망 처리 옵션 적용
            if (deactivateOnDeath)
            {
                gameObject.SetActive(false); // 비활성화
            }
            else if (destroyOnDeath)
            {
                Destroy(gameObject); // 파괴 (다른 스크립트가 참조 중이면 오류 발생 가능)
            }
        }
    }

    /// <summary>
    /// 체력을 치유합니다. (사망한 상태에선 치유되지 않습니다.)
    /// </summary>
    public void Heal(int amount)
    {
        if (isDead) return;                 // 죽었으면 치유 불가
        int heal = Mathf.Max(0, amount);    // 음수 치유 방지
        if (heal == 0) return;              // 0은 무시

        int before = currentHP;
        currentHP = Mathf.Min(maxHP, currentHP + heal); // 최대 체력(maxHP)을 넘지 않도록

        // [이벤트 발생] OnHealed를 구독한 모든 스크립트(예: PlayerHealthDisplay)에게 알림
        OnHealed?.Invoke(heal, currentHP);
    }

    /// <summary>
    /// HP와 사망 상태를 초기화합니다. (게임 재시작 등에 사용)
    /// </summary>
    public void ResetHealth()
    {
        isDead = false;                     // 살아있는 상태로
        currentHP = Mathf.Max(1, maxHP);    // HP 리셋
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true); // 비활성화(deactivateOnDeath)되었다면 다시 켜기
        }
    }

    public void ApplyNetworkedDamage(int newHp)
    {
        int before = currentHP;
        currentHP = Mathf.Clamp(newHp, 0, maxHP);

        if (currentHP < before)
        {
            Debug.Log($"[PlayerHealth] 서버 동기화: HP {before} → {currentHP}");
            OnDamaged?.Invoke(before - currentHP, currentHP);
        }

        if (currentHP <= 0 && !isDead)
        {
            isDead = true;
            OnDied?.Invoke();
            if (deactivateOnDeath)
                gameObject.SetActive(false);
        }
    }

}