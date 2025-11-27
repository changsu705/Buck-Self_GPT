using UnityEngine;
using UnityEngine.SceneManagement; // 씬을 바꾸려면 이것도 꼭 필요해요!

public class ReturnToLobby : MonoBehaviour
{
    void Update()
    {
        // 매 프레임마다 ESC 키를 눌렀는지 확인
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("ESC 눌림: 마우스 잠금 해제 및 로비로 복귀.");

            // ▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼ [ 이 두 줄이 추가되었습니다! ] ▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼▼
            // 1. 마우스 잠금을 해제합니다.
            Cursor.lockState = CursorLockMode.None;
            // 2. 마우스 커서를 다시 보이게 합니다.
            Cursor.visible = true;
            // ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲ [ 여기까지 추가되었습니다! ] ▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲▲

            // "Lobby"라는 이름의 씬을 불러옵니다.
            SceneManager.LoadScene("Lobby");
        }
    }
}