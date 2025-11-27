using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cinemachine;

public class PlayerSetupManager : MonoBehaviour
{
    [Header("필수 참조")]
    [Tooltip("미리 지정할 '사람' 플레이어 오브젝트 (이 플레이어만 사람이 됨)")]
    public Transform humanPlayer;

    [Tooltip("플레이어의 1인칭 시점을 담당하는 시네머신 가상 카메라")]
    public CinemachineVirtualCamera mainVCam;

    [Tooltip("RevolverTurnPossession 스크립트 (총 오브젝트에 있음)")]
    public RevolverTurnPossession revolverTurnPossession;

    [Tooltip("TurnManager 스크립트 (씬에 있는 TurnManager 오브젝트)")]
    public TurnManager turnManager;

    [Header("플레이어 설정")]
    [Tooltip("플레이어를 식별할 태그")]
    public string playerTag = "Player";

    [Tooltip("카메라가 따라다닐 플레이어의 머리 자식 오브젝트 이름")]
    public string headChildName = "Head";

    // ⚠️ [추가] 1인칭 카메라 미세 조정 값
    [Header("1인칭 카메라 보정")]
    [Tooltip("카메라를 머리 중심보다 얼마나 앞으로 뺄지 (메쉬 뚫림 방지)")]
    public Vector3 cameraLocalOffset = new Vector3(0f, 0.05f, 0.15f);

    void Awake()
    {
        // 1. 필수 참조 유효성 검사
        if (humanPlayer == null)
        {
            Debug.LogError("[PlayerSetupManager] 'Human Player'가 인스펙터에 지정되지 않았습니다!");
            return;
        }
        if (revolverTurnPossession == null) revolverTurnPossession = FindObjectOfType<RevolverTurnPossession>();
        if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();

        // 2. 매니저들에게 사람 플레이어 정보 전달
        if (revolverTurnPossession != null) revolverTurnPossession.SetHumanPlayer(humanPlayer);
        if (turnManager != null) turnManager.SetHumanPlayer(humanPlayer);

        // 3. 씬 내 모든 플레이어 설정
        GameObject[] playerGOs = GameObject.FindGameObjectsWithTag(playerTag);
        if (playerGOs.Length == 0)
        {
            Debug.LogError($"[PlayerSetupManager] 태그 '{playerTag}'를 가진 플레이어가 없습니다!");
            return;
        }

        Debug.Log($"<color=cyan>[PLAYER SETUP]</color> 총 {playerGOs.Length}명의 플레이어 설정을 시작합니다.");

        foreach (GameObject currentPlayerGO in playerGOs)
        {
            EnemyAIController aiController = currentPlayerGO.GetComponent<EnemyAIController>();
            if (aiController == null) continue;

            // --- 사람(Human) 설정 ---
            if (currentPlayerGO.transform == humanPlayer)
            {
                currentPlayerGO.name += "_Human";
                Debug.Log($"<color=green>▶ {currentPlayerGO.name} (Human)</color> 1인칭 설정 중...");

                aiController.enabled = false; // AI 끄기

                if (mainVCam != null)
                {
                    // 1. 실제 머리 뼈(Bone)를 찾습니다.
                    Transform headBone = currentPlayerGO.transform.Find(headChildName);
                    Transform targetTransform = headBone ? headBone : currentPlayerGO.transform;

                    // ⚠️ [수정됨] 카메라 타겟 보정 로직 ⚠️
                    // 머리 뼈 바로 위치에 카메라를 두면, 애니메이션 시 뒤통수가 보일 수 있습니다.
                    // 따라서 머리 뼈의 '자식'으로 가상의 타겟 오브젝트를 만들고 위치를 앞으로 뺍니다.
                    if (headBone != null)
                    {
                        // (1) 빈 오브젝트 생성 ("1stPersonCameraTarget")
                        GameObject camTargetObj = new GameObject("1stPersonCameraTarget");

                        // (2) 머리 뼈의 자식으로 설정 (애니메이션 따라감)
                        camTargetObj.transform.SetParent(headBone);

                        // (3) 위치 보정 (머리 중심에서 약간 앞/위로 이동)
                        camTargetObj.transform.localPosition = cameraLocalOffset;
                        camTargetObj.transform.localRotation = Quaternion.identity; // 회전은 머리와 동일하게

                        // (4) 카메라의 타겟을 이 '보정된 오브젝트'로 설정
                        targetTransform = camTargetObj.transform;
                    }

                    mainVCam.Follow = targetTransform;
                    mainVCam.LookAt = targetTransform; // (마우스 회전 스크립트가 있다면 LookAt은 비워야 할 수도 있음)

                    // (RPT에게도 카메라 연동 정보 갱신 필요 시 여기서 처리 가능)
                    if (revolverTurnPossession != null)
                    {
                        revolverTurnPossession.vcam = mainVCam;
                    }
                }

                // ⚠️ [추가 팁] 자신의 머리만 그림자로 처리하여 시야 가림 완전 방지 (선택 사항)
                // RenderShadowsOnly(currentPlayerGO); 
            }
            // --- AI 설정 ---
            else
            {
                currentPlayerGO.name += "_AI";
                aiController.enabled = true; // AI 켜기
            }
        }
    }

    /// <summary>
    /// (선택 사항) 플레이어의 메쉬를 '그림자만(ShadowsOnly)' 나오게 변경하여
    /// 카메라가 몸 안으로 들어갔을 때 메쉬가 보이는 현상을 원천 차단합니다.
    /// (단, 이렇게 하면 손이나 몸도 안 보일 수 있으므로 주의)
    /// </summary>
    private void RenderShadowsOnly(GameObject player)
    {
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }
    }
}