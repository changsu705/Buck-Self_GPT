using UnityEngine;
using System.Collections.Generic; // List를 쓰기 위해 필요

/// <summary>
/// TurnManager를 감시(Monitoring)하다가, 봇의 턴이 되면
/// 해당 봇의 AI를 깨우는 역할만 담당합니다. (100% 새로 추가된 안전한 스크립트)
/// </summary>
public class BotTurnMonitor : MonoBehaviour
{
    [Header("필수 참조")]
    [Tooltip("기존 턴 매니저")]
    public TurnManager turnManager;
    [Tooltip("기존 총 스크립트")]
    public RevolverTurnPossession gun;

    [Header("봇 설정")]
    [Tooltip("봇으로 취급할 모든 플레이어를 여기에 등록")]
    public List<BotAI> bots = new List<BotAI>();

    private Transform _lastPlayer = null; // 이전 턴 플레이어를 기억

    void Update()
    {
        if (turnManager == null || gun == null) return;

        // 1. 현재 턴인 플레이어를 TurnManager에게 물어봅니다.
        Transform currentPlayer = turnManager.GetCurrentPlayer();

        // 2. 이전 턴과 플레이어가 동일하면 아무것도 안 함 (무시)
        if (currentPlayer == _lastPlayer)
        {
            return;
        }

        // 3. 턴이 바뀌었음!
        _lastPlayer = currentPlayer;
        Debug.Log($"[BotTurnMonitor] 턴 변경 감지! 현재 턴: {currentPlayer.name}");

        // 4. 현재 턴인 플레이어가 우리가 등록한 '봇' 리스트에 있는지 확인
        foreach (BotAI bot in bots)
        {
            // .transform은 BotAI 스크립트가 붙어있는 '게임 오브젝트'를 의미
            if (bot.transform == currentPlayer)
            {
                // 찾았다! 이 녀석은 봇입니다.
                Debug.Log($"<color=cyan>[{bot.name} (AI)]</color>의 턴을 시작합니다.");

                // 봇의 뇌를 깨웁니다. (총 스크립트 정보를 넘겨주면서)
                bot.StartBotTurn(gun);
                return; // 봇을 깨웠으니 임무 완료
            }
        }

        // 5. (봇 리스트에 없었음) -> 인간 플레이어의 턴입니다.
        // 인간 플레이어는 기존 `RevolverTurnPossession`이 알아서 처리하므로
        // 우리는 아무것도 할 필요가 없습니다.
        Debug.Log($"<color=green>[{currentPlayer.name} (Player)]</color>의 턴입니다. (입력 대기)");
    }
}