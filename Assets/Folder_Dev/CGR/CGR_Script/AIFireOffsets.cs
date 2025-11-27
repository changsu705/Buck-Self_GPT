using UnityEngine;

/// <summary>
/// 각 AI 플레이어 프리팹에 부착하여,
/// 해당 AI가 총을 쏘거나 카드를 쓸 때 사용할 '전용' 로컬 오프셋 값을 저장합니다.
/// </summary>
public class AIFireOffsets : MonoBehaviour
{
    [Header("1. AI 상대방 사격 (공격)")]
    [Tooltip("상대를 쏠 때 총의 위치 (Head 기준)")]
    public Vector3 fireLocalPosition = new Vector3(0f, 0f, 0.5f);
    [Tooltip("상대를 쏠 때 총의 회전 (Head 기준)")]
    public Vector3 fireLocalEuler = new Vector3(0f, 180f, 0f);

    [Header("2. AI 자신 쏘기 (Self Shot)")]
    [Tooltip("자신을 쏠 때 총의 위치 (Head 기준)")]
    public Vector3 selfShotPosition = new Vector3(0f, 0f, 0.3f);
    [Tooltip("자신을 쏠 때 총의 회전 (Head 기준)")]
    public Vector3 selfShotEuler = new Vector3(0f, 180f, 0f);

    // ⚠️ [추가됨] AI 카드 사용 위치 ⚠️
    [Header("3. AI 카드 사용 (Card Use)")]
    [Tooltip("카드를 보여줄 때 총의 위치 (Head 기준)")]
    public Vector3 cardAnimPosition = new Vector3(0.2f, -0.2f, 0.4f);
    [Tooltip("카드를 보여줄 때 총의 회전 (Head 기준)")]
    public Vector3 cardAnimEuler = new Vector3(90f, 45f, 0f);
}