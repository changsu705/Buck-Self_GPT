using UnityEngine;

/// <summary>
/// 카메라 중앙(십자선)에서 Raycast를 발사하여 상호작용 가능한 대상(InteractableHighlighter)을 감지합니다.
/// - 감지한 대상의 하이라이트 효과를 켜고 끕니다.
/// - 현재 쳐다보고 있는 대상(CurrentTarget)을 외부에 공개하여,
///   RevolverTurnPossession이 '무엇을 클릭했는지' 알 수 있게 합니다.
/// </summary>
public class CrosshairRaycaster : MonoBehaviour
{
    [Header("레이캐스트 설정")]
    [Tooltip("하이라이트를 감지할 최대 거리")]
    public float raycastRange = 5f;

    [Tooltip("하이라이트 효과를 줄 오브젝트들이 포함된 레이어 마스크. (예: Interactable, GunPickup 등)")]
    public LayerMask interactableMask;

    // --- 내부 상태 ---
    private Transform _cameraTransform; // 이 스크립트가 붙은 카메라의 트랜스폼 (캐시)

    /// <summary>
    /// [읽기 전용] 현재 십자선이 쳐다보고 있는 하이라이트 가능한 대상입니다.
    /// (RevolverTurnPossession이 이 값을 읽어서 룩북/카드를 클릭했는지 확인합니다.)
    /// </summary>
    public InteractableHighlighter CurrentTarget { get; private set; } = null;

    void Awake()
    {
        // 이 스크립트는 카메라에 부착되어야 하므로, 자신의 트랜스폼을 캐시합니다.
        _cameraTransform = this.transform;
    }

    void Update()
    {
        // Scene 뷰에서 디버그용 빨간색 레이저를 시각화 (게임 빌드에서는 보이지 않음)
        Debug.DrawRay(_cameraTransform.position, _cameraTransform.forward * raycastRange, Color.red);

        RaycastHit hit; // 레이캐스트가 맞은 대상의 정보를 담을 변수

        // Physics.Raycast: 카메라 위치/정면으로, raycastRange 거리만큼, interactableMask 레이어에만 발사
        if (Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out hit, raycastRange, interactableMask))
        {
            // --- 1. Raycast가 무언가에 맞았을 때 ---

            // 맞은 대상(hit.collider)에서 InteractableHighlighter 컴포넌트를 가져옵니다.
            InteractableHighlighter highlighter = hit.collider.GetComponent<InteractableHighlighter>();

            if (highlighter != null && highlighter != CurrentTarget)
            {
                // --- 1-1. 새로운 하이라이트 대상을 찾았을 때 ---

                // 이전에 쳐다보던 대상(CurrentTarget)이 있었다면, 그 대상의 하이라이트를 먼저 끕니다.
                if (CurrentTarget != null)
                {
                    CurrentTarget.SetHighlight(false); // 기존 타겟 하이라이트 해제
                }

                highlighter.SetHighlight(true); // 새 타겟 하이라이트 적용
                CurrentTarget = highlighter;    // 현재 쳐다보는 대상을 이 타겟으로 갱신
            }
            else if (highlighter == null)
            {
                // --- 1-2. 레이어는 맞았지만, 하이라이터 스크립트가 없는 대상을 맞췄을 때 ---
                ClearLastHighlight(); // 기존에 켜져 있던 하이라이트를 끕니다.
            }
            // (else: highlighter == CurrentTarget) -> 이미 쳐다보고 있으므로 아무것도 안 함
        }
        else
        {
            // --- 2. Raycast가 허공을 쐈을 때 (아무것도 맞추지 못했을 때) ---
            ClearLastHighlight();
        }
    }

    /// <summary>
    /// 현재 타겟의 하이라이트를 해제하고, CurrentTarget 참조를 null로 지웁니다.
    /// </summary>
    private void ClearLastHighlight()
    {
        // 이전에 쳐다보던 대상이 있을 때만 실행
        if (CurrentTarget != null)
        {
            CurrentTarget.SetHighlight(false); // 하이라이트 끄기
            CurrentTarget = null;              // 참조 제거
        }
    }
}