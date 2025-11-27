using UnityEngine;
using System.Collections;

/// <summary>
/// PlayerHealth와 연동하여 '현재 체력'에 맞는 머티리얼을
/// 플레이어 렌더러에 영구적으로 적용합니다.
/// (예: 3HP = 깨끗함, 2HP = 금이 감, 1HP = 더 많이 금이 감, 0HP = 사망)
/// </summary>
[RequireComponent(typeof(PlayerHealth))] // 반드시 PlayerHealth가 있어야 함
public class PlayerVisuals : MonoBehaviour
{
    [Header("시각 효과 설정")]
    [Tooltip("체력 상태별 머티리얼 배열. (배열 크기 = 최대체력 + 1 이어야 함. 0HP 포함)")]
    public Material[] healthStateMaterials;

    [Tooltip("머티리얼을 변경할 렌더러 (비워두면 자식에서 SkinnedMeshRenderer나 MeshRenderer를 자동으로 찾음)")]
    public Renderer targetRenderer;

    // --- 내부 참조 ---
    private PlayerHealth _health; // 이 오브젝트의 PlayerHealth 컴포넌트 (캐시)

    void Awake()
    {
        _health = GetComponent<PlayerHealth>(); // PlayerHealth 캐시

        // 1. 렌더러 자동 찾기 (targetRenderer가 비어있을 경우)
        if (targetRenderer == null)
        {
            // (보통 캐릭터는 SkinnedMeshRenderer를 사용하므로 먼저 찾음)
            targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }
        if (targetRenderer == null)
        {
            // (캐릭터가 아니면 MeshRenderer를 찾음)
            targetRenderer = GetComponentInChildren<MeshRenderer>();
        }

        // 1-1. 렌더러를 못 찾으면 스크립트 비활성화
        if (targetRenderer == null)
        {
            Debug.LogError($"[PlayerVisuals] {name} 또는 그 자식에서 Renderer를 찾지 못했습니다.", this);
            enabled = false;
            return;
        }

        // 2. 머티리얼 배열 유효성 검사
        // PlayerHealth의 maxHP가 3이라면, 이 배열의 크기는 4 (인덱스 0, 1, 2, 3)여야 합니다.
        if (healthStateMaterials == null || healthStateMaterials.Length != _health.maxHP + 1)
        {
            Debug.LogError($"[PlayerVisuals] 'Health State Materials' 배열이 잘못 설정되었습니다. {name}의 maxHP가 {_health.maxHP}이므로, 배열의 크기(Size)는 반드시 {_health.maxHP + 1} 이어야 합니다. (0HP~{_health.maxHP}HP)", this);
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // 3. 게임 시작 시, 현재 체력(최대 체력)에 맞는 머티리얼을 즉시 적용
        UpdateVisuals();
    }

    void OnEnable()
    {
        // 4. PlayerHealth의 이벤트 구독
        // (PlayerHealth는 '실탄'(데미지>0)일 때만 OnDamaged를 호출함)
        _health.OnDamaged += HandleHealthChange;
        _health.OnHealed += HandleHealthChange;
        _health.OnDied += HandleDeath; // 사망 시(0HP)에도 확실하게 0HP 머티리얼로 변경
    }

    void OnDisable()
    {
        // 5. 구독 해제 (메모리 누수 방지)
        _health.OnDamaged -= HandleHealthChange;
        _health.OnHealed -= HandleHealthChange;
        _health.OnDied -= HandleDeath;
    }

    /// <summary>
    /// OnDamaged 또는 OnHealed 이벤트가 발생하면 호출됩니다.
    /// </summary>
    private void HandleHealthChange(int amount, int newHP)
    {
        UpdateVisuals(); // 머티리얼 갱신
    }

    /// <summary>
    /// OnDied 이벤트(HP가 0이 됨)가 발생하면 호출됩니다.
    /// </summary>
    private void HandleDeath()
    {
        UpdateVisuals(); // 0HP 머티리얼로 갱신
    }

    /// <summary>
    /// 현재 체력(CurrentHP)을 기준으로 healthStateMaterials 배열에서
    /// 알맞은 머티리얼을 찾아 targetRenderer에 적용합니다.
    /// </summary>
    [ContextMenu("Update Visuals (Debug)")] // (디버깅용) 인스펙터 우클릭 메뉴
    public void UpdateVisuals()
    {
        if (_health == null) return;

        // 현재 HP 가져오기 (0 ~ maxHP)
        int currentHP = _health.CurrentHP;

        // 배열 범위를 벗어나지 않도록 안전하게 Clamp (예: 0~3 사이로)
        int clampedHP = Mathf.Clamp(currentHP, 0, _health.maxHP);

        // 해당 HP 상태의 머티리얼이 비어있지 않다면
        if (healthStateMaterials[clampedHP] != null)
        {
            // 렌더러의 머티리얼을 교체!
            targetRenderer.material = healthStateMaterials[clampedHP];
        }
        else
        {
            Debug.LogWarning($"[PlayerVisuals] {name}의 HP {clampedHP}에 해당하는 머티리얼이 비어있습니다.");
        }
    }
}