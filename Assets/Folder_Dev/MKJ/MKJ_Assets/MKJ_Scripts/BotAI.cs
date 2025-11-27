using UnityEngine;
using System.Collections;

/// <summary>
/// 봇(AI) 플레이어에 부착하는 스크립트입니다.
/// 턴이 되면 잠시 생각한 뒤, 행동(자신쏘기/상대쏘기)을 결정합니다.
/// </summary>
public class BotAI : MonoBehaviour
{
    [Header("AI 설정")]
    [Tooltip("봇이 항상 공격할 인간 플레이어 (Cha1 등)")]
    public Transform humanPlayer;

    [Tooltip("봇이 행동을 결정하기까지 생각하는 시간(초)")]
    public float thinkingTime = 1.5f;

    // 봇의 행동을 제어할 총기 스크립트 (외부에서 자동으로 받아옴)
    private RevolverTurnPossession _gun;

    /// <summary>
    /// 턴이 시작될 때 봇 매니저가 호출해주는 함수입니다.
    /// </summary>
    public void StartBotTurn(RevolverTurnPossession gun)
    {
        _gun = gun;
        StartCoroutine(Co_ThinkAndAct());
    }

    /// <summary>
    /// 롤 중급봇처럼 간단한 확률 기반으로 생각하고 행동합니다.
    /// </summary>
    private IEnumerator Co_ThinkAndAct()
    {
        // 1. "생각하는 중..." (딜레이)
        yield return new WaitForSeconds(thinkingTime);

        // 2. 행동 결정 (지금은 50:50 확률)
        // 나중에 여기에 카드 확인, 체력 확인 등 복잡한 로직을 추가할 수 있습니다.
        bool shootSelf = (Random.value > 0.5f);

        // 3. 행동 실행
        if (shootSelf)
        {
            // [결정 A] 자신에게 쏜다.
            Debug.Log($"<color=orange>[{name} (AI)]</color>가 자신에게 총을 쏩니다.");
            _gun.ShootSelf(); // 봇이 스스로 쏘도록 총에게 명령
        }
        else
        {
            // [결정 B] 인간 플레이어를 공격한다.
            if (humanPlayer == null)
            {
                Debug.LogError($"[BotAI] {name}의 'humanPlayer' 타겟이 비어있습니다!");
                yield break;
            }

            Debug.Log($"<color=red>[{name} (AI)]</color>가 {humanPlayer.name}을(를) 조준합니다.");
            _gun.HandleAimRequest(humanPlayer);
        }
    }
}