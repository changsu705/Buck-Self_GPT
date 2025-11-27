using UnityEngine;

// ESC 키 입력을 감지해서 패널을 닫고 메인 메뉴로 돌아가는 역할만 합니다.
public class PanelInputManager : MonoBehaviour
{
    [Header("메인 패널")]
    [Tooltip("돌아갈 메인 메뉴 패널")]
    public GameObject mainPanel;

    [Header("ESC로 닫을 패널들")]
    [Tooltip("Help 패널 (Hierarchy에서 연결)")]
    public GameObject helpPanel;

    [Tooltip("Option 패널 (이전에 만들었다면 연결)")]
    public GameObject optionPanel; // Option 패널도 만들었다면 이것도 연결해주세요.

    void Update()
    {
        // 매 프레임마다 ESC 키를 눌렀는지 확인
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 1. Help 패널이 켜져있다면? (null이 아니고, 활성화 상태라면)
            if (helpPanel != null && helpPanel.activeSelf)
            {
                Debug.Log("ESC 눌림: HelpPanel 닫고 MainPanel 엽니다.");
                helpPanel.SetActive(false); // Help 패널 끄기
                mainPanel.SetActive(true);  // Main 패널 켜기
            }
            // 2. (만약 있다면) Option 패널이 켜져있다면?
            else if (optionPanel != null && optionPanel.activeSelf)
            {
                Debug.Log("ESC 눌림: OptionPanel 닫고 MainPanel 엽니다.");
                optionPanel.SetActive(false); // Option 패널 끄기
                mainPanel.SetActive(true);    // Main 패널 켜기
            }
        }
    }
}