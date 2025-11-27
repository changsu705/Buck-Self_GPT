using UnityEngine;
using System.Collections;

/// <summary>
/// [캐릭터 표정] 시작 시 얼굴을 기억했다가, 
/// 1. 맞으면 잠시 'Hit' 표정
/// 2. 죽으면 'Dead' 표정으로 바꿉니다.
/// (인스펙터에는 Hit와 Dead 두 개만 넣으면 됩니다.)
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerVisuals : MonoBehaviour
{
    [Header("1. 변경될 얼굴 설정")]
    [Tooltip("총에 맞았을 때(피격) 잠깐 나올 얼굴")]
    public Material hitMaterial;

    [Tooltip("죽었을 때 고정될 얼굴")]
    public Material deadMaterial;

    [Header("2. 설정")]
    [Tooltip("맞았을 때 표정을 유지할 시간(초)")]
    public float hitDuration = 0.5f;

    [Tooltip("적용할 렌더러 (비워두면 자동 찾기)")]
    public Renderer targetRenderer;

    // --- 내부 변수 ---
    private PlayerHealth _health;
    private Material _originalMaterial; // 게임 시작할 때의 원래 얼굴 저장용
    private Coroutine _hitCoroutine;

    void Awake()
    {
        _health = GetComponent<PlayerHealth>();

        // 렌더러 자동 찾기
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<MeshRenderer>();

        if (targetRenderer == null)
        {
            Debug.LogError($"[PlayerVisuals] {name}: Renderer를 찾을 수 없습니다!");
            enabled = false;
            return;
        }

        // ⚠️ 시작할 때 입고 있는 머티리얼을 '평소 얼굴'로 저장
        _originalMaterial = targetRenderer.material;
    }

    void OnEnable()
    {
        _health.OnDamaged += HandleDamage; // 맞았을 때
        _health.OnDied += HandleDeath;     // 죽었을 때
        _health.OnHealed += HandleHeal;    // 치료받았을 때 (원상복구)
    }

    void OnDisable()
    {
        _health.OnDamaged -= HandleDamage;
        _health.OnDied -= HandleDeath;
        _health.OnHealed -= HandleHeal;
    }

    // ────────────────────────────── 이벤트 핸들러 ──────────────────────────────

    /// <summary>
    /// 총을 맞았을 때 (잠깐 Hit 얼굴 -> 원래 얼굴)
    /// </summary>
    private void HandleDamage(int damage, int currentHP)
    {
        // 이미 죽었거나 데미지가 없으면 무시
        if (_health.IsDead || damage <= 0) return;

        // 실행 중인 표정 복구 코루틴이 있다면 취소하고 다시 시작
        if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
        _hitCoroutine = StartCoroutine(CoShowHitFace());
    }

    /// <summary>
    /// 죽었을 때 (Dead 얼굴 고정)
    /// </summary>
    private void HandleDeath()
    {
        StopAllCoroutines(); // 진행 중인 피격 연출 중단
        if (deadMaterial != null)
        {
            targetRenderer.material = deadMaterial;
        }
    }

    /// <summary>
    /// 치료받았을 때 (원래 얼굴로 복귀)
    /// </summary>
    private void HandleHeal(int amount, int currentHP)
    {
        if (!_health.IsDead)
        {
            if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
            targetRenderer.material = _originalMaterial;
        }
    }

    // ────────────────────────────── 코루틴 ──────────────────────────────

    private IEnumerator CoShowHitFace()
    {
        // 1. 아픈 표정으로 변경
        if (hitMaterial != null)
        {
            targetRenderer.material = hitMaterial;
        }

        // 2. 지정된 시간만큼 대기
        yield return new WaitForSeconds(hitDuration);

        // 3. 아직 살아있다면 원래 얼굴로 복귀
        if (!_health.IsDead)
        {
            targetRenderer.material = _originalMaterial;
        }
    }
}