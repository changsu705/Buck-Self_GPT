using UnityEngine;
using UnityEngine.SceneManagement; // 씬을 바꾸려면 이게 꼭 필요해요!

public class LobbyButtonManager : MonoBehaviour
{
    // 'Create a Lobby' 버튼이 누르면 이 함수를 실행할 겁니다.
    public void LoadBotMatchScene()
    {
        // "BotMatchScene"이라는 이름의 씬을 불러옵니다.
        SceneManager.LoadScene("BotMatchScene");
    }

    // 'Join the Lobby' 버튼이 누르면 이 함수를 실행할 겁니다.
    public void LoadDemmoLoomScene()
    {
        // "DemmoLoom"이라는 이름의 씬을 불러옵니다.
        SceneManager.LoadScene("DemmoLoom");
    }
}