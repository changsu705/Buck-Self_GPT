using UnityEngine;
using System; // Action 콜백 사용을 위해 추가
using System.Collections; // 코루틴 사용을 위해 추가

/// <summary>
/// 리볼버 실린더를 "꺼내서 보여주고(OPEN) → 잠시 유지(HOLD) → 다시 넣는(CLOSE)"
/// 간단한 3단계 프리뷰 연출을 담당하는 스크립트입니다.
/// (예: R키를 누르거나, 총을 픽업했을 때(autoPeekOnPossess) 실행됨)
/// </summary>
public class RevolverCylinderPeek : MonoBehaviour
{
    // ────────────────────────────── 필수 참조 ──────────────────────────────
    [Header("필수: 실린더 피벗")]
    [Tooltip("애니메이션을 적용할 실린더(또는 크레인)의 Transform 피벗")]
    public Transform cylinderPivot;

    // ─────────────────────────── 프리뷰(OPEN/HOLD/CLOSE) ───────────────────────────
    [Header("연출 파라미터 (로컬 기준)")]
    [Tooltip("OPEN 시 '얼마나/어느 방향으로' 빼낼지 (로컬 위치 오프셋)")]
    public Vector3 outLocalPositionOffset = new Vector3(0.045f, 0.0f, -0.010f);
    [Tooltip("OPEN 시 '얼마나/어느 축으로' 회전할지 (로컬 회전 오프셋)")]
    public Vector3 outLocalEulerOffset = new Vector3(0f, 68f, 0f);
    [Tooltip("OPEN(열기)에 걸리는 시간(초)")]
    public float openSeconds = 0.14f;
    [Tooltip("열어둔 상태로 유지하는 시간(초)")]
    public float holdSeconds = 0.28f;
    [Tooltip("CLOSE(닫기)에 걸리는 시간(초)")]
    public float closeSeconds = 0.16f;

    // ────────────────────────────── 스핀(회전) ──────────────────────────────
    [Header("스핀(실린더 자체 회전) 옵션")]
    [Tooltip("실린더가 회전할 로컬 축 (보통 Z축)")]
    public Axis3 spinAxis = Axis3.Z;
    [Tooltip("OPEN(열 때) 스핀을 적용할지")]
    public bool spinOnOpen = true;
    [Tooltip("OPEN 시 스핀 각도")]
    public float spinDegOpen = 30f;
    [Tooltip("CLOSE(닫을 때) 스핀을 적용할지")]
    public bool spinOnClose = true;
    [Tooltip("CLOSE 시 스핀 각도")]
    public float spinDegClose = 60f;
    [Tooltip("CLOSE 후 최종적으로 원래 각도로 정확히 복귀할지")]
    public bool snapToOriginAfterClose = true;

    // ────────────────────────────── 입력 ──────────────────────────────
    [Header("입력")]
    [Tooltip("프리뷰를 테스트 실행할 키")]
    public KeyCode peekKey = KeyCode.R;
    [Tooltip("애니메이션 진행 중 추가 입력을 무시할지")]
    public bool ignoreWhileAnimating = true;

    // ────────────────────────────── 장탄 표시 ──────────────────────────────
    [Header("장탄 표시 (선택 사항)")]
    [Tooltip("실린더 내부 총알 슬롯 오브젝트들을 순서대로 할당")]
    public Transform[] bulletSlots;
    [Tooltip("보여줄 총알 개수")]
    [Range(0, 12)] public int loadedCount = 6;
    [Tooltip("프리뷰 시작/종료 시 총알 가시성을 자동 갱신할지")]
    public bool autoApplyBulletVisibility = true;

    // ────────────────────────────── 내부 상태 ──────────────────────────────
    private Vector3 _originLocalPos;  // 실린더 피벗의 원래 로컬 위치 (복귀 기준)
    private Quaternion _originLocalRot; // 실린더 피벗의 원래 로컬 회전 (복귀 기준)
    private bool _animating;             // 현재 애니메이션 진행 중인지

    /// <summary>스핀 축 선택 (X/Y/Z).</summary>
    public enum Axis3 { X, Y, Z }

    /// <summary>외부에서 애니메이션 진행 여부 확인용 (읽기 전용)</summary>
    public bool IsAnimating => _animating;

    // ────────────────────────────── 라이프사이클 ──────────────────────────────
    void Awake()
    {
        if (!cylinderPivot)
        {
            Debug.LogError("[RevolverCylinderPeek] cylinderPivot이 비어있습니다.", this);
            enabled = false;
            return;
        }

        // 애니메이션의 기준이 될 실린더의 '원래' 로컬 위치/회전 값을 저장합니다.
        _originLocalPos = cylinderPivot.localPosition;
        _originLocalRot = cylinderPivot.localRotation;

        if (autoApplyBulletVisibility) ApplyBulletVisibility(); // 시작 시 총알 가시성 적용
    }

    void Update()
    {
        // R키(peekKey) 입력 감지
        if (Input.GetKeyDown(peekKey) && (!ignoreWhileAnimating || !_animating))
        {
            PlayOnce(); // 애니메이션 1회 재생
        }
    }

    /// <summary>
    /// 외부에서 실린더 프리뷰를 1회 재생하도록 호출하는 Public 함수입니다.
    /// (RevolverTurnPossession이 총을 픽업할 때 이 함수를 호출합니다.)
    /// </summary>
    /// <param name="onComplete">프리뷰 시퀀스(OPEN-HOLD-CLOSE)가 모두 완료되었을 때 실행될 콜백</param>
    public void PlayOnce(Action onComplete = null)
    {
        if (ignoreWhileAnimating && _animating) return; // 애니메이션 중이면 무시
        StopAllCoroutines(); // 기존에 실행 중인 코루틴이 있다면 중지
        StartCoroutine(CoPeek(onComplete)); // 새 시퀀스 시작
    }

    // ────────────────────────────── 메인 시퀀스 ──────────────────────────────

    /// <summary>
    /// OPEN → HOLD → CLOSE 전체 프리뷰 시퀀스를 담당하는 코루틴입니다.
    /// </summary>
    private System.Collections.IEnumerator CoPeek(Action onComplete = null)
    {
        _animating = true; // 애니메이션 시작 상태로 변경

        if (autoApplyBulletVisibility) ApplyBulletVisibility(); // 총알 가시성 갱신

        // ===== 1. OPEN (꺼내기) =====
        Vector3 fromPos = _originLocalPos;
        Quaternion fromRot = _originLocalRot;

        Vector3 toPos = _originLocalPos + outLocalPositionOffset; // 목표 위치 (원래 위치 + 오프셋)
        Quaternion toRotBase = _originLocalRot * Quaternion.Euler(outLocalEulerOffset); // 목표 회전(스윙)

        // OPEN 스핀 적용 (스윙 회전 * 스핀 회전)
        Quaternion toRotOpen = spinOnOpen
            ? toRotBase * Quaternion.AngleAxis(spinDegOpen, AxisVec(spinAxis))
            : toRotBase;

        // openSeconds 동안 부드럽게 이동/회전
        yield return LerpLocalPose(cylinderPivot, fromPos, toPos, fromRot, toRotOpen, openSeconds);

        // ===== 2. HOLD (유지) =====
        if (holdSeconds > 0f)
        {
            yield return new WaitForSeconds(holdSeconds); // 설정된 시간만큼 대기
        }

        // ===== 3. CLOSE (넣기) =====
        Vector3 backPos = _originLocalPos; // 복귀할 위치 = 원래 위치
        Quaternion closeFromRot = cylinderPivot.localRotation; // 현재 열린 상태의 회전값

        // 복귀할 회전(closeTargetRot) 계산
        Quaternion closeTargetRot;
        if (spinOnClose) // 닫을 때 스핀을 사용한다면
        {
            closeTargetRot = snapToOriginAfterClose
                ? _originLocalRot // (스핀 연출은 중간에만 들어가고) 최종적으론 원본 각도로 복귀
                : _originLocalRot * Quaternion.AngleAxis(spinDegClose, AxisVec(spinAxis)); // 스핀이 적용된 각도로 복귀
        }
        else // 닫을 때 스핀을 사용하지 않는다면
        {
            closeTargetRot = _originLocalRot; // 원본 각도로 복귀
        }

        // (참고: LerpLocalPose가 스핀 연출을 자동으로 처리해 줌)

        // closeSeconds 동안 부드럽게 이동/회전 (복귀)
        yield return LerpLocalPose(cylinderPivot,
                                   cylinderPivot.localPosition, backPos,
                                   closeFromRot, closeTargetRot,
                                   closeSeconds);

        // 오차 보정을 위해 최종 위치/회전 고정
        cylinderPivot.localPosition = _originLocalPos;
        cylinderPivot.localRotation = closeTargetRot; // 계산된 최종 목표 회전값으로 스냅

        _animating = false; // 애니메이션 종료 상태로 변경
        onComplete?.Invoke(); // 완료 콜백이 있다면 호출 (예: RevolverTurnPossession에게 "끝났음"을 알림)
    }

    // ────────────────────────────── 유틸리티 함수 ──────────────────────────────

    /// <summary>
    /// 지정된 Transform의 로컬 위치와 로컬 회전을 SmoothStep 이징을 사용하여 부드럽게 보간하는 코루틴입니다.
    /// </summary>
    private System.Collections.IEnumerator LerpLocalPose(Transform tr,
                                                         Vector3 fromPos, Vector3 toPos,
                                                         Quaternion fromRot, Quaternion toRot,
                                                         float seconds)
    {
        float d = Mathf.Max(0.01f, seconds); // 0으로 나누기 방지
        float t = 0f; // 진행도 (0.0 ~ 1.0)
        while (t < 1f)
        {
            t += Time.deltaTime / d;
            // SmoothStep 보간: t * t * (3f - 2f * t)
            // (천천히 시작해서 빨라졌다가 천천히 멈춤)
            float e = t * t * (3f - 2f * t);

            tr.localPosition = Vector3.LerpUnclamped(fromPos, toPos, e);
            tr.localRotation = Quaternion.SlerpUnclamped(fromRot, toRot, e);
            yield return null; // 1프레임 대기
        }
        // 완료 후 최종 위치/회전으로 스냅
        tr.localPosition = toPos;
        tr.localRotation = toRot;
    }

    /// <summary>
    /// 선택된 축(Axis3)에 해당하는 Vector3 방향(로컬 축)을 반환합니다.
    /// </summary>
    private Vector3 AxisVec(Axis3 a)
    {
        switch (a)
        {
            case Axis3.X: return Vector3.right;   // (1, 0, 0)
            case Axis3.Y: return Vector3.up;      // (0, 1, 0)
            default: return Vector3.forward; // (0, 0, 1) (Z축)
        }
    }

    /// <summary>
    /// 'loadedCount' 변수를 기준으로 총알 슬롯 오브젝트의 활성화/비활성화를 갱신합니다.
    /// </summary>
    [ContextMenu("Apply Bullet Visibility (Debug)")] // (디버깅용) 인스펙터 우클릭 메뉴
    public void ApplyBulletVisibility()
    {
        if (bulletSlots == null || bulletSlots.Length == 0) return;

        for (int i = 0; i < bulletSlots.Length; i++)
        {
            if (bulletSlots[i] == null) continue; // 배열 슬롯이 비어있으면 건너뛰기

            // i (0, 1, 2...)가 loadedCount(예: 3)보다 작으면 켠다 (true)
            bulletSlots[i].gameObject.SetActive(i < loadedCount);
        }
    }
}