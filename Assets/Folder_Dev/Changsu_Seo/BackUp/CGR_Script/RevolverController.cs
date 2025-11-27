using UnityEngine;
using System;
using System.Collections; // IEnumerator (코루틴) 사용을 위해 추가

/// <summary>
/// [총기 조작] 리볼버의 발사 입력을 처리하고, 유효 타겟 판별 및 발사 로직을 수행합니다.
/// - '총을 든 상태'일 때(RevolverTurnPossession이 활성화)만 작동합니다.
/// - 유효한 타겟(hitMask)을 조준하고 발사 키(fireKey)를 누르면 'OnAimRequest' 이벤트를 발생시킵니다.
/// - ⚠️ 수정: ExecuteShot이 코루틴으로 변경되어, 반동(RecoilKick)이 끝날 때까지 기다릴 수 있습니다.
/// </summary>
public class RevolverController : MonoBehaviour
{
    [Header("입력")]
    [Tooltip("스크립트 활성화 시 입력 자동 활성화 여부 (보통 false로 두고 TurnPossession이 제어)")]
    public bool enableInputOnEnable = false;
    [Tooltip("발사 키 (마우스 왼클릭)")]
    public KeyCode fireKey = KeyCode.Mouse0;

    [Header("사격")]
    [Tooltip("총구 위치 (총알 Raycast 시작점)")]
    public Transform muzzle;
    [Tooltip("총알 Raycast 최대 사정거리")]
    public float shotRange = 100f;
    [Tooltip("총에 맞거나 조준할 수 있는 '유효' 오브젝트의 레이어 마스크 (예: Player)")]
    public LayerMask hitMask;

    [Header("리코일(시각 테스트)")]
    [Tooltip("리코일 시 총구가 위로 튕기는 각도")]
    public float recoilDegrees = 7f;
    [Tooltip("리코일 후 원래 각도로 돌아오는 속도")]
    public float recoilReturnSpeed = 10f;

    // ────────────────────────────── 이벤트 ──────────────────────────────

    /// <summary>
    /// 발사 등 총기 액션이 완료되었을 때 발생 (현재는 사용되지 않음. RevolverTurnPossession이 직접 턴 넘김)
    /// </summary>
    public event Action OnActionComplete;

    /// <summary>
    /// 유효 타겟 조준 후 발사 키를 눌렀을 때 발생 (조준 연출 트리거)
    /// RevolverTurnPossession이 이 이벤트를 구독하여 CoAimAndFire를 실행합니다.
    /// </summary>
    public event Action<Transform> OnAimRequest;

    // ────────────────────────────── 내부 상태 ──────────────────────────────
    private bool _inputEnabled = false; // 현재 총기 입력이 활성화되었는지 (RevolverTurnPossession이 제어)

    void OnEnable() { _inputEnabled = enableInputOnEnable; }
    void OnDisable() { _inputEnabled = false; }

    void Update()
    {
        // 입력이 비활성화 상태면(예: 총을 안 들었을 때, 연출 중일 때) 아무것도 하지 않음
        if (!_inputEnabled) return;

        // 발사 키(마우스 왼클릭)를 눌렀는지 확인
        if (Input.GetKeyDown(fireKey))
        {
            Camera cam = Camera.main; // 메인 카메라 가져오기

            // 1. 카메라 중앙에서 Raycast 발사 및 유효 타겟(hitMask) 명중 확인
            if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, shotRange, hitMask))
            {
                // 2. 유효한 타겟을 맞췄으므로, OnAimRequest 이벤트를 발생시킵니다.
                //    (맞춘 대상의 Transform을 이벤트 인자로 넘겨줍니다.)
                OnAimRequest?.Invoke(hit.transform);
            }
            else
            {
                // 3. 유효 타겟(hitMask)을 못 맞추거나, 허공을 쐈을 경우
                // ⚠️ 발사를 허용하지 않고 입력을 무시합니다. (비조준 발사 방지)
                Debug.Log("🎯 유효한 타겟을 조준해야 합니다. 발사 취소.");
                return;
            }
        }
    }

    /// <summary>
    /// RevolverTurnPossession이 이 함수를 호출하여 총기 입력을 활성화/비활성화합니다.
    /// </summary>
    public void SetInputEnabled(bool enabled) => _inputEnabled = enabled;

    /// <summary>
    /// 총 발사 로직(리코일)을 실행하는 '코루틴'입니다.
    /// RevolverTurnPossession이 이 코루틴이 끝날 때까지 'yield return'으로 기다립니다.
    /// </summary>
    public IEnumerator ExecuteShot() // 👈 void에서 IEnumerator로 변경됨
    {
        // 발사 로그 (디버그용. 실제 데미지 처리는 RevolverTurnPossession이 담당)
        Transform m = muzzle ? muzzle : transform;
        if (Physics.Raycast(m.position, m.forward, out RaycastHit hit, shotRange))
        {
            if (((1 << hit.collider.gameObject.layer) & hitMask.value) != 0)
            {
                Debug.Log($"🔫 Bang! Hit: {hit.collider.name} (유효 타겟)");
            }
            else
            {
                Debug.Log($"🔫 Bang! (Hit something else: {hit.collider.name})");
            }
        }
        else
        {
            Debug.Log("🔫 Bang! (no hit)");
        }

        // ⚠️ [수정됨] RecoilKick(반동) 코루틴을 실행하고, 
        // 이 코루틴(RecoilKick)이 끝날 때까지 여기서 기다립니다(yield return).
        yield return StartCoroutine(RecoilKick());
    }

    /// <summary>
    /// 총의 리코일(반동)을 부드럽게 연출하는(튕겼다가 돌아오는) 코루틴입니다.
    /// </summary>
    private System.Collections.IEnumerator RecoilKick()
    {
        Quaternion start = transform.localRotation; // 현재(원래) 로컬 회전값
        Quaternion up = start * Quaternion.Euler(-recoilDegrees, 0f, 0f); // 위로 튕기는 목표 각도 계산

        // 1. 튕김 (start -> up)
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * recoilReturnSpeed;
            transform.localRotation = Quaternion.Slerp(start, up, t); // 부드럽게 튕김
            yield return null; // 1프레임 대기
        }

        // 2. 복귀 (up -> start)
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * recoilReturnSpeed;
            transform.localRotation = Quaternion.Slerp(up, start, t); // 부드럽게 복귀
            yield return null; // 1프레임 대기
        }

        transform.localRotation = start; // 오차 보정을 위해 최종적으로 원래 위치로 스냅
    }
}