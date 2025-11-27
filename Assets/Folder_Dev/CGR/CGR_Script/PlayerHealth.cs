using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    [Header("HP 설정")]
    public int maxHP = 3;

    [SerializeField] private FadeScreen fadeScreen;
    [SerializeField] private int currentHP;
    [SerializeField] private bool isDead;

    [Header("사망 처리 옵션")]
    public bool deactivateOnDeath = false; // 애니메이션을 보려면 false 권장
    public bool destroyOnDeath = false;

    public event Action<int, int> OnDamaged;
    public event Action<int, int> OnHealed;
    public event Action OnDied;

    public int CurrentHP => currentHP;
    public bool IsDead => isDead;

    private CharacterAnimManager _animManager; // 애니메이션 매니저

    void Awake()
    {
        currentHP = Mathf.Max(1, maxHP);
        isDead = false;
        _animManager = GetComponent<CharacterAnimManager>();
    }

    public void TakeDamage(int amount)
    {
        if (fadeScreen != null)
        {
            // FadeScreen의 피격 플래시 함수를 호출하여 화면을 깜빡입니다.
            fadeScreen.FlashOnHit();
        }


        if (isDead) return;
        int dmg = Mathf.Max(0, amount);

        if (dmg == 0) return;

        currentHP = Mathf.Max(0, currentHP - dmg);
        OnDamaged?.Invoke(dmg, currentHP);

        if (currentHP <= 0)
        {

            isDead = true;

            // ⚠️ [사망 애니메이션 실행]
            if (_animManager != null) _animManager.TriggerDeath();

            OnDied?.Invoke();

            if (deactivateOnDeath) gameObject.SetActive(false);
            else if (destroyOnDeath) Destroy(gameObject);
        }
    }

    public void Heal(int amount)
    {
        if (isDead) return;
        int heal = Mathf.Max(0, amount);
        if (heal == 0) return;
        currentHP = Mathf.Min(maxHP, currentHP + heal);
        OnHealed?.Invoke(heal, currentHP);
    }

    public void ResetHealth()
    {
        isDead = false;
        currentHP = Mathf.Max(1, maxHP);
        gameObject.SetActive(true);
        if (_animManager) _animManager.SetAimingState(false); // 부활 시 대기 상태로
    }

    public void ApplyNetworkedDamage(int damage)
    {
        TakeDamage(damage);
    }
}