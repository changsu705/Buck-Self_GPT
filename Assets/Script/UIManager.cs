// 유니티 엔진의 핵심 기능을 사용하기 위해 선언하는 부분입니다.
// GameObject, MonoBehaviour 등을 사용하려면 반드시 필요합니다.
using UnityEngine;

/// <summary>
/// 게임의 전체적인 UI 패널(화면) 전환을 관리하는 스크립트입니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    // C#에서는 public으로 변수를 선언하면 유니티 에디터의 Inspector(인스펙터) 창에 노출됩니다.
    // 이곳에 Hierarchy(계층) 창의 게임 오브젝트를 드래그 앤 드롭하여 연결할 수 있습니다.

    // public으로 선언된 메인 메뉴 UI 패널 게임 오브젝트입니다.
    public GameObject mainMenuPanel;

    // public으로 선언된 옵션 UI 패널 게임 오브젝트입니다.
    public GameObject optionPanel;

    // public으로 선언된 도움말 UI 패널 게임 오브젝트입니다.
    public GameObject helpPanel;

    /// <summary>
    /// 게임 씬(Scene)이 시작될 때 단 한 번만 자동으로 호출되는 유니티 생명주기 함수입니다.
    /// 주로 변수를 초기화하는 용도로 사용됩니다.
    /// </summary>
    void Start()
    {
        // 게임이 처음 시작될 때의 화면 상태를 설정합니다.
        // 메인 메뉴는 보여주고, 다른 서브 메뉴들은 보이지 않게 숨겨둡니다.
        mainMenuPanel.SetActive(true);  // SetActive(true)는 게임 오브젝트를 활성화하여 화면에 보이게 합니다.
        optionPanel.SetActive(false);   // SetActive(false)는 게임 오브젝트를 비활성화하여 화면에서 숨깁니다.
        helpPanel.SetActive(false);
    }

    /// <summary>
    /// 메인 메뉴의 'Option' 버튼의 OnClick 이벤트에 연결될 함수입니다.
    /// </summary>
    public void OnClickOptionButton()
    {
        // 메인 메뉴 패널을 숨기고, 옵션 패널을 활성화하여 보여줍니다.
        mainMenuPanel.SetActive(false);
        optionPanel.SetActive(true);
    }

    /// <summary>
    /// 메인 메뉴의 'Help' 버튼의 OnClick 이벤트에 연결될 함수입니다.
    /// </summary>
    public void OnClickHelpButton()
    {
        // 메인 메뉴 패널을 숨기고, 도움말 패널을 활성화하여 보여줍니다.
        mainMenuPanel.SetActive(false);
        helpPanel.SetActive(true);
    }

    /// <summary>
    /// 옵션 또는 도움말 패널의 'Back'(뒤로가기) 버튼의 OnClick 이벤트에 연결될 함수입니다.
    /// </summary>
    public void OnClickBackButton()
    {
        // 어떤 서브 메뉴가 열려있든 상관없이, 다시 메인 메뉴 화면으로 돌아갑니다.
        mainMenuPanel.SetActive(true);
        optionPanel.SetActive(false);
        helpPanel.SetActive(false);
    }

    /// <summary>
    /// 메인 메뉴의 'Exit' 버튼의 OnClick 이벤트에 연결될 함수입니다.
    /// </summary>
    public void OnClickExitButton()
    {
        // Debug.Log는 유니티 콘솔 창에 원하는 메시지를 출력하는 함수입니다.
        // 기능이 정상적으로 연결되었는지 테스트할 때 매우 유용합니다.
        Debug.Log("Exit 버튼이 정상적으로 작동합니다.");
    }
}