using UnityEngine;

namespace Buckshot.UI
{
    // UIManager가 없어도 컴파일 가능한 확장 메서드(빈 구현)
    public static class UIManagerExtensions
    {
        public static void OnGameStart(this UIManager manager)
        {
            Debug.Log("[UI] GameStart (stub)");
        }

        public static void OnGameOver(this UIManager manager, int winnerActor)
        {
            Debug.Log($"[UI] GameOver (stub), winner={winnerActor}");
        }
    }
}
