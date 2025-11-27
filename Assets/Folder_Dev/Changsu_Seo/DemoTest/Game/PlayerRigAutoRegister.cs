using UnityEngine;

/// <summary>
/// PlayerRig가 스폰될 때 TurnManager에 자동 등록 / 파괴 시 자동 해제.
/// </summary>
public class PlayerRigAutoRegister : MonoBehaviour
{
    private TurnManager _tm;

    private void Start()
    {
        _tm = FindObjectOfType<TurnManager>();
        if (_tm == null)
        {
            Debug.LogWarning("[PlayerRigAutoRegister] TurnManager를 찾지 못했습니다.");
            return;
        }
        _tm.RegisterPlayer(transform);
    }

    private void OnDestroy()
    {
        if (_tm != null) _tm.UnregisterPlayer(transform);
    }
}
