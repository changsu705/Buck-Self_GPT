using UnityEngine;
using System.Collections.Generic; // List 사용을 위해 필수

/// <summary>
/// '자신'의 PlayerHealth 컴포넌트를 찾아서,
/// '자신'에게 할당된 3D 하트 오브젝트 리스트(heartObjects)를 
/// 현재 체력(CurrentHP)에 맞게 켜고 끄는(SetActive) 스크립트입니다.
/// (이 스크립트는 TurnManager가 필요 없이 플레이어 프리팹에 직접 붙입니다.)
/// </summary>
public class PlayerHealthDisplay : MonoBehaviour
{
    [Header("3D 오브젝트 참조")]
    [Tooltip("체력을 표시할 3D GameObject 들을 순서대로 할당 (하트 모델, 구체 등)")]
    public List<GameObject> heartObjects = new List<GameObject>();

    // --- 내부 상태 ---
    private PlayerHealth _myHealth; // 이 오브젝트(플레이어)의 체력 컴포넌트 (캐시)

    void Awake()
    {
        // 1. '나' 또는 '나의 부모'에게서 PlayerHealth 컴포넌트를 찾습니다.
        _myHealth = GetComponent<PlayerHealth>();
        if (_myHealth == null)
        {
            _myHealth = GetComponentInParent<PlayerHealth>();
        }

        // 1-1. PlayerHealth를 못 찾으면 스크립트 비활성화
        if (_myHealth == null)
        {
            Debug.LogError($"[PlayerHealthDisplay] {name} 근처에서 PlayerHealth.cs를 찾지 못했습니다!", this);
            enabled = false;
            return;
        }

        // 2. 하트 리스트가 비어있는지 확인
        if (heartObjects == null || heartObjects.Count == 0)
        {
            Debug.LogError($"[PlayerHealthDisplay] {name}의 'Heart Objects' 리스트가 비어있습니다! (하트를 연결해야 함)", this);
            enabled = false;
            return;
        }
    }

    void Start()
    {
        // 3. 게임 시작 시, '내' 체력(최대 체력)에 맞게 하트를 즉시 갱신
        UpdateHeartObjects();
    }

    void OnEnable()
    {
        if (_myHealth == null) return;

        // 4. '내' 체력 이벤트 구독
        // PlayerHealth의 OnDamaged/OnHealed/OnDied 이벤트가 발생할 때마다,
        // HandleHealthChange 또는 HandleDeath 함수를 호출하도록 등록합니다.
        _myHealth.OnDamaged += HandleHealthChange;
        _myHealth.OnHealed += HandleHealthChange;
        _myHealth.OnDied += HandleDeath;
    }

    void OnDisable()
    {
        if (_myHealth == null) return;

        // 5. '내' 체력 이벤트 구독 해제 (메모리 누수 방지)
        // (이 오브젝트가 비활성화되거나 파괴될 때, 등록했던 함수 연결을 해제)
        _myHealth.OnDamaged -= HandleHealthChange;
        _myHealth.OnHealed -= HandleHealthChange;
        _myHealth.OnDied -= HandleDeath;
    }

    // PlayerHealth에서 OnDamaged 또는 OnHealed 이벤트가 호출되면 실행될 함수
    private void HandleHealthChange(int amount, int newHP)
    {
        UpdateHeartObjects(); // 하트 갱신
    }

    // PlayerHealth에서 OnDied 이벤트가 호출되면 실행될 함수
    private void HandleDeath()
    {
        UpdateHeartObjects(); // 하트 갱신 (모두 끄기)
    }

    /// <summary>
    /// '내' 체력을 기반으로 '내' 3D 하트 오브젝트를 갱신(켜고 끄기)하는 핵심 함수입니다.
    /// </summary>
    // (디버깅용) 인스펙터에서 우클릭 메뉴로 이 함수를 강제 실행할 수 있습니다.
    [ContextMenu("Update Heart Objects (Debug)")]
    public void UpdateHeartObjects()
    {
        if (_myHealth == null) return;

        // '내' 현재 체력 가져오기
        int currentHP = _myHealth.CurrentHP;

        // 모든 하트 GameObject를 순회(for 루프)하며 켜고 끄기 (SetActive)
        for (int i = 0; i < heartObjects.Count; i++)
        {
            if (heartObjects[i] == null) continue; // 슬롯이 비었으면(null) 건너뛰기

            // [핵심 로직]
            // i (인덱스, 0부터 시작)가 현재 체력(currentHP)보다 '작으면' 켠다 (SetActive(true))
            //
            // 예: heartObjects.Count = 3 (최대체력 3), currentHP = 2 일 때
            // i=0: (0 < 2) -> true  (첫 번째 하트 켜기)
            // i=1: (1 < 2) -> true  (두 번째 하트 켜기)
            // i=2: (2 < 2) -> false (세 번째 하트 끄기)
            //
            // 예: currentHP = 0 일 때
            // i=0: (0 < 0) -> false (모두 끄기)
            heartObjects[i].SetActive(i < currentHP);
        }
    }
}