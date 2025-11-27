using UnityEngine;
using System.Collections;
using Cinemachine;
using Buckshot.App;
using Photon.Pun;

/// <summary>
/// [핵심 관리자] 턴 시스템과 리볼버를 연결하고 플레이어의 총기 상호작용 및 턴 진행을 관리합니다.
/// - 턴이 바뀌면 총을 테이블의 플레이어 앞으로 이동시킵니다.
/// - 플레이어는 총/카드/룩북을 클릭(상호작용)할 수 있습니다.
/// - 발사 시 FPS 시점에서 조준 카메라(vcamAim)로 전환하고, 카메라가 튀는 현상을 방지합니다.
/// - 룩북 패널이 열렸을 때 ESC 키로 닫습니다.
/// </summary>
[RequireComponent(typeof(RevolverController))] // 이 스크립트는 RevolverController가 반드시 필요함
public class RevolverTurnPossession : MonoBehaviour
{
    // ────────────────────────────── 필수 참조 ──────────────────────────────
    [Header("필수 참조")]
    public TurnManager turnManager; // 턴 관리자
    public Transform tableCenter;    // 테이블 중심 (총기 배치 기준)

    [Header("총기 배치/이동 (테이블 위)")]
    [Tooltip("테이블 중심에서 플레이어 방향으로 얼마나 떨어진 위치에 총을 배치할지 비율")]
    [Range(0f, 1f)] public float placeRatio = 0.7f;
    [Tooltip("총기 배치 시 좌우 오프셋")]
    public float placeHorizontalOffset = 0f;
    [Tooltip("총기 배치 시 높이 오프셋")]
    public float heightOffset = 0.8f;
    [Tooltip("총이 테이블 위로 이동할 때 걸리는 시간")]
    public float moveDuration = 0.45f;
    [Tooltip("총이 테이블 위에 있을 때 플레이어를 바라보게 할지")]
    public bool facePlayer = true;
    [Tooltip("총이 테이블 위에 있을 때 추가 회전 오프셋")]
    public Vector3 extraEulerOffset;
    [Tooltip("플레이어의 머리(카메라) 자식 오브젝트 이름")]
    public string headChildName = "Head";
    [Tooltip("게임 시작 시 첫 배치에 이동 애니메이션을 생략할지")]
    public bool snapFirstTime = true;

    [Header("시네머신 연동")]
    [Tooltip("플레이어의 기본 시점(FPS)을 담당하는 가상 카메라")]
    public CinemachineVirtualCamera vcam;
    [Tooltip("턴이 바뀔 때 vcam의 Follow/LookAt을 현재 플레이어로 자동 설정할지")]
    public bool updateCameraOnPossess = true;

    [Header("조준 연출 옵션")]
    [Tooltip("적을 조준할 때(줌인) 사용할 가상 카메라")]
    public CinemachineVirtualCamera vcamAim;
    [Tooltip("조준 카메라로 전환 후, 발사 전까지 대기하는 시간")]
    public float aimHoldSeconds = 1.0f;

    [Header("턴/입력 옵션")]
    [Tooltip("발사(액션) 완료 시 자동으로 다음 턴으로 넘길지")]
    public bool autoNextTurnOnActionComplete = true;
    [Tooltip("액션 완료 후 다음 턴으로 넘어가기 전 딜레이")]
    public float nextTurnDelay = 0.35f;
    [Tooltip("조준 취소 또는 총 내려놓기 키")]
    public KeyCode cancelAimKey = KeyCode.Mouse1; // 마우스 우클릭

    [Header("자신에게 사격(Self-Shot)")]
    [Tooltip("자신에게 쏘기 키")]
    public KeyCode selfShotKey = KeyCode.Q;
    [Tooltip("자신에게 쏠 때의 데미지")]
    public int selfShotDamage = 1;
    [Tooltip("총이 자신을 향해 회전하는 시간")]
    public float selfShotRotationDuration = 0.3f;

    [Header("임시: 테스트용 탄약 확률")]
    [Range(0f, 1f)]
    public float liveBulletChance = 0.5f; // 실탄일 확률 (테스트용)

    [Header("시야 연동(카메라 부착) 옵션")]
    [Tooltip("총을 픽업했을 때 카메라에 부착할지")]
    public bool attachToCameraOnPossess = true;
    [Tooltip("총을 부착할 카메라(또는 마운트) Transform. 비워두면 Main Camera 사용")]
    public Transform cameraMount;
    [Tooltip("카메라(마운트) 기준 총의 로컬 위치 오프셋")]
    public Vector3 mountLocalPosition = new(0.25f, -0.25f, 0.5f);
    [Tooltip("카메라(마운트) 기준 총의 로컬 회전 오프셋")]
    public Vector3 mountLocalEuler = new(0f, 0f, 0f);
    [Tooltip("총을 카메라에 부착할 때 걸리는 시간")]
    public float attachBlendSeconds = 0.12f;

    [Header("자동 실린더 확인(옵션)")]
    [Tooltip("총을 픽업할 때 자동으로 실린더 확인 애니메이션(Peek)을 재생할지")]
    public bool autoPeekOnPossess = true;
    [Tooltip("실린더 확인 애니메이션 스크립트 (없으면 자동 검색)")]
    public RevolverCylinderPeek peek;

    [Header("픽업(선택) 설정")]
    [Tooltip("테이블 위 총을 클릭해야 픽업되도록 할지")]
    public bool requirePickupOnClick = true;
    [Tooltip("총을 '집을' 수 있는 레이어 마스크 (★'GunPickup' 레이어만 선택해야 함)")]
    public LayerMask pickupMask;
    [Tooltip("총을 픽업할 수 있는 최대 거리")]
    public float pickupRange = 3f;

    [Header("카드 설명 UI")]
    [Tooltip("카드 드로우 연출을 관리하는 매니저 (입력 차단용, 없으면 자동 검색)")]
    public SkillCardManager skillCardManager;
    [Tooltip("카드 설명 UI (UI가 열리면 입력 무시)")]
    public CardDisplayUI cardDisplayUI;
    [Tooltip("조준점 레이캐스터 (★'Interactable'과 'GunPickup'을 모두 감지해야 함)")]
    public CrosshairRaycaster crosshairRaycaster;
    [Tooltip("룩북 UI GameObject (UI가 열리면 입력 무시)")]
    public GameObject lookbookPanel;

    // ────────────────────────────── 내부 상태 및 컴포넌트 ──────────────────────────────
    private RevolverController _controller; // 총기 발사/입력 스크립트 (캐시)
    private Transform _lastOwner;           // 현재 턴 플레이어 (캐시)
    private bool _initialized;              // 첫 배치 완료 여부
    private bool _attachedToCam;            // 총이 카메라에 붙어있는지
    private bool _isAimingSequence;         // 현재 조준 연출(CoAimAndFire) 중인지
    private bool _isGunPickedUp;            // 현재 총을 픽업한 상태인지
    private bool _isSelfShooting;           // 현재 자신에게 쏘는 연출 중인지
    private Coroutine _aimSequenceCoroutine; // 현재 실행 중인 조준 코루틴
    private InteractableHighlighter _highlighter; // 총기 하이라이트 스크립트 (캐시)

    // ────────────────────────────── Photonnetwork 커스텀 프로퍼티 ──────────────────────────────
    [SerializeField] PhotonGameCoordinator coordinator;

    void TryShootOpponent() => coordinator.TryShootOpponent();
    void TryShootSelf() => coordinator.TryShootSelf();

    /// <summary>
    /// 카드 UI 또는 룩북 UI가 열려있는지 확인합니다.
    /// (true이면 Update에서 입력을 차단합니다)
    /// </summary>
    private bool IsUIOpen
    {
        get
        {
            bool cardOpen = cardDisplayUI != null && cardDisplayUI.IsOpen;
            bool lookbookOpen = lookbookPanel != null && lookbookPanel.activeSelf;
            return cardOpen || lookbookOpen; // 둘 중 하나라도 열려있으면 true
        }
    }

    // ────────────────────────────── 라이프사이클 ──────────────────────────────

    void Awake()
    {
        // 필수 컴포넌트 확인 및 캐시
        if (!turnManager) { Debug.LogError("TurnManager가 비었습니다."); enabled = false; return; }
        if (!tableCenter) { Debug.LogError("tableCenter가 비었습니다."); enabled = false; return; }
        _controller = GetComponent<RevolverController>(); 
        if (!peek) peek = GetComponent<RevolverCylinderPeek>();
        _highlighter = GetComponent<InteractableHighlighter>();

        if (skillCardManager == null) skillCardManager = FindObjectOfType<SkillCardManager>();

        // RevolverController의 이벤트 구독
        _controller.OnActionComplete += HandleActionComplete;
        _controller.OnAimRequest += HandleAimRequest;
        _controller.SetInputEnabled(false); // 시작 시 입력 비활성화
    }

    void OnDestroy()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        if (_controller != null)
        {
            _controller.OnActionComplete -= HandleActionComplete;
            _controller.OnAimRequest -= HandleAimRequest;
        }
    }

    void Start()
    {
        TryUpdateOwner(force: true); // 턴 매니저의 현재 턴에 맞춰 총기 즉시 배치
        Cursor.lockState = CursorLockMode.Locked; // 마우스 커서 잠금
    }

    void Update()
    {
        if (!PhotonNetwork.InRoom) return;
        if (!IsMyTurn()) return;

        bool isCardDrawing = (skillCardManager != null && skillCardManager.IsDrawingCard);

        // [ESC 키 입력] 룩북 패널이 활성화되어 있을 때 ESC를 누르면 닫기
        // (IsUIOpen 차단 로직보다 *먼저* 실행되어야 함)
        if (lookbookPanel != null && lookbookPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            lookbookPanel.SetActive(false);
            return; // 입력을 처리했으므로 이번 프레임은 종료
        }

        // [입력 차단 1] UI가 열려있거나, 자신에게 쏘는 중이면 모든 입력 무시
        if (IsUIOpen || _isSelfShooting) return;

        // [입력 차단 2] 적 조준 연출 중일 때는 '조준 취소' 입력만 받음
        if (_isAimingSequence)
        {
            if (Input.GetKeyDown(cancelAimKey)) CancelAimingSequence();
            return;
        }

        TryUpdateOwner(force: false); // 턴이 바뀌었는지 확인하고 총 위치 업데이트

        if (requirePickupOnClick && _lastOwner != null)
        {
            // --- 로직 분리: 총을 들었을 때 / 안 들었을 때 ---

            if (_isGunPickedUp)
            {
                // --- B. 총을 든 상태 (FPS 시점) ---

                // [입력 차단 3] 총을 든 상태에서 카드가 드로우 중이면 모든 입력 차단
                if (isCardDrawing) return;

                // (입력 처리)
                if (Input.GetKeyDown(selfShotKey)) ShootSelf();
                if (Input.GetKeyDown(cancelAimKey)) PutDownGun();
            }
            else
            {
                // --- A/C. 총을 안 든 상태 (테이블 위) ---

                // [카드/룩북 확인 또는 픽업 입력]
                if (Input.GetKeyDown(_controller.fireKey)) // 마우스 좌클릭
                {
                    // 1. 십자선이 무엇을 가리키는지 확인
                    if (crosshairRaycaster != null && crosshairRaycaster.CurrentTarget != null)
                    {
                        // 1-1. 카드 확인
                        CardVisual card = crosshairRaycaster.CurrentTarget.GetComponent<CardVisual>();
                        if (card != null && cardDisplayUI != null)
                        {
                            cardDisplayUI.ShowPanel();
                            return; // 👈 [핵심] 카드 클릭 처리 완료. 총 픽업 로직 실행 방지
                        }

                        // 1-2. 룩북 확인
                        LookbookInteractable lookbook = crosshairRaycaster.CurrentTarget.GetComponent<LookbookInteractable>();
                        if (lookbook != null && lookbookPanel != null)
                        {
                            lookbookPanel.SetActive(true);
                            return; // 👈 [핵심] 룩북 클릭 처리 완료. 총 픽업 로직 실행 방지
                        }
                    }

                    // 2. 픽업 시도 (카드/룩북을 클릭한 게 아닐 때만 이 코드가 실행됨)

                    // [입력 차단 4] 카드 드로우 중에는 픽업(총기 입력)을 차단
                    if (isCardDrawing)
                    {
                        return;
                    }

                    // 카드 드로우 중이 아니므로 총기 픽업 시도
                    TryPickupGun();
                }
            }
        }
    }
    private bool IsMyTurn()
    {
        if (turnManager == null) return false;
        var currentActor = turnManager.GetCurrentActor();
        return PhotonNetwork.LocalPlayer.ActorNumber == currentActor;
    }


    // ────────────────────────────── 핵심 로직: 자신에게 사격 ──────────────────────────────

    /// <summary>
    /// 자신에게 총을 쏘는 시퀀스를 시작합니다. (UI 버튼 등에서 호출 가능하도록 Public)
    /// </summary>
    public void ShootSelf()
    {
        if (_lastOwner == null || _isSelfShooting) return;
        _controller.SetInputEnabled(false); // 입력 비활성화
        if (_aimSequenceCoroutine != null) StopCoroutine(_aimSequenceCoroutine); // 조준 중이었다면 취소
        _isAimingSequence = false;
        DetachFromCameraIfNeeded(); // 카메라에서 총 분리
        StartCoroutine(CoRotateToSelfAndShot(_lastOwner)); // 연출 시작
    }

    /// <summary>
    /// 총을 플레이어 자신에게 회전시키고 발사하는 연출 코루틴입니다.
    /// </summary>
    private IEnumerator CoRotateToSelfAndShot(Transform player)
    {
        _isSelfShooting = true;
        Debug.Log($"<color=red>[{player.name}]</color>이(가) 스스로에게 총을 쏘기 위해 준비합니다.");

        // 총을 180도 회전시키는 연출
        Quaternion startRot = transform.localRotation;
        Quaternion targetRot = startRot * Quaternion.Euler(180f, 0f, 0f);
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, selfShotRotationDuration);
            float e = t * t * (3f - 2f * t); // SmoothStep
            transform.localRotation = Quaternion.SlerpUnclamped(startRot, targetRot, e);
            yield return null;
        }
        transform.localRotation = targetRot;

        // 발사 및 데미지 판정 (테스트용)
        if (coordinator != null)
        {
            Debug.Log("[Client] 서버에 자가격발 요청 전송");
            coordinator.TryShootSelf();
        }


        // [수정] 발사(반동) 코루틴이 끝날 때까지 기다림
        yield return StartCoroutine(_controller.ExecuteShot());

        // 턴 넘김 처리
        HandleActionComplete();
        _isSelfShooting = false;
    }

    // ────────────────────────────── 총기 이동 및 상태 관리 ──────────────────────────────

    /// <summary>
    /// 턴 매니저를 확인하여 턴 주인이 바뀌었으면 총을 새 주인 앞으로 이동시킵니다.
    /// </summary>
    private void TryUpdateOwner(bool force)
    {
        Transform cur = turnManager.GetCurrentPlayer();
        if (!force && cur == _lastOwner) return; // 턴 변경 없음

        // 턴이 바뀌는 순간, 혹시 조준 중이었다면 강제 취소
        if (_isAimingSequence) CancelAimingSequence();

        DetachFromCameraIfNeeded(); // 이전 주인의 카메라에서 총 분리
        _lastOwner = cur;
        _isGunPickedUp = false;
        if (_highlighter != null) _highlighter.SetSelected(false); // 픽업 해제 상태로
        if (cur == null) return; // 턴 주인이 없으면 종료

        _controller.SetInputEnabled(false); // 입력 비활성화
        StopAllCoroutines(); // 진행 중인 모든 이동/연출 중지 (★반동이 여기서 끊겼었음)
        StartCoroutine(CoMoveToPlayer(cur)); // 새 주인 앞으로 총 이동 시작
    }

    /// <summary>
    /// 총을 테이블 위, 지정된 플레이어 앞으로 이동시키는 코루틴입니다.
    /// </summary>
    private System.Collections.IEnumerator CoMoveToPlayer(Transform player)
    {
        // 1. 목표 위치 계산
        Transform head = player.Find(headChildName);
        Vector3 playerPos = head ? head.position : player.position;
        Vector3 center = tableCenter.position;
        Vector3 dir = playerPos - center; dir.y = 0f;
        float dist = dir.magnitude;
        Vector3 dirNorm = dist > 1e-4f ? dir / dist : Vector3.forward;
        Vector3 rightDir = new(dirNorm.z, 0, -dirNorm.x);
        Vector3 flatTarget = center
            + (dirNorm * (dist * Mathf.Clamp01(placeRatio)))
            + (rightDir * placeHorizontalOffset);
        Vector3 targetPos = new(flatTarget.x, center.y + heightOffset, flatTarget.z);

        // 2. 목표 회전 계산
        Vector3 lookDir = (facePlayer ? playerPos : center) - targetPos; lookDir.y = 0f;
        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up) * Quaternion.Euler(extraEulerOffset);

        // 3. 이동 연출 (SmoothStep)
        if (!_initialized && snapFirstTime)
        {
            transform.SetPositionAndRotation(targetPos, targetRot);
            _initialized = true;
        }
        else
        {
            float t = 0f;
            Vector3 fromPos = transform.position;
            Quaternion fromRot = transform.rotation;
            while (t < 1f)
            {
                t += Time.deltaTime / moveDuration;
                float e = t * t * (3f - 2f * t); // SmoothStep
                transform.position = Vector3.LerpUnclamped(fromPos, targetPos, e);
                transform.rotation = Quaternion.SlerpUnclamped(fromRot, targetRot, e);
                yield return null;
            }
        }

        // 4. 메인 카메라(vcam) 타겟 설정
        if (vcam && updateCameraOnPossess)
        {
            var target = head ? head : player;
            vcam.Follow = target;
            vcam.LookAt = target;
        }

        // 5. 자동 픽업 모드 처리
        if (!requirePickupOnClick)
        {
            StartCoroutine(CoPickupSequence());
        }
    }

    /// <summary>
    /// 총을 다시 테이블 위로 내려놓습니다. (CoMoveToPlayer 호출)
    /// </summary>
    private void PutDownGun()
    {
        _controller.SetInputEnabled(false);
        DetachFromCameraIfNeeded();
        _isGunPickedUp = false;
        if (_highlighter != null) _highlighter.SetSelected(false);
        StopAllCoroutines();
        StartCoroutine(CoMoveToPlayer(_lastOwner)); // 현재 턴 주인 앞으로 다시 이동
    }

    // ────────────────────────────── 픽업 로직 ──────────────────────────────

    /// <summary>
    /// 테이블 위의 총을 픽업하려고 시도합니다. (Raycast)
    /// </summary>
    private void TryPickupGun()
    {
        Camera cam = cameraMount ? cameraMount.GetComponent<Camera>() : Camera.main;
        if (cam == null) { Debug.LogError("픽업을 위한 카메라를 찾을 수 없습니다."); return; }

        // 카메라 중앙에서 'Pickup Mask'('GunPickup' 레이어)에 대해서만 레이캐스트 발사
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, pickupRange, pickupMask))
        {
            // 픽업 가능한 총을 맞췄다면 시퀀스 시작
            StartCoroutine(CoPickupSequence());
        }
    }

    /// <summary>
    /// 총을 픽업하는 시퀀스(카메라 부착, 실린더 확인 등)를 실행합니다.
    /// </summary>
    private IEnumerator CoPickupSequence()
    {
        if (_isGunPickedUp) yield break; // 이미 픽업함
        _isGunPickedUp = true;
        if (_highlighter != null) _highlighter.SetSelected(true); // 선택 상태로 변경

        // 1. 카메라에 부착
        if (attachToCameraOnPossess) AttachToCamera();

        // 2. 실린더 확인 애니메이션 (옵션)
        if (autoPeekOnPossess && peek != null && peek.isActiveAndEnabled)
        {
            bool done = false;
            peek.PlayOnce(() => done = true); // 애니메이션 완료 시 done=true
            yield return new WaitUntil(() => done); // 끝날 때까지 대기
        }

        // 3. 입력 활성화
        _controller.SetInputEnabled(true);
    }

    // ────────────────────────────── 이벤트 핸들러 및 턴 관리 ──────────────────────────────

    /// <summary>
    /// 발사(액션)가 완료되었을 때 호출됩니다. (CoAimAndFire 또는 CoRotateToSelfAndShot에서 호출)
    /// </summary>
    private void HandleActionComplete()
    {
        if (!autoNextTurnOnActionComplete || turnManager == null) return;
        _controller.SetInputEnabled(false);
        DetachFromCameraIfNeeded(); // 카메라에서 총 분리
        StartCoroutine(CoNextTurnAfterDelay(nextTurnDelay)); // 딜레이 후 턴 넘김
    }

    /// <summary>
    /// 유효 타겟 조준 시 (발사 키 입력 시) RevolverController에 의해 호출됩니다.
    /// (UI 버튼 등에서도 호출 가능하도록 Public)
    /// </summary>
    public void HandleAimRequest(Transform target)
    {
        if (_isAimingSequence) return;
        _isAimingSequence = true;
        _controller.SetInputEnabled(false); // 조준 연출 중에는 추가 입력 방지
        _aimSequenceCoroutine = StartCoroutine(CoAimAndFire(target));
    }

    /// <summary>
    /// 딜레이(nextTurnDelay) 이후 TurnManager.NextTurn()을 호출합니다.
    /// </summary>
    private System.Collections.IEnumerator CoNextTurnAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        turnManager.NextTurn(); // 👈 실제 턴 넘김
    }

    // ────────────────────────────── 카메라 부착/분리 및 연출 ──────────────────────────────

    /// <summary>
    /// 총을 cameraMount(또는 메인 카메라)에 부드럽게 부착시킵니다.
    /// </summary>
    private void AttachToCamera()
    {
        Transform cam = cameraMount ? cameraMount : (Camera.main ? Camera.main.transform : null);
        if (!cam) return;

        transform.SetParent(cam, true); // 카메라 자식으로 설정
        _attachedToCam = true;
        StopCoroutine(nameof(CoBlendToMountLocal)); // 기존 이동 중지
        StartCoroutine(CoBlendToMountLocal(attachBlendSeconds, mountLocalPosition, Quaternion.Euler(mountLocalEuler)));
    }

    /// <summary>
    /// 총을 카메라에서 분리합니다. (부모를 null로 설정)
    /// </summary>
    private void DetachFromCameraIfNeeded()
    {
        if (!_attachedToCam) return;
        transform.SetParent(null, true);
        _attachedToCam = false;
    }

    /// <summary>
    /// 현재 진행 중인 조준(CoAimAndFire)을 취소하고 원래 상태로 복귀합니다.
    /// </summary>
    private void CancelAimingSequence()
    {
        if (_aimSequenceCoroutine != null)
        {
            StopCoroutine(_aimSequenceCoroutine);
            _aimSequenceCoroutine = null;
        }

        // 조준 카메라 비활성화 (우선순위 낮춤 + 타겟 초기화)
        if (vcamAim)
        {
            vcamAim.Priority = 9;
            vcamAim.LookAt = null;
            vcamAim.Follow = null; // Follow 타겟도 초기화
        }
        _isAimingSequence = false;

        // 조준 취소 시, 총을 들고 있는 상태였다면 다시 입력 활성화
        if (_isGunPickedUp && attachToCameraOnPossess)
        {
            _controller.SetInputEnabled(true);
        }
    }

    /// <summary>
    /// [핵심 연출] FPS 시점에서 적을 조준하고 발사하는 코루틴입니다.
    /// </summary>
    private IEnumerator CoAimAndFire(Transform target)
    {
        // 1. 조준 카메라(vcamAim) 켜기
        if (vcamAim)
        {
            // 1-1. 바라볼 대상 (적)
            Transform targetHead = target.Find(headChildName);
            vcamAim.LookAt = targetHead ? targetHead : target;

            // 1-2. [핵심] 카메라 위치 기준 (현재 플레이어)
            // (이게 없으면 카메라가 (0,0,0)으로 튀는 버그 발생)
            if (_lastOwner == null) _lastOwner = turnManager.GetCurrentPlayer();
            Transform playerHead = _lastOwner.Find(headChildName);
            vcamAim.Follow = playerHead ? playerHead : _lastOwner;

            // 1-3. 우선순위를 높여 카메라 전환
            vcamAim.Priority = 20;
        }

        // 2. 조준 유지 (발사 전 대기)
        yield return new WaitForSeconds(aimHoldSeconds);

        // 3. 발사 및 데미지 판정 (테스트용)
        PlayerHealth targetHealth = target.GetComponent<PlayerHealth>();
        if (coordinator != null)
        {
            Debug.Log($"[Client] 서버에 대인 격발 요청 전송: target={target.name}");
            coordinator.TryShootOpponent();
        }
        else
        {
            Debug.LogWarning("[Client] PhotonGameCoordinator 미지정 — 격발 요청 보류");
            yield break;
        }


        // 4. [핵심] 카메라를 *먼저* 끕니다. (타겟이 방금 죽었을 수 있으므로)
        if (vcamAim)
        {
            vcamAim.Priority = 9;  // 우선순위 복구
            vcamAim.LookAt = null; // 타겟 초기화
            vcamAim.Follow = null; // Follow 초기화
        }

        // 5. [수정] 발사(반동) 코루틴을 *나중에* 실행하고, 끝날 때까지 기다립니다.
        yield return StartCoroutine(_controller.ExecuteShot());

        // 6. 상태 복구
        _isAimingSequence = false;
        _aimSequenceCoroutine = null;

    }


    /// <summary>
    /// 총을 카메라에 부착할 때 로컬 위치/회전을 부드럽게 이동시키는 코루틴입니다.
    /// (SmoothStep 보간 사용)
    /// </summary>
    private System.Collections.IEnumerator CoBlendToMountLocal(float seconds, Vector3 targetLocalPos, Quaternion targetLocalRot)
    {
        Vector3 fromP = transform.localPosition;
        Quaternion fromR = transform.localRotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, seconds);
            float e = t * t * (3f - 2f * t); // SmoothStep 이징 함수
            transform.localPosition = Vector3.LerpUnclamped(fromP, targetLocalPos, e);
            transform.localRotation = Quaternion.SlerpUnclamped(fromR, targetLocalRot, e);
            yield return null;
        }
        transform.localPosition = targetLocalPos; // 최종 위치로 고정
        transform.localRotation = targetLocalRot; // 최종 회전으로 고정
    }
}