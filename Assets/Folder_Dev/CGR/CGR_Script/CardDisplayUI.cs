using UnityEngine;
using System.Collections; // 코루틴(IEnumerator) 사용

/// <summary>
/// 카드 클릭 시 정보를 표시하는 UI 패널을 관리합니다.
/// - ShowPanel()이 호출되면, 패널을 'displayDuration' 시간만큼 켰다가 자동으로 닫습니다.
/// - IsOpen 프로퍼티를 제공하여, 패널이 열려있는 동안 다른 입력을 막도록 도와줍니다.
/// </summary>
public class CardDisplayUI : MonoBehaviour
{
    [Header("UI 참조")]
    [Tooltip("카드 클릭 시 켜질 부모 패널 (카드 이름, 설명 등이 포함된 UI 그룹)")]
    public GameObject descriptionPanel;

    [Header("표시 설정")]
    [Tooltip("패널이 표시될 시간(초)")]
    public float displayDuration = 3.0f; // 3초간 표시

    // --- 내부 상태 ---
    private Coroutine _showCoroutine; // 현재 실행 중인 '켜고 끄기' 타이머 코루틴을 저장

    void Start()
    {
        // 게임 시작 시 패널이 보이지 않도록 확실하게 숨깁니다.
        if (descriptionPanel != null)
        {
            descriptionPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[CardDisplayUI] 'Description Panel'이 연결되지 않았습니다!", this);
        }
    }

    /// <summary>
    /// [읽기 전용] UI 패널이 현재 활성화(열려)되어 있는지 여부입니다.
    /// (RevolverTurnPossession이 이 값을 확인하여 입력을 차단합니다.)
    /// </summary>
    public bool IsOpen => descriptionPanel != null && descriptionPanel.activeSelf;

    /// <summary>
    /// UI 패널을 'displayDuration' 시간 동안 표시하는 타이머를 시작합니다.
    /// (RevolverTurnPossession이 카드 좌클릭 시 이 함수를 호출합니다.)
    /// </summary>
    public void ShowPanel()
    {
        // 만약 이미 패널이 열려있고 이전 타이머 코루틴이 실행 중이었다면,
        if (_showCoroutine != null)
        {
            StopCoroutine(_showCoroutine); // 기존 타이머를 즉시 중지시킵니다.
        }

        // '켜고 끄기' 코루틴을 새로 시작하고, _showCoroutine 변수에 저장합니다.
        _showCoroutine = StartCoroutine(Co_ShowAndHide());
    }

    /// <summary>
    /// [코루틴] 패널을 켜고 (1), 설정된 시간만큼 대기 후 (2), 자동으로 닫습니다. (3)
    /// </summary>
    private IEnumerator Co_ShowAndHide()
    {
        // 1. 패널 활성화
        descriptionPanel.SetActive(true);

        // 2. 'displayDuration' 만큼 대기 (예: 3초)
        yield return new WaitForSeconds(displayDuration);

        // 3. 패널 비활성화
        descriptionPanel.SetActive(false);
        _showCoroutine = null; // 코루틴이 완료되었으므로 참조를 비움
    }
}