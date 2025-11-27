using UnityEngine;
using System.Collections;
using Cinemachine;

/// <summary>
/// [총괄 매니저] 리볼버의 이동, 조준, 발사, 사운드, 턴 관리, 애니메이션 제어 등 
/// 게임 내 모든 물리적 액션과 연출을 담당하는 핵심 스크립트입니다.
/// </summary>
// 이 스크립트가 작동하려면 발사 로직(Controller)과 탄창 데이터(Cylinder)가 필수입니다.
[RequireComponent(typeof(RevolverController))]
[RequireComponent(typeof(RevolverCylinder))]
public class RevolverTurnPossession : MonoBehaviour
{
    // ==================================================================================
    // 1. 필수 참조 및 배치 설정
    // ==================================================================================
    [Header("1. 필수 참조")]
    [Tooltip("게임의 턴 흐름(순서, 승패)을 관리하는 매니저")]
    public TurnManager turnManager;
    [Tooltip("총이 놓일 테이블의 중심점 (배치 기준)")]
    public Transform tableCenter;

    [Header("2. 총기 배치 (테이블 위)")]
    [Tooltip("테이블 좌우 위치 오프셋 (0:중앙, 음수:왼쪽, 양수:오른쪽)")]
    public float placeHorizontalOffset = 0f;

    [Range(0f, 1f), Tooltip("테이블 중심에서 플레이어 쪽으로 얼마나 치우쳐 놓을지 (0:중앙 ~ 1:플레이어 앞)")]
    public float placeRatio = 0.7f;

    [Tooltip("테이블 바닥으로부터 총이 떠있는 높이")]
    public float heightOffset = 0.8f;

    [Tooltip("총이 테이블 위에서 이동하는 데 걸리는 시간 (초) - 1.5초 권장(부드러움)")]
    public float moveDuration = 1.5f;

    [Tooltip("테이블 이동 시 포물선(Arc) 높이 (0:직선 이동, 0.2:살짝 뜸)")]
    public float tableMoveArcHeight = 0.2f;

    public bool facePlayer = true; // 총이 놓일 때 플레이어를 바라볼지
    public Vector3 extraEulerOffset; // 회전 보정값
    public string headChildName = "Head"; // 머리 오브젝트 이름
    public bool snapFirstTime = false; // 시작 시 애니메이션 재생 여부 (false 권장)

    // ==================================================================================
    // 2. 카메라 및 턴 관리
    // ==================================================================================
    [Header("3. 시네머신 카메라")]
    public CinemachineVirtualCamera vcam; // 기본 1인칭 카메라
    public CinemachineVirtualCamera vcamAim; // 조준(줌인)용 카메라

    [Tooltip("조준 위치 도착 후, 발사하기 전까지 대기하는 시간 (긴장감 조성)")]
    public float aimHoldSeconds = 1.0f;

    [Header("4. 턴 및 입력")]
    public bool autoNextTurnOnActionComplete = true; // 행동 후 자동 턴 넘김
    public float nextTurnDelay = 0.35f; // 턴 넘기기 전 대기
    public KeyCode cancelAimKey = KeyCode.Mouse1; // 우클릭 취소

    // ==================================================================================
    // 3. 사격 및 데미지 (사람 전용 설정 포함)
    // ==================================================================================
    [Header("5. 사격 및 데미지")]
    public KeyCode selfShotKey = KeyCode.Q; // 자살 키
    public int selfShotDamage = 1;
    public int opponentShotDamage = 1;
    public float selfShotRotationDuration = 0.3f; // 자살 조준 속도

    // ⚠️ [사람 플레이어 전용 연출]
    [Tooltip("✅ [사람 전용] '자신에게 쏘기' 시, 총구 각도 조절")]
    public Vector3 selfShotLocalEuler = new Vector3(0f, 0f, 0f);
    [Tooltip("✅ [사람 전용] '자신에게 쏘기' 시, 총의 위치 조절")]
    public Vector3 selfShotLocalPosition = new Vector3(0f, -0.2f, 0.8f);

    [Header("6. 오디오 (Sound)")]
    [Tooltip("실탄 발사 효과음 (Bang!)")]
    public AudioClip liveFireSound;
    [Tooltip("공포탄/빈 총 효과음 (Click...)")]
    public AudioClip dryFireSound;
    [Tooltip("조준 완료(도착) 시 재생할 소리 (철컥!)")]
    public AudioClip aimReadySound;

    [Tooltip("소리를 재생할 오디오 소스 (비워두면 자동 생성됨)")]
    public AudioSource audioSource;

    // ==================================================================================
    // 4. 연출 설정 (Visuals)
    // ==================================================================================
    [Header("7. AI 사격 기본값 (AIFireOffsets 없을 때)")]
    public Vector3 aiFireLocalPosition = new Vector3(0f, 0f, 0.5f);
    public Vector3 aiFireLocalEuler = new Vector3(0f, 0f, 0f);
    public float aiArcHeight = 0.2f;
    public float aiMoveDuration = 0.25f;

    [Header("8. 카드 사용 연출 (Card Use)")]
    [Tooltip("카드를 사용할 때 총이 이동하는 속도 (초)")]
    public float cardAnimMoveDuration = 0.25f;
    public float aiCardAnimHoldDuration = 0.5f;

    // ⚠️ [사람 플레이어 전용 연출]
    [Tooltip("✅ [사람 전용] 카드를 사용할 때 총의 위치")]
    public Vector3 playerCardAnimLocalPosition = new Vector3(0.2f, -0.2f, 0.4f);
    [Tooltip("✅ [사람 전용] 카드를 사용할 때 총의 회전")]
    public Vector3 playerCardAnimLocalEuler = new Vector3(90f, 45f, 0f);
    public float playerCardAnimHoldDuration = 1.0f;

    // ==================================================================================
    // 5. 기타 설정 (Misc)
    // ==================================================================================
    [Header("9. 카메라 부착 옵션")]
    public bool attachToCameraOnPossess = true;
    public Transform cameraMount;
    public Vector3 mountLocalPosition = new(0.25f, -0.25f, 0.5f);
    public Vector3 mountLocalEuler = new(0f, 0f, 0f);
    public float attachBlendSeconds = 0.12f;

    [Header("10. 픽업 및 상호작용")]
    public bool requirePickupOnClick = true;
    public LayerMask pickupMask;
    public float pickupRange = 3f;

    [Header("11. UI 연결")]
    public SkillCardManager skillCardManager;
    public CardDisplayUI cardDisplayUI;
    public CrosshairRaycaster crosshairRaycaster;
    public GameObject lookbookPanel;

    // --- 내부 변수 ---
    private RevolverController _controller;
    private RevolverCylinder cylinder;
    private InteractableHighlighter _highlighter;
    private Transform _lastOwner;
    private bool _initialized;
    private bool _attachedToCam;
    private bool _isAimingSequence;
    private bool _isGunPickedUp;
    private bool _isSelfShooting;
    private Coroutine _aimSequenceCoroutine;
    private Transform humanPlayer;

    // ⚠️ [애니메이션 매니저] 현재 총 주인의 애니메이션 제어용
    private CharacterAnimManager _currentOwnerAnim;

    public bool IsCardAnimating { get; private set; } = false;

    private bool IsUIOpen => (cardDisplayUI != null && cardDisplayUI.IsOpen) || (lookbookPanel != null && lookbookPanel.activeSelf);

    // ==================================================================================
    // Unity 라이프사이클 (초기화 및 업데이트)
    // ==================================================================================
    void Awake()
    {
        _controller = GetComponent<RevolverController>();
        cylinder = GetComponent<RevolverCylinder>();
        _highlighter = GetComponent<InteractableHighlighter>();

        if (cylinder == null) { Debug.LogError("RevolverCylinder 없음!"); enabled = false; return; }
        if (skillCardManager == null) skillCardManager = FindObjectOfType<SkillCardManager>();

        // 오디오 소스 자동 생성 및 설정 (2D 강제)
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0.0f; // 2D 사운드 (플레이어에게 잘 들리게)
            }
        }

        _controller.OnActionComplete += HandleActionComplete;
        _controller.OnAimRequest += HandleAimRequest;
        _controller.SetInputEnabled(false);
    }

    void OnDestroy()
    {
        if (_controller != null)
        {
            _controller.OnActionComplete -= HandleActionComplete;
            _controller.OnAimRequest -= HandleAimRequest;
        }
    }

    void Start()
    {
        TryUpdateOwner(force: true);
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        bool isCardDrawing = (skillCardManager != null && skillCardManager.IsDrawingCard);

        if (lookbookPanel != null && lookbookPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            lookbookPanel.SetActive(false);
            return;
        }

        if (IsUIOpen || _isSelfShooting || IsCardAnimating) return;

        if (_isAimingSequence)
        {
            if (Input.GetKeyDown(cancelAimKey)) CancelAimingSequence();
            return;
        }

        TryUpdateOwner(force: false);

        // 상호작용 로직 (총 집기, 카드 사용 등)
        if (requirePickupOnClick && _lastOwner != null)
        {
            if (Input.GetKeyDown(_controller.fireKey)) // 좌클릭
            {
                if (crosshairRaycaster != null && crosshairRaycaster.CurrentTarget != null)
                {
                    InteractableHighlighter target = crosshairRaycaster.CurrentTarget;
                    CardVisual card = target.GetComponentInParent<CardVisual>();
                    if (card != null) { if (cardDisplayUI != null) cardDisplayUI.ShowPanel(); return; }
                    LookbookInteractable lookbook = target.GetComponent<LookbookInteractable>();
                    if (lookbook != null && lookbookPanel != null) { lookbookPanel.SetActive(true); return; }
                }
            }

            if (humanPlayer == null || turnManager == null) return;
            bool isMyTurn = (turnManager.GetCurrentPlayer() == humanPlayer);
            if (!isMyTurn) return;

            if (_isGunPickedUp)
            {
                if (isCardDrawing) return;
                if (Input.GetKeyDown(selfShotKey)) ShootSelf();
                if (Input.GetKeyDown(cancelAimKey)) PutDownGun();
            }
            else
            {
                if (Input.GetKeyDown(_controller.fireKey))
                {
                    if (isCardDrawing) return;
                    TryPickupGun();
                }
                if (Input.GetKeyDown(cancelAimKey))
                {
                    if (isCardDrawing) return;
                    if (crosshairRaycaster != null && crosshairRaycaster.CurrentTarget != null)
                    {
                        InteractableHighlighter target = crosshairRaycaster.CurrentTarget;
                        CardVisual card = target.GetComponentInParent<CardVisual>();
                        if (card != null)
                        {
                            CardLogic logic = card.GetComponentInChildren<CardLogic>();
                            if (logic != null) logic.Use();
                            return;
                        }
                    }
                }
            }
        }
    }

    // ==================================================================================
    // 기능 1: 자신 쏘기 (Self Shot)
    // ==================================================================================
    public void ShootSelf()
    {
        if (_lastOwner == null || _isSelfShooting || IsCardAnimating) return;
        _controller.SetInputEnabled(false);
        if (_aimSequenceCoroutine != null) StopCoroutine(_aimSequenceCoroutine);
        _isAimingSequence = false;
        DetachFromCameraIfNeeded();
        StartCoroutine(CoRotateToSelfAndShot(_lastOwner));
    }

    private IEnumerator CoRotateToSelfAndShot(Transform player)
    {
        _isSelfShooting = true;
        Vector3 tablePos = transform.position;
        Quaternion tableRot = transform.rotation;

        Transform playerHead = player.Find(headChildName);
        Transform aiTargetParent = playerHead ? playerHead : player;
        EnemyAIController ai = player.GetComponent<EnemyAIController>();
        bool isAI = (ai != null && ai.enabled);

        Vector3 posOffset; Vector3 eulerOffset;

        // AI vs 사람 위치 분기
        if (isAI)
        {
            AIFireOffsets customOffsets = player.GetComponent<AIFireOffsets>();
            if (customOffsets != null) { posOffset = customOffsets.selfShotPosition; eulerOffset = customOffsets.selfShotEuler; }
            else { posOffset = this.aiFireLocalPosition; eulerOffset = this.aiFireLocalEuler; }
        }
        else
        {
            posOffset = this.selfShotLocalPosition;
            eulerOffset = this.selfShotLocalEuler;
        }

        Vector3 targetPos = aiTargetParent.TransformPoint(posOffset);
        Vector3 lookDirection = (playerHead ? playerHead.position : player.position) - targetPos;
        if (lookDirection.sqrMagnitude < 0.001f) lookDirection = aiTargetParent.forward;
        Quaternion targetRot = Quaternion.LookRotation(lookDirection.normalized, Vector3.up) * Quaternion.Euler(eulerOffset);

        // 1. 이동
        yield return StartCoroutine(CoMoveGunOverTime(tablePos, targetPos, tableRot, targetRot, selfShotRotationDuration));

        // 2. 도착 후 찰나의 정적 + '철컥' 소리
        yield return new WaitForSeconds(0.1f);
        if (audioSource != null && aimReadySound != null) audioSource.PlayOneShot(aimReadySound);

        // ⚠️ [애니메이션] 도착(철컥) 시점: Aiming 상태 켜기 + 1차 발사 모션(Shooting)
        if (_currentOwnerAnim != null)
        {
            _currentOwnerAnim.SetAimingState(true); // 자세 유지(Aiming) 시작
            _currentOwnerAnim.TriggerFire();        // 철컥 반동(Shooting)
        }

        // 3. 발사 데이터 처리
        PlayerHealth myHealth = player.GetComponentInParent<PlayerHealth>();
        if (myHealth != null)
        {
            bool isLive = cylinder.FireNextRound();
            if (audioSource != null)
            {
                AudioClip clipToPlay = isLive ? liveFireSound : dryFireSound;
                if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
            }
            int damage = isLive ? selfShotDamage : 0;
            PlayerStatusEffects status = player.GetComponent<PlayerStatusEffects>();
            if (isLive && status != null && status.HasDamageBoost) { damage += 1; status.ConsumeDamageBoost(); }
            myHealth.TakeDamage(damage);
        }
        else Debug.LogWarning("[Sound Fail] PlayerHealth 찾지 못함");

        // ⚠️ [애니메이션] 2차 발사 모션 (진짜 쏠 때 Shooting)
        if (_currentOwnerAnim != null) _currentOwnerAnim.TriggerFire();

        // 4. 반동 및 복귀
        yield return StartCoroutine(_controller.ExecuteShot());

        // ⚠️ [애니메이션] 복귀 시작 시 Aiming 해제 (Idle로)
        if (_currentOwnerAnim != null) _currentOwnerAnim.SetAimingState(false);

        yield return StartCoroutine(CoMoveGunOverTime(targetPos, tablePos, targetRot, tableRot, aiMoveDuration));

        HandleActionComplete();
        _isSelfShooting = false;
    }

    // ==================================================================================
    // 기능 2: 상대 쏘기 (Aim & Fire)
    // ==================================================================================
    public void HandleAimRequest(Transform target)
    {
        if (_isAimingSequence || IsCardAnimating) return;
        _isAimingSequence = true;
        _controller.SetInputEnabled(false);
        _aimSequenceCoroutine = StartCoroutine(CoAimAndFire(target, true));
    }

    public void AI_HandleAimRequest(Transform target)
    {
        if (_isAimingSequence || IsCardAnimating) return;
        _isAimingSequence = true;
        _controller.SetInputEnabled(false);
        _aimSequenceCoroutine = StartCoroutine(CoAimAndFire(target, false));
    }

    private IEnumerator CoAimAndFire(Transform target, bool useCameraZoom)
    {
        Vector3 aiTablePosition = Vector3.zero;
        Quaternion aiTableRotation = Quaternion.identity;

        // 1. 조준 이동
        if (!useCameraZoom) // AI
        {
            aiTablePosition = transform.position;
            aiTableRotation = transform.rotation;

            if (_lastOwner == null) _lastOwner = turnManager.GetCurrentPlayer();
            Transform playerHead = _lastOwner.Find(headChildName);
            Transform aiTargetParent = playerHead ? playerHead : _lastOwner;

            AIFireOffsets customOffsets = _lastOwner.GetComponent<AIFireOffsets>();
            Vector3 posOffset = customOffsets ? customOffsets.fireLocalPosition : aiFireLocalPosition;
            Vector3 eulerOffset = customOffsets ? customOffsets.fireLocalEuler : aiFireLocalEuler;

            Vector3 targetPos = aiTargetParent.TransformPoint(posOffset);
            Transform victimHead = target.Find(headChildName);
            Vector3 lookDir = (victimHead ? victimHead.position : target.position) - targetPos;
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up) * Quaternion.Euler(eulerOffset);

            yield return StartCoroutine(CoMoveGunOverTime(aiTablePosition, targetPos, aiTableRotation, targetRot, aiMoveDuration));
        }
        else // 사람
        {
            if (vcamAim)
            {
                Transform targetHead = target.Find(headChildName);
                vcamAim.LookAt = targetHead ? targetHead : target;
                if (vcam != null && vcam.Follow != null) vcamAim.Follow = vcam.Follow;
                else { if (_lastOwner == null) _lastOwner = turnManager.GetCurrentPlayer(); Transform ph = _lastOwner.Find(headChildName); vcamAim.Follow = ph ? ph : _lastOwner; }
                vcamAim.Priority = 20;
            }
        }

        // 2. 이동 완료 후 '철컥' 소리
        yield return new WaitForSeconds(0.1f);
        if (audioSource != null && aimReadySound != null) audioSource.PlayOneShot(aimReadySound);

        // ⚠️ [애니메이션] 도착(철컥) 시점: Aiming 상태 켜기 + 1차 발사 모션(Shooting)
        if (_currentOwnerAnim != null)
        {
            _currentOwnerAnim.SetAimingState(true); // Aiming(조준) 자세 유지 시작
            _currentOwnerAnim.TriggerFire();        // 철컥 반동(Shooting)
        }

        // 3. 조준 대기
        yield return new WaitForSeconds(aimHoldSeconds);

        // 4. 발사 데이터 처리
        PlayerHealth targetHealth = target.GetComponentInParent<PlayerHealth>();
        if (targetHealth != null)
        {
            bool isLive = cylinder.FireNextRound();

            if (audioSource != null)
            {
                AudioClip clipToPlay = isLive ? liveFireSound : dryFireSound;
                if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
            }

            int damage = isLive ? opponentShotDamage : 0;
            if (_lastOwner != null)
            {
                PlayerStatusEffects status = _lastOwner.GetComponent<PlayerStatusEffects>();
                if (isLive && status != null && status.HasDamageBoost) { damage += 1; status.ConsumeDamageBoost(); }
            }
            targetHealth.TakeDamage(damage);
        }
        else Debug.LogWarning("[Sound Fail] PlayerHealth 없음");

        // ⚠️ [애니메이션] 2차 발사 모션 (진짜 발사)
        // Aiming이 켜져 있으므로 Shooting 후 다시 Aiming 자세로 돌아옵니다.
        if (_currentOwnerAnim != null) _currentOwnerAnim.TriggerFire();

        // 5. 복귀
        if (useCameraZoom && vcamAim) { vcamAim.Priority = 9; vcamAim.LookAt = null; vcamAim.Follow = null; }
        yield return StartCoroutine(_controller.ExecuteShot());

        // ⚠️ [애니메이션] 복귀 시작 전 Aiming 해제 (Idle로 복귀)
        if (_currentOwnerAnim != null) _currentOwnerAnim.SetAimingState(false);

        if (!useCameraZoom) { yield return StartCoroutine(CoMoveGunOverTime(transform.position, aiTablePosition, transform.rotation, aiTableRotation, aiMoveDuration)); }

        _isAimingSequence = false;
        _aimSequenceCoroutine = null;
        HandleActionComplete();
    }

    // ==================================================================================
    // 기능 3: 카드 사용 연출 (Card Animation)
    // ==================================================================================
    public void PlayCardAnimation() { if (IsCardAnimating) return; StartCoroutine(Co_AnimateCardUse()); }

    private IEnumerator Co_AnimateCardUse()
    {
        IsCardAnimating = true;
        Vector3 tablePos = transform.position;
        Quaternion tableRot = transform.rotation;

        if (_lastOwner == null) _lastOwner = turnManager.GetCurrentPlayer();
        if (_lastOwner == null) { IsCardAnimating = false; yield break; }

        EnemyAIController ai = _lastOwner.GetComponent<EnemyAIController>();
        bool isAI = (ai != null && ai.enabled);

        Vector3 posOffset; Vector3 eulerOffset; float holdDuration;

        // AI/사람 위치 분기
        if (isAI)
        {
            AIFireOffsets customOffsets = _lastOwner.GetComponent<AIFireOffsets>();
            if (customOffsets != null) { posOffset = customOffsets.cardAnimPosition; eulerOffset = customOffsets.cardAnimEuler; }
            else { posOffset = this.aiFireLocalPosition; eulerOffset = this.aiFireLocalEuler; }
            holdDuration = this.aiCardAnimHoldDuration;
        }
        else
        {
            posOffset = this.playerCardAnimLocalPosition;
            eulerOffset = this.playerCardAnimLocalEuler;
            holdDuration = this.playerCardAnimHoldDuration;
        }

        Transform playerHead = _lastOwner.Find(headChildName);
        Transform aiTargetParent = playerHead ? playerHead : _lastOwner;
        Vector3 targetPos = aiTargetParent.TransformPoint(posOffset);

        Quaternion targetRot;
        if (isAI)
        {
            targetRot = aiTargetParent.rotation * Quaternion.Euler(eulerOffset);
            if (turnManager != null)
            {
                Transform opponent = turnManager.FindOpponentInFront(_lastOwner);
                if (opponent != null)
                {
                    Vector3 lookDir = opponent.position - targetPos;
                    targetRot = Quaternion.LookRotation(lookDir, Vector3.up) * Quaternion.Euler(eulerOffset);
                }
            }
        }
        else
        {
            targetRot = aiTargetParent.rotation * Quaternion.Euler(eulerOffset);
        }

        // 이동 -> 대기 -> 복귀
        yield return StartCoroutine(CoMoveGunOverTime(tablePos, targetPos, tableRot, targetRot, cardAnimMoveDuration));
        yield return new WaitForSeconds(holdDuration);
        yield return StartCoroutine(CoMoveGunOverTime(targetPos, tablePos, targetRot, tableRot, cardAnimMoveDuration));
        IsCardAnimating = false;
    }

    // ==================================================================================
    // 보조 함수: 이동, 픽업, 턴 관리
    // ==================================================================================
    #region Helper Functions

    private void TryUpdateOwner(bool force)
    {
        Transform cur = turnManager.GetCurrentPlayer();
        if (!force && cur == _lastOwner) return;

        if (_isAimingSequence) CancelAimingSequence();
        DetachFromCameraIfNeeded();

        // [애니메이션] 이전 주인: 총 내려놓기
        if (_currentOwnerAnim != null)
        {
            _currentOwnerAnim.TriggerPutDown();
            _currentOwnerAnim.SetAimingState(false);
            _currentOwnerAnim = null;
        }

        _lastOwner = cur;
        _isGunPickedUp = false;

        if (_highlighter != null) _highlighter.SetSelected(false);
        if (cur == null) return;

        // [애니메이션] 새 주인: AnimManager 가져오기 (대기 상태로 시작)
        _currentOwnerAnim = cur.GetComponent<CharacterAnimManager>();

        _controller.SetInputEnabled(false);
        StopAllCoroutines();
        StartCoroutine(CoMoveToPlayer(cur));
    }

    private IEnumerator CoMoveToPlayer(Transform player)
    {
        Transform head = player.Find(headChildName);
        Vector3 playerPos = head ? head.position : player.position;
        Vector3 center = tableCenter.position;

        Vector3 dir = playerPos - center;
        dir.y = 0f;
        float dist = dir.magnitude;
        Vector3 dirNorm = dist > 1e-4f ? dir / dist : Vector3.forward;
        Vector3 rightDir = new(dirNorm.z, 0, -dirNorm.x);

        Vector3 flatTarget = center
            + (dirNorm * (dist * Mathf.Clamp01(placeRatio)))
            + (rightDir * placeHorizontalOffset);

        Vector3 targetPos = new(flatTarget.x, center.y + heightOffset, flatTarget.z);

        Vector3 lookDir = (facePlayer ? playerPos : center) - targetPos;
        lookDir.y = 0f;
        Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized, Vector3.up) * Quaternion.Euler(extraEulerOffset);

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
                float e = t * t * (3f - 2f * t);

                // 포물선(Arc) 이동
                float arc = Mathf.Sin(e * Mathf.PI) * tableMoveArcHeight;
                Vector3 arcOffset = Vector3.up * arc;

                transform.position = Vector3.LerpUnclamped(fromPos, targetPos, e) + arcOffset;
                transform.rotation = Quaternion.SlerpUnclamped(fromRot, targetRot, e);
                yield return null;
            }
        }

        if (!requirePickupOnClick)
        {
            StartCoroutine(CoPickupSequence());
        }
    }

    private void PutDownGun()
    {
        _controller.SetInputEnabled(false);
        DetachFromCameraIfNeeded();
        _isGunPickedUp = false;
        if (_highlighter != null) _highlighter.SetSelected(false);

        // [애니메이션] 내려놓기
        if (_currentOwnerAnim != null) _currentOwnerAnim.TriggerPutDown();

        StopAllCoroutines();
        StartCoroutine(CoMoveToPlayer(_lastOwner));
    }

    private void TryPickupGun()
    {
        Camera cam = (cameraMount != null) ? cameraMount.GetComponent<Camera>() : Camera.main;
        if (cam == null) { cam = Camera.main; if (cam == null) return; }

        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, pickupRange, pickupMask))
        {
            StartCoroutine(CoPickupSequence());
        }
    }

    private IEnumerator CoPickupSequence()
    {
        if (_isGunPickedUp) yield break;

        _isGunPickedUp = true;
        if (_highlighter != null) _highlighter.SetSelected(true);

        // [애니메이션] 줍기
        if (_currentOwnerAnim != null) _currentOwnerAnim.TriggerPickup();

        if (attachToCameraOnPossess) AttachToCamera();
        _controller.SetInputEnabled(true);
        yield return null;
    }

    public void HandleActionComplete()
    {
        if (!autoNextTurnOnActionComplete || turnManager == null) return;
        _controller.SetInputEnabled(false);
        DetachFromCameraIfNeeded();
        StartCoroutine(CoNextTurnAfterDelay(nextTurnDelay));
    }

    private IEnumerator CoNextTurnAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (cylinder.GetRemainingRounds() <= 0) cylinder.LoadCylinder();
        turnManager.NextTurn();
    }

    public void SetHumanPlayer(Transform player) { humanPlayer = player; }

    private void AttachToCamera()
    {
        Transform cam = cameraMount ? cameraMount : (Camera.main ? Camera.main.transform : null);
        if (!cam) return;
        transform.SetParent(cam, true);
        _attachedToCam = true;
        StopCoroutine(nameof(CoBlendToMountLocal));
        StartCoroutine(CoBlendToMountLocal(attachBlendSeconds, mountLocalPosition, Quaternion.Euler(mountLocalEuler)));
    }

    private void DetachFromCameraIfNeeded()
    {
        if (!_attachedToCam) return;
        transform.SetParent(null, true);
        _attachedToCam = false;
    }

    private void CancelAimingSequence()
    {
        if (_aimSequenceCoroutine != null)
        {
            StopCoroutine(_aimSequenceCoroutine);
            _aimSequenceCoroutine = null;
        }
        if (vcamAim) { vcamAim.Priority = 9; vcamAim.LookAt = null; vcamAim.Follow = null; }
        _isAimingSequence = false;

        // [애니메이션] 조준 취소 시 Idle로 복귀
        if (_currentOwnerAnim != null) _currentOwnerAnim.SetAimingState(false);

        if (_isGunPickedUp && attachToCameraOnPossess) _controller.SetInputEnabled(true);
    }

    private IEnumerator CoBlendToMountLocal(float seconds, Vector3 targetLocalPos, Quaternion targetLocalRot)
    {
        Vector3 fromP = transform.localPosition;
        Quaternion fromR = transform.localRotation;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, seconds);
            float e = t * t * (3f - 2f * t);
            transform.localPosition = Vector3.LerpUnclamped(fromP, targetLocalPos, e);
            transform.localRotation = Quaternion.SlerpUnclamped(fromR, targetLocalRot, e);
            yield return null;
        }
        transform.localPosition = targetLocalPos;
        transform.localRotation = targetLocalRot;
    }

    private IEnumerator CoMoveGunOverTime(Vector3 fromPos, Vector3 toPos, Quaternion fromRot, Quaternion toRot, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.001f, duration);
            float e = t * t * (3f - 2f * t);
            float arc = Mathf.Sin(e * Mathf.PI) * aiArcHeight;
            Vector3 arcOffset = Vector3.up * arc;
            transform.position = Vector3.LerpUnclamped(fromPos, toPos, e) + arcOffset;
            transform.rotation = Quaternion.SlerpUnclamped(fromRot, toRot, e);
            yield return null;
        }
        transform.position = toPos;
        transform.rotation = toRot;
    }

    #endregion
}