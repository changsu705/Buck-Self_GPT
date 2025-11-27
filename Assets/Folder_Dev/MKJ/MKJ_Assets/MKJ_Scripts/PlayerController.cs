using UnityEngine;
using UnityEngine.UI; // <<< 1. 레거시 UI를 사용하기 위해 이 줄을 추가!

public class PlayerController : MonoBehaviour
{
    // --- 변수 선언 ---

    // 1. 실린더 회전
    [Tooltip("총 실린더의 Animator 컴포넌트를 연결하세요.")]
    public Animator cylinderAnimator;

    // 2. 대상 지정
    private GameObject selectedTarget;
    private Camera mainCamera;

    // 3. 격발
    [Tooltip("true: 실탄, false: 공포탄")]
    private bool[] bulletChamber = { true, false, true, false, false };
    private int currentBulletIndex = 0;
    private int blankFiredCount = 0;

    // <<< 2. UI 텍스트를 연결할 변수 추가!
    [Tooltip("게임 상태 메시지를 표시할 UI 텍스트(레거시)")]
    public Text statusText;

    // --- Unity 생명주기 함수 ---

    void Start()
    {
        mainCamera = Camera.main; // 게임 시작 시 메인 카메라를 찾아 저장
        if (statusText != null) // <<< 게임 시작 시 안내 문구 설정
        {
            statusText.text = "대상을 선택하세요.";
        }
    }

    void Update()
    {
        // 마우스 왼쪽 버튼을 클릭했을 때 대상 지정 함수 호출
        if (Input.GetMouseButtonDown(0))
        {
            DetectAndSelectTarget();
        }
    }

    // --- 공개 함수 (UI 버튼 등에서 호출) ---

    public void SpinCylinder()
    {
        if (cylinderAnimator != null)
        {
            cylinderAnimator.SetTrigger("Spin");
        }
        else
        {
            Debug.LogError("Cylinder Animator가 할당되지 않았습니다!");
        }
    }

    public void FireGun()
    {
        if (selectedTarget == null)
        {
            if (statusText != null) statusText.text = "먼저 대상을 지정하세요!"; // <<< 3. Debug.Log를 UI 텍스트로 변경
            return;
        }

        if (currentBulletIndex >= bulletChamber.Length)
        {
            if (statusText != null) statusText.text = "총알이 모두 소진되었습니다."; // <<< 3. Debug.Log를 UI 텍스트로 변경
            return;
        }

        bool isLiveRound = bulletChamber[currentBulletIndex];

        if (isLiveRound)
        {
            // 실탄일 경우
            if (statusText != null) statusText.text = "탕! 실탄입니다. 상대 턴으로 넘어갑니다."; // <<< 3. Debug.Log를 UI 텍스트로 변경
            Damageable targetHealth = selectedTarget.GetComponent<Damageable>();
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(1); // 대상에게 데미지 1 주기
            }
            EndTurn();
        }
        else
        {
            // 공포탄일 경우
            blankFiredCount++; // 연속 공포탄 카운트 증가

            if (blankFiredCount < 2)
            {
                if (statusText != null) statusText.text = "공포탄입니다! 한 번 더 쏠 수 있습니다."; // <<< 3. Debug.Log를 UI 텍스트로 변경
            }
            else
            {
                if (statusText != null) statusText.text = "공포탄을 연속 2회 사용했습니다. 턴이 넘어갑니다."; // <<< 3. Debug.Log를 UI 텍스트로 변경
                EndTurn();
            }
        }
        currentBulletIndex++; // 다음 총알로 인덱스 이동
    }

    // --- 내부 처리 함수 ---

    private void DetectAndSelectTarget()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit) && hit.collider.CompareTag("Player"))
        {
            selectedTarget = hit.collider.gameObject;
            if (statusText != null) statusText.text = selectedTarget.name + "을(를) 대상으로 지정했습니다. 격발하세요!"; // <<< 3. Debug.Log를 UI 텍스트로 변경
            MoveCameraToTarget(selectedTarget.transform);
        }
    }

    private void MoveCameraToTarget(Transform targetTransform)
    {
        Vector3 desiredPosition = targetTransform.position + targetTransform.forward * -2f + Vector3.up * 1f;
        mainCamera.transform.position = desiredPosition;
        mainCamera.transform.LookAt(targetTransform);
    }

    private void EndTurn()
    {
        selectedTarget = null;
        blankFiredCount = 0;
        // TODO: 실제 상대방 턴으로 넘기는 게임 로직을 여기에 구현하세요.
    }
}