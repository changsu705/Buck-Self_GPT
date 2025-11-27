/*
 * PlayerHealthDisplay.cs (수동 연결 버전)
 * [주석 수정]
 * - 스크립트의 역할을 명확히 하고 '수동 연결' 방식을 강조했습니다.
 */

using UnityEngine;
using System.Collections.Generic; // List 사용을 위해 필수

/// <summary>
/// [UI] 'PlayerHealth'와 연동하여 현재 체력(HP)에 맞는 머티리얼을 'UI 렌더러'에 적용합니다.
/// (예: 하트 UI)
/// 
/// ⚠️ 이 버전은 'targetPlayerHealth'를 인스펙터에서 **수동으로 연결**해야 합니다.
/// </summary>
public class PlayerHealthDisplay : MonoBehaviour
{
    [Header("상태별 머티리얼 배열")]
    [Tooltip("체력 상태별 머티리얼 배열. (배열 크기 = 최대체력 + 1 이어야 함. 0HP 포함)")]
    public Material[] healthStateMaterials;

    [Header("적용할 렌더러 (1개)")]
    [Tooltip("머티리얼을 변경할 렌더러 (예: 하트 3개가 그려진 Quad)")]
    public Renderer targetHeartRenderer;

    [Header("연동할 대상")]
    [Tooltip("체력을 가져올 PlayerHealth 컴포넌트 (직접 드래그해서 연결)")]
    public PlayerHealth targetPlayerHealth;

    // --- 내부 상태 ---
    private PlayerHealth _myHealth; // 캐시된 PlayerHealth 참조

    void Awake()
    {
        // 1. [수동 연결] 'targetPlayerHealth'가 할당되었는지 확인
        if (targetPlayerHealth == null)
        {
            Debug.LogError($"[PlayerHealthDisplay] {name}의 'Target Player Health' 필드가 비어있습니다! 인스펙터에서 'HumanPlayer'를 연결해주세요.", this);
            enabled = false;
            return;
        }

        // 1-1. 캐시 변수에 할당
        _myHealth = targetPlayerHealth;

        // 2. 렌더러가 할당되었는지 확인
        if (targetHeartRenderer == null)
        {
            Debug.LogError($"[PlayerHealthDisplay] {name}의 'Target Heart Renderer'가 비어있습니다!", this);
            enabled = false;
            return;
        }

        // 3. 머티리얼 배열 유효성 검사 (maxHP 기준)
        if (healthStateMaterials == null || healthStateMaterials.Length != _myHealth.maxHP + 1)
        {
            Debug.LogError($"[PlayerHealthDisplay] 'Health State Materials' 배열이 잘못 설정되었습니다. {name}의 maxHP가 {_myHealth.maxHP}이므로, 배열의 크기(Size)는 반드시 {_myHealth.maxHP + 1} 이어야 합니다. (0HP~{_myHealth.maxHP}HP)", this);
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // 게임 시작 시 하트 UI 즉시 업데이트
        UpdateHeartVisuals();
    }

    void OnEnable()
    {
        // 체력 변경 이벤트 구독
        if (_myHealth == null) return;
        _myHealth.OnDamaged += HandleHealthChange;
        _myHealth.OnHealed += HandleHealthChange;
        _myHealth.OnDied += HandleDeath;
    }

    void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (_myHealth == null) return;
        _myHealth.OnDamaged -= HandleHealthChange;
        _myHealth.OnHealed -= HandleHealthChange;
        _myHealth.OnDied -= HandleDeath;
    }

    // 체력 변경 또는 사망 시 UpdateHeartVisuals 호출
    private void HandleHealthChange(int amount, int newHP) => UpdateHeartVisuals();
    private void HandleDeath() => UpdateHeartVisuals();

    /// <summary>
    /// '현재' 체력을 기반으로 'UI' 렌더러의 '머티리얼'을 갱신합니다.
    /// </summary>
    [ContextMenu("Update Heart Visuals (Debug)")]
    public void UpdateHeartVisuals()
    {
        if (_myHealth == null) return;

        int currentHP = _myHealth.CurrentHP;
        // 배열 인덱스가 범위를 벗어나지 않도록 Clamp
        int clampedHP = Mathf.Clamp(currentHP, 0, _myHealth.maxHP);

        if (healthStateMaterials[clampedHP] != null)
        {
            targetHeartRenderer.material = healthStateMaterials[clampedHP];
        }
        else
        {
            Debug.LogWarning($"[PlayerHealthDisplay] {name}의 HP {clampedHP}에 해당하는 머티리얼이 비어있습니다.");
        }
    }
}